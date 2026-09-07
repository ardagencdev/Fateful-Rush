#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Android startup/render settings for Fateful Rush.
/// OpenGLES3 is preferred, Vulkan is kept as fallback.
/// </summary>
public sealed class AndroidPerformanceBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        // Optimized Frame Pacing
        PlayerSettings.Android.optimizedFramePacing = true;

        // Prefer OpenGLES3, keep Vulkan as fallback.
        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.Android,
            new[]
            {
                GraphicsDeviceType.OpenGLES3,
                GraphicsDeviceType.Vulkan
            }
        );

        PlayerSettings.Android.applicationEntry =
            AndroidApplicationEntry.Activity;

        PlayerSettings.gcIncremental = true;

        PlayerSettings.Android.renderOutsideSafeArea = true;
        PlayerSettings.Android.requestedVisibleInsets =
            AndroidWindowInsetsType.None;

        PlayerSettings.Android.appCategory = "game";
        PlayerSettings.Android.resizeableActivity = true;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        PlayerSettings.Android.maxAspectRatio = 2.4f;
        PlayerSettings.Android.minAspectRatio = 1.0f;

        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = false;

        Debug.Log(
            "[AndroidPerformanceBuildGuard] Applied: " +
            "FramePacing=ON, Graphics=OpenGLES3->Vulkan, " +
            "LandscapeOnly=ON, R8=OFF"
        );
    }
}
#endif