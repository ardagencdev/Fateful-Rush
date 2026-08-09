using System;
using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }

    private const string VibrationKey = "VibrationEnabled";

    private const int AndroidOreoSdk = 26;
    private const int AndroidQSdk = 29;

    private const int EffectClick = 0;
    private const int EffectDoubleClick = 1;
    private const int EffectTick = 2;
    private const int EffectHeavyClick = 5;

    private const int MinAmplitude = 1;
    private const int MaxAmplitude = 255;
    private const int DefaultAmplitude = -1;

    private const float LowImpactMinInterval = 0.035f;

    private static readonly long[] CloneTimings = { 0, 18, 42, 24 };
    private static readonly int[] CloneAmplitudes = { 0, 70, 0, 105 };

    private static readonly long[] SuccessTimings = { 0, 24, 42, 42 };
    private static readonly int[] SuccessAmplitudes = { 0, 90, 0, 165 };

    private static readonly long[] FailureTimings = { 0, 55, 45, 95 };
    private static readonly int[] FailureAmplitudes = { 0, 175, 0, 235 };

    private bool isEnabled;
    private float lastLowImpactHapticTime = -10f;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject vibrator;
    private AndroidJavaClass vibrationEffectClass;

    private int androidSdkVersion;
    private bool hasVibrator;
    private bool hasAmplitudeControl;
#endif

    public bool IsEnabled => isEnabled;

    public bool CanVibrate
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return isEnabled && vibrator != null && hasVibrator;
#elif UNITY_IOS && !UNITY_EDITOR
            return isEnabled;
#else
            return false;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstanceExists()
    {
        if (Instance != null)
            return;

        VibrationManager existing =
            FindAnyObjectByType<VibrationManager>();

        if (existing != null)
            return;

        GameObject managerObject =
            new GameObject("[VibrationManager]");

        managerObject.AddComponent<VibrationManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        isEnabled =
            PlayerPrefs.GetInt(VibrationKey, 1) == 1;

#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroidVibration();
#endif
    }

    public void SetVibration(bool enabled)
    {
        isEnabled = enabled;

        PlayerPrefs.SetInt(
            VibrationKey,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();

        if (!enabled)
            CancelVibration();
    }

    public void VibrateUI()
    {
        if (!TryReserveLowImpactHaptic())
            return;

        PlayPredefined(
            EffectTick,
            fallbackMilliseconds: 12,
            fallbackAmplitude: 42,
            allowIOSFallback: false
        );
    }

    public void VibrateCoin()
    {
        if (!TryReserveLowImpactHaptic())
            return;

        PlayPredefined(
            EffectTick,
            fallbackMilliseconds: 10,
            fallbackAmplitude: 32,
            allowIOSFallback: false
        );
    }

    public void VibrateDash()
    {
        if (!TryReserveLowImpactHaptic())
            return;

        PlayPredefined(
            EffectClick,
            fallbackMilliseconds: 22,
            fallbackAmplitude: 76,
            allowIOSFallback: false
        );
    }

    public void VibrateClone()
    {
        if (!isEnabled)
            return;

        PlayPattern(
            CloneTimings,
            CloneAmplitudes,
            allowIOSFallback: false
        );
    }

    public void VibratePowerUp()
    {
        if (!isEnabled)
            return;

        PlayPredefined(
            EffectClick,
            fallbackMilliseconds: 34,
            fallbackAmplitude: 120,
            allowIOSFallback: true
        );
    }

    public void VibrateArmorBreak()
    {
        if (!isEnabled)
            return;

        CancelVibration();

        PlayPredefined(
            EffectHeavyClick,
            fallbackMilliseconds: 58,
            fallbackAmplitude: 205,
            allowIOSFallback: true
        );
    }

    public void VibrateSuccess()
    {
        if (!isEnabled)
            return;

        CancelVibration();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (androidSdkVersion >= AndroidQSdk)
        {
            if (TryPlayAndroidPredefined(EffectDoubleClick))
                return;
        }
#endif

        PlayPattern(
            SuccessTimings,
            SuccessAmplitudes,
            allowIOSFallback: true
        );
    }

    public void VibrateFailure()
    {
        if (!isEnabled)
            return;

        CancelVibration();

        PlayPattern(
            FailureTimings,
            FailureAmplitudes,
            allowIOSFallback: true
        );
    }

    // Legacy API kept so older scene/button bindings and scripts remain compatible.
    public void VibrateLight() => VibrateUI();
    public void VibrateMedium() => VibratePowerUp();

    public void VibrateHeavy()
    {
        if (!isEnabled)
            return;

        PlayPredefined(
            EffectHeavyClick,
            fallbackMilliseconds: 65,
            fallbackAmplitude: 205,
            allowIOSFallback: true
        );
    }

    public void CancelVibration()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator == null || !hasVibrator)
            return;

        try
        {
            vibrator.Call("cancel");
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Vibration could not be cancelled: {exception.Message}"
            );
        }
#endif
    }

    private bool TryReserveLowImpactHaptic()
    {
        if (!isEnabled)
            return false;

        float now = Time.unscaledTime;

        if (now - lastLowImpactHapticTime <
            LowImpactMinInterval)
        {
            return false;
        }

        lastLowImpactHapticTime = now;
        return true;
    }

    private void PlayPredefined(
        int effectId,
        long fallbackMilliseconds,
        int fallbackAmplitude,
        bool allowIOSFallback
    )
    {
        if (!isEnabled)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator == null || !hasVibrator)
            return;

        if (androidSdkVersion >= AndroidQSdk &&
            TryPlayAndroidPredefined(effectId))
        {
            return;
        }

        PlayAndroidOneShot(
            fallbackMilliseconds,
            fallbackAmplitude
        );

#elif UNITY_IOS && !UNITY_EDITOR
        if (allowIOSFallback)
            Handheld.Vibrate();
#endif
    }

    private void PlayPattern(
        long[] timings,
        int[] amplitudes,
        bool allowIOSFallback
    )
    {
        if (!isEnabled)
            return;

        if (!IsValidPattern(timings, amplitudes))
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator == null || !hasVibrator)
            return;

        try
        {
            if (androidSdkVersion >= AndroidOreoSdk &&
                vibrationEffectClass != null)
            {
                int[] safeAmplitudes =
                    BuildSafeAmplitudes(amplitudes);

                using AndroidJavaObject effect =
                    vibrationEffectClass
                        .CallStatic<AndroidJavaObject>(
                            "createWaveform",
                            timings,
                            safeAmplitudes,
                            -1
                        );

                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call(
                    "vibrate",
                    timings,
                    -1
                );
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Vibration pattern failed: {exception.Message}"
            );
        }

#elif UNITY_IOS && !UNITY_EDITOR
        if (allowIOSFallback)
            Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool TryPlayAndroidPredefined(int effectId)
    {
        if (androidSdkVersion < AndroidQSdk ||
            vibrationEffectClass == null ||
            vibrator == null ||
            !hasVibrator)
        {
            return false;
        }

        try
        {
            using AndroidJavaObject effect =
                vibrationEffectClass
                    .CallStatic<AndroidJavaObject>(
                        "createPredefined",
                        effectId
                    );

            vibrator.Call("vibrate", effect);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void PlayAndroidOneShot(
        long milliseconds,
        int amplitude
    )
    {
        if (milliseconds <= 0 ||
            vibrator == null ||
            !hasVibrator)
        {
            return;
        }

        try
        {
            if (androidSdkVersion >= AndroidOreoSdk &&
                vibrationEffectClass != null)
            {
                int safeAmplitude =
                    hasAmplitudeControl
                        ? Mathf.Clamp(
                            amplitude,
                            MinAmplitude,
                            MaxAmplitude
                        )
                        : DefaultAmplitude;

                using AndroidJavaObject effect =
                    vibrationEffectClass
                        .CallStatic<AndroidJavaObject>(
                            "createOneShot",
                            milliseconds,
                            safeAmplitude
                        );

                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call(
                    "vibrate",
                    milliseconds
                );
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Vibration failed: {exception.Message}"
            );

            // Also keeps Unity's Android VIBRATE permission detection active
            // and provides a last-resort device fallback.
            Handheld.Vibrate();
        }
    }

    private int[] BuildSafeAmplitudes(int[] amplitudes)
    {
        int[] safe = new int[amplitudes.Length];

        for (int i = 0; i < amplitudes.Length; i++)
        {
            int amplitude = amplitudes[i];

            if (amplitude <= 0)
            {
                safe[i] = 0;
                continue;
            }

            safe[i] = hasAmplitudeControl
                ? Mathf.Clamp(
                    amplitude,
                    MinAmplitude,
                    MaxAmplitude
                )
                : DefaultAmplitude;
        }

        return safe;
    }

    private void InitializeAndroidVibration()
    {
        try
        {
            using AndroidJavaClass unityPlayer =
                new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer"
                );

            using AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity"
                );

            vibrator =
                activity.Call<AndroidJavaObject>(
                    "getSystemService",
                    "vibrator"
                );

            using AndroidJavaClass versionClass =
                new AndroidJavaClass(
                    "android.os.Build$VERSION"
                );

            androidSdkVersion =
                versionClass.GetStatic<int>(
                    "SDK_INT"
                );

            hasVibrator =
                vibrator != null &&
                vibrator.Call<bool>(
                    "hasVibrator"
                );

            hasAmplitudeControl =
                hasVibrator &&
                androidSdkVersion >= AndroidOreoSdk &&
                vibrator.Call<bool>(
                    "hasAmplitudeControl"
                );

            if (androidSdkVersion >= AndroidOreoSdk)
            {
                vibrationEffectClass =
                    new AndroidJavaClass(
                        "android.os.VibrationEffect"
                    );
            }
        }
        catch (Exception exception)
        {
            hasVibrator = false;
            hasAmplitudeControl = false;

            Debug.LogWarning(
                $"Android vibration could not be initialized: {exception.Message}"
            );
        }
    }
#endif

    private static bool IsValidPattern(
        long[] timings,
        int[] amplitudes
    )
    {
        if (timings == null ||
            amplitudes == null)
        {
            return false;
        }

        if (timings.Length == 0 ||
            timings.Length != amplitudes.Length)
        {
            return false;
        }

        for (int i = 0; i < timings.Length; i++)
        {
            if (timings[i] < 0)
                return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        CancelVibration();

#if UNITY_ANDROID && !UNITY_EDITOR
        vibrator?.Dispose();
        vibrator = null;

        vibrationEffectClass?.Dispose();
        vibrationEffectClass = null;
#endif

        Instance = null;
    }
}
