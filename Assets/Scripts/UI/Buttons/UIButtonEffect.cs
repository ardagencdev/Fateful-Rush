using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonEffect : MonoBehaviour,
    IUIScheduledVisual,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite highlightedSprite;

    [Header("Scale")]
    [Tooltip("Optional. If left empty, this button's own RectTransform is animated.")]
    [SerializeField] private RectTransform scaleTarget;

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

    [SerializeField, Min(0.000001f)]
    private float settleThreshold = 0.00005f;

    [SerializeField] private Image spriteTarget;

    private Button cachedButton;
    private Vector3 originalScale;

    private bool isHovering;
    private bool isPressed;
    private bool isSelected;

    private void Awake()
    {
        ResolveReferences();

        originalScale = scaleTarget != null
            ? scaleTarget.localScale
            : Vector3.one;

        DisableNonInteractiveTextRaycasts();
        ApplyCurrentSprite();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovering)
            return;

        isHovering = true;
        UIScaleTweenRunner.ScheduleVisual(this);
        AnimateToCurrentState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovering && !isPressed)
            return;

        isHovering = false;
        isPressed = false;

        UIScaleTweenRunner.ScheduleVisual(this);
        AnimateToCurrentState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (cachedButton != null && !cachedButton.interactable)
            return;

        if (isPressed)
            return;

        isPressed = true;
        UIScaleTweenRunner.ScheduleVisual(this);
        AnimateToCurrentState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!isPressed)
            return;

        isPressed = false;
        UIScaleTweenRunner.ScheduleVisual(this);
        AnimateToCurrentState();
    }

    public void SetSelected(bool selected)
    {
        if (!usePersistentSelectedState || isSelected == selected)
            return;

        isSelected = selected;

        UIScaleTweenRunner.ScheduleVisual(this);
        AnimateToCurrentState();
    }

    public void ResetButtonVisual()
    {
        isHovering = false;
        isPressed = false;

        ApplyCurrentSprite();

        UIScaleTweenRunner.CancelAndSnap(
            scaleTarget,
            GetRestingScale()
        );
    }

    private void AnimateToCurrentState()
    {
        if (scaleTarget == null)
            return;

        Vector3 desiredScale;

        if (isPressed)
        {
            desiredScale = originalScale * clickScale;
        }
        else if (isHovering)
        {
            desiredScale = originalScale * hoverScale;
        }
        else
        {
            desiredScale = GetRestingScale();
        }

        UIScaleTweenRunner.TweenTo(
            scaleTarget,
            desiredScale,
            transitionSpeed,
            settleThreshold
        );
    }

    private Vector3 GetRestingScale()
    {
        if (usePersistentSelectedState && isSelected)
            return originalScale * selectedScale;

        return originalScale;
    }

    public void ApplyScheduledVisualState()
    {
        ApplyCurrentSprite();
    }

    private void ApplyCurrentSprite()
    {
        bool shouldHighlight = isHovering || isPressed;

        Sprite desiredSprite = shouldHighlight && highlightedSprite != null
            ? highlightedSprite
            : normalSprite;

        if (spriteTarget == null || desiredSprite == null)
            return;

        // Avoid even a redundant property assignment on a Graphic.
        if (spriteTarget.sprite != desiredSprite)
            spriteTarget.sprite = desiredSprite;
    }

    private void ResolveReferences()
    {
        if (cachedButton == null)
            cachedButton = GetComponent<Button>();

        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        if (spriteTarget != null)
            return;

        if (cachedButton != null && cachedButton.targetGraphic is Image targetImage)
        {
            spriteTarget = targetImage;
            return;
        }

        spriteTarget = GetComponent<Image>();
    }

    private void DisableNonInteractiveTextRaycasts()
    {
        // Unity recommends disabling Raycast Target on non-interactive text
        // inside buttons. The Button's target Graphic already handles input.
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
            texts[i].raycastTarget = false;
    }

    private void OnDisable()
    {
        UIScaleTweenRunner.CancelScheduledVisual(this);
        ResetButtonVisual();
    }

    private void OnDestroy()
    {
        UIScaleTweenRunner.CancelScheduledVisual(this);
        UIScaleTweenRunner.Cancel(scaleTarget);
    }

    private void OnValidate()
    {
        hoverScale = Mathf.Max(0f, hoverScale);
        clickScale = Mathf.Max(0f, clickScale);
        selectedScale = Mathf.Max(0f, selectedScale);
        transitionSpeed = Mathf.Max(0f, transitionSpeed);
        settleThreshold = Mathf.Max(0.000001f, settleThreshold);
    }
}
