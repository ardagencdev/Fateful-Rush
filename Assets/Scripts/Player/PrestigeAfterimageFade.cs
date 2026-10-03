using UnityEngine;

public class PrestigeAfterimageFade :
    MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float lifetime;
    private float elapsed;
    private Color startColor;

    public SpriteRenderer Renderer => spriteRenderer;

    public void Prepare(SpriteRenderer renderer)
    {
        spriteRenderer = renderer;
    }

    public void Initialize(
        SpriteRenderer renderer,
        float duration)
    {
        spriteRenderer = renderer;
        elapsed = 0f;

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

            spriteRenderer.color = color;
        }

        if (elapsed >= lifetime)
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        elapsed = 0f;
    }
}
