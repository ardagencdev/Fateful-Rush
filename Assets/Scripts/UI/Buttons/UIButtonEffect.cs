using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite highlightedSprite;

    [Header("Scale")]
    public float hoverScale = 1.08f;
    public float clickScale = 0.95f;

    [Header("Smooth")]
    public float transitionSpeed = 10f;

    private Image image;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color targetColor;

    private bool isHovering;

    private void Awake()
    {
        image = GetComponent<Image>();
        originalScale = transform.localScale;
        targetScale = originalScale;
        targetColor = Color.white;

        if (image != null && normalSprite != null)
            image.sprite = normalSprite;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * transitionSpeed
        );

        if (image != null)
            image.color = Color.Lerp(
                image.color,
                targetColor,
                Time.unscaledDeltaTime * transitionSpeed
            );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        SetHighlighted(true);
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
       
        SetHighlighted(false);
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
       
        SetHighlighted(true);
        targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
       

        if (isHovering)
        {
            SetHighlighted(true);
            targetScale = originalScale * hoverScale;
        }
        else
        {
            SetHighlighted(false);
            targetScale = originalScale;
        }
    }

    private void SetHighlighted(bool state)
    {
        if (image == null) return;

        if (state && highlightedSprite != null)
            image.sprite = highlightedSprite;
        else if (!state && normalSprite != null)
            image.sprite = normalSprite;
    }

    private void OnDisable()
    {
        ResetButtonVisual();
    }

    public void ResetButtonVisual()
    {
        isHovering = false;

        if (image != null && normalSprite != null)
            image.sprite = normalSprite;

        transform.localScale = originalScale;
        targetScale = originalScale;
    }
}