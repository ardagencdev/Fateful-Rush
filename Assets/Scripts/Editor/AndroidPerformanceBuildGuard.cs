#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Keeps the dedicated Android/mobile project on phone-optimized build settings.
/// The separate Google Play Games on PC project can keep its own PC-specific guard.
/// </summary>
public sealed class AndroidPerformanceBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        // This is the dedicated phone/tablet Android project. Optimized Frame
        // Pacing reduces uneven Android frame presentation and should stay ON
        // here. The separate GPG-PC project can keep this OFF independently.
        PlayerSettings.Android.optimizedFramePacing = true;

        // Store builds must expose exactly one Android application entry
        // point. Keep the existing Activity path used by the project and
        // prevent GameActivity from being accidentally enabled as a second
        // launcher entry in Player Settings.
        PlayerSettings.Android.applicationEntry =
            AndroidApplicationEntry.Activity;

        // Incremental GC reduces large managed-GC spikes and remains useful
        // on both Android phones and Google Play Games on PC.
        PlayerSettings.gcIncremental = true;

        // Android 15+ enforces edge-to-edge rendering for API 35+ apps.
        // Render the game/background into the full display, while runtime
        // HUD code uses Screen.safeArea to keep critical controls clear of
        // display cutouts and system gesture/navigation regions.
        PlayerSettings.Android.renderOutsideSafeArea = true;
        PlayerSettings.Android.requestedVisibleInsets = AndroidWindowInsetsType.None;

        // Treat this package explicitly as a game. Besides being semantically
        // correct, Android 16's large-screen orientation/resizability changes
        // keep a game-specific exception based on android:appCategory="game".
        PlayerSettings.Android.appCategory = "game";

        // Allow window resizing on tablets/ChromeOS/foldables. The game can
        // still remain landscape-only, but Android is free to resize the
        // activity instead of forcing a legacy fixed-size window.
        PlayerSettings.Android.resizeableActivity = true;

        // Support current phone/tablet aspect ratios on pre-Android-15
        // devices as well. 2.4 comfortably includes the 21:9 anchor ratio.
        PlayerSettings.Android.maxAspectRatio = 2.4f;
        PlayerSettings.Android.minAspectRatio = 1.0f;

        // Unity 6 always uses R8 for Android minification. Enable it only for
        // non-development/release builds; keep Development Builds readable
        // and easier to debug.
        bool developmentBuild =
            (report.summary.options & BuildOptions.Development) != 0;

        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = !developmentBuild;

        Debug.Log(
            "[AndroidPerformanceBuildGuard] Android store settings applied: " +
            "FramePacing=ON, EdgeToEdge=ON, AppCategory=game, " +
            "Resizable=ON, R8Release=" + (!developmentBuild)
        );
    }
}
#endif
