using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResultUI : MonoBehaviour
{
    [Header("Main")]
    public GameObject resultPanel;

    [Header("UI Groups")]
    public GameObject winUI;
    public GameObject loseUI;

    [Header("Win Values")]
    public TextMeshProUGUI winScoreValue;
    public TextMeshProUGUI winTimeValue;
    public TextMeshProUGUI winBestTimeValue;

    [Header("Lose Values")]
    public TextMeshProUGUI destroyedByText;
    public TextMeshProUGUI loseScoreValue;
    public TextMeshProUGUI loseSurvivedValue;

    [Header("Level Mode")]
    public GameObject nextLevelButton;
    public LevelConfig[] levels;
    public string gameSceneName = "a";

    [Header("Buttons")]
    public GameObject tryAgainButton;
    public GameObject menuButton;

    private void Awake()
    {
        if (resultPanel == null)
            resultPanel = gameObject;

        Hide();
    }

    public void ShowWin(int score, float time)
    {
        ShowPanel();

        if (winUI != null) winUI.SetActive(true);
        if (loseUI != null) loseUI.SetActive(false);

        LevelManager levelManager = FindAnyObjectByType<LevelManager>();
        string bestTimeKey = GetBestTimeKey(levelManager);
        float bestTime = PlayerPrefs.GetFloat(bestTimeKey, time);

        if (winScoreValue != null)
            winScoreValue.text = score.ToString();

        if (winTimeValue != null)
            winTimeValue.text = time.ToString("F1") + " s";

        if (winBestTimeValue != null)
            winBestTimeValue.text = bestTime.ToString("F1") + " s";

        UpdateNextLevelButton();
    }

    public void ShowLose(int score, float time)
    {
        ShowPanel();

        if (winUI != null) winUI.SetActive(false);
        if (loseUI != null) loseUI.SetActive(true);

        if (destroyedByText != null)
            destroyedByText.text = LastDeathInfo.Cause;

        if (loseScoreValue != null)
            loseScoreValue.text = score.ToString();

        if (loseSurvivedValue != null)
            loseSurvivedValue.text = time.ToString("F1") + " s";

        if (nextLevelButton != null)
            nextLevelButton.SetActive(false);
    }

    private string GetBestTimeKey(LevelManager levelManager)
    {
        if (SelectedLevelData.isLevelMode && levelManager != null && levelManager.currentLevel != null)
            return "BestTime_Level_" + levelManager.currentLevel.levelNumber;

        return "BestTime_DevRoom";
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton == null)
            return;

        LevelConfig currentLevel = FindAnyObjectByType<LevelManager>()?.currentLevel;

        if (!SelectedLevelData.isLevelMode || currentLevel == null)
        {
            nextLevelButton.SetActive(false);
            return;
        }

        bool hasNextLevel = GetNextLevel(currentLevel) != null;
        nextLevelButton.SetActive(hasNextLevel);
    }

    private LevelConfig GetNextLevel(LevelConfig currentLevel)
    {
        if (currentLevel == null || levels == null)
            return null;

        int nextLevelNumber = currentLevel.levelNumber + 1;

        foreach (LevelConfig level in levels)
        {
            if (level != null && level.levelNumber == nextLevelNumber)
                return level;
        }

        return null;
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        RestorePhysics();

        LevelConfig currentLevel = FindAnyObjectByType<LevelManager>()?.currentLevel;
        LevelConfig nextLevel = GetNextLevel(currentLevel);

        if (nextLevel == null)
        {
            GoMenu();
            return;
        }

        SelectedLevelData.SetMission(nextLevel);

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade(gameSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void TryAgain()
    {
        Time.timeScale = 1f;
        RestorePhysics();

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoMenu()
    {
        Time.timeScale = 1f;
        RestorePhysics();

        SelectedLevelData.Clear();

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    public void Hide()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void ShowPanel()
    {
        gameObject.SetActive(true);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();

            CanvasGroup canvasGroup = resultPanel.GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        if (tryAgainButton != null)
            tryAgainButton.SetActive(true);

        if (menuButton != null)
            menuButton.SetActive(true);
    }

    private void RestorePhysics()
    {
        Rigidbody2D[] bodies = FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Exclude);

        foreach (Rigidbody2D rb in bodies)
            rb.simulated = true;
    }
}