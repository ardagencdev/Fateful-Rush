using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    [Min(0f)]
    public float hoverScale = 1.08f;

    [Min(0f)]
    public float clickScale = 0.95f;

    [Header("Persistent Selected State")]
    [SerializeField]
    private bool usePersistentSelectedState;

    [SerializeField, Min(0f)]
    private float selectedScale = 1.05f;

    [Header("Smooth")]
    [Min(0f)]
    public float transitionSpeed = 10f;

    private Image image;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private bool isHovering;
    private bool isPressed;
    private bool isSelected;

    private void Awake()
    {
        RefreshReferences();

        originalScale = transform.localScale;
        targetScale = GetRestingScale();

        ApplyCurrentSprite();
    }

    private void Update()
    {
        if (transitionSpeed <= 0f)
        {
            transform.localScale = targetScale;
            return;
        }

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * transitionSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        ApplyCurrentSprite();
        RefreshTargetScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        isPressed = false;

        ApplyCurrentSprite();
        RefreshTargetScale();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isPressed = true;

        ApplyCurrentSprite();
        RefreshTargetScale();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isPressed = false;

        ApplyCurrentSprite();
        RefreshTargetScale();
    }

    public void SetSelected(bool selected)
    {
        if (!usePersistentSelectedState)
            return;

        isSelected = selected;

        ApplyCurrentSprite();
        RefreshTargetScale();
    }

    public void ResetButtonVisual()
    {
        isHovering = false;
        isPressed = false;

        ApplyCurrentSprite();

        targetScale = GetRestingScale();
        transform.localScale = targetScale;
    }

    private void RefreshTargetScale()
    {
        if (isPressed)
        {
            targetScale = originalScale * clickScale;
            return;
        }

        if (isHovering)
        {
            targetScale = originalScale * hoverScale;
            return;
        }

        targetScale = GetRestingScale();
    }

    private Vector3 GetRestingScale()
    {
        if (usePersistentSelectedState && isSelected)
            return originalScale * selectedScale;

        return originalScale;
    }

    private void ApplyCurrentSprite()
    {
        bool shouldHighlight = isHovering || isPressed;
        SetHighlighted(shouldHighlight);
    }

    private void SetHighlighted(bool highlighted)
    {
        if (image == null)
            return;

        if (highlighted && highlightedSprite != null)
        {
            image.sprite = highlightedSprite;
            return;
        }

        if (normalSprite != null)
            image.sprite = normalSprite;
    }

    private void RefreshReferences()
    {
        if (image == null)
            image = GetComponent<Image>();
    }

    private void OnDisable()
    {
        ResetButtonVisual();
    }

    private void OnValidate()
    {
        hoverScale = Mathf.Max(0f, hoverScale);
        clickScale = Mathf.Max(0f, clickScale);
        selectedScale = Mathf.Max(0f, selectedScale);
        transitionSpeed = Mathf.Max(0f, transitionSpeed);
    }
}