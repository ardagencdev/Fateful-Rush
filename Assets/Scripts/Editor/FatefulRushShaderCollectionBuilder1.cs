#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

public static class FatefulRushShaderCollectionBuilder
{
    private const string OutputPath = "Assets/Resources/FatefulRushRuntimeShaders.shadervariants";

    // These assets are loaded explicitly by gameplay code, so scene
    // dependencies alone do not cover them. No project-wide shader scan.
    private static readonly string[] RuntimeAssets =
    {
        "Assets/Resources/BackgroundPlanets/BackgroundPlanet.mat",
        "Assets/Resources/BackgroundAsteroids/BackgroundAsteroid.mat",
        "Assets/Resources/ComboElectricFX/ComboElectric.mat",
        "Assets/Resources/ObstacleReadability/ObstacleEdge.mat",
        "Assets/Resources/BackgroundPlanets/HomePlanetTheme.shader",
        "Assets/Resources/SolarAtmosphere/SolarAtmosphere.shader",
        "Assets/Resources/SolarAtmosphere/SolarAtmosphereBake.shader",
        "Assets/Resources/SolarAtmosphere/SolarAtmosphereDisplay.shader",
        "Assets/Resources/ResultEdgeGlow/ResultEdgeGlow.shader",
        "Assets/Resources/ResultEdgeGlow/ResultOverlay.shader",
        "Assets/Shaders/BossDangerPreview.shader"
    };

    private static readonly string[] RuntimeFallbackShaders =
    {
        "UI/Default", "Sprites/Default", "TextMeshPro/Mobile/Distance Field"
    };

    // Only visible passes used by this unlit 2D game. Do not prewarm shadow,
    // meta/lightmapping, deferred or HDRP passes just because they exist.
    private static readonly PassType[] RuntimePasses =
    {
        PassType.Normal, PassType.ScriptableRenderPipeline,
        PassType.ScriptableRenderPipelineDefaultUnlit
    };

    [MenuItem("Fateful Rush/Build Runtime Shader Collection")]
    public static void Build()
    {
        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled) scenes.Add(scene.path);
        BuildForScenes(scenes.ToArray());
        ShaderVariantCollection saved = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(OutputPath);
        Selection.activeObject = saved;
        EditorGUIUtility.PingObject(saved);
    }

    public static void BuildForScenes(string[] scenes)
    {
        if (scenes == null || scenes.Length == 0)
            throw new BuildFailedException("[ShaderWarmup] No build scenes were supplied.");

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var candidate = new ShaderVariantCollection { name = "FatefulRushRuntimeShaders" };
        bool savedAsNewAsset = false;
        try
        {
            var roots = new HashSet<string>(scenes, StringComparer.Ordinal);
            foreach (string path in RuntimeAssets)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                    throw new BuildFailedException("[ShaderWarmup] Required runtime asset is missing: " + path);
                roots.Add(path);
            }

            // Runtime-created TMP UI uses the default font, including debug
            // UI. This also reaches fallback fonts without all unused presets.
            if (TMP_Settings.defaultFontAsset != null)
            {
                string fontPath = AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset);
                if (!string.IsNullOrEmpty(fontPath)) roots.Add(fontPath);
            }

            var materials = new HashSet<Material>();
            var paths = new List<string>(roots);
            string[] dependencies = AssetDatabase.GetDependencies(paths.ToArray(), true);
            Array.Sort(dependencies, StringComparer.Ordinal);
            foreach (string path in dependencies)
            {
                // Scene dependencies reach actual font/prefab/material assets.
                // Load sub-assets too: font .asset files contain their material.
                string extension = System.IO.Path.GetExtension(path);
                if (extension != ".mat" && extension != ".asset" && extension != ".fbx") continue;
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Material material) materials.Add(material);
            }

            foreach (Material material in materials)
                if (material != null && material.shader != null && !IsExcluded(material.shader))
                {
                    var keywords = new List<string>();
                    foreach (UnityEngine.Rendering.LocalKeyword keyword in material.enabledKeywords)
                        keywords.Add(keyword.name);
                    var rejections = new List<string>();
                    if (AddStates(candidate, material.shader, keywords.ToArray(), rejections) == 0)
                        throw new BuildFailedException("[ShaderWarmup] No valid runtime variant for material: "
                            + AssetDatabase.GetAssetPath(material) + " / " + material.name
                            + " / shader=" + material.shader.name + " / keywords=" + string.Join("|", keywords)
                            + " / Unity details: " + string.Join("; ", rejections));
                }

            foreach (string path in RuntimeAssets)
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader != null) RequireStates(candidate, shader);
            }
            foreach (string name in RuntimeFallbackShaders)
            {
                Shader shader = Shader.Find(name);
                if (shader == null)
                    throw new BuildFailedException("[ShaderWarmup] Required runtime shader is missing: " + name);
                RequireStates(candidate, shader);
            }

            if (candidate.variantCount == 0)
                throw new BuildFailedException("[ShaderWarmup] Runtime shader collection is empty.");

            // Commit only after successful collection. Never delete the asset:
            // direct GUID references must remain intact across rebuilds.
            ShaderVariantCollection existing = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(OutputPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(candidate, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(candidate, OutputPath);
                savedAsNewAsset = true;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[ShaderWarmup] Updated required runtime shaders: " + candidate.shaderCount
                + " shaders / " + candidate.variantCount + " variants from " + scenes.Length
                + " build scenes and " + materials.Count + " reachable materials. Existing asset GUID preserved.");
        }
        finally
        {
            if (!savedAsNewAsset) UnityEngine.Object.DestroyImmediate(candidate);
        }
    }

    private static void RequireStates(ShaderVariantCollection collection, Shader shader)
    {
        var rejections = new List<string>();
        if (AddStates(collection, shader, Array.Empty<string>(), rejections) == 0)
            throw new BuildFailedException("[ShaderWarmup] No valid runtime pass for required shader: " + shader.name
                + " / Unity details: " + string.Join("; ", rejections));
    }

    private static int AddStates(ShaderVariantCollection collection, Shader shader, string[] materialKeywords,
        List<string> rejections = null)
    {
        int valid = AddPasses(collection, shader, materialKeywords, rejections);
        bool ui = shader.name.StartsWith("UI/", StringComparison.Ordinal)
            || shader.name.StartsWith("TextMeshPro/", StringComparison.Ordinal)
            || shader.name.StartsWith("FatefulRush/UI/", StringComparison.Ordinal);
        if (!ui) return valid;

        // uGUI/TMP can add these two keywords at runtime for clipping.
        // Preserve the material's actual outline/underlay state; do not invent
        // every possible feature combination of a font shader.
        var baseKeywords = new List<string>();
        foreach (string keyword in materialKeywords)
            if (keyword != "UNITY_UI_CLIP_RECT" && keyword != "UNITY_UI_ALPHACLIP")
                baseKeywords.Add(keyword);
        for (int flags = 0; flags < 4; flags++)
        {
            var keywords = new List<string>(baseKeywords);
            if ((flags & 1) != 0) keywords.Add("UNITY_UI_CLIP_RECT");
            if ((flags & 2) != 0) keywords.Add("UNITY_UI_ALPHACLIP");
            valid += AddPasses(collection, shader, keywords.ToArray(), rejections);
        }
        return valid;
    }

    private static int AddPasses(ShaderVariantCollection collection, Shader shader, string[] keywords,
        List<string> rejections)
    {
        int valid = 0;
        foreach (PassType pass in RuntimePasses)
        {
            try
            {
                var variant = new ShaderVariantCollection.ShaderVariant(shader, pass, keywords);
                collection.Add(variant);
                valid++; // An already present valid variant is still valid.
            }
            catch (ArgumentException exception)
            {
                // This shader does not have that pass/keyword combination.
                // Preserve the actual Unity reason if every candidate fails.
                rejections?.Add(pass + ": " + exception.Message);
            }
        }
        return valid;
    }

    private static bool IsExcluded(Shader shader)
    {
        return shader.name.IndexOf("HDRP", StringComparison.OrdinalIgnoreCase) >= 0
            || shader.name.StartsWith("Hidden/Internal", StringComparison.OrdinalIgnoreCase);
    }
}

// Editor-only hook in the existing file; no new scene component. Uses the
// actual BuildPlayerOptions scenes, including a custom Build Profile or APK.
public sealed class FatefulRushShaderBuildPreparation : BuildPlayerProcessor
{
    public override int callbackOrder => -10000;
    public override void PrepareForBuild(BuildPlayerContext context)
    {
        if (context.BuildPlayerOptions.target == BuildTarget.Android)
            FatefulRushShaderCollectionBuilder.BuildForScenes(context.BuildPlayerOptions.scenes);
    }
}
#endif
