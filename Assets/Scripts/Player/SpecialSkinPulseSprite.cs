using UnityEngine;

public class SpecialSkinPulseSprite :
    MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private float duration;
    private float elapsed;
    private float maximumAlpha;

    private Vector3 startScale;
    private Vector3 finalScale;
    private Color pulseColor;

    public void Prepare(
        int sortingLayerId,
        int sortingOrder)
    {
        spriteRenderer =
            gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sortingLayerID =
            sortingLayerId;

        spriteRenderer.sortingOrder =
            sortingOrder;

        spriteRenderer.color = Color.clear;
    }

    public void Play(
        Vector3 worldPosition,
        Sprite sprite,
        Quaternion rotation,
        Vector3 playerWorldScale,
        bool flipX,
        bool flipY,
        float startScaleMultiplier,
        float finalScaleMultiplier,
        float effectDuration,
        float alpha,
        Color color)
    {
        if (spriteRenderer == null ||
            sprite == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        spriteRenderer.sprite = sprite;
        spriteRenderer.flipX = flipX;
        spriteRenderer.flipY = flipY;

        transform.position = worldPosition;
        transform.rotation = rotation;

        duration =
            Mathf.Max(0.05f, effectDuration);

        maximumAlpha =
            Mathf.Clamp01(alpha);

        pulseColor = color;
        pulseColor.a = maximumAlpha;

        elapsed = 0f;

        startScale =
            Vector3.Scale(
                playerWorldScale,
                Vector3.one *
                Mathf.Max(0.01f, startScaleMultiplier)
            );

        finalScale =
            Vector3.Scale(
                playerWorldScale,
                Vector3.one *
                Mathf.Max(
                    startScaleMultiplier,
                    finalScaleMultiplier
                )
            );

        transform.localScale = startScale;
        spriteRenderer.color = pulseColor;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        float t =
            Mathf.Clamp01(
                elapsed / duration
            );

        float eased =
            1f - Mathf.Pow(1f - t, 3f);

        transform.localScale =
            Vector3.Lerp(
                startScale,
                finalScale,
                eased
            );

        Color color = pulseColor;

        color.a =
            maximumAlpha *
            Mathf.Pow(1f - t, 2f);

        if (spriteRenderer != null)
            spriteRenderer.color = color;

        if (elapsed >= duration)
            gameObject.SetActive(false);
    }
}
