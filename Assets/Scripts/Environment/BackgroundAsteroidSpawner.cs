using System.Collections.Generic;
using UnityEngine;

/// <summary>Cosmetic background only: zero to two slowly spinning rocks and one pooled drifting rock.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(260)]
public sealed class BackgroundAsteroidSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Sprite[] asteroidSprites = new Sprite[5];
    [SerializeField] private Material asteroidMaterial;

    [Header("Size: fraction of shorter screen dimension")]
    [SerializeField] private Vector2 fixedSizeRange = new Vector2(0.07f, 0.16f);
    [SerializeField] private Vector2 movingSizeRange = new Vector2(0.06f, 0.18f);

    [Header("Random counts per match")]
    [SerializeField] private Vector2Int fixedCountRange = new Vector2Int(0, 2);
    [SerializeField] private Vector2Int movingPassageCountRange = new Vector2Int(1, 3);
    [Tooltip("Slow rotation without changing the stationary asteroid position.")]
    [SerializeField] private Vector2 fixedSpinSpeedRange = new Vector2(0.4f, 1.4f);

    [Header("Fixed asteroid placement")]
    [SerializeField] private Vector2 viewportXRange = new Vector2(0.10f, 0.90f);
    [SerializeField] private Vector2 viewportYRange = new Vector2(0.15f, 0.85f);
    [SerializeField, Range(0f, 0.4f)] private float centerExclusionRadius = 0.22f;

    [Header("Natural drifting passages")]
    [Tooltip("Seconds for one complete edge-to-edge passage. Speed stays constant.")]
    [SerializeField] private Vector2 passageDurationRange = new Vector2(14f, 22f);
    [Tooltip("Seconds AFTER the previous rock fully exits; never overlapping.")]
    [SerializeField] private Vector2 gapBetweenPassages = new Vector2(8f, 14f);
    [Tooltip("Slow spin in degrees/second; random direction for each passage.")]
    [SerializeField] private Vector2 spinSpeedRange = new Vector2(2f, 7f);
    [Tooltip("Makes drifting asteroid rotation visible without changing saved spin ranges. 4 = four times the Spin Speed Range; 0 disables continuous spin.")]
    [SerializeField, Min(0f)] private float movingSpinMultiplier = 4f;

    [Tooltip("Multiplies passage speed, on top of the duration RNG. Each passage differs from the previous.")]
    [SerializeField] private Vector2 travelSpeedMultiplierRange = new Vector2(0.65f, 1.55f);

    [Header("MainMenu ambient passages (automatic in MainMenu scene)")]
    [Tooltip("Seconds between passage STARTS. Also used for the initial menu delay. Only one moving asteroid is active at a time.")]
    [SerializeField] private Vector2 menuPassageIntervalRange = new Vector2(40f, 60f);
    [SerializeField] private Vector2 menuPassageDurationRange = new Vector2(18f, 30f);
    [SerializeField] private Vector2 menuMovingSizeRange = new Vector2(0.05f, 0.10f);
    [SerializeField, Range(0f, 1f)] private float menuShakeStrength = 0.35f;
    [Tooltip("Menu asteroids stay in front of the default Earth/Moon (-58). Saved values below -57 are automatically raised to -57.")]
    [SerializeField] private int menuSortingOrder = -57;

    [Header("MainMenu appearance (independent of gameplay)")]
    [Tooltip("Menu brightness before dimming. Gameplay Appearance values are ignored in MainMenu.")]
    [SerializeField, Range(0f, 1f)] private float menuBrightness = 0.8f;
    [SerializeField, Range(0f, 1f)] private float menuOpacity = 1f;
    [Tooltip("Brightness multiplier: 1 = no dimming, 0 = black.")]
    [SerializeField, Range(0f, 1f)] private float menuDimming = 0.9f;
    [Tooltip("0 = original sprite colors, 1 = grayscale.")]
    [SerializeField, Range(0f, 1f)] private float menuDesaturation = 0.15f;
    [Tooltip("1 = original contrast; lower values soften surface detail.")]
    [SerializeField, Range(0f, 1f)] private float menuContrast = 0.9f;
    [SerializeField, Range(0f, 1f)] private float menuOpacityMultiplier = 1f;
    [Tooltip("Response to the existing skin-aware sunlight.")]
    [SerializeField, Range(0f, 1f)] private float menuSolarResponse = 0.65f;

    [Header("Speed-dependent drifting shake")]
    [SerializeField] private bool movingShake = true;
    [Tooltip("Speed in shorter-screen-dimensions per second, independent of camera zoom.")]
    [SerializeField] private Vector2 shakeSpeedReferenceRange = new Vector2(0.04f, 0.15f);
    [Tooltip("Bounded position amplitude, as a fraction of the shorter screen dimension.")]
    [SerializeField] private Vector2 shakeAmplitudeRange = new Vector2(0.0006f, 0.0035f);
    [SerializeField] private Vector2 shakeAngleRange = new Vector2(0.35f, 1.8f);
    [SerializeField] private Vector2 shakeFrequencyRange = new Vector2(0.6f, 2.2f);

    [Header("Gameplay background separation")]
    [SerializeField] private bool softenBackground = true;
    [SerializeField, Range(0f, 1f)] private float backgroundDimming = 0.55f;
    [SerializeField, Range(0f, 1f)] private float backgroundDesaturation = 0.75f;
    [SerializeField, Range(0f, 1f)] private float backgroundContrast = 0.5f;
    [SerializeField, Range(0f, 1f)] private float backgroundOpacityMultiplier = 0.82f;
    [SerializeField, Range(0f, 1f)] private float backgroundSolarResponse = 0.2f;

    [Header("Appearance")]
    [SerializeField, Range(0f, 1f)] private float brightness = 0.45f;
    [SerializeField, Range(0f, 1f)] private float opacity = 1f;
    [SerializeField] private string sortingLayerName = "Default";
    [Tooltip("Planet=-58; asteroid=-56; MidStars=-55. All stay behind gameplay.")]
    [SerializeField] private int sortingOrder = -56;
    [SerializeField] private float worldZ = 5f;

    private readonly List<Sprite> validSprites = new List<Sprite>(5);
    private readonly SpriteRenderer[] fixedRocks = new SpriteRenderer[2];
    private readonly Vector2[] fixedAnchors = new Vector2[2];
    private readonly float[] fixedSizes = new float[2], fixedAngles = new float[2], fixedSpins = new float[2];
    private readonly int[] fixedIndices = new int[2];
    private SpriteRenderer movingRock;
    private int fixedCount, plannedPassages, launchedPassages;
    private float matchClock, lastTravelSpeed = -1f;
    private BackgroundSpritePropertyCache propertyCache;
    private Vector2 startViewport, endViewport;
    private float movingSize, movingAngle, spin;
    private float elapsed, duration, waitRemaining;
    private float normalizedTravelSpeed, shakeSeed;
    private bool passageActive, lastStarted, hasStartedMatch;
    private bool menuMode;
    private int lastMovingIndex = -1;

    private void Start()
    {
        menuMode = gameObject.scene.name == "MainMenu";
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || !targetCamera.orthographic)
        {
            Debug.LogWarning("BackgroundAsteroidSpawner needs an orthographic Main Camera.", this);
            enabled = false;
            return;
        }
        if (asteroidSprites != null)
            foreach (Sprite sprite in asteroidSprites)
                if (sprite != null && !validSprites.Contains(sprite)) validSprites.Add(sprite);
        if (validSprites.Count == 0)
        {
            Debug.LogWarning("BackgroundAsteroidSpawner: assign asteroid sprites first.", this);
            enabled = false;
            return;
        }
        if (asteroidMaterial == null)
            asteroidMaterial = Resources.Load<Material>("BackgroundAsteroids/BackgroundAsteroid");
        if (asteroidMaterial == null)
        {
            Debug.LogWarning("BackgroundAsteroidSpawner: bundled material is missing.", this);
            enabled = false;
            return;
        }
        propertyCache = new BackgroundSpritePropertyCache();
        if (!menuMode)
            for (int i = 0; i < fixedRocks.Length; i++)
                fixedRocks[i] = CreateRock("FixedBackgroundAsteroid" + (i + 1));
        movingRock = CreateRock("DriftingBackgroundAsteroid");
        PrepareMatch();
        if (menuMode)
        {
            waitRemaining = Sample(menuPassageIntervalRange, 5f, 300f);
            return;
        }
        lastStarted = GameStateManager.IsGameplayStarted;
        hasStartedMatch = lastStarted;
        if (lastStarted && !GameStateManager.IsGameplayEnded) BeginPassage();
    }

    private SpriteRenderer CreateRock(string objectName)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        int layer = LayerMask.NameToLayer("Background");
        child.layer = layer >= 0 ? layer : gameObject.layer;
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = asteroidMaterial;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = menuMode ? Mathf.Max(-57, menuSortingOrder) : sortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }

    private void PrepareMatch()
    {
        fixedCount = menuMode ? 0 : SampleCount(fixedCountRange, 0, 2);
        plannedPassages = SampleCount(movingPassageCountRange, 1, 10);
        launchedPassages = 0;
        matchClock = 0f;
        lastTravelSpeed = -1f;
        for (int i = 0; i < fixedRocks.Length; i++)
        {
            if (fixedRocks[i] == null) continue;
            fixedRocks[i].enabled = false;
            if (i >= fixedCount) continue;
            int index = Random.Range(0, validSprites.Count);
            if (i > 0 && validSprites.Count > 1 && index == fixedIndices[0])
                index = (index + Random.Range(1, validSprites.Count)) % validSprites.Count;
            fixedIndices[i] = index;
            fixedRocks[i].sprite = validSprites[index];
            fixedSizes[i] = Sample(fixedSizeRange, 0.02f, 0.5f);
            fixedAngles[i] = Random.Range(0f, 360f);
            fixedSpins[i] = Sample(fixedSpinSpeedRange, 0.1f, 5f) * (Random.value < 0.5f ? -1f : 1f);
            fixedAnchors[i] = i == 0 ? new Vector2(0.15f, 0.75f) :
                new Vector2(fixedAnchors[0].x < 0.5f ? 0.85f : 0.15f, fixedAnchors[0].y < 0.5f ? 0.85f : 0.15f);
            for (int attempt = 0; attempt < 32; attempt++)
            {
                Vector2 candidate = new Vector2(Sample(viewportXRange, 0f, 1f), Sample(viewportYRange, 0f, 1f));
                if ((candidate - Vector2.one * 0.5f).sqrMagnitude < centerExclusionRadius * centerExclusionRadius) continue;
                if (i > 0 && Vector2.Distance(candidate, fixedAnchors[0]) < 0.3f) continue;
                fixedAnchors[i] = candidate;
                break;
            }
        }
        passageActive = false;
        movingRock.enabled = false;
        lastMovingIndex = -1;
        waitRemaining = 0f;
        ApplyPlacement();
    }

    private void BeginPassage()
    {
        int count = 0;
        for (int i = 0; i < validSprites.Count; i++)
            if (!IsFixedSprite(i) && i != lastMovingIndex) count++;
        // With only one/two assigned sprites, allow a repeat rather than omit a passage.
        bool allowRepeat = count == 0;
        if (allowRepeat)
            for (int i = 0; i < validSprites.Count; i++) if (!IsFixedSprite(i)) count++;
        int selected = 0;
        if (count > 0)
        {
            int choice = Random.Range(0, count);
            for (int i = 0; i < validSprites.Count; i++)
                if (!IsFixedSprite(i) && (allowRepeat || i != lastMovingIndex) && choice-- == 0)
                { selected = i; break; }
        }
        else selected = Random.Range(0, validSprites.Count);
        launchedPassages++;
        lastMovingIndex = selected;
        movingRock.sprite = validSprites[selected];
        movingSize = Sample(menuMode ? menuMovingSizeRange : movingSizeRange, 0.02f, 0.5f);
        float baseDuration = Sample(menuMode ? menuPassageDurationRange : passageDurationRange, 4f, 120f);
        movingAngle = Random.Range(0f, 360f);
        spin = Sample(spinSpeedRange, 0f, 30f)
            * Mathf.Clamp(movingSpinMultiplier, 0f, 8f)
            * (Random.value < 0.5f ? -1f : 1f);
        elapsed = 0f;
        // Margin covers the sprite's full diagonal, including its rotation.
        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;
        float radius = movingSize * Mathf.Min(width, height) * 0.72f;
        float marginX = radius / width + 0.015f;
        float marginY = radius / height + 0.015f;
        float entry = Random.Range(0.12f, 0.88f);
        float exit = Random.Range(0.12f, 0.88f);
        switch (Random.Range(0, 4))
        {
            case 0: startViewport = new Vector2(-marginX, entry); endViewport = new Vector2(1f + marginX, exit); break;
            case 1: startViewport = new Vector2(1f + marginX, entry); endViewport = new Vector2(-marginX, exit); break;
            case 2: startViewport = new Vector2(entry, -marginY); endViewport = new Vector2(exit, 1f + marginY); break;
            default: startViewport = new Vector2(entry, 1f + marginY); endViewport = new Vector2(exit, -marginY); break;
        }
        Vector2 screenDelta = endViewport - startViewport;
        float pathLength = new Vector2(screenDelta.x * width, screenDelta.y * height).magnitude;
        float multiplier = menuMode ? Random.Range(0.85f, 1.15f) : Sample(travelSpeedMultiplierRange, 0.25f, 3f);
        float travelSpeed = pathLength / baseDuration * multiplier;
        // Ensure consecutive passages do not have almost identical speeds.
        if (lastTravelSpeed > 0f && Mathf.Abs(travelSpeed / lastTravelSpeed - 1f) < 0.20f)
            travelSpeed = lastTravelSpeed * (travelSpeed >= lastTravelSpeed ? 1.25f : 0.75f);
        duration = pathLength / Mathf.Max(0.001f, travelSpeed);
        lastTravelSpeed = travelSpeed;
        normalizedTravelSpeed = travelSpeed / Mathf.Max(0.001f, Mathf.Min(width, height));
        shakeSeed = Random.Range(0f, 1000f);
        passageActive = true;
    }

    private void LateUpdate()
    {
        if (movingRock == null || targetCamera == null) return;
        if (menuMode)
        {
            AdvancePassages(Time.unscaledDeltaTime, true);
            ApplyPlacement();
            return;
        }
        bool started = GameStateManager.IsGameplayStarted;
        if (started && !lastStarted)
        {
            if (hasStartedMatch) PrepareMatch();
            hasStartedMatch = true;
            BeginPassage(); // No RNG chance of skipping the first passage.
        }
        lastStarted = started;
        if (started && !GameStateManager.IsGameplayEnded && Time.timeScale > 0f)
            AdvancePassages(Time.deltaTime, false);
        ApplyPlacement();
    }

    private void AdvancePassages(float deltaTime, bool repeatForever)
    {
        matchClock += deltaTime;
        if (passageActive)
        {
            elapsed += deltaTime;
            if (elapsed >= duration)
            {
                passageActive = false;
                // Menu intervals are start-to-start, rather than adding a full
                // interval after the previous passage duration. Never overlap.
                waitRemaining = menuMode
                    ? Mathf.Max(0f, Sample(menuPassageIntervalRange, 5f, 300f) - elapsed)
                    : Sample(gapBetweenPassages, 3f, 120f);
            }
        }
        else if (repeatForever || launchedPassages < plannedPassages)
        {
            waitRemaining -= deltaTime;
            if (waitRemaining <= 0f) BeginPassage();
        }
    }

    private void ApplyPlacement()
    {
        for (int i = 0; i < fixedRocks.Length; i++)
        {
            if (fixedRocks[i] == null) continue;
            if (i < fixedCount)
                Place(fixedRocks[i], fixedAnchors[i], fixedSizes[i], fixedAngles[i] + matchClock * fixedSpins[i]);
            else fixedRocks[i].enabled = false;
        }
        if (passageActive)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
            Vector2 position = Vector2.Lerp(startViewport, endViewport, t);
            float angle = movingAngle + elapsed * spin;
            if (movingShake)
            {
                float speed = Mathf.InverseLerp(Mathf.Min(shakeSpeedReferenceRange.x, shakeSpeedReferenceRange.y),
                    Mathf.Max(shakeSpeedReferenceRange.x, shakeSpeedReferenceRange.y), normalizedTravelSpeed);
                float shakeStrength = menuMode ? menuShakeStrength : 1f;
                float amplitude = OrderedLerp(shakeAmplitudeRange, speed) * shakeStrength;
                float frequency = OrderedLerp(shakeFrequencyRange, speed);
                float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(t / 0.1f, (1f - t) / 0.1f));
                float noiseX = Mathf.PerlinNoise(shakeSeed, elapsed * frequency) * 2f - 1f;
                float noiseY = Mathf.PerlinNoise(shakeSeed + 37f, elapsed * frequency * 0.73f) * 2f - 1f;
                float height = targetCamera.orthographicSize * 2f;
                float width = height * targetCamera.aspect;
                float shortSide = Mathf.Min(width, height);
                Vector2 path = endViewport - startViewport;
                Vector2 tangent = new Vector2(path.x * width, path.y * height).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                Vector2 worldOffset = (normal * noiseX + tangent * noiseY * 0.35f) * amplitude * shortSide * envelope;
                position += new Vector2(worldOffset.x / width, worldOffset.y / height);
                float angularNoise = Mathf.PerlinNoise(shakeSeed + 83f, elapsed * frequency * 0.87f) * 2f - 1f;
                angle += angularNoise * OrderedLerp(shakeAngleRange, speed) * envelope * shakeStrength;
            }
            Place(movingRock, position, movingSize, angle);
        }
        else movingRock.enabled = false;
    }

    private void Place(SpriteRenderer rock, Vector2 viewport, float size, float angle)
    {
        float depth = Vector3.Dot(new Vector3(0f, 0f, worldZ) - targetCamera.transform.position,
            targetCamera.transform.forward);
        rock.enabled = depth > targetCamera.nearClipPlane && depth < targetCamera.farClipPlane;
        if (!rock.enabled) return;
        Bounds bounds = rock.sprite.bounds;
        float light = menuMode ? menuBrightness * menuDimming
            : brightness * (softenBackground ? backgroundDimming : 1f);
        float alpha = menuMode ? menuOpacity * menuOpacityMultiplier
            : opacity * (softenBackground ? backgroundOpacityMultiplier : 1f);
        Vector4 style = menuMode
            ? new Vector4(menuDesaturation, menuContrast, menuSolarResponse, 0f)
            : (softenBackground ? new Vector4(backgroundDesaturation, backgroundContrast, backgroundSolarResponse, 0f)
                : new Vector4(0f, 1f, 1f, 0f));
        propertyCache.Apply(rock, new Color(light, light, light, alpha), style, bounds);
        rock.transform.position = targetCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
        float height = targetCamera.orthographicSize * 2f;
        float spriteSize = Mathf.Max(rock.sprite.bounds.size.x, rock.sprite.bounds.size.y);
        float scale = Mathf.Min(height, height * targetCamera.aspect) * size / Mathf.Max(0.001f, spriteSize);
        Vector3 parentScale = transform.lossyScale;
        rock.transform.localScale = new Vector3(scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
            scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
        rock.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static float OrderedLerp(Vector2 range, float t)
    {
        return Mathf.Lerp(Mathf.Max(0f, Mathf.Min(range.x, range.y)), Mathf.Max(0f, Mathf.Max(range.x, range.y)), t);
    }

    private bool IsFixedSprite(int index)
    {
        for (int i = 0; i < fixedCount; i++) if (fixedIndices[i] == index) return true;
        return false;
    }

    private static int SampleCount(Vector2Int range, int minimum, int maximum)
    {
        int low = Mathf.Clamp(Mathf.Min(range.x, range.y), minimum, maximum);
        int high = Mathf.Clamp(Mathf.Max(range.x, range.y), low, maximum);
        return Random.Range(low, high + 1);
    }

    private static float Sample(Vector2 range, float minimum, float maximum)
    {
        float low = Mathf.Clamp(Mathf.Min(range.x, range.y), minimum, maximum);
        float high = Mathf.Clamp(Mathf.Max(range.x, range.y), low, maximum);
        return Random.Range(low, high);
    }
}
