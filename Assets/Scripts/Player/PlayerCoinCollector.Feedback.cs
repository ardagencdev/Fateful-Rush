using TMPro;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in PlayerCoinCollector.cs.
public partial class PlayerCoinCollector
{
    private void PlaySpecialSkinCoinEffect(
        Coin coin,
        Collider2D coinCollider,
        int coinValue)
    {
        if (specialSkinVisuals == null)
        {
            specialSkinVisuals =
                GetComponent<SpecialSkinVisuals>();
        }

        if (specialSkinVisuals == null)
            return;

        Vector3 burstPosition;

        if (coin != null)
        {
            burstPosition = coin.transform.position;
        }
        else if (coinCollider != null)
        {
            burstPosition = coinCollider.bounds.center;
        }
        else
        {
            return;
        }

        float coinWorldSize = GetCoinWorldSize(
            coin,
            coinCollider
        );

        specialSkinVisuals.PlayCoinCollectBurst(
            burstPosition,
            coinValue,
            coinWorldSize
        );
    }

    private static float GetCoinWorldSize(
        Coin coin,
        Collider2D coinCollider)
    {
        SpriteRenderer coinRenderer = null;

        if (coin != null)
        {
            coinRenderer =
                coin.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (coinRenderer == null && coinCollider != null)
        {
            coinRenderer =
                coinCollider.GetComponentInParent<SpriteRenderer>();

            if (coinRenderer == null)
            {
                coinRenderer =
                    coinCollider.GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        if (coinRenderer != null)
        {
            Vector3 size = coinRenderer.bounds.size;
            float worldSize = Mathf.Max(size.x, size.y);

            if (worldSize > 0.001f)
                return worldSize;
        }

        if (coinCollider != null)
        {
            Vector3 size = coinCollider.bounds.size;
            float worldSize = Mathf.Max(size.x, size.y);

            if (worldSize > 0.001f)
                return worldSize;
        }

        return 0.6f;
    }

    private void PlayCollectEffect(
        Coin coin,
        Collider2D coinCollider)
    {
        SpawnScaleEffect coinEffect = null;

        if (coin != null)
        {
            // Efekt coin prefabındaki Visual child objesinde bulunuyor.
            coinEffect =
                coin.GetComponentInChildren<SpawnScaleEffect>(true);
        }

        if (coinEffect == null && coinCollider != null)
        {
            coinEffect =
                coinCollider.GetComponentInChildren<SpawnScaleEffect>(true);
        }

        if (coinEffect != null)
        {
            coinEffect.Collect();
            return;
        }

        GameObject coinObject =
            coin != null
                ? coin.gameObject
                : coinCollider.transform.root.gameObject;

        Destroy(coinObject);
    }

    private static void DisableFallbackCoinPhysics(
        Collider2D coinCollider)
    {
        Transform root = coinCollider.transform.root;

        Collider2D[] colliders =
            root.GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Rigidbody2D[] rigidbodies =
            root.GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody2D body = rigidbodies[i];

            if (body != null)
                body.simulated = false;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText == null)
            return;

        scoreText.text = FatefulRushLocalization.Text(
            "hud.score", "SCORE: {0}", score
        );
    }

    public void RefreshLocalizedScoreUI()
    {
        UpdateScoreUI();
    }
}
