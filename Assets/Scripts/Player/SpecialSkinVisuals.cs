using UnityEngine;

/// <summary>
/// Cosmetic-only prestige skin effects.
///
/// Silver / Dark / Golden:
/// - dash afterimages
///
/// Dark:
/// - unique sprite-based coin collection effect
///
/// Golden:
/// - unique sprite-based coin collection effect
///
/// Aura / wings were intentionally removed.
/// No gameplay values are changed here.
/// </summary>
[DisallowMultipleComponent]
public class SpecialSkinVisuals : MonoBehaviour
{
    private const string SilverSkinId = "silver";
    private const string DarkSkinId = "dark";
    private const string GoldenSkinId = "golden";

    private const int BurstPoolSize = 10;

    private const string DarkCoinBurstResourcePath =
        "SpecialSkinVFX/DarkCoinBurst";

    private const string GoldenCoinBurstResourcePath =
        "SpecialSkinVFX/GoldenCoinBurst";

    [Header("Prestige Dash Afterimage")]
    [SerializeField, Min(0.01f)]
    private float afterimageInterval = 0.045f;

    [SerializeField, Min(0.05f)]
    private float afterimageLifetime = 0.18f;

    [SerializeField, Range(0f, 1f)]
    private float afterimageAlpha = 0.24f;

    [Header("Coin Collection Sprites")]
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
    private float afterimageTimer;

    private GameObject burstPoolRoot;
    private SpecialSkinCoinBurstSprite[] burstPool;
    private int burstPoolCursor;

    public string ActiveSkinId => activeSkinId;

    private bool IsSilver => activeSkinId == SilverSkinId;
    private bool IsDark => activeSkinId == DarkSkinId;
    private bool IsGolden => activeSkinId == GoldenSkinId;

    private bool UsesPrestigeAfterimage =>
        IsSilver || IsDark || IsGolden;

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
        UpdatePrestigeAfterimages();
    }

    private void OnDestroy()
    {
        if (burstPoolRoot != null)
            Destroy(burstPoolRoot);
    }

    public void ApplySkin(
        PlayerSkinCatalog.SkinEntry skin)
    {
        activeSkinId =
            skin != null &&
            !string.IsNullOrWhiteSpace(skin.id)
                ? skin.id.Trim().ToLowerInvariant()
                : string.Empty;

        afterimageTimer = 0f;

        FindPlayerRenderer();
        LoadOptionalSprites();
    }

    public void PlayCoinCollectBurst(
        Vector3 worldPosition,
        int coinValue,
        float coinWorldSize)
    {
        if (!IsDark && !IsGolden)
            return;

        Sprite selectedSprite =
            IsDark
                ? darkCoinBurstSprite
                : goldenCoinBurstSprite;

        if (selectedSprite == null)
            return;

        int safeValue =
            Mathf.Max(1, coinValue);

        float safeCoinSize =
            Mathf.Max(0.05f, coinWorldSize);

        // Higher-value coins get slightly larger effects,
        // but the burst always stays close to the coin.
        float valueScale =
            Mathf.Clamp(
                1f + (safeValue - 1) * 0.10f,
                1f,
                1.30f
            );

        float finalWorldSize =
            safeCoinSize *
            finalBurstSizeMultiplier *
            valueScale;

        float startWorldSize =
            finalWorldSize *
            startSizeRatio;

        CreateSpriteBurst(
            worldPosition,
            selectedSprite,
            startWorldSize,
            finalWorldSize,
            burstDuration,
            burstAlpha
        );
    }

    // Compatibility overload for any older call sites.
    public void PlayCoinCollectBurst(
        Vector3 worldPosition,
        int coinValue)
    {
        PlayCoinCollectBurst(
            worldPosition,
            coinValue,
            0.6f
        );
    }

    private void RefreshFromCurrentSkin()
    {
        if (skinApplier == null)
            skinApplier =
                GetComponent<PlayerSkinApplier>();

        ApplySkin(
            skinApplier != null
                ? skinApplier.CurrentSkin
                : null
        );
    }

    private void LoadOptionalSprites()
    {
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
                    playerRenderer =
                        candidate;
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

    private void UpdatePrestigeAfterimages()
    {
        if (!UsesPrestigeAfterimage ||
            playerDash == null ||
            !playerDash.IsDashing)
        {
            afterimageTimer = 0f;
            return;
        }

        afterimageTimer -=
            Time.unscaledDeltaTime;

        if (afterimageTimer > 0f)
            return;

        afterimageTimer =
            afterimageInterval;

        SpawnAfterimage();
    }

    private void SpawnAfterimage()
    {
        if (playerRenderer == null ||
            playerRenderer.sprite == null)
        {
            FindPlayerRenderer();
        }

        if (playerRenderer == null ||
            playerRenderer.sprite == null)
        {
            return;
        }

        GameObject ghost =
            new GameObject(
                "PrestigeDashAfterimage"
            );

        ghost.transform.position =
            playerRenderer.transform.position;

        ghost.transform.rotation =
            playerRenderer.transform.rotation;

        ghost.transform.localScale =
            playerRenderer.transform.lossyScale;

        SpriteRenderer ghostRenderer =
            ghost.AddComponent<SpriteRenderer>();

        ghostRenderer.sprite =
            playerRenderer.sprite;

        ghostRenderer.flipX =
            playerRenderer.flipX;

        ghostRenderer.flipY =
            playerRenderer.flipY;

        ghostRenderer.sortingLayerID =
            playerRenderer.sortingLayerID;

        ghostRenderer.sortingOrder =
            playerRenderer.sortingOrder - 1;

        // Preserve the actual skin colours.
        ghostRenderer.color =
            new Color(
                1f,
                1f,
                1f,
                afterimageAlpha
            );

        SilverAfterimageFade fade =
            ghost.AddComponent<
                SilverAfterimageFade
            >();

        fade.Initialize(
            ghostRenderer,
            afterimageLifetime
        );
    }

    private void CreateSpriteBurst(
        Vector3 worldPosition,
        Sprite sprite,
        float startWorldSize,
        float finalWorldSize,
        float duration,
        float alpha)
    {
        EnsureBurstPool();

        if (burstPool == null ||
            burstPool.Length == 0)
        {
            return;
        }

        SpecialSkinCoinBurstSprite selected =
            null;

        for (int i = 0;
             i < burstPool.Length;
             i++)
        {
            int index =
                (burstPoolCursor + i) %
                burstPool.Length;

            SpecialSkinCoinBurstSprite candidate =
                burstPool[index];

            if (candidate != null &&
                !candidate.gameObject.activeSelf)
            {
                selected = candidate;

                burstPoolCursor =
                    (index + 1) %
                    burstPool.Length;

                break;
            }
        }

        if (selected == null)
        {
            selected =
                burstPool[
                    burstPoolCursor
                ];

            burstPoolCursor =
                (burstPoolCursor + 1) %
                burstPool.Length;
        }

        if (selected == null)
            return;

        selected.Play(
            worldPosition,
            sprite,
            startWorldSize,
            finalWorldSize,
            duration,
            alpha
        );
    }

    private void EnsureBurstPool()
    {
        if (burstPool != null &&
            burstPool.Length ==
            BurstPoolSize)
        {
            return;
        }

        if (burstPoolRoot == null)
        {
            burstPoolRoot =
                new GameObject(
                    "SpecialSkinCoinBurstPool"
                );
        }

        FindPlayerRenderer();

        int sortingLayerId =
            playerRenderer != null
                ? playerRenderer.sortingLayerID
                : 0;

        int sortingOrder =
            playerRenderer != null
                ? playerRenderer.sortingOrder + 2
                : 20;

        burstPool =
            new SpecialSkinCoinBurstSprite[
                BurstPoolSize
            ];

        for (int i = 0;
             i < burstPool.Length;
             i++)
        {
            GameObject burstObject =
                new GameObject(
                    $"SpecialSkinCoinBurst_{i}"
                );

            burstObject.transform.SetParent(
                burstPoolRoot.transform,
                false
            );

            SpecialSkinCoinBurstSprite burst =
                burstObject.AddComponent<
                    SpecialSkinCoinBurstSprite
                >();

            burst.Prepare(
                sortingLayerId,
                sortingOrder
            );

            burstObject.SetActive(false);

            burstPool[i] = burst;
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
}

/// <summary>
/// Short-lived dash ghost.
/// Kept under the old class name for compatibility.
/// </summary>
public class SilverAfterimageFade :
    MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float lifetime;
    private float elapsed;
    private Color startColor;

    public void Initialize(
        SpriteRenderer renderer,
        float duration)
    {
        spriteRenderer = renderer;

        lifetime =
            Mathf.Max(
                0.01f,
                duration
            );

        startColor =
            spriteRenderer != null
                ? spriteRenderer.color
                : Color.white;
    }

    private void Update()
    {
        elapsed +=
            Time.unscaledDeltaTime;

        float t =
            Mathf.Clamp01(
                elapsed / lifetime
            );

        if (spriteRenderer != null)
        {
            Color color = startColor;

            color.a =
                Mathf.Lerp(
                    startColor.a,
                    0f,
                    t
                );

            spriteRenderer.color =
                color;
        }

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}

/// <summary>
/// Pooled sprite-based coin collection effect.
///
/// Starts very small at the collected coin,
/// expands beyond the coin,
/// and fades out while expanding.
/// </summary>
public class SpecialSkinCoinBurstSprite :
    MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private float duration;
    private float elapsed;
    private float maxAlpha;

    private Vector3 startScale;
    private Vector3 finalScale;

    public void Prepare(
        int sortingLayerId,
        int sortingOrder)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                gameObject.AddComponent<
                    SpriteRenderer
                >();
        }

        spriteRenderer.sortingLayerID =
            sortingLayerId;

        spriteRenderer.sortingOrder =
            sortingOrder;

        spriteRenderer.color =
            Color.clear;
    }

    public void Play(
        Vector3 worldPosition,
        Sprite sprite,
        float startWorldSize,
        float finalWorldSize,
        float effectDuration,
        float alpha)
    {
        if (sprite == null ||
            spriteRenderer == null)
        {
            gameObject.SetActive(false);
            return;
        }

        spriteRenderer.sprite = sprite;

        duration =
            Mathf.Max(
                0.05f,
                effectDuration
            );

        maxAlpha =
            Mathf.Clamp01(alpha);

        elapsed = 0f;

        transform.position =
            worldPosition;

        transform.rotation =
            Quaternion.identity;

        float spriteLocalSize =
            Mathf.Max(
                sprite.bounds.size.x,
                sprite.bounds.size.y
            );

        if (spriteLocalSize <= 0.001f)
            spriteLocalSize = 1f;

        float startScaleValue =
            Mathf.Max(
                0.001f,
                startWorldSize /
                spriteLocalSize
            );

        float finalScaleValue =
            Mathf.Max(
                startScaleValue,
                finalWorldSize /
                spriteLocalSize
            );

        startScale =
            Vector3.one *
            startScaleValue;

        finalScale =
            Vector3.one *
            finalScaleValue;

        transform.localScale =
            startScale;

        spriteRenderer.color =
            new Color(
                1f,
                1f,
                1f,
                maxAlpha
            );

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    private void Update()
    {
        elapsed +=
            Time.unscaledDeltaTime;

        float t =
            Mathf.Clamp01(
                elapsed / duration
            );

        // Fast opening, then soft finish.
        float expandT =
            1f -
            Mathf.Pow(
                1f - t,
                3f
            );

        transform.localScale =
            Vector3.Lerp(
                startScale,
                finalScale,
                expandT
            );

        // Alpha continuously drops while the sprite expands.
        float fade =
            1f - t;

        fade *= fade;

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    maxAlpha * fade
                );
        }

        if (elapsed >= duration)
            gameObject.SetActive(false);
    }
}
