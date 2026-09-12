using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public sealed class OptionsCategorySwitcher : MonoBehaviour
{
    private const string SavedCategoryKey = "OptionsLastCategory";

    private enum OptionsCategory
    {
        Audio = 0,
        Game = 1,
        Language = 2
    }

    [Header("Pages")]
    [Tooltip("Swipe sadece bu alanın icinde baslar.")]
    [SerializeField] private RectTransform swipeArea;
    [SerializeField] private RectTransform audioPage;
    [SerializeField] private RectTransform gamePage;
    [SerializeField] private RectTransform languagePage;

    [Header("Category Button")]
    [SerializeField] private Button switchCategoryButton;
    [SerializeField] private TMP_Text switchCategoryButtonText;
    [SerializeField] private string audioButtonLabel = "AUDIO";
    [SerializeField] private string gameButtonLabel = "GAME";
    [SerializeField] private string languageButtonLabel = "LANGUAGE";

    [Header("Page Indicator")]
    [SerializeField] private TMP_Text pageIndicatorText;

    [Header("Swipe")]
    [SerializeField] private bool swipeEnabled = true;
    [SerializeField, Min(1f)] private float minSwipeDistance = 90f;
    [SerializeField, Range(0f, 1f)]
    private float maxVerticalToHorizontalRatio = 0.75f;

    [Header("Transition")]
    [SerializeField, Min(1f)] private float slideDistance = 850f;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.22f;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private OptionsCategory currentCategory;
    private CanvasGroup audioCanvasGroup;
    private CanvasGroup gameCanvasGroup;
    private CanvasGroup languageCanvasGroup;
    private Canvas rootCanvas;
    private Coroutine transitionRoutine;

    private Vector2 pointerStartPosition;
    private bool isTrackingPointer;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();

        audioCanvasGroup = GetOrAddCanvasGroup(audioPage);
        gameCanvasGroup = GetOrAddCanvasGroup(gamePage);
        languageCanvasGroup = GetOrAddCanvasGroup(languagePage);

        if (switchCategoryButton != null)
        {
            switchCategoryButton.onClick.RemoveListener(ToggleCategoryFromButton);
            switchCategoryButton.onClick.AddListener(ToggleCategoryFromButton);
        }
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnLocaleChanged;

        StopTransition();
        currentCategory = LoadSavedCategory();
        ApplyCategoryInstant(currentCategory);
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnLocaleChanged;

        StopTransition();
        isTrackingPointer = false;
    }

    private void OnLocaleChanged(Locale locale)
    {
        UpdateSwitchButtonText();
    }

    private void OnDestroy()
    {
        if (switchCategoryButton != null)
            switchCategoryButton.onClick.RemoveListener(ToggleCategoryFromButton);
    }

    private void Update()
    {
        if (!swipeEnabled || transitionRoutine != null)
            return;

        bool touchHandled = HandleTouchSwipe();
        if (!touchHandled)
            HandleMouseSwipe();
    }

    public void ShowAudio()
    {
        SwitchToCategory(OptionsCategory.Audio);
    }

    public void ShowGame()
    {
        SwitchToCategory(OptionsCategory.Game);
    }

    public void ShowLanguage()
    {
        SwitchToCategory(OptionsCategory.Language);
    }

    public void ToggleCategory()
    {
        ToggleCategoryFromButton();
    }

    private void ToggleCategoryFromButton()
    {
        SwitchToCategory(GetNextCategory(currentCategory), 1);
    }

    private bool HandleTouchSwipe()
    {
        if (Touchscreen.current == null)
            return false;

        var touch = Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            BeginPointerTracking(touch.position.ReadValue());
            return true;
        }

        if (touch.press.wasReleasedThisFrame)
        {
            EndPointerTracking(touch.position.ReadValue());
            return true;
        }

        return touch.press.isPressed;
    }

    private void HandleMouseSwipe()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            BeginPointerTracking(Mouse.current.position.ReadValue());

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            EndPointerTracking(Mouse.current.position.ReadValue());
    }

    private void BeginPointerTracking(Vector2 screenPosition)
    {
        isTrackingPointer = false;

        if (!IsInsideSwipeArea(screenPosition))
            return;

        if (IsPointerOverSelectable(screenPosition))
            return;

        pointerStartPosition = screenPosition;
        isTrackingPointer = true;
    }

    private void EndPointerTracking(Vector2 screenPosition)
    {
        if (!isTrackingPointer)
            return;

        isTrackingPointer = false;

        Vector2 swipeDelta = screenPosition - pointerStartPosition;
        float horizontalDistance = Mathf.Abs(swipeDelta.x);
        float verticalDistance = Mathf.Abs(swipeDelta.y);

        if (horizontalDistance < minSwipeDistance)
            return;

        if (verticalDistance > horizontalDistance * maxVerticalToHorizontalRatio)
            return;

        bool swipedLeft = swipeDelta.x < 0f;

        OptionsCategory targetCategory = swipedLeft
            ? GetNextCategory(currentCategory)
            : GetPreviousCategory(currentCategory);

        int swipeDirection = swipedLeft ? 1 : -1;
        SwitchToCategory(targetCategory, swipeDirection);
    }

    private void SwitchToCategory(
        OptionsCategory targetCategory,
        int? directionOverride = null)
    {
        if (transitionRoutine != null || targetCategory == currentCategory)
            return;

        if (switchCategoryButton != null)
            switchCategoryButton.interactable = false;

        OptionsCategory previousCategory = currentCategory;

        int direction = directionOverride ?? GetDirection(previousCategory, targetCategory);

        currentCategory = targetCategory;
        SaveCurrentCategory();
        UpdateSwitchButtonText();
        UpdatePageIndicator();

        transitionRoutine = StartCoroutine(
            AnimateCategorySwitch(previousCategory, targetCategory, direction)
        );
    }

    private IEnumerator AnimateCategorySwitch(
        OptionsCategory previousCategory,
        OptionsCategory targetCategory,
        int direction)
    {
        RectTransform previousPage = GetPage(previousCategory);
        RectTransform targetPage = GetPage(targetCategory);
        CanvasGroup previousGroup = GetCanvasGroup(previousCategory);
        CanvasGroup targetGroup = GetCanvasGroup(targetCategory);

        if (previousPage == null || targetPage == null ||
            previousGroup == null || targetGroup == null)
        {
            ApplyCategoryInstant(targetCategory);
            transitionRoutine = null;

            if (switchCategoryButton != null)
                switchCategoryButton.interactable = true;

            yield break;
        }

        previousPage.gameObject.SetActive(true);
        targetPage.gameObject.SetActive(true);

        previousPage.anchoredPosition = Vector2.zero;
        targetPage.anchoredPosition = Vector2.right * slideDistance * direction;

        SetCanvasGroupState(previousGroup, 1f, false);
        SetCanvasGroupState(targetGroup, 0f, false);

        float timer = 0f;
        float safeDuration = Mathf.Max(0.01f, transitionDuration);
        Vector2 previousTargetPosition = Vector2.left * slideDistance * direction;

        while (timer < safeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(timer / safeDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            previousPage.anchoredPosition = Vector2.LerpUnclamped(
                Vector2.zero,
                previousTargetPosition,
                easedProgress
            );

            targetPage.anchoredPosition = Vector2.LerpUnclamped(
                Vector2.right * slideDistance * direction,
                Vector2.zero,
                easedProgress
            );

            previousGroup.alpha = 1f - easedProgress;
            targetGroup.alpha = easedProgress;

            yield return null;
        }

        previousPage.anchoredPosition = Vector2.zero;
        targetPage.anchoredPosition = Vector2.zero;

        SetCanvasGroupState(previousGroup, 0f, false);
        previousPage.gameObject.SetActive(false);

        SetCanvasGroupState(targetGroup, 1f, true);
        targetPage.gameObject.SetActive(true);

        transitionRoutine = null;

        if (switchCategoryButton != null)
            switchCategoryButton.interactable = true;
    }

    private void ApplyCategoryInstant(OptionsCategory category)
    {
        ApplyPageInstant(
            audioPage,
            audioCanvasGroup,
            category == OptionsCategory.Audio
        );

        ApplyPageInstant(
            gamePage,
            gameCanvasGroup,
            category == OptionsCategory.Game
        );

        ApplyPageInstant(
            languagePage,
            languageCanvasGroup,
            category == OptionsCategory.Language
        );

        UpdateSwitchButtonText();
        UpdatePageIndicator();
    }

    private static void ApplyPageInstant(
        RectTransform page,
        CanvasGroup canvasGroup,
        bool visible)
    {
        if (page == null || canvasGroup == null)
            return;

        page.anchoredPosition = Vector2.zero;
        page.gameObject.SetActive(visible);

        SetCanvasGroupState(
            canvasGroup,
            visible ? 1f : 0f,
            visible
        );
    }

    private void UpdateSwitchButtonText()
    {
        if (switchCategoryButtonText == null)
            return;

        OptionsCategory target =
            GetNextCategory(currentCategory);

        switchCategoryButtonText.text =
            GetLocalizedCategoryLabel(target);
    }

    private string GetLocalizedCategoryLabel(
        OptionsCategory category)
    {
        switch (category)
        {
            case OptionsCategory.Audio:
                return GetLocalizedString(
                    "options.audio",
                    audioButtonLabel
                );

            case OptionsCategory.Game:
                return GetLocalizedString(
                    "options.game",
                    gameButtonLabel
                );

            case OptionsCategory.Language:
                return GetLocalizedString(
                    "options.language",
                    languageButtonLabel
                );

            default:
                return string.Empty;
        }
    }

    private static string GetLocalizedString(
        string key,
        string fallback)
    {
        try
        {
            var operation =
                LocalizationSettings.StringDatabase
                    .GetLocalizedStringAsync(
                        "UI",
                        key
                    );

            string value =
                operation.IsDone
                    ? operation.Result
                    : operation.WaitForCompletion();

            if (!string.IsNullOrWhiteSpace(value) &&
                !value.StartsWith(
                    "No translation found",
                    System.StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith(
                    "No table found",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }
        catch
        {
            // Keep the options menu usable if localization is not ready.
        }

        return fallback;
    }

    private void UpdatePageIndicator()
    {
        if (pageIndicatorText == null)
            return;

        switch (currentCategory)
        {
            case OptionsCategory.Audio:
                pageIndicatorText.text = "1 / 3";
                break;

            case OptionsCategory.Game:
                pageIndicatorText.text = "2 / 3";
                break;

            case OptionsCategory.Language:
                pageIndicatorText.text = "3 / 3";
                break;
        }
    }

    private OptionsCategory LoadSavedCategory()
    {
        int savedValue = PlayerPrefs.GetInt(
            SavedCategoryKey,
            (int)OptionsCategory.Audio
        );

        if (savedValue < (int)OptionsCategory.Audio ||
            savedValue > (int)OptionsCategory.Language)
        {
            return OptionsCategory.Audio;
        }

        return (OptionsCategory)savedValue;
    }

    private void SaveCurrentCategory()
    {
        PlayerPrefs.SetInt(SavedCategoryKey, (int)currentCategory);
        PlayerPrefs.Save();
    }

    private static OptionsCategory GetNextCategory(OptionsCategory category)
    {
        switch (category)
        {
            case OptionsCategory.Audio:
                return OptionsCategory.Game;
            case OptionsCategory.Game:
                return OptionsCategory.Language;
            default:
                return OptionsCategory.Audio;
        }
    }

    private static OptionsCategory GetPreviousCategory(OptionsCategory category)
    {
        switch (category)
        {
            case OptionsCategory.Audio:
                return OptionsCategory.Language;
            case OptionsCategory.Game:
                return OptionsCategory.Audio;
            default:
                return OptionsCategory.Game;
        }
    }

    private static int GetDirection(OptionsCategory from, OptionsCategory to)
    {
        if (GetNextCategory(from) == to)
            return 1;

        if (GetPreviousCategory(from) == to)
            return -1;

        return 1;
    }

    private bool IsInsideSwipeArea(Vector2 screenPosition)
    {
        if (swipeArea == null)
            return true;

        return RectTransformUtility.RectangleContainsScreenPoint(
            swipeArea,
            screenPosition,
            GetUICamera()
        );
    }

    private bool IsPointerOverSelectable(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject == null)
                continue;

            Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();

            if (selectable != null && selectable.IsInteractable())
                return true;
        }

        return false;
    }

    private Camera GetUICamera()
    {
        if (rootCanvas == null ||
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return rootCanvas.worldCamera;
    }

    private RectTransform GetPage(OptionsCategory category)
    {
        switch (category)
        {
            case OptionsCategory.Audio:
                return audioPage;
            case OptionsCategory.Game:
                return gamePage;
            case OptionsCategory.Language:
                return languagePage;
            default:
                return null;
        }
    }

    private CanvasGroup GetCanvasGroup(OptionsCategory category)
    {
        switch (category)
        {
            case OptionsCategory.Audio:
                return audioCanvasGroup;
            case OptionsCategory.Game:
                return gameCanvasGroup;
            case OptionsCategory.Language:
                return languageCanvasGroup;
            default:
                return null;
        }
    }

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform page)
    {
        if (page == null)
            return null;

        CanvasGroup canvasGroup = page.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = page.gameObject.AddComponent<CanvasGroup>();

        return canvasGroup;
    }

    private static void SetCanvasGroupState(
        CanvasGroup canvasGroup,
        float alpha,
        bool interactable)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    private void StopTransition()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (switchCategoryButton != null)
            switchCategoryButton.interactable = true;
    }

    private void OnValidate()
    {
        minSwipeDistance = Mathf.Max(1f, minSwipeDistance);
        slideDistance = Mathf.Max(1f, slideDistance);
        transitionDuration = Mathf.Max(0.01f, transitionDuration);
        maxVerticalToHorizontalRatio = Mathf.Clamp01(maxVerticalToHorizontalRatio);
    }
}
