using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PausePanelTransition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Image backdropImage;
    [SerializeField] private RectTransform pauseMainPanel;
    [SerializeField] private CanvasGroup pauseMainCanvasGroup;

    [Header("Objective")]
    [SerializeField] private bool showObjective = true;
    [SerializeField] private GameObject objectiveRoot;
    [SerializeField] private TextMeshProUGUI objectiveTitleText;
    [SerializeField] private TextMeshProUGUI objectiveValueText;

    [Header("Pause Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Backdrop")]
    [SerializeField, Range(0f, 1f)]
    private float targetBackdropAlpha = 0.68f;

    [SerializeField, Min(0.05f)]
    private float backdropFadeDuration = 0.18f;

    [Header("Objective Intro")]
    [SerializeField, Min(0f)]
    private float objectiveSlideDistance = 42f;

    [SerializeField, Min(0.05f)]
    private float objectiveIntroDuration = 0.16f;

    [Header("Button Intro")]
    [Tooltip("Continue starts first.")]
    [SerializeField, Min(0f)]
    private float continueDelay = 0.015f;

    [Tooltip("Restart starts shortly after Continue.")]
    [SerializeField, Min(0f)]
    private float restartDelay = 0.055f;

    [Tooltip("Options and Main Menu start together.")]
    [SerializeField, Min(0f)]
    private float bottomRowDelay = 0.095f;

    [SerializeField, Min(0.05f)]
    private float buttonIntroDuration = 0.115f;

    [SerializeField, Range(0.7f, 1f)]
    private float buttonStartScale = 0.91f;

    [Header("Close")]
    [SerializeField, Min(0.05f)]
    private float closeDuration = 0.15f;

    [SerializeField, Range(0.85f, 1f)]
    private float endScale = 0.98f;

    private Coroutine routine;

    private RectTransform objectiveRect;
    private CanvasGroup objectiveCanvasGroup;
    private Vector2 objectiveTargetPosition;
    private Vector3 objectiveTargetScale;

    private ButtonVisual continueVisual;
    private ButtonVisual restartVisual;
    private ButtonVisual optionsVisual;
    private ButtonVisual mainMenuVisual;

    public bool IsTransitioning => routine != null;

    private sealed class ButtonVisual
    {
        public RectTransform rect;
        public CanvasGroup canvasGroup;
        public Vector3 targetScale;
    }

    public void Show()
    {
        StopCurrent();

        if (pausePanel == null)
            return;

        pausePanel.SetActive(true);

        RefreshObjective();
        ResolvePauseButtons();
        CacheIntroVisuals();
        PrepareIntroVisuals();

        // Input is enabled immediately. The player never has to wait
        // for the short intro animation before pressing a button.
        if (pauseMainCanvasGroup != null)
        {
            pauseMainCanvasGroup.alpha = 1f;
            pauseMainCanvasGroup.interactable = true;
            pauseMainCanvasGroup.blocksRaycasts = true;
        }

        routine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        if (pausePanel == null || !pausePanel.activeSelf)
        {
            SetInstant(false);
            return;
        }

        StopCurrent();
        routine = StartCoroutine(HideRoutine());
    }

    public void SetInstant(bool visible)
    {
        StopCurrent();

        if (pausePanel == null)
            return;

        if (visible)
        {
            pausePanel.SetActive(true);

            RefreshObjective();
            ResolvePauseButtons();
            CacheIntroVisuals();
            RestoreIntroVisuals();

            SetBackdropAlpha(targetBackdropAlpha);

            if (pauseMainCanvasGroup != null)
            {
                pauseMainCanvasGroup.alpha = 1f;
                pauseMainCanvasGroup.interactable = true;
                pauseMainCanvasGroup.blocksRaycasts = true;
            }

            if (pauseMainPanel != null)
                pauseMainPanel.localScale = Vector3.one;
        }
        else
        {
            SetBackdropAlpha(0f);

            if (pauseMainCanvasGroup != null)
            {
                pauseMainCanvasGroup.alpha = 1f;
                pauseMainCanvasGroup.interactable = false;
                pauseMainCanvasGroup.blocksRaycasts = false;
            }

            if (pauseMainPanel != null)
                pauseMainPanel.localScale = Vector3.one;

            RestoreIntroVisuals();
            pausePanel.SetActive(false);
        }
    }

    private IEnumerator ShowRoutine()
    {
        if (pauseMainPanel != null)
            pauseMainPanel.localScale = Vector3.one;

        float totalDuration = Mathf.Max(
            backdropFadeDuration,
            objectiveIntroDuration,
            continueDelay + buttonIntroDuration,
            restartDelay + buttonIntroDuration,
            bottomRowDelay + buttonIntroDuration
        );

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float backdropT = Mathf.Clamp01(
                elapsed / Mathf.Max(0.001f, backdropFadeDuration)
            );

            SetBackdropAlpha(
                Mathf.Lerp(
                    0f,
                    targetBackdropAlpha,
                    EaseOutCubic(backdropT)
                )
            );

            AnimateObjective(elapsed);

            AnimateButton(
                continueVisual,
                elapsed,
                continueDelay
            );

            AnimateButton(
                restartVisual,
                elapsed,
                restartDelay
            );

            // Bottom row arrives together.
            AnimateButton(
                optionsVisual,
                elapsed,
                bottomRowDelay
            );

            AnimateButton(
                mainMenuVisual,
                elapsed,
                bottomRowDelay
            );

            yield return null;
        }

        SetBackdropAlpha(targetBackdropAlpha);
        RestoreIntroVisuals();

        routine = null;
    }

    private IEnumerator HideRoutine()
    {
        if (pauseMainCanvasGroup != null)
        {
            pauseMainCanvasGroup.interactable = false;
            pauseMainCanvasGroup.blocksRaycasts = false;
        }

        float startBackdrop =
            backdropImage != null
                ? backdropImage.color.a
                : targetBackdropAlpha;

        Vector3 startScale =
            pauseMainPanel != null
                ? pauseMainPanel.localScale
                : Vector3.one;

        float elapsed = 0f;

        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / closeDuration
            );

            float eased = EaseInCubic(t);

            SetBackdropAlpha(
                Mathf.Lerp(
                    startBackdrop,
                    0f,
                    eased
                )
            );

            if (pauseMainCanvasGroup != null)
            {
                pauseMainCanvasGroup.alpha =
                    Mathf.Lerp(1f, 0f, eased);
            }

            if (pauseMainPanel != null)
            {
                pauseMainPanel.localScale =
                    Vector3.LerpUnclamped(
                        startScale,
                        Vector3.one * endScale,
                        eased
                    );
            }

            yield return null;
        }

        routine = null;
        SetInstant(false);
    }

    private void AnimateObjective(float elapsed)
    {
        if (objectiveRect == null ||
            objectiveCanvasGroup == null ||
            !objectiveRect.gameObject.activeSelf)
        {
            return;
        }

        float t = Mathf.Clamp01(
            elapsed / Mathf.Max(0.001f, objectiveIntroDuration)
        );

        float eased = EaseOutCubic(t);

        objectiveCanvasGroup.alpha = eased;

        objectiveRect.anchoredPosition =
            Vector2.LerpUnclamped(
                objectiveTargetPosition +
                Vector2.up * objectiveSlideDistance,
                objectiveTargetPosition,
                eased
            );

        objectiveRect.localScale = objectiveTargetScale;
    }

    private void AnimateButton(
        ButtonVisual visual,
        float elapsed,
        float delay)
    {
        if (visual == null ||
            visual.rect == null ||
            visual.canvasGroup == null)
        {
            return;
        }

        float localTime = elapsed - delay;

        if (localTime <= 0f)
        {
            visual.canvasGroup.alpha = 0f;

            visual.rect.localScale =
                visual.targetScale * buttonStartScale;

            return;
        }

        float t = Mathf.Clamp01(
            localTime / Mathf.Max(0.001f, buttonIntroDuration)
        );

        float alphaT = EaseOutCubic(t);
        float scaleT = EaseOutBackSubtle(t);

        visual.canvasGroup.alpha = alphaT;

        visual.rect.localScale =
            Vector3.LerpUnclamped(
                visual.targetScale * buttonStartScale,
                visual.targetScale,
                scaleT
            );
    }

    private void CacheIntroVisuals()
    {
        if (objectiveRoot != null)
        {
            objectiveRect =
                objectiveRoot.transform as RectTransform;

            if (objectiveRect != null)
            {
                objectiveCanvasGroup =
                    GetOrAddCanvasGroup(objectiveRoot);

                objectiveTargetPosition =
                    objectiveRect.anchoredPosition;

                objectiveTargetScale =
                    objectiveRect.localScale;
            }
        }

        continueVisual =
            CreateButtonVisual(continueButton);

        restartVisual =
            CreateButtonVisual(restartButton);

        optionsVisual =
            CreateButtonVisual(optionsButton);

        mainMenuVisual =
            CreateButtonVisual(mainMenuButton);
    }

    private void PrepareIntroVisuals()
    {
        SetBackdropAlpha(0f);

        if (objectiveRect != null &&
            objectiveCanvasGroup != null &&
            objectiveRect.gameObject.activeSelf)
        {
            objectiveCanvasGroup.alpha = 0f;

            objectiveRect.anchoredPosition =
                objectiveTargetPosition +
                Vector2.up * objectiveSlideDistance;

            objectiveRect.localScale =
                objectiveTargetScale;
        }

        PrepareButtonVisual(continueVisual);
        PrepareButtonVisual(restartVisual);
        PrepareButtonVisual(optionsVisual);
        PrepareButtonVisual(mainMenuVisual);
    }

    private void RestoreIntroVisuals()
    {
        if (objectiveRect != null &&
            objectiveCanvasGroup != null)
        {
            objectiveCanvasGroup.alpha = 1f;
            objectiveRect.anchoredPosition =
                objectiveTargetPosition;
            objectiveRect.localScale =
                objectiveTargetScale;
        }

        RestoreButtonVisual(continueVisual);
        RestoreButtonVisual(restartVisual);
        RestoreButtonVisual(optionsVisual);
        RestoreButtonVisual(mainMenuVisual);

        if (pauseMainCanvasGroup != null)
            pauseMainCanvasGroup.alpha = 1f;
    }

    private void PrepareButtonVisual(ButtonVisual visual)
    {
        if (visual == null ||
            visual.rect == null ||
            visual.canvasGroup == null)
        {
            return;
        }

        visual.canvasGroup.alpha = 0f;

        // Keep raycasts enabled from frame 1 so the player can click
        // immediately even while the visual animation is running.
        visual.canvasGroup.interactable = true;
        visual.canvasGroup.blocksRaycasts = true;

        visual.rect.localScale =
            visual.targetScale * buttonStartScale;
    }

    private static void RestoreButtonVisual(ButtonVisual visual)
    {
        if (visual == null ||
            visual.rect == null ||
            visual.canvasGroup == null)
        {
            return;
        }

        visual.canvasGroup.alpha = 1f;
        visual.canvasGroup.interactable = true;
        visual.canvasGroup.blocksRaycasts = true;
        visual.rect.localScale = visual.targetScale;
    }

    private static ButtonVisual CreateButtonVisual(Button button)
    {
        if (button == null)
            return null;

        RectTransform rect =
            button.transform as RectTransform;

        if (rect == null)
            return null;

        return new ButtonVisual
        {
            rect = rect,
            canvasGroup =
                GetOrAddCanvasGroup(button.gameObject),
            targetScale = rect.localScale
        };
    }

    private static CanvasGroup GetOrAddCanvasGroup(
        GameObject target)
    {
        CanvasGroup group =
            target.GetComponent<CanvasGroup>();

        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        return group;
    }

    private void ResolvePauseButtons()
    {
        if (pauseMainPanel == null)
            return;

        Button[] buttons =
            pauseMainPanel.GetComponentsInChildren<Button>(true);

        if (continueButton == null)
            continueButton =
                FindButton(buttons, "CONTINUE");

        if (restartButton == null)
            restartButton =
                FindButton(buttons, "RESTART");

        if (optionsButton == null)
            optionsButton =
                FindButton(buttons, "OPTIONS");

        if (mainMenuButton == null)
            mainMenuButton =
                FindButton(buttons, "MAIN MENU");
    }

    private static Button FindButton(
        Button[] buttons,
        string expectedLabel)
    {
        if (buttons == null)
            return null;

        string compactExpected =
            expectedLabel.Replace(" ", string.Empty);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            string compactName =
                button.name.Replace(" ", string.Empty);

            if (compactName.IndexOf(
                    compactExpected,
                    System.StringComparison.OrdinalIgnoreCase
                ) >= 0)
            {
                return button;
            }
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>(true);

            if (label != null &&
                string.Equals(
                    label.text.Trim(),
                    expectedLabel,
                    System.StringComparison.OrdinalIgnoreCase
                ))
            {
                return button;
            }
        }

        return null;
    }

    private void RefreshObjective()
    {
        if (!showObjective)
        {
            SetObjectiveVisible(false);
            return;
        }

        LevelConfig level = ResolveCurrentLevel();

        if (level == null)
        {
            SetObjectiveVisible(false);
            return;
        }

        if (objectiveTitleText != null)
            objectiveTitleText.text = "OBJECTIVE";

        if (objectiveValueText != null)
            objectiveValueText.text =
                BuildObjectiveText(level);

        SetObjectiveVisible(true);
    }

    private static LevelConfig ResolveCurrentLevel()
    {
        LevelManager[] managers =
            UnityFindCompat.FindObjectsByType<LevelManager>(
                FindObjectsInactive.Include
            );

        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null &&
                managers[i].currentLevel != null)
            {
                return managers[i].currentLevel;
            }
        }

        return SelectedLevelData.selectedLevel;
    }

    private static string BuildObjectiveText(
        LevelConfig level)
    {
        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                return
                    $"REACH SCORE: {level.SafeWinScore}";

            case WinConditionType.SurviveTime:
                return
                    $"SURVIVE: " +
                    FormatSeconds(level.SafeTimeLimit);

            case WinConditionType.ReachScoreWithinTime:
                return
                    $"REACH SCORE: {level.SafeWinScore}  ·  " +
                    FormatSeconds(level.SafeTimeLimit);

            default:
                return "COMPLETE THE MISSION";
        }
    }

    private static string FormatSeconds(float seconds)
    {
        float safeSeconds =
            Mathf.Max(0f, seconds);

        int roundedSeconds =
            Mathf.RoundToInt(safeSeconds);

        if (Mathf.Approximately(
                safeSeconds,
                roundedSeconds
            ))
        {
            return $"{roundedSeconds}s";
        }

        return $"{safeSeconds:0.#}s";
    }

    private void SetObjectiveVisible(bool visible)
    {
        if (objectiveRoot != null)
        {
            objectiveRoot.SetActive(visible);
            return;
        }

        if (objectiveTitleText != null)
            objectiveTitleText.gameObject.SetActive(visible);

        if (objectiveValueText != null)
            objectiveValueText.gameObject.SetActive(visible);
    }

    private void SetBackdropAlpha(float alpha)
    {
        if (backdropImage == null)
            return;

        Color color = backdropImage.color;
        color.a = alpha;
        backdropImage.color = color;
    }

    private void StopCurrent()
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }

    private static float EaseOutCubic(float t)
    {
        float x =
            1f - Mathf.Clamp01(t);

        return 1f - x * x * x;
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private static float EaseOutBackSubtle(float t)
    {
        t = Mathf.Clamp01(t);

        const float c1 = 0.72f;
        const float c3 = c1 + 1f;

        float x = t - 1f;

        return 1f +
               c3 * x * x * x +
               c1 * x * x;
    }
}