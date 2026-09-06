#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Conservative Android startup/render settings for Fateful Rush.
/// Avoids the Vulkan + autorotation + optimized-frame-pacing startup combination.
/// </summary>
public sealed class AndroidPerformanceBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        // Stability first: disable Swappy / Optimized Frame Pacing.
        PlayerSettings.Android.optimizedFramePacing = false;

        // Start with OpenGLES3. Keep Vulkan secondary for later testing.
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

        // Landscape-only. Do not change orientation during splash/startup.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        PlayerSettings.Android.maxAspectRatio = 2.4f;
        PlayerSettings.Android.minAspectRatio = 1.0f;

        // Keep R8 code shrinking; only the extra resource shrinker is removed.
        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = false;

        Debug.Log(
            "[AndroidPerformanceBuildGuard] Applied: " +
            "FramePacing=OFF, Graphics=OpenGLES3->Vulkan, " +
            "LandscapeOnly=ON, R8=OFF"
        );
    }
}
#endif
