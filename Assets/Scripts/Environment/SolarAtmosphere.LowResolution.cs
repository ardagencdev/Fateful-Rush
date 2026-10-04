using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class SolarAtmosphere
{
    [Header("Atmosphere rendering budget")]
    [Tooltip("Only the soft light overlay is reduced; sprites, UI and gameplay stay at full resolution.")]
    [SerializeField] private bool useReducedResolutionAtmosphere = true;
    [SerializeField, Range(0.25f, 1f)] private float atmosphereResolutionScale = 0.5f;

    private static readonly int SolarAxisId = Shader.PropertyToID("_SolarAxis");
    private static readonly int RayCenters0Id = Shader.PropertyToID("_RayCenters0");
    private static readonly int RayCenters1Id = Shader.PropertyToID("_RayCenters1");
    private static readonly int SolarBreatheId = Shader.PropertyToID("_SolarBreathe");
    private Material atmosphereBakeMaterial;
    private Material atmosphereDisplayMaterial;
    private RenderTexture atmosphereTexture;
    private CommandBuffer atmosphereCommands;
    private Vector4 lastBakedViewport;
    private Color lastBakedColor;
    private float lastBakedClock, lastBakedIntensity, lastBakedProtection;
    private bool atmosphereBaked;
    private bool reducedAtmosphereFailed;

    private void InitializeReducedAtmosphere()
    {
        Shader bakeShader = Resources.Load<Shader>("SolarAtmosphere/SolarAtmosphereBake");
        Shader displayShader = Resources.Load<Shader>("SolarAtmosphere/SolarAtmosphereDisplay");
        // Keep the original full-resolution renderer as a compatibility fallback.
        if (bakeShader == null || displayShader == null || !bakeShader.isSupported ||
            !displayShader.isSupported)
            return;
        atmosphereBakeMaterial = new Material(bakeShader);
        atmosphereDisplayMaterial = new Material(displayShader);
    }

    private void RenderReducedAtmosphere(bool visible)
    {
        float strength = isMenu ? menuIntensity : gameplayIntensity;
        if (!visible || strength <= 0f)
        {
            backdrop.enabled = false;
            return;
        }
        if (!useReducedResolutionAtmosphere || reducedAtmosphereFailed || atmosphereBakeMaterial == null ||
            atmosphereDisplayMaterial == null)
        {
            backdrop.sharedMaterial = material;
            return;
        }
        int width = Mathf.Clamp(Mathf.CeilToInt(targetCamera.pixelWidth *
            Mathf.Clamp(atmosphereResolutionScale, 0.25f, 1f)), 1, SystemInfo.maxTextureSize);
        int height = Mathf.Clamp(Mathf.CeilToInt(targetCamera.pixelHeight *
            Mathf.Clamp(atmosphereResolutionScale, 0.25f, 1f)), 1, SystemInfo.maxTextureSize);
        if (atmosphereTexture == null || atmosphereTexture.width != width ||
            atmosphereTexture.height != height || !atmosphereTexture.IsCreated())
        {
            ReleaseAtmosphereTarget();
            RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            atmosphereTexture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear)
            {
                name = "SolarAtmosphereHalfResolution",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1
            };
            if (!atmosphereTexture.Create())
            {
                ReleaseAtmosphereTarget();
                reducedAtmosphereFailed = true;
                backdrop.sharedMaterial = material;
                return;
            }
            atmosphereDisplayMaterial.SetTexture("_MainTex", atmosphereTexture);
            atmosphereCommands = new CommandBuffer { name = "Bake soft solar atmosphere" };
            atmosphereCommands.SetRenderTarget(atmosphereTexture);
            atmosphereCommands.SetViewport(new Rect(0, 0, width, height));
            atmosphereCommands.DrawProcedural(Matrix4x4.identity, atmosphereBakeMaterial,
                0, MeshTopology.Triangles, 3, 1);
        }
        backdrop.sharedMaterial = atmosphereDisplayMaterial;
        Vector4 viewport = new Vector4(source.x, source.y, targetCamera.aspect, 0f);
        float protection = isMenu ? menuCenterProtection : gameplayCenterProtection;
        if (atmosphereBaked && viewport.Equals(lastBakedViewport) &&
            currentColor.Equals(lastBakedColor) && clock == lastBakedClock &&
            strength == lastBakedIntensity && protection == lastBakedProtection)
            return;

        // These coefficients depend on time/source, not pixels. Evaluate once.
        Vector2 axis = new Vector2((0.5f - source.x) * targetCamera.aspect, 0.5f - source.y).normalized;
        atmosphereBakeMaterial.SetVector(SolarAxisId, new Vector4(axis.x, axis.y, 0f, 0f));
        Vector4 centers0 = Vector4.zero, centers1 = Vector4.zero;
        for (int i = 0; i < 6; i++)
        {
            float center = (i - 2.5f) * 0.13f + Mathf.Sin(clock * 0.045f + i * 2.3f) * 0.028f;
            if (i < 4) centers0[i] = center; else centers1[i - 4] = center;
        }
        atmosphereBakeMaterial.SetVector(RayCenters0Id, centers0);
        atmosphereBakeMaterial.SetVector(RayCenters1Id, centers1);
        atmosphereBakeMaterial.SetFloat(SolarBreatheId, 1f + Mathf.Sin(clock * 0.12f) * 0.025f);
        RenderTexture previousTarget = RenderTexture.active;
        try
        {
            Graphics.ExecuteCommandBuffer(atmosphereCommands);
        }
        finally
        {
            RenderTexture.active = previousTarget;
        }
        lastBakedViewport = viewport;
        lastBakedColor = currentColor;
        lastBakedClock = clock;
        lastBakedIntensity = strength;
        lastBakedProtection = protection;
        atmosphereBaked = true;
    }

    private void ReleaseAtmosphereTarget()
    {
        atmosphereBaked = false;
        if (atmosphereCommands != null)
        {
            atmosphereCommands.Release();
            atmosphereCommands = null;
        }
        if (atmosphereTexture != null)
        {
            atmosphereTexture.Release();
            Destroy(atmosphereTexture);
            atmosphereTexture = null;
        }
    }

    private void ReleaseReducedAtmosphere()
    {
        ReleaseAtmosphereTarget();
        if (atmosphereBakeMaterial != null) Destroy(atmosphereBakeMaterial);
        if (atmosphereDisplayMaterial != null) Destroy(atmosphereDisplayMaterial);
    }
}
