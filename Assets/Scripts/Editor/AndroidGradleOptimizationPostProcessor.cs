#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Enables Android resource shrinking in the generated launcher RELEASE buildType.
/// Unity's Minify Release setting enables R8 code shrinking; this adds the
/// matching resource shrinker in the correct Gradle block.
/// </summary>
public sealed class AndroidGradleOptimizationPostProcessor :
    IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 10000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        DirectoryInfo unityLibrary = new DirectoryInfo(path);
        DirectoryInfo projectRoot = unityLibrary.Parent;

        if (projectRoot == null)
        {
            Debug.LogWarning(
                "[AndroidGradleOptimization] Gradle root not found."
            );
            return;
        }

        string launcherGradle = Path.Combine(
            projectRoot.FullName,
            "launcher",
            "build.gradle"
        );

        if (!File.Exists(launcherGradle))
        {
            Debug.LogWarning(
                "[AndroidGradleOptimization] launcher/build.gradle not found."
            );
            return;
        }

        string text = File.ReadAllText(launcherGradle);

        // Clean up any old/bad insertion from a previous failed incremental build.
        text = Regex.Replace(
            text,
            @"^[ \t]*shrinkResources\s*(?:=)?\s*true\s*$\r?\n?",
            "",
            RegexOptions.Multiline
        );

        int buildTypesIndex = text.IndexOf(
            "buildTypes",
            StringComparison.Ordinal
        );

        if (buildTypesIndex < 0)
        {
            throw new BuildFailedException(
                "Could not find buildTypes in launcher/build.gradle."
            );
        }

        int buildTypesBrace = text.IndexOf('{', buildTypesIndex);

        if (buildTypesBrace < 0)
        {
            throw new BuildFailedException(
                "Malformed buildTypes block in launcher/build.gradle."
            );
        }

        int buildTypesEnd = FindMatchingBrace(text, buildTypesBrace);

        if (buildTypesEnd < 0)
        {
            throw new BuildFailedException(
                "Could not resolve buildTypes block in launcher/build.gradle."
            );
        }

        int releaseIndex = text.IndexOf(
            "release",
            buildTypesBrace,
            buildTypesEnd - buildTypesBrace,
            StringComparison.Ordinal
        );

        if (releaseIndex < 0)
        {
            throw new BuildFailedException(
                "Could not find release buildType in launcher/build.gradle."
            );
        }

        int releaseBrace = text.IndexOf('{', releaseIndex);

        if (releaseBrace < 0 || releaseBrace > buildTypesEnd)
        {
            throw new BuildFailedException(
                "Malformed release buildType in launcher/build.gradle."
            );
        }

        int releaseEnd = FindMatchingBrace(text, releaseBrace);

        if (releaseEnd < 0 || releaseEnd > buildTypesEnd)
        {
            throw new BuildFailedException(
                "Could not resolve release buildType in launcher/build.gradle."
            );
        }

        string releaseBlock = text.Substring(
            releaseBrace,
            releaseEnd - releaseBrace + 1
        );

        bool codeShrinkingEnabled =
            releaseBlock.Contains("minifyEnabled true") ||
            releaseBlock.Contains("minifyEnabled = true");

        if (!codeShrinkingEnabled)
        {
            throw new BuildFailedException(
                "Release minification is not enabled. " +
                "Resource shrinking requires R8 code shrinking."
            );
        }

        string insertion =
            Environment.NewLine +
            "            // Fateful Rush: remove unused Android resources." +
            Environment.NewLine +
            "            shrinkResources = true";

        text = text.Insert(releaseBrace + 1, insertion);

        File.WriteAllText(launcherGradle, text);

        Debug.Log(
            "[AndroidGradleOptimization] Release buildTypes: " +
            "minifyEnabled=ON, shrinkResources=ON."
        );
    }

    private static int FindMatchingBrace(string text, int openBraceIndex)
    {
        int depth = 0;

        for (int i = openBraceIndex; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;

                if (depth == 0)
                    return i;
            }
        }

        return -1;
    }
}
#endif
