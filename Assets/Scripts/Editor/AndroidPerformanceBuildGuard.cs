#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Enforces Android store/build settings for Fateful Rush before every Android build.
/// Keeps the project compatible with modern Android, large screens and R8.
/// </summary>
public sealed class AndroidPerformanceBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        PlayerSettings.Android.optimizedFramePacing = true;

        // Fateful Rush uses the classic UnityPlayerActivity entry point.
        PlayerSettings.Android.applicationEntry =
            AndroidApplicationEntry.Activity;

        PlayerSettings.gcIncremental = true;

        // Full-screen / edge-to-edge rendering. Runtime safe-area code keeps
        // important HUD controls clear of cutouts and gesture regions.
        PlayerSettings.Android.renderOutsideSafeArea = true;
        PlayerSettings.Android.requestedVisibleInsets =
            AndroidWindowInsetsType.None;

        // Android 16+ large-screen behavior explicitly recognizes games.
        PlayerSettings.Android.appCategory = "game";

        // Do not opt out of resize/multi-window support.
        PlayerSettings.Android.resizeableActivity = true;

        // Let the manifest be orientation-adaptive. A runtime policy keeps
        // phones landscape while tablets/foldables (sw600dp+) can rotate.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        // Keep broad aspect-ratio support.
        PlayerSettings.Android.maxAspectRatio = 2.4f;
        PlayerSettings.Android.minAspectRatio = 1.0f;

        // R8 code shrinking for release/store builds.
        bool developmentBuild =
            (report.summary.options & BuildOptions.Development) != 0;

        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = !developmentBuild;

        Debug.Log(
            "[AndroidPerformanceBuildGuard] Applied: " +
            "FramePacing=ON, EdgeToEdge=ON, AppCategory=game, " +
            "Resizable=ON, AdaptiveOrientation=ON, R8Release=" +
            (!developmentBuild)
        );
    }
}
#endif
