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

    [Tooltip("Only used while the device is already thermally throttling.")]
    [Min(15)]
    [SerializeField] private int thermalFallbackFrameRate = 30;

    [Header("Adaptive Performance")]
    [SerializeField] private bool enableAdaptivePerformance = true;

    [Tooltip("Lets Unity's Adaptive Performance provider regulate CPU/GPU performance levels automatically.")]
    [SerializeField] private bool useAutomaticPerformanceControl = true;

    [Tooltip("If the device reaches the actual Throttling state, temporarily use thermalFallbackFrameRate to help it cool down.")]
    [SerializeField] private bool useThermalFrameRateFallback = true;

    [Tooltip("How long the device must remain back at NoWarning before restoring full quality/FPS. Prevents rapid quality toggling.")]
    [Min(0f)]
    [SerializeField] private float recoveryDelay = 10f;

    [Header("Adaptive Render Scale (Android build only)")]
    [Tooltip("Small URP render-scale reductions are only applied when thermal pressure is reported. Editor quality is not changed.")]
    [SerializeField] private bool useAdaptiveRenderScale = true;

    [Range(0.85f, 1f)]
    [SerializeField] private float imminentRenderScaleMultiplier = 0.96f;

    [Range(0.80f, 1f)]
    [SerializeField] private float throttlingRenderScaleMultiplier = 0.90f;

    [Tooltip("Never let Adaptive Performance lower URP render scale below this value.")]
    [Range(0.75f, 1f)]
    [SerializeField] private float minimumRenderScale = 0.85f;

    [Header("Device Behaviour")]
    [SerializeField] private bool preventScreenSleep = true;
    [SerializeField] private bool runInBackground = false;

    [Header("Diagnostics")]
    [Tooltip("Only logs state changes in Development Builds / Editor.")]
    [SerializeField] private bool logStateChanges = true;

    private IAdaptivePerformance adaptivePerformance;
    private Coroutine adaptiveBindRoutine;
    private Coroutine recoveryRoutine;
    private bool thermalEventSubscribed;

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

        if (enableAdaptivePerformance)
            adaptiveBindRoutine = StartCoroutine(BindAdaptivePerformanceRoutine());
    }

    private void OnDisable()
    {
        Application.lowMemory -= HandleLowMemory;

        if (adaptiveBindRoutine != null)
        {
            StopCoroutine(adaptiveBindRoutine);
            adaptiveBindRoutine = null;
        }

        if (recoveryRoutine != null)
        {
            StopCoroutine(recoveryRoutine);
            recoveryRoutine = null;
        }

        UnsubscribeAdaptivePerformance();
        RestoreFullPerformance();
    }

    private void ApplyBaseSettings()
    {
        // Application.targetFrameRate controls our 60 FPS target.
        // VSync must stay disabled for this path.
        QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = Mathf.Max(30, targetFrameRate);
        Application.runInBackground = runInBackground;

        Screen.sleepTimeout = preventScreenSleep
            ? SleepTimeout.NeverSleep
            : SleepTimeout.SystemSetting;
    }

    private IEnumerator BindAdaptivePerformanceRoutine()
    {
        // The Android provider can finish startup slightly after scene Awake/OnEnable.
        // Wait briefly instead of assuming Holder.Instance is already available.
        const float maxWaitSeconds = 8f;
        const float retryDelaySeconds = 0.25f;

        float elapsed = 0f;

        while (elapsed < maxWaitSeconds)
        {
            adaptivePerformance = Holder.Instance;

            if (adaptivePerformance != null && adaptivePerformance.Initialized)
            {
                SubscribeAdaptivePerformance();
                yield break;
            }

            yield return new WaitForSecondsRealtime(retryDelaySeconds);
            elapsed += retryDelaySeconds;
        }

        Log("Adaptive Performance provider is not active on this device. Base 60 FPS settings remain enabled.");
        adaptiveBindRoutine = null;
    }

    private void SubscribeAdaptivePerformance()
    {
        if (adaptivePerformance == null || thermalEventSubscribed)
            return;

        if (useAutomaticPerformanceControl && adaptivePerformance.DevicePerformanceControl != null)
            adaptivePerformance.DevicePerformanceControl.AutomaticPerformanceControl = true;

        if (adaptivePerformance.ThermalStatus != null)
        {
            adaptivePerformance.ThermalStatus.ThermalEvent += HandleThermalEvent;
            thermalEventSubscribed = true;

            HandleThermalEvent(adaptivePerformance.ThermalStatus.ThermalMetrics);
        }

        Log("Adaptive Performance connected.");
        adaptiveBindRoutine = null;
    }

    private void UnsubscribeAdaptivePerformance()
    {
        if (!thermalEventSubscribed || adaptivePerformance == null || adaptivePerformance.ThermalStatus == null)
            return;

        adaptivePerformance.ThermalStatus.ThermalEvent -= HandleThermalEvent;
        thermalEventSubscribed = false;
    }

    private void HandleThermalEvent(ThermalMetrics metrics)
    {
        if (!enableAdaptivePerformance)
            return;

        WarningLevel newLevel = metrics.WarningLevel;

        if (newLevel == currentWarningLevel && newLevel != WarningLevel.NoWarning)
            return;

        currentWarningLevel = newLevel;

        switch (newLevel)
        {
            case WarningLevel.NoWarning:
                BeginRecovery();
                break;

            case WarningLevel.ThrottlingImminent:
                CancelRecovery();
                Application.targetFrameRate = Mathf.Max(30, targetFrameRate);
                ApplyAdaptiveRenderScale(imminentRenderScaleMultiplier);

                Log($"Thermal pressure detected (Imminent). FPS stays at {Application.targetFrameRate}; render load reduced slightly.");
                break;

            case WarningLevel.Throttling:
                CancelRecovery();

                if (useThermalFrameRateFallback)
                    Application.targetFrameRate = Mathf.Clamp(thermalFallbackFrameRate, 15, Mathf.Max(30, targetFrameRate));

                ApplyAdaptiveRenderScale(throttlingRenderScaleMultiplier);

                Log($"Device is thermally throttling. Temporary target FPS: {Application.targetFrameRate}.");
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

        // Only recover if the thermal state stayed healthy for the whole delay.
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
        Application.targetFrameRate = Mathf.Max(30, targetFrameRate);
        RestoreBaseRenderScale();

        Log($"Full mobile performance restored ({Application.targetFrameRate} FPS target).");
    }

    private void CacheRenderPipelineSettings()
    {
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urpAsset == null)
            return;

        baseRenderScale = urpAsset.renderScale;
        baseRenderScaleCached = true;
    }

    private void ApplyAdaptiveRenderScale(float multiplier)
    {
        if (!useAdaptiveRenderScale)
            return;

        // Do not mutate the project's URP asset while testing in the Editor.
        // The actual adaptive scaling is intended for the Android player.
        if (Application.isEditor)
            return;

        if (!baseRenderScaleCached)
            CacheRenderPipelineSettings();

        if (urpAsset == null)
            return;

        float targetScale = Mathf.Max(
            minimumRenderScale,
            baseRenderScale * Mathf.Clamp01(multiplier)
        );

        urpAsset.renderScale = Mathf.Min(baseRenderScale, targetScale);
    }

    private void RestoreBaseRenderScale()
    {
        if (Application.isEditor || !baseRenderScaleCached || urpAsset == null)
            return;

        urpAsset.renderScale = baseRenderScale;
    }

    private void HandleLowMemory()
    {
        // Keep this as an emergency-only action because unloading assets can cause a hitch.
        Resources.UnloadUnusedAssets();

        Debug.LogWarning(
            "Low memory warning received. Unused assets are being unloaded."
        );
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
        thermalFallbackFrameRate = Mathf.Max(15, thermalFallbackFrameRate);
        recoveryDelay = Mathf.Max(0f, recoveryDelay);

        imminentRenderScaleMultiplier = Mathf.Clamp(imminentRenderScaleMultiplier, 0.85f, 1f);
        throttlingRenderScaleMultiplier = Mathf.Clamp(throttlingRenderScaleMultiplier, 0.80f, 1f);
        minimumRenderScale = Mathf.Clamp(minimumRenderScale, 0.75f, 1f);

        // Severe state should never preserve more rendering load than imminent state.
        throttlingRenderScaleMultiplier = Mathf.Min(
            throttlingRenderScaleMultiplier,
            imminentRenderScaleMultiplier
        );

        if (Application.isPlaying)
            ApplyBaseSettings();
    }
}
