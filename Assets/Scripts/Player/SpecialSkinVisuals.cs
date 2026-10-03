using UnityEngine;

/// <summary>
/// Cosmetic-only prestige skin effects.
///
/// Dark / Golden:
/// - dash afterimages
/// - level start / spawn pulse
/// - death pulse
///
/// Purple:
/// - level start / spawn pulse only
///
/// White / Blue / Cyan / Yellow / Orange / Green / Red / Purple:
/// - shared white coin collection sprite tinted with Armor Visual Color
///
/// Dark:
/// - unique sprite-based coin collection effect
///
/// Golden:
/// - unique sprite-based coin collection effect
///
/// No gameplay values are changed here.
/// </summary>
[DisallowMultipleComponent]
public partial class SpecialSkinVisuals : MonoBehaviour
{
    private const string DarkSkinId = "dark";
    private const string GoldenSkinId = "golden";
    private const string PurpleSkinId = "purple";

    private const int BurstPoolSize = 10;
    private const int PrestigePulsePoolSize = 4;
    private const int AfterimagePoolSize = 6;

    private const string DarkCoinBurstResourcePath =
        "SpecialSkinVFX/DarkCoinBurst";

    private const string GoldenCoinBurstResourcePath =
        "SpecialSkinVFX/GoldenCoinBurst";

    private const string StandardCoinBurstResourcePath =
        "SpecialSkinVFX/StandardCoinBurst";

    [Header("Prestige Dash Afterimage")]
    [SerializeField, Min(0.01f)]
    private float afterimageInterval = 0.045f;

    [SerializeField, Min(0.05f)]
    private float afterimageLifetime = 0.18f;

    [SerializeField, Range(0f, 1f)]
    private float afterimageAlpha = 0.24f;

    [Header("Prestige Spawn / Death")]
    [SerializeField, Min(0.05f)]
    private float spawnBurstDuration = 0.34f;

    [SerializeField, Min(0.05f)]
    private float deathBurstDuration = 0.40f;

    [SerializeField, Range(0.05f, 1f)]
    private float spawnPulseAlpha = 0.42f;

    [SerializeField, Range(0.05f, 1f)]
    private float deathPulseAlpha = 0.62f;

    [Header("Coin Collection Sprites")]
    [Tooltip(
        "White generic coin collect sprite used by every non-prestige skin. " +
        "It is tinted automatically with that skin's Armor Visual Color. " +
        "If empty, Resources/SpecialSkinVFX/StandardCoinBurst is loaded."
    )]
    [SerializeField]
    private Sprite standardCoinBurstSprite;

    [Tooltip(
        "Optional. If empty, Resources/SpecialSkinVFX/DarkCoinBurst is loaded."
    )]
    [SerializeField]
    private Sprite darkCoinBurstSprite;

    [Tooltip(
        "Optional. If empty, Resources/SpecialSkinVFX/GoldenCoinBurst is loaded."
    )]
    [SerializeField]
    private Sprite goldenCoinBurstSprite;

    [Header("Coin Burst Animation")]
    [SerializeField, Min(0.05f)]
    private float burstDuration = 0.28f;

    [Tooltip(
        "How large the effect becomes compared with the collected coin."
    )]
    [SerializeField, Min(1f)]
    private float finalBurstSizeMultiplier = 2.5f;

    [Tooltip(
        "The effect begins very small, then expands to its final size."
    )]
    [SerializeField, Range(0.01f, 0.9f)]
    private float startSizeRatio = 0.30f;

    [SerializeField, Range(0f, 1f)]
    private float burstAlpha = 0.85f;

    private PlayerSkinApplier skinApplier;
    private PlayerDash playerDash;
    private SpriteRenderer playerRenderer;

    private string activeSkinId = string.Empty;
    private Color activeDashColor = Color.white;
    private Color activeArmorColor = Color.white;

    private float afterimageTimer;
    private bool levelSpawnPlayed;

    private GameObject afterimagePoolRoot;
    private PrestigeAfterimageFade[] afterimagePool;
    private int afterimagePoolCursor;

    private GameObject burstPoolRoot;
    private SpecialSkinCoinBurstSprite[] burstPool;
    private int burstPoolCursor;

    private GameObject prestigePulsePoolRoot;
    private SpecialSkinPulseSprite[] prestigePulsePool;
    private int prestigePulsePoolCursor;

    public string ActiveSkinId => activeSkinId;

    private bool IsDark => activeSkinId == DarkSkinId;
    private bool IsGolden => activeSkinId == GoldenSkinId;
    private bool IsPurple => activeSkinId == PurpleSkinId;

    private bool UsesPrestigeEffects =>
        IsDark || IsGolden;

    // Purple shares only the level-start spawn pulse.
    // Dash afterimages and death pulse remain exclusive to Dark / Golden.
    private bool UsesSpawnEffect =>
        UsesPrestigeEffects || IsPurple;

    private void Awake()
    {
        skinApplier = GetComponent<PlayerSkinApplier>();
        playerDash = GetComponent<PlayerDash>();

        FindPlayerRenderer();
        LoadOptionalSprites();
    }

    private void OnEnable()
    {
        RefreshFromCurrentSkin();
    }

    private void Update()
    {
        TryPlayLevelSpawnEffect();
        UpdatePrestigeAfterimages();
    }

    private void OnDestroy()
    {
        if (afterimagePoolRoot != null)
            Destroy(afterimagePoolRoot);

        if (burstPoolRoot != null)
            Destroy(burstPoolRoot);

        // This root lives outside the player hierarchy so a death pulse can
        // finish even if the player is destroyed immediately afterwards.
        if (prestigePulsePoolRoot != null)
            Destroy(prestigePulsePoolRoot, 1f);
    }

    public void ApplySkin(
        PlayerSkinCatalog.SkinEntry skin)
    {
        string newSkinId =
            skin != null &&
            !string.IsNullOrWhiteSpace(skin.id)
                ? skin.id.Trim().ToLowerInvariant()
                : string.Empty;

        bool skinChanged = activeSkinId != newSkinId;

        activeSkinId = newSkinId;

        if (skin != null)
        {
            activeDashColor = MakeVisibleColor(
                skin.dashTrailColor,
                Color.white
            );

            activeArmorColor = MakeVisibleColor(
                skin.armorVisualColor,
                activeDashColor
            );
        }
        else
        {
            activeDashColor = Color.white;
            activeArmorColor = Color.white;
        }

        if (skinChanged)
            levelSpawnPlayed = false;

        afterimageTimer = 0f;

        FindPlayerRenderer();
        LoadOptionalSprites();
    }

    public void PlayDeathEffect()
    {
        if (!UsesPrestigeEffects)
            return;

        FindPlayerRenderer();

        Vector3 effectPosition =
            playerRenderer != null
                ? playerRenderer.transform.position
                : transform.position;

        PrestigeStyle style = GetPrestigeStyle();

        PlayPrestigePulse(
            effectPosition,
            1.00f,
            1.90f * style.pulseScaleMultiplier,
            deathBurstDuration,
            deathPulseAlpha,
            style.secondaryColor
        );

    }

    // Compatibility overload for older call sites.

    private void RefreshFromCurrentSkin()
    {
        if (skinApplier == null)
            skinApplier = GetComponent<PlayerSkinApplier>();

        ApplySkin(
            skinApplier != null
                ? skinApplier.CurrentSkin
                : null
        );
    }

    private void LoadOptionalSprites()
    {
        if (standardCoinBurstSprite == null)
        {
            standardCoinBurstSprite =
                Resources.Load<Sprite>(
                    StandardCoinBurstResourcePath
                );
        }

        if (darkCoinBurstSprite == null)
        {
            darkCoinBurstSprite =
                Resources.Load<Sprite>(
                    DarkCoinBurstResourcePath
                );
        }

        if (goldenCoinBurstSprite == null)
        {
            goldenCoinBurstSprite =
                Resources.Load<Sprite>(
                    GoldenCoinBurstResourcePath
                );
        }
    }

    private void FindPlayerRenderer()
    {
        Sprite expectedSprite =
            skinApplier != null
                ? skinApplier.CurrentSprite
                : null;

        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(
                true
            );

        if (expectedSprite != null)
        {
            for (int i = 0;
                 i < renderers.Length;
                 i++)
            {
                SpriteRenderer candidate =
                    renderers[i];

                if (candidate != null &&
                    candidate.sprite ==
                    expectedSprite)
                {
                    playerRenderer = candidate;
                    return;
                }
            }
        }

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            SpriteRenderer candidate =
                renderers[i];

            if (candidate == null ||
                candidate.sprite == null)
            {
                continue;
            }

            playerRenderer = candidate;
            return;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        afterimageInterval =
            Mathf.Max(
                0.01f,
                afterimageInterval
            );

        afterimageLifetime =
            Mathf.Max(
                0.05f,
                afterimageLifetime
            );

        spawnBurstDuration =
            Mathf.Max(
                0.05f,
                spawnBurstDuration
            );

        deathBurstDuration =
            Mathf.Max(
                0.05f,
                deathBurstDuration
            );

        burstDuration =
            Mathf.Max(
                0.05f,
                burstDuration
            );

        finalBurstSizeMultiplier =
            Mathf.Max(
                1f,
                finalBurstSizeMultiplier
            );
    }
#endif

    private struct PrestigeStyle
    {
        public Color secondaryColor;
        public float pulseScaleMultiplier;
    }
}

/// <summary>
/// Short-lived dash ghost used by prestige skins.
/// </summary>

/// <summary>
/// A pooled tinted copy of the player's sprite used as the soft pulse layer
/// for prestige spawn and death effects.
/// </summary>

/// <summary>
/// Pooled sprite-based coin collection effect.
///
/// Starts very small at the collected coin,
/// expands beyond the coin,
/// and fades out while expanding.
/// </summary>
