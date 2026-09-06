using UnityEngine;

/// <summary>
/// Keeps normal phones landscape, while allowing Android large-screen devices
/// (tablets/foldables/desktop-style windows, sw600dp+) to rotate and resize.
/// This avoids hard-coding USER_LANDSCAPE in the Android manifest.
/// </summary>
public static class AndroidAdaptiveOrientation
{
    private const int LargeScreenSmallestWidthDp = 600;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Apply()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int smallestWidthDp = GetSmallestWidthDp();

        bool largeScreen =
            smallestWidthDp >= LargeScreenSmallestWidthDp;

        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = largeScreen;
        Screen.autorotateToPortraitUpsideDown = largeScreen;
        Screen.orientation = ScreenOrientation.AutoRotation;

        Debug.Log(
            "[AndroidAdaptiveOrientation] smallestWidthDp=" +
            smallestWidthDp +
            ", largeScreen=" + largeScreen
        );
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static int GetSmallestWidthDp()
    {
        try
        {
            using AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            using AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using AndroidJavaObject resources =
                activity.Call<AndroidJavaObject>("getResources");

            using AndroidJavaObject configuration =
                resources.Call<AndroidJavaObject>("getConfiguration");

            return configuration.Get<int>("smallestScreenWidthDp");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "[AndroidAdaptiveOrientation] Could not read " +
                "smallestScreenWidthDp; keeping phone landscape policy. " +
                exception.Message
            );

            return 0;
        }
    }
#endif
}
