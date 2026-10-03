using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in SpecialSkinVisuals.cs.
public partial class SpecialSkinVisuals
{
    public void PlayCoinCollectBurst(
        Vector3 worldPosition,
        int coinValue,
        float coinWorldSize)
    {
        Sprite selectedSprite;
        Color burstColor;

        if (IsDark)
        {
            selectedSprite = darkCoinBurstSprite;
            burstColor = Color.white;
        }
        else if (IsGolden)
        {
            selectedSprite = goldenCoinBurstSprite;
            burstColor = Color.white;
        }
        else
        {
            selectedSprite = standardCoinBurstSprite;
            burstColor = activeArmorColor;
        }

        if (selectedSprite == null)
            return;

        int safeValue =
            Mathf.Max(1, coinValue);

        float safeCoinSize =
            Mathf.Max(0.05f, coinWorldSize);

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
            burstAlpha,
            burstColor
        );
    }

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

    private void TryPlayLevelSpawnEffect()
    {
        if (levelSpawnPlayed ||
            !UsesSpawnEffect ||
            !GameStateManager.IsGameplayStarted)
        {
            return;
        }

        levelSpawnPlayed = true;

        FindPlayerRenderer();

        Vector3 effectPosition =
            playerRenderer != null
                ? playerRenderer.transform.position
                : transform.position;

        PrestigeStyle style = GetPrestigeStyle();

        PlayPrestigePulse(
            effectPosition,
            0.58f,
            1.55f * style.pulseScaleMultiplier,
            spawnBurstDuration,
            spawnPulseAlpha,
            style.secondaryColor
        );

    }

    private void PlayPrestigePulse(
        Vector3 worldPosition,
        float startScaleMultiplier,
        float finalScaleMultiplier,
        float duration,
        float alpha,
        Color color)
    {
        FindPlayerRenderer();

        if (playerRenderer == null ||
            playerRenderer.sprite == null)
        {
            return;
        }

        SpecialSkinPulseSprite pulse =
            GetPrestigePulse();

        if (pulse == null)
            return;

        pulse.Play(
            worldPosition,
            playerRenderer.sprite,
            playerRenderer.transform.rotation,
            playerRenderer.transform.lossyScale,
            playerRenderer.flipX,
            playerRenderer.flipY,
            startScaleMultiplier,
            finalScaleMultiplier,
            duration,
            alpha,
            color
        );
    }

    private PrestigeStyle GetPrestigeStyle()
    {
        PrestigeStyle style = new PrestigeStyle
        {
            secondaryColor = activeArmorColor,
            pulseScaleMultiplier = 1f
        };

        if (IsDark)
        {
            style.pulseScaleMultiplier = 1.08f;
        }
        else if (IsGolden)
        {
            style.pulseScaleMultiplier = 1.12f;
        }

        return style;
    }

    private SpecialSkinPulseSprite
        GetPrestigePulse()
    {
        EnsurePrestigePulsePool();

        if (prestigePulsePool == null ||
            prestigePulsePool.Length == 0)
        {
            return null;
        }

        for (int i = 0;
             i < prestigePulsePool.Length;
             i++)
        {
            int index =
                (prestigePulsePoolCursor + i) %
                prestigePulsePool.Length;

            SpecialSkinPulseSprite candidate =
                prestigePulsePool[index];

            if (candidate != null &&
                !candidate.gameObject.activeSelf)
            {
                prestigePulsePoolCursor =
                    (index + 1) %
                    prestigePulsePool.Length;

                return candidate;
            }
        }

        SpecialSkinPulseSprite fallback =
            prestigePulsePool[
                prestigePulsePoolCursor
            ];

        prestigePulsePoolCursor =
            (prestigePulsePoolCursor + 1) %
            prestigePulsePool.Length;

        return fallback;
    }

    private void EnsurePrestigePulsePool()
    {
        if (prestigePulsePool != null &&
            prestigePulsePool.Length ==
            PrestigePulsePoolSize)
        {
            return;
        }

        if (prestigePulsePoolRoot == null)
        {
            prestigePulsePoolRoot =
                new GameObject(
                    "PrestigeSkinPulsePool"
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
                : 29;

        prestigePulsePool =
            new SpecialSkinPulseSprite[
                PrestigePulsePoolSize
            ];

        for (int i = 0;
             i < prestigePulsePool.Length;
             i++)
        {
            GameObject pulseObject =
                new GameObject(
                    $"PrestigePulse_{i}"
                );

            pulseObject.transform.SetParent(
                prestigePulsePoolRoot.transform,
                false
            );

            SpecialSkinPulseSprite pulse =
                pulseObject.AddComponent<
                    SpecialSkinPulseSprite
                >();

            pulse.Prepare(
                sortingLayerId,
                sortingOrder
            );

            pulseObject.SetActive(false);
            prestigePulsePool[i] = pulse;
        }
    }

    private void CreateSpriteBurst(
        Vector3 worldPosition,
        Sprite sprite,
        float startWorldSize,
        float finalWorldSize,
        float duration,
        float alpha,
        Color tintColor)
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
            alpha,
            tintColor
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

    private static Color MakeVisibleColor(
        Color color,
        Color fallback)
    {
        float maximumChannel =
            Mathf.Max(
                color.r,
                Mathf.Max(color.g, color.b)
            );

        if (maximumChannel <= 0.001f)
            color = fallback;

        color.a = 1f;
        return color;
    }
}
