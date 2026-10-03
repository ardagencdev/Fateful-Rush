using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in SpecialSkinVisuals.cs.
public partial class SpecialSkinVisuals
{
    private void UpdatePrestigeAfterimages()
    {
        if (!UsesPrestigeEffects ||
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

        PrestigeAfterimageFade fade =
            GetAfterimageFromPool();

        if (fade == null)
            return;

        GameObject ghost = fade.gameObject;
        SpriteRenderer ghostRenderer = fade.Renderer;

        if (ghostRenderer == null)
            return;

        ghost.transform.position =
            playerRenderer.transform.position;

        ghost.transform.rotation =
            playerRenderer.transform.rotation;

        ghost.transform.localScale =
            playerRenderer.transform.lossyScale;

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

        ghostRenderer.color =
            new Color(
                1f,
                1f,
                1f,
                afterimageAlpha
            );

        fade.Initialize(
            ghostRenderer,
            afterimageLifetime
        );

        ghost.SetActive(true);
    }

    private PrestigeAfterimageFade GetAfterimageFromPool()
    {
        EnsureAfterimagePool();

        if (afterimagePool == null ||
            afterimagePool.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < afterimagePool.Length; i++)
        {
            int index =
                (afterimagePoolCursor + i) %
                afterimagePool.Length;

            PrestigeAfterimageFade candidate =
                afterimagePool[index];

            if (candidate != null &&
                !candidate.gameObject.activeSelf)
            {
                afterimagePoolCursor =
                    (index + 1) % afterimagePool.Length;

                return candidate;
            }
        }

        PrestigeAfterimageFade fallback =
            afterimagePool[afterimagePoolCursor];

        afterimagePoolCursor =
            (afterimagePoolCursor + 1) % afterimagePool.Length;

        return fallback;
    }

    private void EnsureAfterimagePool()
    {
        if (afterimagePool != null &&
            afterimagePool.Length == AfterimagePoolSize)
        {
            return;
        }

        if (afterimagePoolRoot == null)
        {
            afterimagePoolRoot =
                new GameObject(
                    "PrestigeDashAfterimagePool"
                );
        }

        afterimagePool =
            new PrestigeAfterimageFade[AfterimagePoolSize];

        for (int i = 0; i < afterimagePool.Length; i++)
        {
            GameObject ghost =
                new GameObject(
                    $"PrestigeDashAfterimage_{i}"
                );

            ghost.transform.SetParent(
                afterimagePoolRoot.transform,
                false
            );

            SpriteRenderer ghostRenderer =
                ghost.AddComponent<SpriteRenderer>();

            PrestigeAfterimageFade fade =
                ghost.AddComponent<PrestigeAfterimageFade>();

            fade.Prepare(ghostRenderer);
            ghost.SetActive(false);
            afterimagePool[i] = fade;
        }
    }
}
