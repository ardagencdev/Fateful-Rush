using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fateful Rush: one or two cosmetic planets per match, with random sprite/size/position.
/// Add to an empty GameScene object and assign planet sprites imported as Sprite (2D and UI).
/// A planet appears during briefing; the first gameplay start keeps that selection.
/// Scene reload or a subsequent gameplay start on the same scene creates a new selection.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(250)]
public sealed class BackgroundPlanetSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Sprite[] planetSprites = new Sprite[10];
    [Tooltip("Optional override; otherwise loads the bundled unlit material automatically.")]
    [SerializeField] private Material planetMaterial;

    [Header("Random size")]
    [Tooltip("Largest sprite dimension as fraction of the SHORTER screen dimension.")]
    [SerializeField] private Vector2 sizeRange = new Vector2(0.28f, 0.65f);

    [Tooltip("Chance of a planet larger than the normal Size Range. Existing size settings are preserved.")]
    [SerializeField, Range(0f, 1f)] private float largePlanetChance = 0.25f;
    [SerializeField] private Vector2 largePlanetSizeRange = new Vector2(0.85f, 1.05f);

    [Header("Gameplay background separation")]
    [SerializeField] private bool softenBackground = true;
    [SerializeField, Range(0f, 1f)] private float backgroundDimming = 0.65f;
    [SerializeField, Range(0f, 1f)] private float backgroundDesaturation = 0.65f;
    [SerializeField, Range(0f, 1f)] private float backgroundContrast = 0.55f;
    [SerializeField, Range(0f, 1f)] private float backgroundSolarResponse = 0.25f;

    [Header("Second planet: distant appearance")]
    [SerializeField, Range(0f, 1f)] private float secondPlanetChance = 0.5f;
    [Tooltip("Distant planet size relative to the main planet; capped below 0.65.")]
    [SerializeField] private Vector2 distantSizeRatioRange = new Vector2(0.35f, 0.55f);
    [SerializeField, Range(0.1f, 1f)] private float distantBrightnessMultiplier = 0.65f;
    [SerializeField, Range(0f, 1f)] private float distantDriftMultiplier = 0.35f;

    [Header("Random position (viewport: 0 to 1)")]
    [SerializeField] private Vector2 viewportXRange = new Vector2(0.08f, 0.92f);
    [SerializeField] private Vector2 viewportYRange = new Vector2(0.12f, 0.88f);
    [Tooltip("Keep planets away from the central Player spawn area.")]
    [SerializeField, Range(0f, 0.4f)] private float centerExclusionRadius = 0.18f;

    [Header("Appearance")]
    [Tooltip("0.45 darkens the planet without making the shadow hemisphere transparent.")]
    [SerializeField, Range(0f, 1f)] private float brightness = 0.45f;
    [SerializeField, Range(0f, 1f)] private float opacity = 1f;
    [SerializeField] private string sortingLayerName = "Default";
    [Tooltip("Repo: FarStars=-60, MidStars=-55, NearStars=-50. Planet=-58.")]
    [SerializeField] private int sortingOrder = -58;
    [SerializeField] private float worldZ = 5f;
    [SerializeField] private bool avoidConsecutivePlanet = true;

    [Header("Very slow bounded drift")]
    [SerializeField] private bool enableDrift = true;
    [Tooltip("Fraction of screen width/height. Never moves indefinitely off screen.")]
    [SerializeField, Range(0f, 0.05f)] private float driftAmount = 0.012f;
    [SerializeField, Min(0f)] private float driftSpeed = 0.08f;
    [SerializeField, Range(0f, 5f)] private float rotationSwayDegrees = 2f;

    private static Sprite lastSelectedPlanet;
    private readonly List<Sprite> validSprites = new List<Sprite>(10);
    private SpriteRenderer planet, distantPlanet;
    private bool hasDistantPlanet;
    private Vector2 distantAnchor;
    private float distantSize, distantPhase;
    private BackgroundSpritePropertyCache propertyCache;
    private Vector2 anchor;
    private float sizeFraction;
    private float clock;
    private float phase;
    private bool lastGameplayStarted;
    private bool hasStartedMatch;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || !targetCamera.orthographic)
        {
            Debug.LogWarning("BackgroundPlanetSpawner needs the game's orthographic Main Camera.", this);
            enabled = false;
            return;
        }
        for (int i = 0; planetSprites != null && i < planetSprites.Length; i++)
            if (planetSprites[i] != null && !validSprites.Contains(planetSprites[i]))
                validSprites.Add(planetSprites[i]);
        if (validSprites.Count == 0)
        {
            Debug.LogWarning("BackgroundPlanetSpawner: assign at least one imported planet Sprite.", this);
            enabled = false;
            return;
        }
        if (planetMaterial == null)
            planetMaterial = Resources.Load<Material>("BackgroundPlanets/BackgroundPlanet");
        if (planetMaterial == null)
        {
            Debug.LogWarning("BackgroundPlanetSpawner: bundled BackgroundPlanet material is missing.", this);
            enabled = false;
            return;
        }
        planet = CreatePlanet("RandomBackgroundPlanet", sortingOrder);
        distantPlanet = CreatePlanet("DistantBackgroundPlanet", sortingOrder - 1);
        propertyCache = new BackgroundSpritePropertyCache();
        lastGameplayStarted = GameStateManager.IsGameplayStarted;
        hasStartedMatch = lastGameplayStarted;
        GenerateNewPlanet();
    }

    private SpriteRenderer CreatePlanet(string objectName, int order)
    {
        GameObject child = new GameObject(objectName);
        int backgroundLayer = LayerMask.NameToLayer("Background");
        child.layer = backgroundLayer >= 0 ? backgroundLayer : gameObject.layer;
        child.transform.SetParent(transform, false);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = planetMaterial;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = order;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }

    [ContextMenu("Generate New Planet (Play Mode)")]
    public void GenerateNewPlanet()
    {
        if (!Application.isPlaying || planet == null || validSprites.Count == 0) return;
        int excluded = avoidConsecutivePlanet && validSprites.Count > 1
            ? validSprites.IndexOf(lastSelectedPlanet) : -1;
        int index = Random.Range(0, validSprites.Count - (excluded >= 0 ? 1 : 0));
        if (excluded >= 0 && index >= excluded) index++;
        planet.sprite = validSprites[index];
        lastSelectedPlanet = planet.sprite;
        bool large = Random.value < largePlanetChance;
        sizeFraction = RandomInRange(large ? largePlanetSizeRange : sizeRange, 0.05f, 1.5f);
        for (int attempt = 0; attempt < 24; attempt++)
        {
            anchor = new Vector2(RandomInRange(viewportXRange, 0f, 1f),
                RandomInRange(viewportYRange, 0f, 1f));
            if ((anchor - Vector2.one * 0.5f).sqrMagnitude >= centerExclusionRadius * centerExclusionRadius)
                break;
        }
        hasDistantPlanet = Random.value < secondPlanetChance;
        if (hasDistantPlanet)
        {
            int distantIndex = index;
            if (validSprites.Count > 1)
            {
                distantIndex = Random.Range(0, validSprites.Count - 1);
                if (distantIndex >= index) distantIndex++;
            }
            distantPlanet.sprite = validSprites[distantIndex];
            distantSize = sizeFraction * RandomInRange(distantSizeRatioRange, 0.15f, 0.65f);
            distantPhase = Random.Range(0f, Mathf.PI * 2f);
            // Choose the farthest eligible candidate, accounting for screen aspect.
            distantAnchor = new Vector2(anchor.x < 0.5f ? 0.88f : 0.12f,
                anchor.y < 0.5f ? 0.85f : 0.15f);
            float bestDistance = -1f;
            for (int attempt = 0; attempt < 48; attempt++)
            {
                Vector2 candidate = new Vector2(RandomInRange(viewportXRange, 0f, 1f),
                    RandomInRange(viewportYRange, 0f, 1f));
                if ((candidate - Vector2.one * 0.5f).sqrMagnitude < centerExclusionRadius * centerExclusionRadius) continue;
                Vector2 delta = candidate - anchor;
                float distance = new Vector2(delta.x * targetCamera.aspect, delta.y).sqrMagnitude;
                if (distance > bestDistance) { bestDistance = distance; distantAnchor = candidate; }
            }
        }
        distantPlanet.enabled = false;
        clock = 0f;
        phase = Random.Range(0f, Mathf.PI * 2f);
        ApplyPlacement();
    }

    private void LateUpdate()
    {
        if (planet == null || targetCamera == null) return;
        bool started = GameStateManager.IsGameplayStarted;
        if (started && !lastGameplayStarted)
        {
            if (hasStartedMatch) GenerateNewPlanet();
            hasStartedMatch = true;
        }
        lastGameplayStarted = started;
        if (started && !GameStateManager.IsGameplayEnded && Time.timeScale > 0f)
            clock += Time.deltaTime;
        ApplyPlacement(); // Keeps placement correct across aspect and camera zoom changes.
    }

    private void ApplyPlacement()
    {
        if (planet == null || planet.sprite == null) return;
        PlacePlanet(planet, anchor, sizeFraction, phase, 1f, brightness);
        if (hasDistantPlanet)
            PlacePlanet(distantPlanet, distantAnchor, distantSize, distantPhase,
                distantDriftMultiplier, brightness * distantBrightnessMultiplier);
        else distantPlanet.enabled = false;
    }

    private void PlacePlanet(SpriteRenderer renderer, Vector2 position, float size, float seed,
        float driftScale, float light)
    {
        Vector2 offset = enableDrift ? new Vector2(
            Mathf.Sin(clock * driftSpeed + seed),
            Mathf.Sin(clock * driftSpeed * 0.71f + seed + 0.8f)) * driftAmount * driftScale : Vector2.zero;
        Vector2 viewport = position + offset;
        float depth = Vector3.Dot(new Vector3(0f, 0f, worldZ) - targetCamera.transform.position,
            targetCamera.transform.forward);
        renderer.enabled = depth > targetCamera.nearClipPlane && depth < targetCamera.farClipPlane;
        if (!renderer.enabled) return;
        float effectiveLight = light * (softenBackground ? backgroundDimming : 1f);
        propertyCache.Apply(renderer, new Color(effectiveLight, effectiveLight, effectiveLight, opacity),
            softenBackground ? new Vector4(backgroundDesaturation, backgroundContrast, backgroundSolarResponse, 0f)
                : new Vector4(0f, 1f, 1f, 0f), renderer.sprite.bounds);
        renderer.transform.position = targetCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
        float screenHeight = targetCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * targetCamera.aspect;
        float spriteSize = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
        float scale = Mathf.Min(screenWidth, screenHeight) * size / Mathf.Max(0.001f, spriteSize);
        Vector3 parentScale = transform.lossyScale;
        renderer.transform.localScale = new Vector3(scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
            scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
        float sway = enableDrift ? Mathf.Sin(clock * driftSpeed * 0.5f + seed) * rotationSwayDegrees * driftScale : 0f;
        renderer.transform.rotation = Quaternion.Euler(0f, 0f, sway);
    }

    private static float RandomInRange(Vector2 range, float minimum, float maximum)
    {
        float low = Mathf.Clamp(Mathf.Min(range.x, range.y), minimum, maximum);
        float high = Mathf.Clamp(Mathf.Max(range.x, range.y), low, maximum);
        return Random.Range(low, high);
    }
}
