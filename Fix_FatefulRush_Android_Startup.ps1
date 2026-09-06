$ErrorActionPreference = "Stop"

$root = Get-Location

function Write-Utf8NoBom([string]$Path, [string]$Content) {
    $full = Join-Path $root $Path
    $dir = Split-Path $full -Parent
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    [System.IO.File]::WriteAllText($full, $Content, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "UPDATED: $Path"
}

$buildGuard = @'
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
        bool developmentBuild =
            (report.summary.options & BuildOptions.Development) != 0;

        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = !developmentBuild;

        Debug.Log(
            "[AndroidPerformanceBuildGuard] Applied: " +
            "FramePacing=OFF, Graphics=OpenGLES3->Vulkan, " +
            "LandscapeOnly=ON, R8Release=" + (!developmentBuild)
        );
    }
}
#endif

'@

$manifest = @'
<?xml version="1.0" encoding="utf-8"?>
<manifest
    xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:tools="http://schemas.android.com/tools">

    <uses-permission android:name="android.permission.VIBRATE" />
    <uses-permission android:name="com.google.android.gms.permission.AD_ID" />

    <application
        android:appCategory="game"
        android:resizeableActivity="true"
        tools:replace="android:appCategory,android:resizeableActivity">

        <activity
            android:name="com.unity3d.player.UnityPlayerActivity"
            android:theme="@style/UnityThemeSelector"
            android:exported="true"
            android:resizeableActivity="true"
            android:screenOrientation="userLandscape"
            tools:replace="android:resizeableActivity,android:screenOrientation">

            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>

            <meta-data
                android:name="unityplayer.UnityActivity"
                android:value="true" />
        </activity>

    </application>

</manifest>

'@

Write-Utf8NoBom "Assets/Scripts/Editor/AndroidPerformanceBuildGuard.cs" $buildGuard
Write-Utf8NoBom "Assets/Plugins/Android/AndroidManifest.xml" $manifest

$remove = @(
    "Assets/Scripts/Platform/AndroidAdaptiveOrientation.cs",
    "Assets/Scripts/Platform/AndroidAdaptiveOrientation.cs.meta",
    "Assets/Scripts/Platform/AndroidEdgeToEdgeBootstrap.cs",
    "Assets/Scripts/Platform/AndroidEdgeToEdgeBootstrap.cs.meta",
    "Assets/Scripts/Editor/AndroidGradleOptimizationPostProcessor.cs",
    "Assets/Scripts/Editor/AndroidGradleOptimizationPostProcessor.cs.meta",
    "Assets/Plugins/Android/FatefulRushCompat.androidlib"
)

foreach ($item in $remove) {
    $full = Join-Path $root $item
    if (Test-Path $full) {
        Remove-Item -Recurse -Force $full
        Write-Host "REMOVED: $item"
    }
}

$compatMeta = Join-Path $root "Assets/Plugins/Android/FatefulRushCompat.androidlib.meta"
if (Test-Path $compatMeta) {
    Remove-Item -Force $compatMeta
    Write-Host "REMOVED: Assets/Plugins/Android/FatefulRushCompat.androidlib.meta"
}

Write-Host ""
Write-Host "Fateful Rush Android startup recovery patch applied."
Write-Host "Cloud Save, HUD, AdMob and shader warmup were NOT removed."
Write-Host "Open Unity, let it reimport, then make a fresh Android build."
