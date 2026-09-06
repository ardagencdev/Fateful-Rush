using System;
using UnityEngine;

public static class AndroidEdgeToEdgeBootstrap
{
    private const string HelperClass =
        "com.youngdevstudios.fatefulrush.compat.FatefulRushEdgeToEdge";

    private static bool focusHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Apply();

        if (!focusHookRegistered)
        {
            Application.focusChanged += OnFocusChanged;
            focusHookRegistered = true;
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void OnFocusChanged(bool hasFocus)
    {
        if (hasFocus)
            Apply();
    }

    private static void Apply()
    {
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
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[AndroidEdgeToEdge] Could not apply edge-to-edge: " +
                exception.Message
            );
        }
    }
#endif
}
