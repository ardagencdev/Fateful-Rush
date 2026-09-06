$ErrorActionPreference = "Stop"
$root = Get-Location

$guardPath = Join-Path $root "Assets/Scripts/Editor/AndroidPerformanceBuildGuard.cs"
$settingsPath = Join-Path $root "ProjectSettings/ProjectSettings.asset"

if (!(Test-Path $guardPath)) {
    throw "AndroidPerformanceBuildGuard.cs bulunamadi. Scripti Unity proje kokunde calistir."
}

$guard = Get-Content $guardPath -Raw

$guard = $guard -replace 'PlayerSettings\.Android\.minifyDebug\s*=\s*false;', 'PlayerSettings.Android.minifyDebug = false;'
$guard = $guard -replace 'PlayerSettings\.Android\.minifyRelease\s*=\s*!developmentBuild;', 'PlayerSettings.Android.minifyRelease = false;'

# Clean up the no-longer-needed developmentBuild block if present.
$guard = $guard -replace '(?s)\s*bool developmentBuild\s*=\s*\(report\.summary\.options & BuildOptions\.Development\) != 0;\s*', "`r`n        "

# Replace log tail if it still references R8Release/developmentBuild.
$guard = $guard -replace '"LandscapeOnly=ON, R8Release="\s*\+\s*\(!developmentBuild\)', '"LandscapeOnly=ON, R8=OFF"'
$guard = $guard -replace '"LandscapeOnly=ON, AppCategory=game, Resizable=ON, R8Release="\s*\+\s*\(!developmentBuild\)', '"LandscapeOnly=ON, AppCategory=game, Resizable=ON, R8=OFF"'

[System.IO.File]::WriteAllText(
    $guardPath,
    $guard,
    (New-Object System.Text.UTF8Encoding($false))
)
Write-Host "UPDATED: Assets/Scripts/Editor/AndroidPerformanceBuildGuard.cs"

if (Test-Path $settingsPath) {
    $settings = Get-Content $settingsPath -Raw
    $settings = $settings -replace 'AndroidMinifyRelease:\s*1', 'AndroidMinifyRelease: 0'
    $settings = $settings -replace 'AndroidMinifyDebug:\s*1', 'AndroidMinifyDebug: 0'

    [System.IO.File]::WriteAllText(
        $settingsPath,
        $settings,
        (New-Object System.Text.UTF8Encoding($false))
    )
    Write-Host "UPDATED: ProjectSettings/ProjectSettings.asset"
}

Write-Host ""
Write-Host "R8 / Minify tamamen kapatildi."
Write-Host "ProGuard dosyasi kalabilir; minify kapaliyken kullanilmaz."
Write-Host "Unity'ye don, reimport/compile bitsin ve yeni build al."
