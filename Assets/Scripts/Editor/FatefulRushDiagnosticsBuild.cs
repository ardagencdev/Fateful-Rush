#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FatefulRushDiagnosticsBuild
{
    const string DebugId = "com.youngdevstudios.fatefulrush.debug";
    [MenuItem("Tools/Fateful Rush/Build Diagnostics APK")]
    public static void Build() => BuildAPK(false);
    [MenuItem("Tools/Fateful Rush/Build Diagnostics APK - CPU GPU Timings")]
    public static void BuildWithTimings() => BuildAPK(true);
    static void BuildAPK(bool frameTimings)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUtility.DisplayDialog("Android required", "Build Profiles > Android > Switch Platform yap, sonra bu komutu tekrar çalıştır.", "OK");
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play Mode and wait for compilation before building diagnostics."); return;
        }
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("Build Profiles > Scene List: enabled scenes are required."); return;
        }
        string path = EditorUtility.SaveFilePanel("Save diagnostics APK", "", "FaithfulRush-debug", "apk");
        if (string.IsNullOrEmpty(path)) return;
        string originalId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        string originalName = PlayerSettings.productName;
        bool originalTiming = PlayerSettings.enableFrameTimingStats;
        bool originalGpuRecorders = PlayerSettings.enableOpenGLProfilerGPURecorders;
        bool originalKeystore = PlayerSettings.Android.useCustomKeystore;
        bool originalBundle = EditorUserBuildSettings.buildAppBundle;
        bool originalDevelopment = EditorUserBuildSettings.development;
        bool originalConnect = EditorUserBuildSettings.connectProfiler;
        bool originalDebug = EditorUserBuildSettings.allowDebugging;
        bool originalExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        try
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, DebugId);
            PlayerSettings.productName = "Faithful Rush debug";
            // Adaptive Performance requires the frame-timing setting at build time.
            // The normal test does not call CaptureFrameTimings from the HUD.
            PlayerSettings.enableFrameTimingStats = true;
            // Keep Adaptive Performance prerequisites. Isolate optional OpenGL
            // GPU profiler recording in the normal diagnostics APK.
            PlayerSettings.enableOpenGLProfilerGPURecorders = frameTimings;
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = path, target = BuildTarget.Android,
                options = BuildOptions.None,
                extraScriptingDefines = frameTimings
                    ? new[] { "FATEFULRUSH_DIAGNOSTICS", "FATEFULRUSH_FRAME_TIMINGS" }
                    : new[] { "FATEFULRUSH_DIAGNOSTICS", "FATEFULRUSH_GPU_RECORDERS_OFF" }
            });
            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(path);
                Debug.Log("Diagnostics APK ready: " + path + " | " + DebugId + " | production settings restored after build.");
            }
            else Debug.LogError("Diagnostics APK failed. Inspect the first build error in Console.");
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, originalId);
            PlayerSettings.productName = originalName;
            PlayerSettings.enableFrameTimingStats = originalTiming;
            PlayerSettings.enableOpenGLProfilerGPURecorders = originalGpuRecorders;
            PlayerSettings.Android.useCustomKeystore = originalKeystore;
            EditorUserBuildSettings.buildAppBundle = originalBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = originalExport;
            EditorUserBuildSettings.development = originalDevelopment;
            EditorUserBuildSettings.connectProfiler = originalConnect;
            EditorUserBuildSettings.allowDebugging = originalDebug;
        }
    }
}
#endif
