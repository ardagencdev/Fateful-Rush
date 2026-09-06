using UnityEngine;

/// <summary>
/// Applies a modern edge-to-edge window policy through a tiny Android helper.
/// Uses supported APIs and avoids LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES.
/// </summary>
public static class AndroidEdgeToEdgeBootstrap
{
    private const string HelperClass =
        "com.youngdevstudios.fatefulrush.compat.FatefulRushWindowCompat";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            using AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using AndroidJavaClass helper =
                new AndroidJavaClass(HelperClass);

            helper.CallStatic("apply", activity);
        }
        catch (System.Exception exception)
        {
            // Never block startup because of a compatibility helper.
            Debug.LogWarning(
                "[AndroidEdgeToEdgeBootstrap] Edge-to-edge helper " +
                "failed safely: " + exception.Message
            );
        }
#endif
    }
}
