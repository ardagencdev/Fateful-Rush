#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Android startup/render/release settings for Fateful Rush.
/// OpenGLES3 is preferred, Vulkan is kept as fallback.
/// Release builds use Android code minification (R8).
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

        // Diagnostics are supplied as per-build extra defines by the dedicated
        // APK tool, never as persistent project defines for a Play release.
        string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
        foreach (string symbol in defines.Split(';'))
        {
            if (symbol.Trim().StartsWith("FATEFULRUSH_DIAGNOSTICS", System.StringComparison.Ordinal) ||
                symbol.Trim() == "FATEFULRUSH_FRAME_TIMINGS" ||
                symbol.Trim() == "FATEFULRUSH_GPU_RECORDERS_OFF" ||
                symbol.Trim() == "FATEFULRUSH_FRAME_PACING_OFF_TEST")
                throw new BuildFailedException("Remove debug-only FATEFULRUSH defines from Android Scripting Define Symbols. Use Tools > Fateful Rush > Build Diagnostics APK for the separate test app.");
        }

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

        // Incremental GC
        PlayerSettings.gcIncremental = true;

        // Edge-to-edge / fullscreen
        PlayerSettings.Android.renderOutsideSafeArea = true;
        PlayerSettings.Android.requestedVisibleInsets =
            AndroidWindowInsetsType.None;

        // Android application behavior
        PlayerSettings.Android.appCategory = "game";
        PlayerSettings.Android.resizeableActivity = true;

        // Landscape only
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        // Supported aspect ratios
        PlayerSettings.Android.maxAspectRatio = 2.4f;
        PlayerSettings.Android.minAspectRatio = 1.0f;

        // Android code shrinking / optimization.
        // Keep debug builds unminified for easier debugging.
        // Production release builds use R8.
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
