using System;
using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }

    private const string VibrationKey = "VibrationEnabled";

    private const int AndroidOreoSdk = 26;
    private const int MinAmplitude = 1;
    private const int MaxAmplitude = 255;

    private bool isEnabled;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject vibrator;
    private AndroidJavaClass vibrationEffectClass;

    private int androidSdkVersion;
    private bool hasVibrator;
#endif

    public bool IsEnabled => isEnabled;

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

    public void VibrateLight()
    {
        Vibrate(
            milliseconds: 25,
            amplitude: 60
        );
    }

    public void VibrateMedium()
    {
        Vibrate(
            milliseconds: 45,
            amplitude: 120
        );
    }

    public void VibrateHeavy()
    {
        Vibrate(
            milliseconds: 80,
            amplitude: 200
        );
    }

    public void VibrateSuccess()
    {
        VibratePattern(
            new long[]
            {
                0,
                35,
                50,
                45
            },
            new int[]
            {
                0,
                100,
                0,
                160
            }
        );
    }

    public void VibrateFailure()
    {
        VibratePattern(
            new long[]
            {
                0,
                80,
                60,
                120
            },
            new int[]
            {
                0,
                200,
                0,
                255
            }
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

    private void Vibrate(
        long milliseconds,
        int amplitude
    )
    {
        if (!isEnabled || milliseconds <= 0)
            return;

        amplitude = Mathf.Clamp(
            amplitude,
            MinAmplitude,
            MaxAmplitude
        );

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator == null || !hasVibrator)
            return;

        try
        {
            if (androidSdkVersion >= AndroidOreoSdk &&
                vibrationEffectClass != null)
            {
                using AndroidJavaObject effect =
                    vibrationEffectClass
                        .CallStatic<AndroidJavaObject>(
                            "createOneShot",
                            milliseconds,
                            amplitude
                        );

                vibrator.Call(
                    "vibrate",
                    effect
                );
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
        }

#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    private void VibratePattern(
        long[] timings,
        int[] amplitudes
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
                int[] clampedAmplitudes =
                    ClampAmplitudes(amplitudes);

                using AndroidJavaObject effect =
                    vibrationEffectClass
                        .CallStatic<AndroidJavaObject>(
                            "createWaveform",
                            timings,
                            clampedAmplitudes,
                            -1
                        );

                vibrator.Call(
                    "vibrate",
                    effect
                );
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
        Handheld.Vibrate();
#endif
    }

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

    private static int[] ClampAmplitudes(
        int[] amplitudes
    )
    {
        int[] result =
            new int[amplitudes.Length];

        for (int i = 0;
             i < amplitudes.Length;
             i++)
        {
            /*
             * Pattern içindeki 0 değeri,
             * o bölümde titreşim olmadığını ifade eder.
             */
            if (amplitudes[i] <= 0)
            {
                result[i] = 0;
                continue;
            }

            result[i] = Mathf.Clamp(
                amplitudes[i],
                MinAmplitude,
                MaxAmplitude
            );
        }

        return result;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
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

            Debug.LogWarning(
                $"Android vibration could not be initialized: {exception.Message}"
            );
        }
    }
#endif

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