using System.Collections;
using UnityEngine;

public static class EnemyDangerPreviewMesh
{
    public static GameObject CreatePreview(
        Vector2 origin,
        bool useRadius,
        float radius,
        LayerMask coverLayers,
        Color color,
        int rayCount,
        int sortingOrder,
        float innerRadius = 0.35f,
        int radialSegments = 14,
        int smoothingPasses = 2,
        float innerAlphaMultiplier = 0.22f,
        float waveFrontWidth = 0.12f,
        float waveFrontBoost = 0.75f,
        float innerBrightness = 0.55f,
        Shader customShader = null,
        float visibilityRefreshRate = 15f,
        float coverFeather = 0.08f)
    {
        if (!TryGetArenaRect(
                out float minX,
                out float maxX,
                out float minY,
                out float maxY))
        {
            return null;
        }

        Shader shader = customShader;

        if (shader == null)
            shader = Shader.Find("FatefulRush/BossDangerPreview");

        if (shader == null)
        {
            Debug.LogError(
                "BossDangerPreview shader bulunamadi. " +
                "BossDangerPreview.shader dosyasini projeye ekle " +
                "veya Inspector'daki Danger Preview Shader alanina ata."
            );
            return null;
        }

        int requestedSamples = Mathf.Max(180, rayCount);

        int angularSamples = Application.isMobilePlatform
            ? Mathf.Clamp(requestedSamples, 256, 512)
            : Mathf.Clamp(requestedSamples * 2, 512, 1024);

        float maxRange =
            useRadius
                ? Mathf.Max(0.01f, radius)
                : GetFarthestCornerDistance(
                    origin,
                    minX,
                    maxX,
                    minY,
                    maxY
                );

        TextureFormat visibilityTextureFormat =
            SystemInfo.SupportsTextureFormat(
                TextureFormat.RHalf
            )
                ? TextureFormat.RHalf
                : SystemInfo.SupportsTextureFormat(
                    TextureFormat.RGBAHalf
                )
                    ? TextureFormat.RGBAHalf
                    : TextureFormat.RGBAFloat;

        Texture2D visibilityTexture =
            new Texture2D(
                angularSamples,
                1,
                visibilityTextureFormat,
                false,
                true
            )
            {
                name = "AOE_Visibility1D",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                anisoLevel = 0
            };

        Color[] visibilityPixels =
            new Color[angularSamples];

        for (int i = 0; i < angularSamples; i++)
            visibilityPixels[i] = Color.white;

        visibilityTexture.SetPixels(visibilityPixels);
        visibilityTexture.Apply(false, false);

        GameObject previewObject =
            new GameObject("AOE_DangerPreview");

        previewObject.transform.position =
            Vector3.zero;

        MeshFilter meshFilter =
            previewObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            previewObject.AddComponent<MeshRenderer>();

        Mesh quad =
            CreateArenaQuad(
                minX,
                maxX,
                minY,
                maxY
            );

        meshFilter.sharedMesh = quad;

        Material material =
            new Material(shader)
            {
                name =
                    "AOE_DangerPreviewMaterial"
            };

        material.SetTexture(
            "_VisibilityTex",
            visibilityTexture
        );

        material.SetColor(
            "_DangerColor",
            color
        );

        material.SetVector(
            "_Origin",
            new Vector4(
                origin.x,
                origin.y,
                0f,
                0f
            )
        );

        material.SetFloat(
            "_InnerRadius",
            Mathf.Max(0f, innerRadius)
        );

        material.SetFloat(
            "_MaxRange",
            maxRange
        );

        material.SetFloat(
            "_UseRadius",
            useRadius ? 1f : 0f
        );

        material.SetFloat(
            "_Radius",
            Mathf.Max(0.01f, radius)
        );

        material.SetFloat(
            "_InnerAlphaMultiplier",
            Mathf.Clamp01(
                innerAlphaMultiplier
            )
        );

        material.SetFloat(
            "_WaveFrontWidth",
            Mathf.Clamp(
                waveFrontWidth,
                0.01f,
                0.5f
            )
        );

        material.SetFloat(
            "_WaveFrontBoost",
            Mathf.Max(
                0f,
                waveFrontBoost
            )
        );

        material.SetFloat(
            "_InnerBrightness",
            Mathf.Clamp(
                innerBrightness,
                0.1f,
                1f
            )
        );

        material.SetFloat(
            "_CoverFeather",
            Mathf.Max(
                0.001f,
                coverFeather
            )
        );

        material.SetFloat("_Progress", 0f);
        material.SetFloat("_Opacity", 1f);
        material.SetFloat("_StrikeWaveProgress", -1f);
        material.SetFloat("_StrikeWaveWidth", 0.10f);
        material.SetFloat("_StrikeWaveBoost", 1.35f);

        meshRenderer.sharedMaterial =
            material;

        meshRenderer.sortingOrder =
            sortingOrder;

        EnemyDangerPreviewRuntime runtime =
            previewObject.AddComponent<EnemyDangerPreviewRuntime>();

        runtime.Initialize(
            material,
            visibilityTexture,
            visibilityPixels,
            origin,
            useRadius,
            radius,
            coverLayers,
            minX,
            maxX,
            minY,
            maxY,
            maxRange,
            angularSamples,
            visibilityRefreshRate
        );

        return previewObject;
    }

    public static void SetPreviewAlpha(
        GameObject previewObject,
        Color targetColor,
        float normalizedAlpha)
    {
        if (previewObject == null)
            return;

        EnemyDangerPreviewRuntime runtime =
            previewObject.GetComponent<EnemyDangerPreviewRuntime>();

        if (runtime != null)
        {
            runtime.SetProgress(normalizedAlpha);
            return;
        }
    }

    public static void SetPreviewOpacity(
        GameObject previewObject,
        float normalizedOpacity)
    {
        if (previewObject == null)
            return;

        EnemyDangerPreviewRuntime runtime =
            previewObject.GetComponent<EnemyDangerPreviewRuntime>();

        if (runtime != null)
        {
            runtime.SetOpacity(normalizedOpacity);
            return;
        }
    }

    public static void SetStrikeWave(
        GameObject previewObject,
        float progress,
        float width,
        float boost)
    {
        if (previewObject == null)
            return;

        EnemyDangerPreviewRuntime runtime =
            previewObject.GetComponent<EnemyDangerPreviewRuntime>();

        if (runtime != null)
        {
            runtime.SetStrikeWave(
                progress,
                width,
                boost
            );
        }
    }

    public static void DestroyPreview(
        ref GameObject previewObject)
    {
        if (previewObject == null)
            return;

        EnemyDangerPreviewRuntime runtime =
            previewObject.GetComponent<EnemyDangerPreviewRuntime>();

        if (runtime != null)
            runtime.ReleaseResources();

        MeshFilter filter =
            previewObject.GetComponent<MeshFilter>();

        MeshRenderer renderer =
            previewObject.GetComponent<MeshRenderer>();

        if (filter != null &&
            filter.sharedMesh != null)
        {
            Object.Destroy(
                filter.sharedMesh
            );
        }

        if (renderer != null &&
            renderer.sharedMaterial != null)
        {
            Object.Destroy(
                renderer.sharedMaterial
            );
        }

        Object.Destroy(previewObject);
        previewObject = null;
    }

    private static Mesh CreateArenaQuad(
        float minX,
        float maxX,
        float minY,
        float maxY)
    {
        Vector3[] vertices =
        {
            new Vector3(minX, minY, 0f),
            new Vector3(maxX, minY, 0f),
            new Vector3(minX, maxY, 0f),
            new Vector3(maxX, maxY, 0f)
        };

        Vector2[] uvs =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        int[] triangles =
        {
            0, 2, 1,
            1, 2, 3
        };

        Mesh mesh =
            new Mesh
            {
                name =
                    "AOE_DangerPreview_ScreenQuad"
            };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }

    private static float GetFarthestCornerDistance(
        Vector2 origin,
        float minX,
        float maxX,
        float minY,
        float maxY)
    {
        float maxDistance = 0.01f;

        maxDistance = Mathf.Max(
            maxDistance,
            Vector2.Distance(
                origin,
                new Vector2(minX, minY)
            )
        );

        maxDistance = Mathf.Max(
            maxDistance,
            Vector2.Distance(
                origin,
                new Vector2(minX, maxY)
            )
        );

        maxDistance = Mathf.Max(
            maxDistance,
            Vector2.Distance(
                origin,
                new Vector2(maxX, minY)
            )
        );

        maxDistance = Mathf.Max(
            maxDistance,
            Vector2.Distance(
                origin,
                new Vector2(maxX, maxY)
            )
        );

        return maxDistance;
    }

    private static bool TryGetArenaRect(
        out float minX,
        out float maxX,
        out float minY,
        out float maxY)
    {
        CameraWorldBounds worldBounds =
            CameraWorldBounds.Instance;

        if (worldBounds != null)
        {
            minX = worldBounds.MinX;
            maxX = worldBounds.MaxX;
            minY = worldBounds.MinY;
            maxY = worldBounds.MaxY;
            return true;
        }

        Camera camera = Camera.main;

        if (camera == null)
        {
            minX = maxX = minY = maxY = 0f;
            return false;
        }

        float planeDistance =
            Mathf.Abs(
                camera.transform.position.z
            );

        Vector3 bottomLeft =
            camera.ViewportToWorldPoint(
                new Vector3(
                    0f,
                    0f,
                    planeDistance
                )
            );

        Vector3 topRight =
            camera.ViewportToWorldPoint(
                new Vector3(
                    1f,
                    1f,
                    planeDistance
                )
            );

        minX = Mathf.Min(
            bottomLeft.x,
            topRight.x
        );

        maxX = Mathf.Max(
            bottomLeft.x,
            topRight.x
        );

        minY = Mathf.Min(
            bottomLeft.y,
            topRight.y
        );

        maxY = Mathf.Max(
            bottomLeft.y,
            topRight.y
        );

        return true;
    }
}
