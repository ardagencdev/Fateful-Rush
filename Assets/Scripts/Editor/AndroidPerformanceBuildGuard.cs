#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Keeps Android performance build settings consistent with the game's
/// Google Play Games on PC distribution requirements.
/// </summary>
public sealed class AndroidPerformanceBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        // Google currently recommends disabling Unity's Optimized Frame
        // Pacing for Google Play Games on PC. The same Android artifact can
        // run on phones and GPG on PC, and Unity does not provide a reliable
        // runtime toggle for this Player Setting.
        PlayerSettings.Android.optimizedFramePacing = false;

        // Incremental GC reduces large managed-GC spikes and remains useful
        // on both Android phones and Google Play Games on PC.
        PlayerSettings.gcIncremental = true;
    }
}
#endif
