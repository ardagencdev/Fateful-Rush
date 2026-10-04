using UnityEngine;

/// <summary>
/// Fateful Rush Android, source commit de128a0.
/// Add once to the real Player alongside PlayerCoinCollector.
/// Reads existing gameplay state; never writes to movement, armor, dash or skin.
/// The bundled Resources material is loaded automatically.
/// </summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class PlayerComboElectricFX : MonoBehaviour
{
    [Header("References (automatically resolved on Player)")]
    [SerializeField] private PlayerCoinCollector coinCollector;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private PlayerArmor playerArmor;
    [SerializeField] private PlayerSkinApplier skinApplier;
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private SpriteRenderer armorSprite;
    [SerializeField] private Material electricMaterial;

    [Header("Color")]
    [Tooltip("Uses the skin's armor color; during dash uses its dash trail color.")]
    [SerializeField] private bool followSkinColor = true;
    [SerializeField] private Color electricColor = new Color(1f, 0.65f, 0.2f, 0.8f);
    [SerializeField, Range(0f, 0.6f)] private float whiteHighlight = 0.22f;

    [Header("Size (relative to visible player radius)")]
    [SerializeField, Min(0.01f)] private float fallbackRadius = 0.3f;
    [Tooltip("Compact radius multiplier. Existing Inspector values are preserved.")]
    [SerializeField, Range(0.4f, 1.2f)] private float auraRadiusScale = 0.65f;
    [SerializeField, Range(0.02f, 0.3f)] private float edgePadding = 0.12f;
    [SerializeField, Range(0f, 0.3f)] private float armorPadding = 0.08f;
    [SerializeField, Range(0.01f, 0.15f)] private float widthRatio = 0.055f;
    [SerializeField, Range(0.01f, 0.3f)] private float jitterRatio = 0.12f;
    [SerializeField, Range(0.3f, 3f)] private float dashLengthRatio = 1.8f;
    [SerializeField] private int sortingOrderOffset = 1;

    [Header("Timing / mobile budget")]
    [SerializeField, Range(6, 20)] private int maxBolts = 12;
    [SerializeField, Range(4, 10)] private int pointsPerBolt = 7;
    [SerializeField, Range(0.06f, 0.3f)] private float boltLifetime = 0.16f;
    [SerializeField, Min(0.02f)] private float lowComboInterval = 0.3f;
    [SerializeField, Min(0.02f)] private float highComboInterval = 0.055f;
    [SerializeField, Min(0.01f)] private float fadeSpeed = 4f;

    [Header("Stage visibility")]
    [Tooltip("Makes 2x sparks brighter, thicker and less intermittent without enlarging the aura.")]
    [SerializeField, Range(1f, 2.5f)] private float combo2Presence = 1.8f;
    [SerializeField, Range(1f, 2.5f)] private float combo5Presence = 1.3f;
    [SerializeField, Range(1f, 2.5f)] private float combo6Presence = 1.5f;

    [Header("Combo upgrade impact")]
    [SerializeField, Range(0.1f, 0.4f)] private float upgradeDuration = 0.22f;
    [SerializeField, Range(1f, 2.5f)] private float upgradeWidthMultiplier = 1.6f;
    [SerializeField, Range(0f, 0.7f)] private float upgradeWhiteHighlight = 0.48f;

    [Header("Play Mode preview (disable for normal gameplay)")]
    [SerializeField] private bool previewInPlayMode;
    [SerializeField, Range(1, 6)] private int previewCombo = 6;
    [Tooltip("Only previews spark placement; does not grant armor.")]
    [SerializeField] private bool previewArmor;
    [Tooltip("Only previews spark trails; does not move Player.")]
    [SerializeField] private bool previewDash;

    private sealed class Bolt
    {
        public LineRenderer line;
        public Vector3[] points;
        public float age;
        public float strength;
        public bool active;
        public bool worldSpace;
        public bool burst;
    }

    private Bolt[] bolts;
    private Transform effectRoot;
    private Vector2 dashDirection = Vector2.right;
    private Vector3 lastPosition;
    private Vector3 effectCenter;
    private float effectRadius;
    private float bodyRadius;
    private float intensity;
    private float spawnTimer;
    private int previousCombo = 1;
    private int visualCombo = 1;
    private float secondBurstTimer = -1f;
    private int regularSparkSequence;
    private bool previousDash;
    private bool previousArmor;

    private void Awake() { FindReferences(); }

    private void Start()
    {
        // Start runs after every Awake, including PlayerSkinApplier (order 100).
        FindReferences();
        if (coinCollector == null || GetComponent<VoidClone>() != null)
        {
            Debug.LogWarning("Combo electricity belongs on the real Player with PlayerCoinCollector.", this);
            enabled = false;
            return;
        }
        if (electricMaterial == null)
            electricMaterial = Resources.Load<Material>("ComboElectricFX/ComboElectric");
        if (electricMaterial == null)
        {
            Debug.LogWarning("Combo electricity: bundled ComboElectric material is missing.", this);
            enabled = false;
            return;
        }

        maxBolts = Mathf.Clamp(maxBolts, 6, 20);
        pointsPerBolt = Mathf.Clamp(pointsPerBolt, 4, 10);
        GameObject root = new GameObject("ComboElectricFX");
        root.layer = gameObject.layer;
        effectRoot = root.transform;
        effectRoot.SetParent(transform, false);
        bolts = new Bolt[maxBolts];
        for (int i = 0; i < bolts.Length; i++)
        {
            GameObject child = new GameObject("Spark_" + i);
            child.layer = gameObject.layer;
            child.transform.SetParent(effectRoot, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = electricMaterial;
            line.useWorldSpace = true;
            line.textureMode = LineTextureMode.Stretch;
            line.positionCount = pointsPerBolt;
            line.numCapVertices = 2;
            line.numCornerVertices = 1;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            bolts[i] = new Bolt { line = line, points = new Vector3[pointsPerBolt] };
        }
    }

    private void FindReferences()
    {
        if (coinCollector == null) coinCollector = GetComponent<PlayerCoinCollector>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerDash == null) playerDash = GetComponent<PlayerDash>();
        if (playerArmor == null) playerArmor = GetComponent<PlayerArmor>();
        if (skinApplier == null) skinApplier = GetComponent<PlayerSkinApplier>();
        if (playerArmor != null)
        {
            if (playerSprite == null) playerSprite = playerArmor.playerSpriteRenderer;
            if (armorSprite == null && playerArmor.ArmorVisualObject != null)
                armorSprite = playerArmor.ArmorVisualObject.GetComponentInChildren<SpriteRenderer>(true);
        }
        // This project also has a disabled SpriteRenderer on the Player root.
        // Select the actual visible renderer rather than blindly using the first.
        if (playerSprite == null || !playerSprite.enabled)
        {
            SpriteRenderer[] candidates = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < candidates.Length; i++)
            {
                SpriteRenderer candidate = candidates[i];
                if (candidate == armorSprite || !candidate.enabled || candidate.sprite == null) continue;
                if (skinApplier != null && skinApplier.CurrentSprite != null
                    && candidate.sprite != skinApplier.CurrentSprite) continue;
                playerSprite = candidate;
                break;
            }
        }
    }

    private void OnEnable() { lastPosition = transform.position; }

    private void LateUpdate()
    {
        if (bolts == null) return;
        // Clear before the pause check: death/results may also set timeScale to 0.
        if (!previewInPlayMode && (!GameStateManager.IsGameplayStarted
            || GameStateManager.IsGameplayEnded
            || (playerMovement != null && playerMovement.IsGameOver)))
        {
            ResetVisuals();
            lastPosition = transform.position;
            return;
        }
        if (Time.timeScale <= 0f) { lastPosition = transform.position; return; }
        float dt = Time.unscaledDeltaTime; // Player keeps its speed during Slow.
        Vector2 movement = (Vector2)(transform.position - lastPosition);
        lastPosition = transform.position;

        int current = previewInPlayMode ? previewCombo
            : coinCollector != null && coinCollector.comboEnabled ? coinCollector.Combo : 1;
        current = Mathf.Clamp(current, 1, 6);
        visualCombo = current;
        bool armored = previewInPlayMode ? previewArmor : playerArmor != null && playerArmor.HasArmor;
        bool dashing = previewInPlayMode ? previewDash : playerDash != null && playerDash.IsDashing;
        if (dashing && playerMovement != null)
            dashDirection = playerMovement.LastMoveDirection.normalized;
        else if (movement.sqrMagnitude > 0.000001f) dashDirection = movement.normalized;
        if (dashDirection.sqrMagnitude < 0.001f) dashDirection = Vector2.right;

        if (armored != previousArmor || dashing != previousDash)
        {
            // Prevent an old body spark from ending up inside newly acquired armor.
            ClearBolts();
            spawnTimer = 0f;
        }
        previousArmor = armored;
        previousDash = dashing;
        UpdateBounds(armored);
        float target = current < 2 ? 0f : Mathf.Lerp(0.28f, 1f, (current - 2) / 4f);
        intensity = Mathf.MoveTowards(intensity, target, fadeSpeed * dt);
        // Every upward stage change gets a separate impact, not just a faster idle effect.
        if (current > previousCombo && current >= 2)
        {
            ClearBolts();
            PlayUpgradeBurst(dashing, current);
            spawnTimer = upgradeDuration;
            secondBurstTimer = current == 6 ? 0.09f : -1f;
        }
        else if (current < previousCombo)
            secondBurstTimer = -1f;
        else if (secondBurstTimer >= 0f)
        {
            secondBurstTimer -= dt;
            if (secondBurstTimer <= 0f)
            {
                PlayUpgradeBurst(dashing, current, true);
                secondBurstTimer = -1f;
            }
        }
        previousCombo = current;

        spawnTimer -= dt;
        if (current >= 2 && spawnTimer <= 0f)
        {
            float angle = Random.value * Mathf.PI * 2f;
            SpawnBolt(dashing, false, angle);
            regularSparkSequence++;
            // Add intermittent opposing arcs at high stages; keep dash trails uncluttered.
            if (!dashing && ((current == 5 && regularSparkSequence % 3 == 0)
                || (current == 6 && regularSparkSequence % 2 == 0)))
                SpawnBolt(false, false, angle + Mathf.PI);
            spawnTimer = Mathf.Max(0.02f, Mathf.Lerp(lowComboInterval, highComboInterval,
                (current - 2) / 4f) / GetStagePresence(current)) * (dashing ? 1.3f : 1f);
        }

        Color tint = GetEffectColor(dashing);
        float presence = GetStagePresence(current);
        float visibility = playerSprite != null && playerSprite.enabled
            && playerSprite.gameObject.activeInHierarchy ? Mathf.Clamp01(playerSprite.color.a) : 0f;
        SpriteRenderer sortingSource = ArmorVisible(armored) ? armorSprite : playerSprite;
        for (int i = 0; i < bolts.Length; i++)
        {
            Bolt b = bolts[i];
            if (!b.active) continue;
            b.age += dt;
            float life = Mathf.Max(0.02f, b.burst ? upgradeDuration
                : boltLifetime * Mathf.Min(1.2f, Mathf.Sqrt(presence)));
            if (b.age >= life || intensity <= 0f)
            {
                b.active = false;
                b.line.enabled = false;
                continue;
            }
            float expansion = b.burst ? 1f + 0.12f * b.age / life : 1f;
            for (int p = 0; p < b.points.Length; p++)
                b.line.SetPosition(p, b.worldSpace ? b.points[p]
                    : effectCenter + b.points[p] * (effectRadius * expansion));
            if (sortingSource != null)
            {
                b.line.sortingLayerID = sortingSource.sortingLayerID;
                b.line.sortingOrder = sortingSource.sortingOrder + sortingOrderOffset;
            }
            float fade = 1f - b.age / life;
            Color color = tint;
            if (b.burst) color = Color.Lerp(color, Color.white, upgradeWhiteHighlight * fade);
            float brightness = b.burst ? Mathf.Max(0.85f, intensity) : intensity;
            brightness = Mathf.Clamp01(brightness * presence);
            if (current == 6)
            {
                color.r *= 1.15f;
                color.g *= 1.15f;
                color.b *= 1.15f;
            }
            float fadeEnvelope = Mathf.Pow(fade, presence > 1f ? 1.35f : 2f);
            color.a = electricColor.a * visibility * brightness * b.strength * fadeEnvelope;
            b.line.startColor = color;
            color.a *= 0.35f;
            b.line.endColor = color;
            b.line.startWidth = Mathf.Max(0.001f, bodyRadius * widthRatio)
                * Mathf.Lerp(0.65f, 1.3f, (visualCombo - 2) / 4f)
                * Mathf.Sqrt(presence)
                * (b.burst ? upgradeWidthMultiplier : 1f);
            b.line.endWidth = b.line.startWidth * 0.3f;
        }
    }

    private bool ArmorVisible(bool armored)
    {
        return armored && armorSprite != null && armorSprite.enabled
            && armorSprite.gameObject.activeInHierarchy && armorSprite.sprite != null;
    }

    private float GetStagePresence(int stage)
    {
        switch (stage)
        {
            case 2: return Mathf.Clamp(combo2Presence, 1f, 2.5f);
            case 5: return Mathf.Clamp(combo5Presence, 1f, 2.5f);
            case 6: return Mathf.Clamp(combo6Presence, 1f, 2.5f);
            default: return 1f;
        }
    }

    private void UpdateBounds(bool armored)
    {
        effectCenter = playerSprite != null ? playerSprite.bounds.center : transform.position;
        bodyRadius = playerSprite != null && playerSprite.sprite != null
            ? Mathf.Max(playerSprite.bounds.extents.x, playerSprite.bounds.extents.y)
            : fallbackRadius;
        bodyRadius = Mathf.Max(0.01f, bodyRadius);
        effectRadius = bodyRadius * (1f + edgePadding) * auraRadiusScale;
        if (ArmorVisible(armored))
        {
            effectCenter = armorSprite.bounds.center;
            float radius = Mathf.Max(armorSprite.bounds.extents.x, armorSprite.bounds.extents.y);
            // Armor keeps its outer clearance; compacting Player's aura must not bury it.
            effectRadius = Mathf.Max(effectRadius, radius + bodyRadius * armorPadding * 0.5f);
        }
        else if (armored) effectRadius += bodyRadius * 0.25f;
    }

    private Color GetEffectColor(bool dashing)
    {
        Color color = electricColor;
        if (followSkinColor && skinApplier != null && skinApplier.CurrentSkin != null)
            color = dashing ? skinApplier.CurrentDashTrailColor : skinApplier.CurrentArmorVisualColor;
        float highest = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        if (highest > 1f) { color.r /= highest; color.g /= highest; color.b /= highest; }
        // Dash colors can have alpha=0 in the catalog: only use their RGB here.
        color.a = 1f;
        return Color.Lerp(color, Color.white, whiteHighlight);
    }

    private void SpawnBolt(bool dashing, bool burst, float angle)
    {
        Bolt b = null;
        for (int i = 0; i < bolts.Length; i++)
            if (!bolts[i].active) { b = bolts[i]; break; }
        if (b == null) return;
        Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 backward = -dashDirection;
        Vector2 perpendicular = new Vector2(-backward.y, backward.x);
        float stage = Mathf.Clamp01((visualCombo - 2) / 4f);
        // Higher stages have longer and thicker individual arcs as well as more sparks.
        float sweep = Random.Range(0.25f, 0.55f) * Mathf.Lerp(0.75f, 1.3f, stage);
        for (int p = 0; p < b.points.Length; p++)
        {
            float t = p / (float)(b.points.Length - 1);
            Vector2 point;
            if (dashing)
            {
                point = radial * effectRadius + backward * (t * bodyRadius * dashLengthRatio)
                    + perpendicular * Random.Range(-jitterRatio, jitterRatio) * bodyRadius;
                b.points[p] = effectCenter + new Vector3(point.x, point.y, 0f);
            }
            else
            {
                float a = angle + (burst ? 0f : sweep * t);
                float radius = 1f + Random.Range(0f, jitterRatio * 0.65f)
                    + (burst ? t * Mathf.Lerp(0.18f, 0.34f, stage) : 0f);
                point = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                if (burst) point += new Vector2(-radial.y, radial.x) * Random.Range(-jitterRatio, jitterRatio);
                b.points[p] = new Vector3(point.x, point.y, 0f);
            }
        }
        b.worldSpace = dashing;
        b.burst = burst;
        b.age = 0f;
        b.strength = burst ? 1f : Random.Range(0.7f, 1f);
        b.active = true;
        b.line.enabled = true;
    }

    private void PlayUpgradeBurst(bool dashing, int stage, bool echo = false)
    {
        int count = echo ? 4 : stage == 6 ? 8 : stage + 1;
        float offset = Random.value * Mathf.PI * 2f;
        for (int i = 0; i < count; i++)
            SpawnBolt(dashing, true, offset + i * Mathf.PI * 2f / count);
    }

    private void ClearBolts()
    {
        if (bolts == null) return;
        for (int i = 0; i < bolts.Length; i++)
        {
            bolts[i].active = false;
            bolts[i].line.enabled = false;
        }
    }

    private void ResetVisuals()
    {
        ClearBolts();
        intensity = spawnTimer = 0f;
        secondBurstTimer = -1f;
        visualCombo = 1;
        regularSparkSequence = 0;
        previousCombo = 1;
        previousDash = previousArmor = false;
    }

    private void OnDisable() { ResetVisuals(); }
    private void OnDestroy() { if (effectRoot != null) Destroy(effectRoot.gameObject); }
}
