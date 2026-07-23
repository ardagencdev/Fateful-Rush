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
    [SerializeField] private TextMeshProUGUI winBestTimeValue;

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

        Hide();
    }

    public void ShowWin(int score, float time)
    {
        ShowPanel();
        SetResultState(true);

        string bestTimeKey =
            GetBestTimeKey();

        float bestTime =
            PlayerPrefs.GetFloat(
                bestTimeKey,
                time
            );

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

        if (winBestTimeValue != null)
        {
            winBestTimeValue.text =
                FormatTime(bestTime);
        }

        UpdateNextLevelButton();
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
    }

    private void SetResultState(bool won)
    {
        if (winUI != null)
            winUI.SetActive(won);

        if (loseUI != null)
            loseUI.SetActive(!won);
    }

    private string GetBestTimeKey()
    {
        if (SelectedLevelData.IsLevelMode &&
            levelManager != null &&
            levelManager.currentLevel != null)
        {
            return "BestTime_Level_" +
                   levelManager
                       .currentLevel
                       .levelNumber;
        }

        return "BestTime_DevRoom";
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton == null)
            return;

        LevelConfig currentLevel =
            levelManager != null
                ? levelManager.currentLevel
                : null;

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
}

