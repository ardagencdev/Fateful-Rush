using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private UIPanelFadeSwitcher fadeSwitcher;

    [Header("Level Buttons")]
    [SerializeField] private Transform levelButtonsContainer;
    [SerializeField] private LevelButtonUI levelButtonPrefab;
    [SerializeField] private LevelConfig[] levels;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "a";

    private readonly List<LevelButtonUI> createdButtons =
        new List<LevelButtonUI>();

    private bool buttonsCreated;
    private bool isLoadingLevel;

    public void OpenPanel()
    {
        if (!ValidatePanelReferences())
            return;

        if (!buttonsCreated)
            CreateButtons();

        RefreshButtons();
        SwitchPanels(mainMenuPanel, levelSelectPanel);
    }

    public void ClosePanel()
    {
        if (!ValidatePanelReferences())
            return;

        SwitchPanels(levelSelectPanel, mainMenuPanel);
    }

    public void StartLevel(LevelConfig config)
    {
        if (isLoadingLevel)
            return;

        if (config == null)
        {
            Debug.LogWarning(
                "LevelSelectPanel received a null LevelConfig.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "LevelSelectPanel game scene name is empty.",
                this
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError(
                $"Scene '{gameSceneName}' could not be loaded. " +
                "Make sure it exists in Build Profiles.",
                this
            );

            return;
        }

        isLoadingLevel = true;

        SelectedLevelData.SetMission(config);

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadSceneWithFade(
                gameSceneName
            );
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void RefreshButtons()
    {
        CleanupButtonList();

        foreach (LevelButtonUI button in createdButtons)
        {
            if (button != null)
                button.Refresh();
        }
    }

    private void CreateButtons()
    {
        if (!ValidateButtonReferences())
            return;

        ClearCreatedButtons();

        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning(
                "LevelSelectPanel has no LevelConfig entries.",
                this
            );

            buttonsCreated = true;
            return;
        }

        foreach (LevelConfig levelConfig in levels)
        {
            if (levelConfig == null)
            {
                Debug.LogWarning(
                    "LevelSelectPanel contains a null LevelConfig entry.",
                    this
                );

                continue;
            }

            LevelButtonUI levelButton = Instantiate(
                levelButtonPrefab,
                levelButtonsContainer
            );

            levelButton.Setup(levelConfig, this);
            createdButtons.Add(levelButton);
        }

        buttonsCreated = true;
    }

    private void ClearCreatedButtons()
    {
        foreach (LevelButtonUI button in createdButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        createdButtons.Clear();

        LevelButtonUI[] existingButtons =
            levelButtonsContainer.GetComponentsInChildren<LevelButtonUI>(
                true
            );

        foreach (LevelButtonUI button in existingButtons)
        {
            if (button == null)
                continue;

            if (button.transform.parent != levelButtonsContainer)
                continue;

            Destroy(button.gameObject);
        }
    }

    private void CleanupButtonList()
    {
        for (int i = createdButtons.Count - 1; i >= 0; i--)
        {
            if (createdButtons[i] == null)
                createdButtons.RemoveAt(i);
        }
    }

    private void SwitchPanels(
        GameObject panelToHide,
        GameObject panelToShow)
    {
        if (fadeSwitcher != null)
        {
            fadeSwitcher.SwitchPanel(
                panelToHide,
                panelToShow
            );

            return;
        }

        if (panelToHide != null)
            panelToHide.SetActive(false);

        if (panelToShow != null)
            panelToShow.SetActive(true);
    }

    private bool ValidatePanelReferences()
    {
        if (mainMenuPanel == null ||
            levelSelectPanel == null)
        {
            Debug.LogError(
                "LevelSelectPanel panel references are missing.",
                this
            );

            return false;
        }

        return true;
    }

    private bool ValidateButtonReferences()
    {
        if (levelButtonsContainer == null)
        {
            Debug.LogError(
                "LevelSelectPanel levelButtonsContainer is missing.",
                this
            );

            return false;
        }

        if (levelButtonPrefab == null)
        {
            Debug.LogError(
                "LevelSelectPanel levelButtonPrefab is missing.",
                this
            );

            return false;
        }

        return true;
    }

    private void OnEnable()
    {
        isLoadingLevel = false;
    }

    private void OnValidate()
    {
        if (levels == null)
            return;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
                continue;

            for (int j = i + 1; j < levels.Length; j++)
            {
                if (levels[j] == null)
                    continue;

                if (levels[i].levelNumber ==
                    levels[j].levelNumber)
                {
                    Debug.LogWarning(
                        $"Duplicate level number found: " +
                        $"{levels[i].levelNumber}",
                        this
                    );
                }
            }
        }
    }
}