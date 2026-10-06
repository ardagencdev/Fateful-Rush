using UnityEngine;

/// <summary>Fixed menu planet with slow rotation and optional skin-driven transformation.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(250)]
public sealed class HomePlanetVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Sprite earthSprite;
    [Tooltip("Leave empty to reuse the background planet material.")]
    [SerializeField] private Material planetMaterial;

    [Header("Skin themes (enable on Earth and Moon for themed sprites)")]
    [SerializeField] private bool followEquippedSkin = false;
    [SerializeField] private Sprite darkPlanetSprite;
    [SerializeField] private Sprite goldenPlanetSprite;
    [SerializeField, Min(0.1f)] private float transitionDuration = 1.2f;

    [Header("Normal skin Moons (enable ONLY on the Moon object)")]
    [SerializeField] private bool useColoredSkinMoons;
    [SerializeField] private Sprite blueMoonSprite;
    [SerializeField] private Sprite orangeMoonSprite;
    [SerializeField] private Sprite redMoonSprite;
    [SerializeField] private Sprite greenMoonSprite;
    [SerializeField] private Sprite pinkMoonSprite;
    [SerializeField] private Sprite yellowMoonSprite;
    [SerializeField] private Sprite cyanMoonSprite;
    [SerializeField] private Sprite purpleMoonSprite;

    [Header("Special skin transformation")]
    [SerializeField] private bool dramaticTransitions = true;
    [Tooltip("Shared duration for Dark and Golden; preserves your existing Golden timing.")]
    [SerializeField, Min(0.1f)] private float goldenTransitionDuration = 1.4f;
    [SerializeField, Range(0f, 1f)] private float flashStrength = 0.65f;
    [SerializeField, Range(0f, 2f)] private float scaleStrength = 1f;

    [Header("Fixed placement")]
    [SerializeField] private Vector2 viewportPosition = new Vector2(0.68f, 0.60f);
    [Tooltip("Largest sprite dimension as a fraction of the shorter screen dimension.")]
    [SerializeField, Range(0.05f, 1f)] private float sizeFraction = 0.48f;
    [SerializeField] private float worldZ = 5f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = -58;

    [Header("Appearance")]
    [SerializeField, Range(0f, 1f)] private float brightness = 0.65f;
    [SerializeField, Range(0f, 1f)] private float opacity = 1f;

    [Header("Very slow rotation")]
    [Tooltip("Degrees per second; negative reverses direction.")]
    [SerializeField, Range(-0.5f, 0.5f)] private float rotationDegreesPerSecond = 0.08f;

    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int FlashColorId = Shader.PropertyToID("_ThemeFlashColor");
    private static readonly int FlashAmountId = Shader.PropertyToID("_ThemeFlashAmount");
    private static readonly int SweepId = Shader.PropertyToID("_ThemeSweep");
    private static readonly int BoundsId = Shader.PropertyToID("_SurfaceBounds");
    private Material ownedMaterial;
    private float visualScale = 1f, visualFlash, visualSweep = -1f;
    private float startScale = 1f, startFlash;
    private bool specialTransition;
    private readonly SpriteRenderer[] layers = new SpriteRenderer[3];
    private readonly float[] weights = new float[3];
    private readonly float[] startWeights = new float[3];
    private readonly Sprite[] layerSprites = new Sprite[3];
    private MaterialPropertyBlock properties;
    private struct LayerShaderState
    {
        public bool valid;
        public Color tint, flashColor;
        public Vector4 bounds;
        public float flash, sweep;
    }
    private readonly LayerShaderState[] layerShaderStates = new LayerShaderState[3];
    private float rotationAngle, transitionElapsed;
    private int targetIndex = -1;
    private int targetLayer;
    private bool selectionQueued, lastUseColoredSkinMoons;
    private bool initialized, transitioning, lastFollowEquippedSkin;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || !targetCamera.orthographic || earthSprite == null)
        {
            Debug.LogWarning("HomePlanetVisual: assign Earth Sprite and an orthographic Main Camera.", this);
            enabled = false;
            return;
        }
        if (planetMaterial == null)
            planetMaterial = Resources.Load<Material>("BackgroundPlanets/BackgroundPlanet");
        if (planetMaterial == null)
        {
            Debug.LogWarning("HomePlanetVisual: background planet material is missing.", this);
            enabled = false;
            return;
        }
        Shader themeShader = Resources.Load<Shader>("BackgroundPlanets/HomePlanetTheme");
        if (themeShader != null)
        {
            ownedMaterial = new Material(themeShader);
            ownedMaterial.CopyPropertiesFromMaterial(planetMaterial);
        }
        properties = new MaterialPropertyBlock();
        for (int i = 0; i < layers.Length; i++)
        {
            GameObject child = new GameObject("HomePlanetLayer" + i);
            child.transform.SetParent(transform, false);
            int backgroundLayer = LayerMask.NameToLayer("Background");
            child.layer = backgroundLayer >= 0 ? backgroundLayer : gameObject.layer;
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = ownedMaterial != null ? ownedMaterial : planetMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            layers[i] = renderer;
        }
        initialized = true;
        lastFollowEquippedSkin = followEquippedSkin;
        lastUseColoredSkinMoons = useColoredSkinMoons;
        RefreshSelection(true); // Open the menu with the equipped theme; no Earth flash.
        ApplyPlacement();
    }

    private void OnEnable()
    {
        PlayerSkinCatalog.SelectedSkinChanged -= HandleSkinChanged;
        PlayerSkinCatalog.SelectedSkinChanged += HandleSkinChanged;
        if (initialized)
        {
            RefreshSelection(false);
            ApplyPlacement();
        }
    }

    private void OnDisable()
    {
        PlayerSkinCatalog.SelectedSkinChanged -= HandleSkinChanged;
        for (int i = 0; i < layers.Length; i++)
            if (layers[i] != null) layers[i].enabled = false;
    }

    private void OnDestroy()
    {
        if (ownedMaterial != null) Destroy(ownedMaterial);
    }

    private void HandleSkinChanged()
    {
        if (initialized) RefreshSelection(false);
    }

    private Sprite SpriteFor(int index)
    {
        if (index == 1) return darkPlanetSprite;
        if (index == 2) return goldenPlanetSprite;
        switch (index)
        {
            case 3: return blueMoonSprite;
            case 4: return orangeMoonSprite;
            case 5: return redMoonSprite;
            case 6: return greenMoonSprite;
            case 7: return pinkMoonSprite;
            case 8: return yellowMoonSprite;
            case 9: return cyanMoonSprite;
            case 10: return purpleMoonSprite;
        }
        return earthSprite;
    }

    private int SelectedThemeIndex()
    {
        if (!followEquippedSkin) return 0;
        string id = PlayerPrefs.GetString(PlayerSkinCatalog.SelectedSkinKey, string.Empty)
            .Trim().ToLowerInvariant().Replace("_", string.Empty)
            .Replace("-", string.Empty).Replace(" ", string.Empty);
        if ((id == "dark" || id == "black") && darkPlanetSprite != null) return 1;
        if ((id == "golden" || id == "gold") && goldenPlanetSprite != null) return 2;
        if (useColoredSkinMoons)
        {
            int index;
            switch (id)
            {
                case "blue": index = 3; break;
                case "orange": index = 4; break;
                case "red": index = 5; break;
                case "green": index = 6; break;
                case "pink": case "deeppink": case "hotpink": index = 7; break;
                case "yellow": index = 8; break;
                case "cyan": case "lightblue": index = 9; break;
                case "purple": index = 10; break;
                default: index = 0; break;
            }
            if (SpriteFor(index) != null) return index;
        }
        return 0; // White/basic and missing assets keep the existing base sprite.
    }

    private void RefreshSelection(bool immediate)
    {
        int selected = SelectedThemeIndex();
        selectionQueued = false;
        if (!immediate && selected == targetIndex) return;
        Sprite selectedSprite = SpriteFor(selected);
        int slot = -1;
        for (int i = 0; i < layerSprites.Length; i++)
        {
            if (layerSprites[i] == selectedSprite) { slot = i; break; }
        }
        if (slot < 0)
        {
            for (int i = 0; i < weights.Length; i++)
                if (immediate || weights[i] <= 0f) { slot = i; break; }
        }
        if (slot < 0)
        {
            // Very rapid equips can keep all three layers visible. Finish the
            // current blend, then resolve only the latest selection; never
            // allocate more renderers or snap away a visible moon.
            selectionQueued = true;
            return;
        }
        layerSprites[slot] = selectedSprite;
        targetLayer = slot;
        startScale = visualScale;
        startFlash = visualFlash;
        specialTransition = !immediate && dramaticTransitions &&
            (selected != 0 || useColoredSkinMoons || targetIndex == 1 || targetIndex == 2);
        if (immediate)
        {
            visualScale = 1f;
            visualFlash = 0f;
            visualSweep = -1f;
        }
        targetIndex = selected;
        transitionElapsed = 0f;
        transitioning = !immediate;
        for (int i = 0; i < weights.Length; i++)
        {
            // A new equip during the fade starts from exactly the currently visible mix.
            startWeights[i] = weights[i];
            if (immediate) weights[i] = i == targetLayer ? 1f : 0f;
        }
    }

    private void LateUpdate()
    {
        if (!initialized || targetCamera == null) return;
        if (lastFollowEquippedSkin != followEquippedSkin ||
            lastUseColoredSkinMoons != useColoredSkinMoons)
        {
            lastFollowEquippedSkin = followEquippedSkin;
            lastUseColoredSkinMoons = useColoredSkinMoons;
            RefreshSelection(false);
        }
        float dt = Time.unscaledDeltaTime;
        rotationAngle = Mathf.Repeat(rotationAngle + rotationDegreesPerSecond * dt, 360f);
        if (transitioning)
        {
            transitionElapsed += dt;
            float duration = specialTransition ? goldenTransitionDuration : transitionDuration;
            float t = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.1f, duration));
            float blend = specialTransition
                ? Mathf.InverseLerp(0.12f, 0.72f, t) : t;
            float smooth = Smooth(blend);
            float settle = Smooth(Mathf.Clamp01(t / 0.20f));
            float pulse = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(0.18f, 0.72f, t));
            pulse = Mathf.Max(0f, pulse);
            // Both special themes use the approved Divine motion; only their light color differs.
            float scaleOffset = specialTransition ? 0.035f * Mathf.Sin(Mathf.PI * t) : 0f;
            visualScale = Mathf.Lerp(startScale, 1f + scaleOffset * scaleStrength, settle);
            visualFlash = Mathf.Lerp(startFlash, specialTransition ? pulse * flashStrength : 0f, settle);
            visualSweep = specialTransition ? Mathf.Lerp(-0.25f, 1.25f, t) : -1f;
            for (int i = 0; i < weights.Length; i++)
                weights[i] = Mathf.Lerp(startWeights[i], i == targetLayer ? 1f : 0f, smooth);
            if (t >= 1f)
            {
                transitioning = false;
                visualScale = 1f;
                visualFlash = 0f;
                visualSweep = -1f;
            }
        }
        if (!transitioning && selectionQueued) RefreshSelection(false);
        ApplyPlacement();
    }

    private Color TransitionColor()
    {
        switch (targetIndex)
        {
            case 1: return new Color(0.72f, 0.045f, 0.10f, 1f);
            case 2: return new Color(1f, 0.85f, 0.40f, 1f);
            case 3: return new Color32(40, 170, 232, 255);
            case 4: return new Color32(255, 166, 6, 255);
            case 5: return new Color32(245, 30, 34, 255);
            case 6: return new Color32(85, 255, 100, 255);
            case 7: return new Color32(255, 20, 147, 255);
            case 8: return new Color32(254, 236, 7, 255);
            case 9: return new Color32(49, 233, 241, 255);
            case 10: return new Color32(170, 81, 209, 255);
            default: return Color.white;
        }
    }

    private static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private void ApplyPlacement()
    {
        float depth = Vector3.Dot(new Vector3(0f, 0f, worldZ) - targetCamera.transform.position,
            targetCamera.transform.forward);
        bool visibleDepth = depth > targetCamera.nearClipPlane && depth < targetCamera.farClipPlane;
        float height = targetCamera.orthographicSize * 2f;
        float targetSize = Mathf.Min(height, height * targetCamera.aspect) * sizeFraction;
        Vector3 position = targetCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, depth));
        Vector3 parentScale = transform.lossyScale;
        for (int i = 0; i < layers.Length; i++)
        {
            SpriteRenderer renderer = layers[i];
            Sprite sprite = layerSprites[i];
            renderer.enabled = visibleDepth && sprite != null && weights[i] > 0f;
            if (!renderer.enabled) continue;
            if (renderer.sprite != sprite) renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder; // Same background depth; creation order stays stable.
            renderer.transform.position = position;
            float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            float scale = targetSize * visualScale / Mathf.Max(0.001f, spriteSize);
            renderer.transform.localScale = new Vector3(scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);
            Color tint = new Color(brightness, brightness, brightness, opacity * weights[i]);
            Color flashColor = TransitionColor();
            Bounds spriteBounds = sprite.bounds;
            Vector4 bounds = new Vector4(spriteBounds.center.x, spriteBounds.center.y,
                spriteBounds.size.x, spriteBounds.size.y);
            LayerShaderState state = layerShaderStates[i];
            if (!state.valid || !state.tint.Equals(tint) || !state.flashColor.Equals(flashColor) ||
                !state.bounds.Equals(bounds) || state.flash != visualFlash || state.sweep != visualSweep)
            {
                properties.SetColor(TintId, tint);
                properties.SetColor(FlashColorId, flashColor);
                properties.SetFloat(FlashAmountId, visualFlash);
                properties.SetFloat(SweepId, visualSweep);
                properties.SetVector(BoundsId, bounds);
                renderer.SetPropertyBlock(properties);
                layerShaderStates[i] = new LayerShaderState { valid = true, tint = tint,
                    flashColor = flashColor, bounds = bounds, flash = visualFlash, sweep = visualSweep };
            }
        }
    }
}
