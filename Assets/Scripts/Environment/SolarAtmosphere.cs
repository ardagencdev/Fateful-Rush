using UnityEngine;

/// <summary>One background-only sun source per scene, shared by atmosphere and sprite shaders.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(350)]
public sealed partial class SolarAtmosphere : MonoBehaviour
{
    [Header("Camera and placement")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("MainMenu is detected automatically; enable for a renamed menu scene.")]
    [SerializeField] private bool forceMainMenu;
    [SerializeField] private Vector2 menuSourceViewport = new Vector2(1.05f, 1.05f);
    [SerializeField, Range(0.02f, 0.25f)] private float sourceOutsideMargin = 0.07f;
    [SerializeField] private float worldZ = 5f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = -54;
    [Header("Atmosphere")]
    [SerializeField, Range(0f, 1f)] private float menuIntensity = 0.34f;
    [SerializeField, Range(0f, 1f)] private float gameplayIntensity = 0.28f;
    [SerializeField, Range(0f, 1f)] private float surfaceLightStrength = 0.35f;
    [SerializeField, Range(0f, 1f)] private float gameplayCenterProtection = 0.85f;
    [SerializeField, Range(0f, 1f)] private float menuCenterProtection = 0.25f;
    [Header("Skin palettes")]
    [SerializeField] private Color normalLight = new Color(1f, 0.83f, 0.62f, 1f);
    [SerializeField] private Color darkLight = new Color(0.78f, 0.075f, 0.13f, 1f);
    [SerializeField] private Color goldenLight = new Color(1f, 0.91f, 0.67f, 1f);
    [SerializeField, Min(0.1f)] private float skinTransitionDuration = 1.4f;
    private static SolarAtmosphere owner;
    private static int previousGameplayEdge = -1;
    private static readonly int SourceId = Shader.PropertyToID("_FRSolarPosition");
    private static readonly int ViewportId = Shader.PropertyToID("_FRSolarViewport");
    private static readonly int ColorId = Shader.PropertyToID("_FRSolarColor");
    private static readonly int AtmosphereId = Shader.PropertyToID("_FRSolarAtmosphereStrength");
    private static readonly int SurfaceId = Shader.PropertyToID("_FRSolarSurfaceStrength");
    private static readonly int ClockId = Shader.PropertyToID("_FRSolarClock");
    private static readonly int ProtectionId = Shader.PropertyToID("_FRSolarCenterProtection");
    private Material material;
    private Mesh mesh;
    private MeshRenderer backdrop;
    private Vector2 source;
    private Color currentColor, fromColor, targetColor;
    private float colorElapsed, clock;
    private bool initialized, isMenu, lastStarted, hasStartedMatch;

    private void OnEnable()
    {
        if (owner != null && owner != this && owner.isActiveAndEnabled)
        {
            Debug.LogWarning("SolarAtmosphere: use only one active component per scene.", this);
            enabled = false;
            return;
        }
        owner = this;
        PlayerSkinCatalog.SelectedSkinChanged += HandleSkinChanged;
        if (initialized)
        {
            backdrop.enabled = true;
            atmosphereBaked = false;
            HandleSkinChanged();
        }
    }
    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        Shader shader = Resources.Load<Shader>("SolarAtmosphere/SolarAtmosphere");
        if (targetCamera == null || !targetCamera.orthographic || shader == null)
        {
            Debug.LogWarning("SolarAtmosphere: requires an orthographic camera and the supplied Resources shader.", this);
            enabled = false;
            return;
        }
        isMenu = forceMainMenu || gameObject.scene.name == "MainMenu";
        material = new Material(shader);
        mesh = new Mesh { name = "SolarAtmosphereQuad" };
        mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        GameObject child = new GameObject("SolarAtmosphereBackdrop");
        child.transform.SetParent(transform, false);
        int backgroundLayer = LayerMask.NameToLayer("Background");
        child.layer = backgroundLayer >= 0 ? backgroundLayer : gameObject.layer;
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        backdrop = child.AddComponent<MeshRenderer>();
        backdrop.sharedMaterial = material;
        backdrop.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        backdrop.receiveShadows = false;
        currentColor = fromColor = targetColor = EquippedColor();
        colorElapsed = skinTransitionDuration;
        source = isMenu ? menuSourceViewport : ChooseGameplaySource();
        lastStarted = GameStateManager.IsGameplayStarted;
        hasStartedMatch = lastStarted;
        initialized = true;
        InitializeReducedAtmosphere();
        Apply();
    }
    private Color EquippedColor()
    {
        string id = PlayerPrefs.GetString(PlayerSkinCatalog.SelectedSkinKey, "white")
            .Trim().ToLowerInvariant().Replace("_", "").Replace("-", "").Replace(" ", "");
        if (id == "dark" || id == "black") return darkLight;
        if (id == "gold" || id == "golden") return goldenLight;
        return normalLight;
    }
    private void HandleSkinChanged()
    {
        if (!initialized) return;
        fromColor = currentColor;
        targetColor = EquippedColor();
        colorElapsed = 0f;
    }
    private Vector2 ChooseGameplaySource()
    {
        int edge = Random.Range(0, 4);
        if (edge == previousGameplayEdge) edge = (edge + Random.Range(1, 4)) % 4;
        previousGameplayEdge = edge;
        float along = Random.Range(0.12f, 0.88f);
        float margin = Mathf.Max(0.02f, sourceOutsideMargin);
        if (edge == 0) return new Vector2(-margin, along);
        if (edge == 1) return new Vector2(1f + margin, along);
        if (edge == 2) return new Vector2(along, -margin);
        return new Vector2(along, 1f + margin);
    }
    private void LateUpdate()
    {
        if (!initialized || owner != this || targetCamera == null) return;
        bool started = GameStateManager.IsGameplayStarted;
        if (!isMenu && started && !lastStarted)
        {
            if (hasStartedMatch) source = ChooseGameplaySource();
            hasStartedMatch = true;
        }
        lastStarted = started;
        float dt = Time.unscaledDeltaTime;
        if (isMenu || (Time.timeScale > 0f && !GameStateManager.IsGameplayEnded)) clock += dt;
        else if (GameStateManager.IsGameplayEnded) clock += Mathf.Min(dt, 0.1f) * 0.4f;
        colorElapsed += dt;
        float t = Mathf.Clamp01(colorElapsed / Mathf.Max(0.1f, skinTransitionDuration));
        currentColor = Color.Lerp(fromColor, targetColor, t * t * (3f - 2f * t));
        if (isMenu) source = menuSourceViewport;
        Apply();
    }
    private void Apply()
    {
        float depth = Vector3.Dot(new Vector3(0f, 0f, worldZ) - targetCamera.transform.position, targetCamera.transform.forward);
        bool visible = depth > targetCamera.nearClipPlane && depth < targetCamera.farClipPlane;
        backdrop.enabled = visible;
        backdrop.sortingLayerName = sortingLayerName;
        backdrop.sortingOrder = sortingOrder;
        Transform quad = backdrop.transform;
        quad.position = targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
        quad.rotation = targetCamera.transform.rotation;
        float height = targetCamera.orthographicSize * 2f;
        Vector3 parentScale = transform.lossyScale;
        quad.localScale = new Vector3(height * targetCamera.aspect / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
            height / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
        Vector3 sourceWorld = targetCamera.ViewportToWorldPoint(new Vector3(source.x, source.y, depth));
        Shader.SetGlobalVector(SourceId, new Vector4(sourceWorld.x, sourceWorld.y, sourceWorld.z, 1f));
        Shader.SetGlobalVector(ViewportId, new Vector4(source.x, source.y, targetCamera.aspect, 0f));
        Shader.SetGlobalColor(ColorId, currentColor);
        Shader.SetGlobalFloat(AtmosphereId, visible ? (isMenu ? menuIntensity : gameplayIntensity) : 0f);
        Shader.SetGlobalFloat(SurfaceId, visible ? surfaceLightStrength : 0f);
        Shader.SetGlobalFloat(ClockId, clock);
        Shader.SetGlobalFloat(ProtectionId, isMenu ? menuCenterProtection : gameplayCenterProtection);
        RenderReducedAtmosphere(visible);
    }
    private void OnDisable()
    {
        PlayerSkinCatalog.SelectedSkinChanged -= HandleSkinChanged;
        if (backdrop != null) backdrop.enabled = false;
        if (owner != this) return;
        Shader.SetGlobalFloat(AtmosphereId, 0f);
        Shader.SetGlobalFloat(SurfaceId, 0f);
        owner = null;
    }
    private void OnDestroy()
    {
        ReleaseReducedAtmosphere();
        if (material != null) Destroy(material);
        if (mesh != null) Destroy(mesh);
    }
}
