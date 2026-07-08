using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TutorialPanelUI : MonoBehaviour
{
    public static TutorialPanelUI Instance;

    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text pageIndicatorText;
    [SerializeField] private Button startButton;

    [Header("Swipe")]
    [SerializeField] private float minSwipeDistance = 80f;

    [Header("Animation")]
    [SerializeField] private float pageSlideDistance = 160f;
    [SerializeField] private float pageAnimDuration = 0.22f;
    [SerializeField] private float startButtonAnimDuration = 0.18f;

    [Header("Panel Animation")]
    [SerializeField] private float panelFadeDuration = 0.22f;
    [SerializeField] private float panelStartScale = 0.96f;

    private System.Action onContinue;

    private string[] pages;
    private int currentPageIndex;
    private Vector2 dragStartPosition;

    private RectTransform descriptionRect;
    private CanvasGroup descriptionGroup;
    private CanvasGroup startButtonGroup;
    private Vector2 descriptionStartPos;

    private Coroutine pageRoutine;
    private Coroutine startButtonRoutine;

    private CanvasGroup panelGroup;
    private RectTransform panelRect;
    private Coroutine panelRoutine;

    private void Awake()
    {
        Instance = this;

        if (descriptionText != null)
        {
            descriptionRect = descriptionText.GetComponent<RectTransform>();
            descriptionGroup = descriptionText.GetComponent<CanvasGroup>();

            if (descriptionGroup == null)
                descriptionGroup = descriptionText.gameObject.AddComponent<CanvasGroup>();

            descriptionStartPos = descriptionRect.anchoredPosition;
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(CloseTutorial);

            startButtonGroup = startButton.GetComponent<CanvasGroup>();

            if (startButtonGroup == null)
                startButtonGroup = startButton.gameObject.AddComponent<CanvasGroup>();
        }

        if (tutorialPanel != null)
        {
            panelRect = tutorialPanel.GetComponent<RectTransform>();

            panelGroup = tutorialPanel.GetComponent<CanvasGroup>();

            if (panelGroup == null)
                panelGroup = tutorialPanel.AddComponent<CanvasGroup>();
        }

        HideInstant();
    }

    private void Update()
    {
        if (tutorialPanel == null || !tutorialPanel.activeSelf)
            return;

        HandleMouseSwipe();
        HandleTouchSwipe();
    }

    public void ShowTutorial(string title, string[] tutorialPages, System.Action continueCallback)
    {
        if (tutorialPanel == null) return;

        onContinue = continueCallback;
        pages = tutorialPages;

        if (pages == null || pages.Length == 0)
            pages = new string[] { "Tutorial text..." };

        currentPageIndex = 0;

        if (titleText != null)
            titleText.text = title;

        tutorialPanel.SetActive(true);

        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }

        if (panelRect != null)
            panelRect.localScale = Vector3.one * panelStartScale;

        RefreshPageInstant();

        if (panelRoutine != null)
            StopCoroutine(panelRoutine);

        panelRoutine = StartCoroutine(PlayPanelIntro());
    }

    private void RefreshPageInstant()
    {
        if (descriptionText != null)
            descriptionText.text = pages[currentPageIndex];

        if (descriptionRect != null)
            descriptionRect.anchoredPosition = descriptionStartPos;

        if (descriptionGroup != null)
            descriptionGroup.alpha = 1f;

        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{currentPageIndex + 1} / {pages.Length}";

        bool isLastPage = currentPageIndex == pages.Length - 1;
        SetStartButtonInstant(isLastPage);
    }

    private void HandleMouseSwipe()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            dragStartPosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector2 dragEndPosition = Mouse.current.position.ReadValue();
            TrySwipe(dragEndPosition);
        }
    }

    private void HandleTouchSwipe()
    {
        if (Touchscreen.current == null)
            return;

        var touch = Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            dragStartPosition = touch.position.ReadValue();
        }

        if (touch.press.wasReleasedThisFrame)
        {
            Vector2 dragEndPosition = touch.position.ReadValue();
            TrySwipe(dragEndPosition);
        }
    }

    private void TrySwipe(Vector2 dragEndPosition)
    {
        if (pageRoutine != null)
            return;

        float swipeX = dragEndPosition.x - dragStartPosition.x;

        if (Mathf.Abs(swipeX) < minSwipeDistance)
            return;

        if (swipeX < 0)
            NextPage();
        else
            PreviousPage();
    }

    private void NextPage()
    {
        if (currentPageIndex >= pages.Length - 1)
            return;

        int oldPage = currentPageIndex;
        currentPageIndex++;

        StartPageAnimation(oldPage, currentPageIndex, -1);
    }

    private void PreviousPage()
    {
        if (currentPageIndex <= 0)
            return;

        int oldPage = currentPageIndex;
        currentPageIndex--;

        StartPageAnimation(oldPage, currentPageIndex, 1);
    }

    private void StartPageAnimation(int oldPage, int newPage, int direction)
    {
        if (pageRoutine != null)
            StopCoroutine(pageRoutine);

        pageRoutine = StartCoroutine(PageTransitionRoutine(oldPage, newPage, direction));
    }

    private IEnumerator PageTransitionRoutine(int oldPage, int newPage, int direction)
    {
        bool newPageIsLast = newPage == pages.Length - 1;
        AnimateStartButton(newPageIsLast);

        float timer = 0f;

        while (timer < pageAnimDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / pageAnimDuration);
            float eased = EaseOutCubic(t);

            if (descriptionRect != null)
            {
                descriptionRect.anchoredPosition =
                    descriptionStartPos + Vector2.right * direction * pageSlideDistance * eased;
            }

            if (descriptionGroup != null)
                descriptionGroup.alpha = 1f - eased;

            yield return null;
        }

        if (descriptionText != null)
            descriptionText.text = pages[newPage];

        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{newPage + 1} / {pages.Length}";

        timer = 0f;

        while (timer < pageAnimDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / pageAnimDuration);
            float eased = EaseOutCubic(t);

            if (descriptionRect != null)
            {
                descriptionRect.anchoredPosition =
                    descriptionStartPos - Vector2.right * direction * pageSlideDistance * (1f - eased);
            }

            if (descriptionGroup != null)
                descriptionGroup.alpha = eased;

            yield return null;
        }

        if (descriptionRect != null)
            descriptionRect.anchoredPosition = descriptionStartPos;

        if (descriptionGroup != null)
            descriptionGroup.alpha = 1f;

        pageRoutine = null;
    }

    private void AnimateStartButton(bool show)
    {
        if (startButton == null || startButtonGroup == null)
            return;

        if (startButtonRoutine != null)
            StopCoroutine(startButtonRoutine);

        startButtonRoutine = StartCoroutine(StartButtonRoutine(show));
    }

    private IEnumerator StartButtonRoutine(bool show)
    {
        if (show)
            startButton.gameObject.SetActive(true);

        float startAlpha = startButtonGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        Vector3 startScale = startButton.transform.localScale;
        Vector3 targetScale = show ? Vector3.one : Vector3.one * 0.85f;

        float timer = 0f;

        while (timer < startButtonAnimDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / startButtonAnimDuration);
            float eased = EaseOutCubic(t);

            startButtonGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            startButton.transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

            yield return null;
        }

        startButtonGroup.alpha = targetAlpha;
        startButton.transform.localScale = targetScale;

        startButton.interactable = show;

        if (!show)
            startButton.gameObject.SetActive(false);

        startButtonRoutine = null;
    }

    private void SetStartButtonInstant(bool show)
    {
        if (startButton == null)
            return;

        startButton.gameObject.SetActive(show);
        startButton.interactable = show;

        if (startButtonGroup != null)
            startButtonGroup.alpha = show ? 1f : 0f;

        startButton.transform.localScale = show ? Vector3.one : Vector3.one * 0.85f;
    }

    public void CloseTutorial()
    {
        HideInstant();

        onContinue?.Invoke();
        onContinue = null;
    }

    public void HideInstant()
    {
        if (panelRoutine != null)
            StopCoroutine(panelRoutine);

        if (panelGroup != null)
            panelGroup.alpha = 0f;

        if (panelRect != null)
            panelRect.localScale = Vector3.one * panelStartScale;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private IEnumerator PlayPanelIntro()
    {
        panelGroup.alpha = 0f;
        panelRect.localScale = Vector3.one * panelStartScale;

        float timer = 0f;

        while (timer < panelFadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / panelFadeDuration);
            float eased = t * t * (3f - 2f * t); // SmoothStep

            panelGroup.alpha = eased;
            panelRect.localScale = Vector3.Lerp(
                Vector3.one * panelStartScale,
                Vector3.one,
                eased);

            yield return null;
        }

        panelGroup.alpha = 1f;
        panelRect.localScale = Vector3.one;

        panelRoutine = null;

        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
    }
}