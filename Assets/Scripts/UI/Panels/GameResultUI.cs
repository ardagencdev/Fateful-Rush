using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameResultUI : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private GameObject resultPanel;

    [Header("UI Groups")]
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject loseUI;

    [Header("Win Values")]
    [SerializeField] private TextMeshProUGUI winScoreValue;
    [SerializeField] private TextMeshProUGUI winTimeValue;

    [Header("Lose Values")]
    [SerializeField] private TextMeshProUGUI destroyedByText;
    [SerializeField] private TextMeshProUGUI loseScoreValue;
    [SerializeField] private TextMeshProUGUI loseSurvivedValue;

    [Header("Level Mode")]
    [SerializeField] private GameObject nextLevelButton;
    [SerializeField] private LevelConfig[] levels;
    [SerializeField] private string gameSceneName = "a";

    [Header("Buttons")]
    [SerializeField] private GameObject tryAgainButton;
    [SerializeField] private GameObject menuButton;

    [Header("Skin Unlock Reward")]
    [SerializeField] private PlayerSkinCatalog playerSkinCatalog;
    [SerializeField] private GameObject skinUnlockUI;
    [SerializeField] private TextMeshProUGUI skinUnlockedTitleText;
    [SerializeField] private TextMeshProUGUI unlockedSkinNameText;
    [SerializeField] private CanvasGroup skinUnlockCanvasGroup;
    [SerializeField] private RectTransform skinUnlockRect;

    [Header("Skin Unlock Animation")]
    [SerializeField, Min(0f)] private float skinUnlockDelay = 0.25f;

    [Tooltip("New Skin panelinin Victory SFX bitmeden ne kadar once gelmeye baslayacagi.")]
    [SerializeField, Min(0f)] private float skinUnlockWinSoundTailOverlap = 0.65f;

    [SerializeField, Min(0.05f)] private float skinUnlockAnimationDuration = 0.42f;
    [SerializeField, Min(0f)] private float skinUnlockSlideDistance = 180f;

    private Coroutine skinUnlockRoutine;
    private Vector2 skinUnlockRestPosition;
    private bool skinUnlockPositionCached;

    private LevelManager levelManager;

    // Cached result metric layout.
    // Survive Time missions temporarily center the time metric,
    // then the original Inspector positions are restored for every other mode.
    private RectTransform winTimeLabelRect;
    private RectTransform winTimeValueRect;
    private RectTransform loseSurvivedLabelRect;
    private RectTransform loseSurvivedValueRect;

    private Vector2 winTimeLabelDefaultPosition;
    private Vector2 winTimeValueDefaultPosition;
    private Vector2 loseSurvivedLabelDefaultPosition;
    private Vector2 loseSurvivedValueDefaultPosition;

    private bool metricLayoutCached;
    private bool isSceneChangeRequested;

    private void Awake()
    {
        levelManager =
            FindAnyObjectByType<LevelManager>();

        if (resultPanel == null)
        {
            Debug.LogError(
                "[GameResultUI] Result Panel atanmamış.",
                this
            );

            return;
        }

        if (resultPanel == gameObject)
        {
            Debug.LogWarning(
                "[GameResultUI] Result Panel, scriptin bulunduğu GameObject ile aynı. " +
                "Script root objede, Result Panel ise alt objede bulunmalı.",
                this
            );
        }

        CacheMetricLayout();

        PrepareSkinUnlockUI();
        HideSkinUnlockImmediate();
        Hide();
    }

    public void ShowWin(int score, float time)
    {
        ShowWin(
            score,
            time,
            0,
            false
        );
    }

    public void ShowWin(
        int score,
        float time,
        int completedLevelNumber,
        bool isFirstCompletion)
    {
        ShowPanel();
        SetResultState(true);
        ApplyMetricVisibility();

        if (winScoreValue != null)
        {
            winScoreValue.text =
                score.ToString();
        }

        if (winTimeValue != null)
        {
            winTimeValue.text =
                FormatTime(time);
        }

        UpdateNextLevelButton();

        UpdateSkinUnlockReward(
            completedLevelNumber,
            isFirstCompletion
        );
    }

    public void ShowLose(int score, float time)
    {
        ShowLose(
            score,
            time,
            LastDeathInfo.Cause
        );
    }

    public void ShowLose(
        int score,
        float time,
        string cause)
    {
        ShowPanel();
        SetResultState(false);
        ApplyMetricVisibility();

        if (destroyedByText != null)
        {
            destroyedByText.text =
                string.IsNullOrWhiteSpace(cause)
                    ? "UNKNOWN"
                    : cause;
        }

        if (loseScoreValue != null)
        {
            loseScoreValue.text =
                score.ToString();
        }

        if (loseSurvivedValue != null)
        {
            loseSurvivedValue.text =
                FormatTime(time);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(false);
        }

        HideSkinUnlockImmediate();
    }

    private void ApplyMetricVisibility()
    {
        LevelConfig currentLevel = GetCurrentLevel();

        // Game Result UI rules:
        // Reach Score             -> Score + Survived Time
        // Survive Time            -> Survived Time only
        // Reach Score Within Time -> Score + Survived Time
        //
        // If no LevelConfig exists (debug/non-level launch), keep both visible.
        bool showScore = true;
        bool showTime = true;

        if (currentLevel != null)
        {
            showScore =
                currentLevel.winCondition !=
                WinConditionType.SurviveTime;
        }

        SetMetricVisible(
            winScoreValue,
            winUI,
            showScore,
            "SCORE"
        );

        SetMetricVisible(
            loseScoreValue,
            loseUI,
            showScore,
            "SCORE"
        );

        SetMetricVisible(
            winTimeValue,
            winUI,
            showTime,
            "TIME",
            "SURVIVED"
        );

        SetMetricVisible(
            loseSurvivedValue,
            loseUI,
            showTime,
            "SURVIVED",
            "TIME"
        );

        ApplyMetricLayout(currentLevel);
    }

    private void CacheMetricLayout()
    {
        winTimeValueRect =
            winTimeValue != null
                ? winTimeValue.rectTransform
                : null;

        loseSurvivedValueRect =
            loseSurvivedValue != null
                ? loseSurvivedValue.rectTransform
                : null;

        TextMeshProUGUI winTimeLabel =
            FindMetricLabel(
                winTimeValue,
                winUI,
                "TIME",
                "SURVIVED"
            );

        TextMeshProUGUI loseSurvivedLabel =
            FindMetricLabel(
                loseSurvivedValue,
                loseUI,
                "SURVIVED",
                "TIME"
            );

        winTimeLabelRect =
            winTimeLabel != null
                ? winTimeLabel.rectTransform
                : null;

        loseSurvivedLabelRect =
            loseSurvivedLabel != null
                ? loseSurvivedLabel.rectTransform
                : null;

        if (winTimeLabelRect != null)
        {
            winTimeLabelDefaultPosition =
                winTimeLabelRect.anchoredPosition;
        }

        if (winTimeValueRect != null)
        {
            winTimeValueDefaultPosition =
                winTimeValueRect.anchoredPosition;
        }

        if (loseSurvivedLabelRect != null)
        {
            loseSurvivedLabelDefaultPosition =
                loseSurvivedLabelRect.anchoredPosition;
        }

        if (loseSurvivedValueRect != null)
        {
            loseSurvivedValueDefaultPosition =
                loseSurvivedValueRect.anchoredPosition;
        }

        metricLayoutCached = true;
    }

    private void ApplyMetricLayout(
        LevelConfig currentLevel)
    {
        if (!metricLayoutCached)
        {
            CacheMetricLayout();
        }

        bool centerTime =
            currentLevel != null &&
            currentLevel.winCondition ==
            WinConditionType.SurviveTime;

        SetMetricHorizontalPosition(
            winTimeLabelRect,
            winTimeLabelDefaultPosition,
            centerTime
        );

        SetMetricHorizontalPosition(
            winTimeValueRect,
            winTimeValueDefaultPosition,
            centerTime
        );

        SetMetricHorizontalPosition(
            loseSurvivedLabelRect,
            loseSurvivedLabelDefaultPosition,
            centerTime
        );

        SetMetricHorizontalPosition(
            loseSurvivedValueRect,
            loseSurvivedValueDefaultPosition,
            centerTime
        );
    }

    private static void SetMetricHorizontalPosition(
        RectTransform rect,
        Vector2 defaultPosition,
        bool centered)
    {
        if (rect == null)
            return;

        Vector2 position = defaultPosition;

        if (centered)
        {
            position.x = 0f;
        }

        rect.anchoredPosition = position;
    }

    private static TextMeshProUGUI FindMetricLabel(
        TextMeshProUGUI valueText,
        GameObject uiGroup,
        params string[] labelKeywords)
    {
        if (valueText == null)
            return null;

        Transform parent = valueText.transform.parent;

        if (parent == null)
            return null;

        TextMeshProUGUI[] siblingTexts =
            parent.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < siblingTexts.Length; i++)
        {
            TextMeshProUGUI text = siblingTexts[i];

            if (text == null || text == valueText)
                continue;

            if (uiGroup != null &&
                !text.transform.IsChildOf(uiGroup.transform))
            {
                continue;
            }

            if (MatchesAnyMetricKeyword(
                    text,
                    labelKeywords))
            {
                return text;
            }
        }

        return null;
    }

    private static void SetMetricVisible(
        TextMeshProUGUI valueText,
        GameObject uiGroup,
        bool visible,
        params string[] labelKeywords
    )
    {
        if (valueText == null)
            return;

        valueText.gameObject.SetActive(visible);

        Transform parent = valueText.transform.parent;

        if (parent == null)
            return;

        // Result rows in the existing UI use a label + value under the same parent.
        // Hide only matching labels so we never risk disabling the whole result group.
        TextMeshProUGUI[] siblingTexts =
            parent.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < siblingTexts.Length; i++)
        {
            TextMeshProUGUI text = siblingTexts[i];

            if (text == null || text == valueText)
                continue;

            if (uiGroup != null &&
                !text.transform.IsChildOf(uiGroup.transform))
            {
                continue;
            }

            if (!MatchesAnyMetricKeyword(
                    text,
                    labelKeywords))
            {
                continue;
            }

            text.gameObject.SetActive(visible);
        }
    }

    private static bool MatchesAnyMetricKeyword(
        TextMeshProUGUI text,
        string[] keywords
    )
    {
        if (text == null ||
            keywords == null ||
            keywords.Length == 0)
        {
            return false;
        }

        string objectName =
            text.gameObject.name.ToUpperInvariant();

        string visibleText =
            string.IsNullOrWhiteSpace(text.text)
                ? string.Empty
                : text.text
                    .Trim()
                    .TrimEnd(':')
                    .ToUpperInvariant();

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];

            if (string.IsNullOrWhiteSpace(keyword))
                continue;

            keyword = keyword.ToUpperInvariant();

            if (visibleText.Contains(keyword) ||
                objectName.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    private void SetResultState(bool won)
    {
        if (winUI != null)
            winUI.SetActive(won);

        if (loseUI != null)
            loseUI.SetActive(!won);
    }

    private LevelConfig GetCurrentLevel()
    {
        return levelManager != null
            ? levelManager.currentLevel
            : null;
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton == null)
            return;

        LevelConfig currentLevel =
            GetCurrentLevel();

        bool hasNextLevel =
            SelectedLevelData.IsLevelMode &&
            currentLevel != null &&
            GetNextLevel(currentLevel) != null;

        nextLevelButton.SetActive(
            hasNextLevel
        );
    }

    private LevelConfig GetNextLevel(
        LevelConfig currentLevel)
    {
        if (currentLevel == null ||
            levels == null ||
            levels.Length == 0)
        {
            return null;
        }

        int nextLevelNumber =
            currentLevel.levelNumber + 1;

        foreach (LevelConfig level in levels)
        {
            if (level != null &&
                level.levelNumber ==
                nextLevelNumber)
            {
                return level;
            }
        }

        return null;
    }

    public void NextLevel()
    {
        if (!TryBeginSceneChange())
            return;

        PrepareForSceneChange();

        LevelConfig currentLevel =
            levelManager != null
                ? levelManager.currentLevel
                : null;

        LevelConfig nextLevel =
            GetNextLevel(currentLevel);

        if (nextLevel == null)
        {
            SelectedLevelData.Clear();

            if (!LoadScene("MainMenu"))
                CancelSceneChangeRequest();

            return;
        }

        SelectedLevelData.SetMission(
            nextLevel
        );

        if (!LoadScene(gameSceneName))
            CancelSceneChangeRequest();
    }

    public void TryAgain()
    {
        if (!TryBeginSceneChange())
            return;

        PrepareForSceneChange();

        if (!LoadScene(
            SceneManager
                .GetActiveScene()
                .name
        ))
        {
            CancelSceneChangeRequest();
        }
    }

    public void GoMenu()
    {
        if (!TryBeginSceneChange())
            return;

        PrepareForSceneChange();

        SelectedLevelData.Clear();

        if (!LoadScene("MainMenu"))
            CancelSceneChangeRequest();
    }

    public void Hide()
    {
        HideSkinUnlockImmediate();

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void ShowPanel()
    {
        if (resultPanel == null)
            return;

        isSceneChangeRequested = false;
        SetSceneButtonsInteractable(true);

        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();

        CanvasGroup canvasGroup =
            resultPanel.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (tryAgainButton != null)
        {
            tryAgainButton.SetActive(true);
        }

        if (menuButton != null)
        {
            menuButton.SetActive(true);
        }
    }

    private void PrepareSkinUnlockUI()
    {
        if (skinUnlockUI == null)
            return;

        if (skinUnlockRect == null)
        {
            skinUnlockRect =
                skinUnlockUI.GetComponent<RectTransform>();
        }

        if (skinUnlockCanvasGroup == null)
        {
            skinUnlockCanvasGroup =
                skinUnlockUI.GetComponent<CanvasGroup>();

            if (skinUnlockCanvasGroup == null)
            {
                skinUnlockCanvasGroup =
                    skinUnlockUI.AddComponent<CanvasGroup>();
            }
        }

        CacheSkinUnlockRestPosition();
    }

    private void CacheSkinUnlockRestPosition()
    {
        if (skinUnlockRect == null)
            return;

        skinUnlockRestPosition =
            skinUnlockRect.anchoredPosition;

        skinUnlockPositionCached = true;
    }

    private void UpdateSkinUnlockReward(
        int completedLevelNumber,
        bool isFirstCompletion)
    {
        HideSkinUnlockImmediate();

        if (!isFirstCompletion ||
            completedLevelNumber <= 0 ||
            playerSkinCatalog == null ||
            skinUnlockUI == null)
        {
            return;
        }

        PlayerSkinCatalog.SkinEntry unlockedSkin =
            FindSkinUnlockedByLevel(
                completedLevelNumber
            );

        if (unlockedSkin == null)
            return;

        if (skinUnlockedTitleText != null)
        {
            skinUnlockedTitleText.text =
                "NEW SKIN UNLOCKED";
        }

        if (unlockedSkinNameText != null)
        {
            string displayName =
                string.IsNullOrWhiteSpace(
                    unlockedSkin.displayName)
                    ? unlockedSkin.id
                    : unlockedSkin.displayName;

            unlockedSkinNameText.text =
                string.IsNullOrWhiteSpace(displayName)
                    ? "NEW SKIN"
                    : displayName.ToUpperInvariant();

            unlockedSkinNameText.color =
                GetReadableRewardColor(
                    unlockedSkin.dashTrailColor
                );
        }

        PrepareSkinUnlockUI();
        skinUnlockUI.SetActive(true);

        skinUnlockRoutine =
            StartCoroutine(
                AnimateSkinUnlock()
            );
    }

    private PlayerSkinCatalog.SkinEntry
        FindSkinUnlockedByLevel(
            int completedLevelNumber)
    {
        if (playerSkinCatalog == null ||
            playerSkinCatalog.Skins == null)
        {
            return null;
        }

        for (int i = 0;
             i < playerSkinCatalog.Skins.Count;
             i++)
        {
            PlayerSkinCatalog.SkinEntry skin =
                playerSkinCatalog.Skins[i];

            if (skin != null &&
                skin.requiredCompletedLevel ==
                completedLevelNumber)
            {
                return skin;
            }
        }

        return null;
    }

    private IEnumerator AnimateSkinUnlock()
    {
        if (skinUnlockUI == null)
            yield break;

        PrepareSkinUnlockUI();

        float effectiveUnlockDelay =
            Mathf.Max(0f, skinUnlockDelay);

        SoundManager soundManager = SoundManager.Instance;

        if (soundManager != null &&
            soundManager.WinSoundDuration > 0f)
        {
            float delayFromWinSound = Mathf.Max(
                0f,
                soundManager.WinSoundDuration -
                skinUnlockWinSoundTailOverlap
            );

            effectiveUnlockDelay = Mathf.Max(
                effectiveUnlockDelay,
                delayFromWinSound
            );
        }

        if (effectiveUnlockDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    effectiveUnlockDelay
                );
        }

        if (skinUnlockUI == null)
            yield break;

        SoundManager.Instance?.PlayNewSkinUnlockedSound(skinUnlockRect);

        if (!skinUnlockPositionCached)
            CacheSkinUnlockRestPosition();

        Vector2 startPosition =
            skinUnlockRestPosition +
            Vector2.right *
            skinUnlockSlideDistance;

        Vector3 startScale =
            Vector3.one * 0.92f;

        if (skinUnlockRect != null)
        {
            skinUnlockRect.anchoredPosition =
                startPosition;

            skinUnlockRect.localScale =
                startScale;
        }

        if (skinUnlockCanvasGroup != null)
        {
            skinUnlockCanvasGroup.alpha = 0f;
            skinUnlockCanvasGroup.interactable = false;
            skinUnlockCanvasGroup.blocksRaycasts = false;
        }

        float duration =
            Mathf.Max(
                0.05f,
                skinUnlockAnimationDuration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float eased =
                EaseOutBack(progress);

            if (skinUnlockRect != null)
            {
                skinUnlockRect.anchoredPosition =
                    Vector2.LerpUnclamped(
                        startPosition,
                        skinUnlockRestPosition,
                        eased
                    );

                skinUnlockRect.localScale =
                    Vector3.LerpUnclamped(
                        startScale,
                        Vector3.one,
                        eased
                    );
            }

            if (skinUnlockCanvasGroup != null)
            {
                skinUnlockCanvasGroup.alpha =
                    Mathf.Clamp01(
                        progress / 0.65f
                    );
            }

            yield return null;
        }

        if (skinUnlockRect != null)
        {
            skinUnlockRect.anchoredPosition =
                skinUnlockRestPosition;

            skinUnlockRect.localScale =
                Vector3.one;
        }

        if (skinUnlockCanvasGroup != null)
        {
            skinUnlockCanvasGroup.alpha = 1f;
        }

        skinUnlockRoutine = null;
    }

    private void HideSkinUnlockImmediate()
    {
        if (skinUnlockRoutine != null)
        {
            StopCoroutine(skinUnlockRoutine);
            skinUnlockRoutine = null;
        }

        if (skinUnlockRect != null &&
            skinUnlockPositionCached)
        {
            skinUnlockRect.anchoredPosition =
                skinUnlockRestPosition;

            skinUnlockRect.localScale =
                Vector3.one;
        }

        if (skinUnlockCanvasGroup != null)
        {
            skinUnlockCanvasGroup.alpha = 0f;
            skinUnlockCanvasGroup.interactable = false;
            skinUnlockCanvasGroup.blocksRaycasts = false;
        }

        if (skinUnlockUI != null)
        {
            skinUnlockUI.SetActive(false);
        }
    }

    private static Color GetReadableRewardColor(
        Color source)
    {
        Color result = source;
        result.a = 1f;

        float brightness =
            result.r * 0.2126f +
            result.g * 0.7152f +
            result.b * 0.0722f;

        if (brightness < 0.28f)
        {
            result = Color.Lerp(
                result,
                Color.white,
                0.5f
            );
        }

        return result;
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.18f;
        float shifted = value - 1f;

        return 1f +
               (overshoot + 1f) *
               shifted *
               shifted *
               shifted +
               overshoot *
               shifted *
               shifted;
    }

    private void PrepareForSceneChange()
    {
        Time.timeScale = 1f;
        RestorePhysics();
    }

    private bool TryBeginSceneChange()
    {
        if (isSceneChangeRequested)
            return false;

        if (SceneTransition.Instance != null &&
            SceneTransition.Instance.IsTransitioning)
        {
            return false;
        }

        isSceneChangeRequested = true;
        SetSceneButtonsInteractable(false);
        return true;
    }

    private void CancelSceneChangeRequest()
    {
        isSceneChangeRequested = false;
        SetSceneButtonsInteractable(true);
    }

    private void SetSceneButtonsInteractable(bool interactable)
    {
        SetButtonInteractable(nextLevelButton, interactable);
        SetButtonInteractable(tryAgainButton, interactable);
        SetButtonInteractable(menuButton, interactable);
    }

    private static void SetButtonInteractable(
        GameObject buttonObject,
        bool interactable)
    {
        if (buttonObject == null)
            return;

        Button button =
            buttonObject.GetComponent<Button>();

        if (button == null)
        {
            button =
                buttonObject.GetComponentInChildren<Button>(
                    true
                );
        }

        if (button != null)
            button.interactable = interactable;
    }

    private bool LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "[GameResultUI] Yüklenecek sahne adı boş.",
                this
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[GameResultUI] Sahne yüklenemiyor: '{sceneName}'. " +
                "Build Profiles ayarını kontrol et.",
                this
            );

            return false;
        }

        if (SceneTransition.Instance != null)
        {
            if (SceneTransition.Instance.IsTransitioning)
                return false;

            SceneTransition.Instance
                .LoadSceneWithFade(
                    sceneName
                );

            return true;
        }

        SceneManager.LoadScene(
            sceneName
        );

        return true;
    }

    private static string FormatTime(float time)
    {
        return
            Mathf.Max(0f, time)
                .ToString("F1") +
            " s";
    }

    private static void RestorePhysics()
    {
        Rigidbody2D[] bodies =
            FindObjectsByType<Rigidbody2D>(
                FindObjectsInactive.Exclude
            );

        foreach (Rigidbody2D body in bodies)
        {
            if (body != null)
            {
                body.simulated = true;
            }
        }
    }

    private void OnDisable()
    {
        HideSkinUnlockImmediate();
    }
}