using UnityEngine;

/// <summary>
/// Centralized frame-rate policy for Android mobile, Google Play Games on PC,
/// and native desktop/editor runs.
/// </summary>
public static class RuntimePerformancePolicy
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string GooglePlayGamesPcFeature =
        "com.google.android.play.feature.HPE_EXPERIENCE";

    private static int googlePlayGamesPcState = -1;
#endif

    /// <summary>
    /// True when the Android build is running inside Google Play Games on PC.
    /// Uses Google's documented HPE_EXPERIENCE system feature.
    /// </summary>
    public static bool IsGooglePlayGamesOnPC
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (googlePlayGamesPcState >= 0)
                return googlePlayGamesPcState == 1;

            try
            {
                using AndroidJavaClass unityPlayer =
                    new AndroidJavaClass("com.unity3d.player.UnityPlayer");

                using AndroidJavaObject currentActivity =
                    unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                if (currentActivity == null)
                    return false;

                using AndroidJavaObject packageManager =
                    currentActivity.Call<AndroidJavaObject>("getPackageManager");

                if (packageManager == null)
                    return false;

                bool isPc = packageManager.Call<bool>(
                    "hasSystemFeature",
                    GooglePlayGamesPcFeature
                );

                googlePlayGamesPcState = isPc ? 1 : 0;
                return isPc;
            }
            catch
            {
                // Do not let platform detection break startup. If Android's
                // activity is not ready yet, a later call can try again.
                return false;
            }
#else
            return false;
#endif
        }
    }

    public static bool IsNativeDesktopOrEditor
    {
        get
        {
            if (Application.isEditor)
                return true;

            RuntimePlatform platform = Application.platform;

            return platform == RuntimePlatform.WindowsPlayer ||
                   platform == RuntimePlatform.OSXPlayer ||
                   platform == RuntimePlatform.LinuxPlayer;
        }
    }

    public static bool IsPhysicalMobileRuntime
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return !IsGooglePlayGamesOnPC;
#elif UNITY_IOS && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Applies the user's 30/60 preference on real mobile devices.
    /// Google Play Games on PC instead targets the display refresh rate.
    /// Native desktop/editor uses hardware VSync for smooth frame pacing.
    /// </summary>
    public static void ApplyFrameRate(int mobileTargetFrameRate)
    {
        int validatedMobileTarget =
            mobileTargetFrameRate <= 30 ? 30 : 60;

        if (IsGooglePlayGamesOnPC)
        {
            // GPG on PC is still reported to Unity as Android, so -1 would
            // fall back to Android's low default. Explicitly request the
            // display refresh rate instead.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = GetDisplayRefreshRate();
            return;
        }

        if (IsNativeDesktopOrEditor)
        {
            // Unity recommends VSync rather than a software targetFrameRate
            // cap on desktop when smooth frame pacing is the priority.
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            return;
        }

        // Android/iOS ignore vSyncCount and are controlled by targetFrameRate.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = validatedMobileTarget;
    }

    public static int GetDisplayRefreshRate()
    {
        float refreshRate =
            (float)Screen.currentResolution.refreshRateRatio.value;

        if (refreshRate < 1f || float.IsNaN(refreshRate))
            return 60;

        return Mathf.Max(60, Mathf.RoundToInt(refreshRate));
    }
}
