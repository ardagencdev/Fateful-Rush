using UnityEngine;

public class SpecialSkinCoinBurstSprite :
    MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private float duration;
    private float elapsed;
    private float maxAlpha;

    private Vector3 startScale;
    private Vector3 finalScale;
    private Color activeTintColor = Color.white;

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
        float alpha,
        Color tintColor)
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

        activeTintColor = MakeRenderableTint(tintColor);

        Color startColor = activeTintColor;
        startColor.a = maxAlpha;
        spriteRenderer.color = startColor;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }


    private static Color MakeRenderableTint(Color color)
    {
        float maximumChannel = Mathf.Max(
            color.r,
            Mathf.Max(color.g, color.b)
        );

        if (maximumChannel <= 0.001f)
            color = Color.white;

        color.a = 1f;
        return color;
    }

    private void Update()
    {
        elapsed +=
            Time.unscaledDeltaTime;

        float t =
            Mathf.Clamp01(
                elapsed / duration
            );

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

        float fade =
            1f - t;

        fade *= fade;

        if (spriteRenderer != null)
        {
            Color color = activeTintColor;
            color.a = maxAlpha * fade;
            spriteRenderer.color = color;
        }

        if (elapsed >= duration)
            gameObject.SetActive(false);
    }
}
