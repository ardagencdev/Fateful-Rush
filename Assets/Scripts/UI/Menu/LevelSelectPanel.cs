using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectPanel : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;

    public Transform levelButtonsContainer;
    public LevelButtonUI levelButtonPrefab;

    public LevelConfig[] levels;
    public string gameSceneName = "a";

    public UIPanelFadeSwitcher fadeSwitcher;

    private bool buttonsCreated;

    public void OpenPanel()
    {
        if (!buttonsCreated)
            CreateButtons();

        RefreshButtons();

        if (fadeSwitcher != null)
            fadeSwitcher.SwitchPanel(mainMenuPanel, levelSelectPanel);
        else
        {
            mainMenuPanel.SetActive(false);
            levelSelectPanel.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        if (fadeSwitcher != null)
            fadeSwitcher.SwitchPanel(levelSelectPanel, mainMenuPanel);
        else
        {
            levelSelectPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }

    private void CreateButtons()
    {
        foreach (Transform child in levelButtonsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < levels.Length; i++)
        {
            LevelButtonUI button = Instantiate(levelButtonPrefab, levelButtonsContainer);
            button.Setup(levels[i], this);
        }

        buttonsCreated = true;
    }

    private void RefreshButtons()
    {
        foreach (LevelButtonUI button in levelButtonsContainer.GetComponentsInChildren<LevelButtonUI>(true))
            button.Refresh();
    }

    public void StartLevel(LevelConfig config)
    {
        SelectedLevelData.SetMission(config);

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade(gameSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }
}