#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FatefulRushShaderCollectionBuilder
{
    private const string OutputPath =
        "Assets/Resources/FatefulRushRuntimeShaders.shadervariants";

    // Runtime shaders that can be used without a standalone Material asset.
    private static readonly string[] RuntimeFallbackShaders =
    {
        "UI/Default",
        "Sprites/Default",
        "TextMeshPro/Distance Field",
        "TextMeshPro/Mobile/Distance Field",
        "Universal Render Pipeline/2D/Sprite-Unlit-Default",
        "Universal Render Pipeline/2D/Sprite-Lit-Default",
        "Universal Render Pipeline/Particles/Unlit"
    };

    [MenuItem("Fateful Rush/Build Runtime Shader Collection")]
    public static void Build()
    {
        EnsureResourcesFolder();

        var collection = new ShaderVariantCollection();
        var materials = FindAllProjectMaterials();

        int scannedMaterials = 0;
        int skippedHdrpShaders = 0;
        int skippedEditorShaders = 0;

        foreach (Material material in materials)
        {
            if (material == null || material.shader == null)
                continue;

            string shaderName = material.shader.name;

            if (IsHdrpShader(shaderName))
            {
                skippedHdrpShaders++;
                continue;
            }

            if (IsEditorOnlyShader(shaderName))
            {
                skippedEditorShaders++;
                continue;
            }

            scannedMaterials++;

            string[] keywords =
                material.shaderKeywords ?? Array.Empty<string>();

            AddValidVariants(
                collection,
                material.shader,
                keywords
            );

            // Also include the base/no-keyword state because the same shader can
            // be reused by materials or runtime-created renderers with no keywords.
            if (keywords.Length > 0)
            {
                AddValidVariants(
                    collection,
                    material.shader,
                    Array.Empty<string>()
                );
            }
        }

        // Add core runtime shaders which might not have explicit material assets.
        foreach (string shaderName in RuntimeFallbackShaders)
        {
            Shader shader = Shader.Find(shaderName);

            if (shader == null)
                continue;

            if (IsHdrpShader(shader.name) || IsEditorOnlyShader(shader.name))
                continue;

            AddValidVariants(
                collection,
                shader,
                Array.Empty<string>()
            );
        }

        if (AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(OutputPath) != null)
            AssetDatabase.DeleteAsset(OutputPath);

        AssetDatabase.CreateAsset(collection, OutputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = collection;
        EditorGUIUtility.PingObject(collection);

        Debug.Log(
            "[Fateful Rush] Runtime Shader Collection rebuilt.\n" +
            "Materials included: " + scannedMaterials + "\n" +
            "HDRP shaders skipped: " + skippedHdrpShaders + "\n" +
            "Editor/internal shaders skipped: " + skippedEditorShaders + "\n" +
            "Shaders in collection: " + collection.shaderCount + "\n" +
            "Variants in collection: " + collection.variantCount + "\n" +
            "Output: " + OutputPath
        );
    }

    private static HashSet<Material> FindAllProjectMaterials()
    {
        var result = new HashSet<Material>();

        string[] guids =
            AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                continue;

            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Material material)
                    result.Add(material);
            }
        }

        return result;
    }

    private static void AddValidVariants(
        ShaderVariantCollection collection,
        Shader shader,
        string[] keywords)
    {
        foreach (PassType passType in Enum.GetValues(typeof(PassType)))
        {
            try
            {
                var variant =
                    new ShaderVariantCollection.ShaderVariant(
                        shader,
                        passType,
                        keywords
                    );

                collection.Add(variant);
            }
            catch (ArgumentException)
            {
                // Normal: this pass/keyword combination does not exist.
            }
        }
    }

    private static bool IsHdrpShader(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
            return false;

        return shaderName.IndexOf(
            "HDRP",
            StringComparison.OrdinalIgnoreCase
        ) >= 0;
    }

    private static bool IsEditorOnlyShader(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
            return true;

        return shaderName.StartsWith(
                   "Hidden/Internal-GUI",
                   StringComparison.OrdinalIgnoreCase
               )
               || shaderName.Equals(
                   "Hidden/BlitCopy",
                   StringComparison.OrdinalIgnoreCase
               )
               || shaderName.Equals(
                   "Hidden/InternalErrorShader",
                   StringComparison.OrdinalIgnoreCase
               );
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
    }
}
#endif
