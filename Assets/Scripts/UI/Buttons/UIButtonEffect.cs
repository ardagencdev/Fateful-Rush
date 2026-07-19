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

    [Header("Persistent Selected State")]
    [SerializeField] private bool usePersistentSelectedState;
    [SerializeField] private float selectedScale = 1.05f;

    [Header("Smooth")]
    public float transitionSpeed = 10f;

    private Image image;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private bool isHovering;
    private bool isSelected;

    private void Awake()
    {
        image = GetComponent<Image>();

        originalScale = transform.localScale;
        targetScale = originalScale;

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
        targetScale = GetRestingScale();
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
            targetScale = GetRestingScale();
        }
    }

    public void SetSelected(bool selected)
    {
        if (!usePersistentSelectedState)
            return;

        isSelected = selected;

        if (!isHovering)
            targetScale = GetRestingScale();
    }

    private Vector3 GetRestingScale()
    {
        if (usePersistentSelectedState && isSelected)
            return originalScale * selectedScale;

        return originalScale;
    }

    private void SetHighlighted(bool state)
    {
        if (image == null)
            return;

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

        targetScale = GetRestingScale();
        transform.localScale = targetScale;
    }
}