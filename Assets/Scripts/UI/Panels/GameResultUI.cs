using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField, Min(0.05f)] private float skinUnlockAnimationDuration = 0.42f;
    [SerializeField, Min(0f)] private float skinUnlockSlideDistance = 180f;

    private Coroutine skinUnlockRoutine;
    private Vector2 skinUnlockRestPosition;
    private bool skinUnlockPositionCached;

    private LevelManager levelManager;

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
        PrepareForSceneChange();

        LevelConfig currentLevel =
            levelManager != null
                ? levelManager.currentLevel
                : null;

        LevelConfig nextLevel =
            GetNextLevel(currentLevel);

        if (nextLevel == null)
        {
            GoMenu();
            return;
        }

        SelectedLevelData.SetMission(
            nextLevel
        );

        LoadScene(gameSceneName);
    }

    public void TryAgain()
    {
        PrepareForSceneChange();

        LoadScene(
            SceneManager
                .GetActiveScene()
                .name
        );
    }

    public void GoMenu()
    {
        PrepareForSceneChange();

        SelectedLevelData.Clear();

        LoadScene("MainMenu");
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

        if (skinUnlockDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    skinUnlockDelay
                );
        }

        if (skinUnlockUI == null)
            yield break;

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

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "[GameResultUI] Yüklenecek sahne adı boş.",
                this
            );

            return;
        }

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance
                .LoadSceneWithFade(
                    sceneName
                );
        }
        else
        {
            SceneManager.LoadScene(
                sceneName
            );
        }
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