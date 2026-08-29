using System.Collections;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class MobilePerformanceSettings : MonoBehaviour
{
    [Header("Frame Rate")]
    [Min(30)]
    [SerializeField] private int targetFrameRate = 60;

    [Header("Adaptive Performance")]
    [Tooltip("Uses Adaptive Performance only as thermal telemetry on physical mobile devices. It never lowers the FPS target automatically.")]
    [SerializeField] private bool enableAdaptivePerformance = true;

    [Header("Adaptive Render Scale (physical mobile only)")]
    [Tooltip("When thermal pressure is reported, reduce URP render scale slightly instead of dropping 60 FPS to 30 FPS.")]
    [SerializeField] private bool useAdaptiveRenderScale = true;

    [Range(0.85f, 1f)]
    [SerializeField] private float imminentRenderScaleMultiplier = 0.96f;

    [Range(0.80f, 1f)]
    [SerializeField] private float throttlingRenderScaleMultiplier = 0.90f;

    [Tooltip("Never let the controlled thermal response lower URP render scale below this value.")]
    [Range(0.75f, 1f)]
    [SerializeField] private float minimumRenderScale = 0.85f;

    [Tooltip("How long the device must remain back at NoWarning before restoring the original render scale.")]
    [Min(0f)]
    [SerializeField] private float recoveryDelay = 10f;

    [Header("Device Behaviour")]
    [SerializeField] private bool preventScreenSleep = true;
    [SerializeField] private bool runInBackground = false;

    [Header("Diagnostics")]
    [Tooltip("Only logs Adaptive Performance state changes in Development Builds / Editor.")]
    [SerializeField] private bool logStateChanges = true;

    private IAdaptivePerformance adaptivePerformance;
    private Coroutine adaptiveBindRoutine;
    private Coroutine recoveryRoutine;
    private Coroutine lowMemoryCleanupRoutine;
    private bool thermalEventSubscribed;
    private bool lowMemoryCleanupPending;

    private UniversalRenderPipelineAsset urpAsset;
    private float baseRenderScale = 1f;
    private bool baseRenderScaleCached;

    private WarningLevel currentWarningLevel = WarningLevel.NoWarning;

    private void Awake()
    {
        ApplyBaseSettings();
        CacheRenderPipelineSettings();
    }

    private void OnEnable()
    {
        Application.lowMemory += HandleLowMemory;

        // Adaptive Performance is a mobile thermal/power system. Do not bind
        // it in Editor, native desktop, or Google Play Games on PC.
        if (enableAdaptivePerformance &&
            RuntimePerformancePolicy.IsPhysicalMobileRuntime)
        {
            adaptiveBindRoutine =
                StartCoroutine(BindAdaptivePerformanceRoutine());
        }
    }

    private void OnDisable()
    {
        Application.lowMemory -= HandleLowMemory;

        if (adaptiveBindRoutine != null)
        {
            StopCoroutine(adaptiveBindRoutine);
            adaptiveBindRoutine = null;
        }

        CancelRecovery();

        if (lowMemoryCleanupRoutine != null)
        {
            StopCoroutine(lowMemoryCleanupRoutine);
            lowMemoryCleanupRoutine = null;
        }

        UnsubscribeAdaptivePerformance();
        RestoreFullPerformance();
    }

    private void Update()
    {
        if (!lowMemoryCleanupPending || lowMemoryCleanupRoutine != null)
            return;

        // UnloadUnusedAssets can hitch badly. Never start it in the middle of
        // an active run; wait for the result/non-gameplay state instead.
        if (!GameStateManager.IsGameplayStarted ||
            GameStateManager.IsGameplayEnded)
        {
            lowMemoryCleanupRoutine =
                StartCoroutine(DeferredLowMemoryCleanupRoutine());
        }
    }

    private void ApplyBaseSettings()
    {
        RuntimePerformancePolicy.ApplyFrameRate(
            GetConfiguredMobileTargetFrameRate()
        );
        Application.runInBackground = runInBackground;

        Screen.sleepTimeout = preventScreenSleep
            ? SleepTimeout.NeverSleep
            : SleepTimeout.SystemSetting;
    }

    private IEnumerator BindAdaptivePerformanceRoutine()
    {
        // The Android provider can finish startup slightly after scene
        // Awake/OnEnable. Wait briefly instead of assuming it is ready.
        const float maxWaitSeconds = 8f;
        const float retryDelaySeconds = 0.25f;

        float elapsed = 0f;

        while (elapsed < maxWaitSeconds)
        {
            adaptivePerformance = Holder.Instance;

            if (adaptivePerformance != null &&
                adaptivePerformance.Initialized)
            {
                SubscribeAdaptivePerformance();
                yield break;
            }

            yield return new WaitForSecondsRealtime(retryDelaySeconds);
            elapsed += retryDelaySeconds;
        }

        Log("Adaptive Performance provider is not active. The selected mobile FPS target remains unchanged.");
        adaptiveBindRoutine = null;
    }

    private void SubscribeAdaptivePerformance()
    {
        if (adaptivePerformance == null || thermalEventSubscribed)
            return;

        // Keep performance behavior deterministic. We only consume thermal
        // telemetry here; Unity's provider is not allowed to automatically
        // change CPU/GPU performance levels behind our own policy.
        if (adaptivePerformance.DevicePerformanceControl != null)
        {
            adaptivePerformance.DevicePerformanceControl
                .AutomaticPerformanceControl = false;
        }

        if (adaptivePerformance.ThermalStatus != null)
        {
            adaptivePerformance.ThermalStatus.ThermalEvent +=
                HandleThermalEvent;

            thermalEventSubscribed = true;

            HandleThermalEvent(
                adaptivePerformance.ThermalStatus.ThermalMetrics
            );
        }

        Log("Adaptive Performance connected in thermal-telemetry mode. Automatic FPS/CPU/GPU control is disabled.");
        adaptiveBindRoutine = null;
    }

    private void UnsubscribeAdaptivePerformance()
    {
        if (!thermalEventSubscribed ||
            adaptivePerformance == null ||
            adaptivePerformance.ThermalStatus == null)
        {
            return;
        }

        adaptivePerformance.ThermalStatus.ThermalEvent -=
            HandleThermalEvent;

        thermalEventSubscribed = false;
    }

    private void HandleThermalEvent(ThermalMetrics metrics)
    {
        if (!enableAdaptivePerformance ||
            !RuntimePerformancePolicy.IsPhysicalMobileRuntime)
        {
            return;
        }

        WarningLevel newLevel = metrics.WarningLevel;

        // Avoid restarting recovery on repeated NoWarning events and avoid
        // reapplying the same quality state over and over.
        if (newLevel == currentWarningLevel)
            return;

        currentWarningLevel = newLevel;

        switch (newLevel)
        {
            case WarningLevel.NoWarning:
                BeginRecovery();
                break;

            case WarningLevel.ThrottlingImminent:
                CancelRecovery();
                ApplyAdaptiveRenderScale(imminentRenderScaleMultiplier);
                Log("Thermal pressure is imminent. FPS target is unchanged; render scale is reduced slightly.");
                break;

            case WarningLevel.Throttling:
                CancelRecovery();
                ApplyAdaptiveRenderScale(throttlingRenderScaleMultiplier);
                Log("Device is thermally throttling. FPS target is still unchanged; render scale is reduced further.");
                break;
        }
    }

    private void BeginRecovery()
    {
        CancelRecovery();
        recoveryRoutine = StartCoroutine(RecoveryRoutine());
    }

    private IEnumerator RecoveryRoutine()
    {
        if (recoveryDelay > 0f)
            yield return new WaitForSecondsRealtime(recoveryDelay);

        if (currentWarningLevel == WarningLevel.NoWarning)
            RestoreFullPerformance();

        recoveryRoutine = null;
    }

    private void CancelRecovery()
    {
        if (recoveryRoutine == null)
            return;

        StopCoroutine(recoveryRoutine);
        recoveryRoutine = null;
    }

    private void RestoreFullPerformance()
    {
        // Re-apply the correct platform policy. This restores 30/60 on a
        // phone but never accidentally re-caps Google Play Games on PC to 60.
        RuntimePerformancePolicy.ApplyFrameRate(
            GetConfiguredMobileTargetFrameRate()
        );
        RestoreBaseRenderScale();

        Log("Full rendering quality restored. FPS policy was not reduced by Adaptive Performance.");
    }


    private int GetConfiguredMobileTargetFrameRate()
    {
        if (SettingsManager.Instance != null)
            return SettingsManager.Instance.GetFPS();

        int saved = PlayerPrefs.GetInt(
            "FPSMode",
            targetFrameRate
        );

        return saved == 30 ? 30 : 60;
    }

    private void CacheRenderPipelineSettings()
    {
        urpAsset =
            GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;

        if (urpAsset == null)
            return;

        baseRenderScale = urpAsset.renderScale;
        baseRenderScaleCached = true;

        // If the URP asset has its own Adaptive Performance integration
        // enabled, it can change quality independently of this script. Disable
        // that hidden path in player builds so this class is the only place
        // allowed to adapt render quality.
        if (!Application.isEditor &&
            RuntimePerformancePolicy.IsPhysicalMobileRuntime)
        {
            urpAsset.useAdaptivePerformance = false;
        }
    }

    private void ApplyAdaptiveRenderScale(float multiplier)
    {
        if (!useAdaptiveRenderScale ||
            !RuntimePerformancePolicy.IsPhysicalMobileRuntime)
        {
            return;
        }

        if (!baseRenderScaleCached)
            CacheRenderPipelineSettings();

        if (urpAsset == null)
            return;

        float targetScale = Mathf.Max(
            minimumRenderScale,
            baseRenderScale * Mathf.Clamp01(multiplier)
        );

        urpAsset.renderScale =
            Mathf.Min(baseRenderScale, targetScale);
    }

    private void RestoreBaseRenderScale()
    {
        if (!baseRenderScaleCached || urpAsset == null)
            return;

        urpAsset.renderScale = baseRenderScale;
    }

    private void HandleLowMemory()
    {
        lowMemoryCleanupPending = true;

        Debug.LogWarning(
            "Low memory warning received. Cleanup was queued for the next non-gameplay state to avoid a mid-run hitch."
        );
    }

    private IEnumerator DeferredLowMemoryCleanupRoutine()
    {
        lowMemoryCleanupPending = false;

        yield return null;

        AsyncOperation unloadOperation =
            Resources.UnloadUnusedAssets();

        if (unloadOperation != null)
            yield return unloadOperation;

        lowMemoryCleanupRoutine = null;
    }

    private void Log(string message)
    {
        if (!logStateChanges)
            return;

        if (Application.isEditor || Debug.isDebugBuild)
            Debug.Log($"[MobilePerformance] {message}");
    }

    private void OnValidate()
    {
        targetFrameRate = Mathf.Max(30, targetFrameRate);
        recoveryDelay = Mathf.Max(0f, recoveryDelay);

        imminentRenderScaleMultiplier =
            Mathf.Clamp(imminentRenderScaleMultiplier, 0.85f, 1f);

        throttlingRenderScaleMultiplier =
            Mathf.Clamp(throttlingRenderScaleMultiplier, 0.80f, 1f);

        minimumRenderScale =
            Mathf.Clamp(minimumRenderScale, 0.75f, 1f);

        throttlingRenderScaleMultiplier = Mathf.Min(
            throttlingRenderScaleMultiplier,
            imminentRenderScaleMultiplier
        );

        if (Application.isPlaying)
            ApplyBaseSettings();
    }
}
