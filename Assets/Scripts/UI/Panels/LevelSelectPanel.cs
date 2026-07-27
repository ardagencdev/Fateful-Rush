using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private UIPanelFadeSwitcher fadeSwitcher;

    [Header("Level Buttons")]
    [SerializeField] private RectTransform levelButtonsContainer;
    [SerializeField] private LevelButtonUI levelButtonPrefab;
    [SerializeField] private LevelConfig[] levels;

    [Header("Pagination")]
    [SerializeField, Min(1)] private int levelsPerPage = 10;
    [SerializeField] private TMP_Text pageIndicatorText;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;

    [Header("Swipe")]
    [SerializeField, Min(1f)] private float minSwipeDistance = 80f;

    [Header("Page Animation")]
    [SerializeField, Min(0f)] private float pageSlideDistance = 700f;
    [SerializeField, Min(0.01f)] private float pageAnimDuration = 0.25f;

    [Header("Mission Briefing")]
    [SerializeField]
    private MissionBriefingPanelUI missionBriefingPanel;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "a";

    private readonly List<LevelButtonUI> createdButtons =
        new List<LevelButtonUI>();

    private int currentPageIndex;
    private int totalPageCount;

    private Vector2 dragStartPosition;
    private Vector2 containerStartPosition;

    private bool isDragging;
    private bool isLoadingLevel;

    private Coroutine pageRoutine;
    private CanvasGroup containerGroup;

    private void Awake()
    {
        PrepareContainer();
        PreparePageButtons();
    }

    private void OnDestroy()
    {
        if (previousPageButton != null)
            previousPageButton.onClick.RemoveListener(PreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.RemoveListener(NextPage);
    }

    private void Update()
    {
        if (levelSelectPanel == null ||
            !levelSelectPanel.activeSelf ||
            pageRoutine != null)
        {
            return;
        }

        if (missionBriefingPanel != null &&
            missionBriefingPanel.IsOpen)
        {
            isDragging = false;
            return;
        }

        if (Touchscreen.current != null)
            HandleTouchSwipe();
        else
            HandleMouseSwipe();
    }

    public void OpenPanel()
    {
        if (!ValidatePanelReferences())
            return;

        CalculatePageCount();
        OpenLatestUnlockedPage();
        CreateCurrentPageButtons();
        RefreshPageUI();

        MainMenuStarColorRandomizer.Instance?
              .ShowLevelSelectionColor();

        SwitchPanels(
            mainMenuPanel,
            levelSelectPanel
        );
    }

    public void ClosePanel()
    {
        if (!ValidatePanelReferences())
            return;

        StopPageAnimation();

        MainMenuStarColorRandomizer.Instance?
            .ShowMainMenuColor();

        SwitchPanels(
            levelSelectPanel,
            mainMenuPanel
        );
    }

    public void ShowMissionBriefing(
        LevelConfig config
    )
    {
        if (config == null)
        {
            Debug.LogWarning(
                "LevelSelectPanel received a null LevelConfig.",
                this
            );

            return;
        }

        if (missionBriefingPanel == null)
        {
            Debug.LogError(
                "LevelSelectPanel missionBriefingPanel is missing.",
                this
            );

            return;
        }

        isDragging = false;

        missionBriefingPanel.Show(
            config,
            StartLevel,
            RefreshButtons
        );
    }

    public void StartLevel(
        LevelConfig config
    )
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

    public void NextPage()
    {
        if (pageRoutine != null)
            return;

        if (currentPageIndex >= totalPageCount - 1)
            return;

        StartPageTransition(
            currentPageIndex + 1,
            -1
        );
    }

    public void PreviousPage()
    {
        if (pageRoutine != null)
            return;

        if (currentPageIndex <= 0)
            return;

        StartPageTransition(
            currentPageIndex - 1,
            1
        );
    }

    private void PrepareContainer()
    {
        if (levelButtonsContainer == null)
            return;

        containerStartPosition =
            levelButtonsContainer.anchoredPosition;

        containerGroup =
            levelButtonsContainer.GetComponent<CanvasGroup>();

        if (containerGroup == null)
        {
            containerGroup =
                levelButtonsContainer.gameObject
                    .AddComponent<CanvasGroup>();
        }
    }

    private void PreparePageButtons()
    {
        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(
                PreviousPage
            );

            previousPageButton.onClick.AddListener(
                PreviousPage
            );
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(
                NextPage
            );

            nextPageButton.onClick.AddListener(
                NextPage
            );
        }
    }

    private void CalculatePageCount()
    {
        int validLevelCount = 0;

        if (levels != null)
        {
            foreach (LevelConfig level in levels)
            {
                if (level != null)
                    validLevelCount++;
            }
        }

        totalPageCount = Mathf.Max(
            1,
            Mathf.CeilToInt(
                validLevelCount /
                (float)levelsPerPage
            )
        );

        currentPageIndex = Mathf.Clamp(
            currentPageIndex,
            0,
            totalPageCount - 1
        );
    }

    private void CreateCurrentPageButtons()
    {
        if (!ValidateButtonReferences())
            return;

        ClearCreatedButtons();

        List<LevelConfig> validLevels =
            GetValidLevels();

        int firstIndex =
            currentPageIndex * levelsPerPage;

        int lastIndex = Mathf.Min(
            firstIndex + levelsPerPage,
            validLevels.Count
        );

        for (int i = firstIndex; i < lastIndex; i++)
        {
            LevelButtonUI levelButton =
                Instantiate(
                    levelButtonPrefab,
                    levelButtonsContainer
                );

            levelButton.Setup(
                validLevels[i],
                this
            );

            createdButtons.Add(levelButton);
        }
    }

    private List<LevelConfig> GetValidLevels()
    {
        List<LevelConfig> validLevels =
            new List<LevelConfig>();

        if (levels == null)
            return validLevels;

        foreach (LevelConfig levelConfig in levels)
        {
            if (levelConfig != null)
                validLevels.Add(levelConfig);
        }

        validLevels.Sort(
            (left, right) =>
                left.levelNumber.CompareTo(
                    right.levelNumber
                )
        );

        return validLevels;
    }

    private void StartPageTransition(
        int newPageIndex,
        int direction
    )
    {
        pageRoutine = StartCoroutine(
            PageTransitionRoutine(
                newPageIndex,
                direction
            )
        );
    }

    private IEnumerator PageTransitionRoutine(
        int newPageIndex,
        int direction
    )
    {
        SetPageButtonsInteractable(false);

        float timer = 0f;

        while (timer < pageAnimDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                timer / pageAnimDuration
            );

            float eased = EaseOutCubic(progress);

            if (levelButtonsContainer != null)
            {
                levelButtonsContainer.anchoredPosition =
                    containerStartPosition +
                    Vector2.right *
                    direction *
                    pageSlideDistance *
                    eased;
            }

            if (containerGroup != null)
                containerGroup.alpha = 1f - eased;

            yield return null;
        }

        currentPageIndex = newPageIndex;

        CreateCurrentPageButtons();
        RefreshPageUI();

        if (levelButtonsContainer != null)
        {
            levelButtonsContainer.anchoredPosition =
                containerStartPosition -
                Vector2.right *
                direction *
                pageSlideDistance;
        }

        timer = 0f;

        while (timer < pageAnimDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                timer / pageAnimDuration
            );

            float eased = EaseOutCubic(progress);

            if (levelButtonsContainer != null)
            {
                levelButtonsContainer.anchoredPosition =
                    Vector2.Lerp(
                        containerStartPosition -
                        Vector2.right *
                        direction *
                        pageSlideDistance,
                        containerStartPosition,
                        eased
                    );
            }

            if (containerGroup != null)
                containerGroup.alpha = eased;

            yield return null;
        }

        if (levelButtonsContainer != null)
        {
            levelButtonsContainer.anchoredPosition =
                containerStartPosition;
        }

        if (containerGroup != null)
            containerGroup.alpha = 1f;

        SetPageButtonsInteractable(true);

        pageRoutine = null;
    }

    private void RefreshPageUI()
    {
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text =
                $"{currentPageIndex + 1} / {totalPageCount}";
        }

        RefreshNavigationButtons();
        SetPageButtonsInteractable(true);
    }

    private void RefreshNavigationButtons()
    {
        bool canGoPrevious =
            currentPageIndex > 0;

        bool canGoNext =
            currentPageIndex < totalPageCount - 1;

        if (previousPageButton != null)
        {
            previousPageButton.gameObject.SetActive(
                canGoPrevious
            );
        }

        if (nextPageButton != null)
        {
            nextPageButton.gameObject.SetActive(
                canGoNext
            );
        }
    }

    private void SetPageButtonsInteractable(
    bool interactable
)
    {
        if (previousPageButton != null &&
            previousPageButton.gameObject.activeSelf)
        {
            previousPageButton.interactable =
                interactable;
        }

        if (nextPageButton != null &&
            nextPageButton.gameObject.activeSelf)
        {
            nextPageButton.interactable =
                interactable;
        }

        if (containerGroup != null)
        {
            containerGroup.interactable =
                interactable;

            containerGroup.blocksRaycasts =
                interactable;
        }
    }

    private void HandleMouseSwipe()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            dragStartPosition =
                Mouse.current.position.ReadValue();

            isDragging = true;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame &&
            isDragging)
        {
            Vector2 dragEndPosition =
                Mouse.current.position.ReadValue();

            isDragging = false;

            TrySwipe(dragEndPosition);
        }
    }

    private void HandleTouchSwipe()
    {
        if (Touchscreen.current == null)
            return;

        var touch =
            Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            dragStartPosition =
                touch.position.ReadValue();

            isDragging = true;
        }

        if (touch.press.wasReleasedThisFrame &&
            isDragging)
        {
            Vector2 dragEndPosition =
                touch.position.ReadValue();

            isDragging = false;

            TrySwipe(dragEndPosition);
        }
    }

    private void TrySwipe(
        Vector2 dragEndPosition
    )
    {
        float swipeX =
            dragEndPosition.x -
            dragStartPosition.x;

        if (Mathf.Abs(swipeX) <
            minSwipeDistance)
        {
            return;
        }

        if (swipeX < 0f)
            NextPage();
        else
            PreviousPage();
    }

    private void ClearCreatedButtons()
    {
        foreach (LevelButtonUI button in createdButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        createdButtons.Clear();

        if (levelButtonsContainer == null)
            return;

        LevelButtonUI[] existingButtons =
            levelButtonsContainer
                .GetComponentsInChildren<LevelButtonUI>(
                    true
                );

        foreach (LevelButtonUI button in existingButtons)
        {
            if (button == null)
                continue;

            if (button.transform.parent !=
                levelButtonsContainer)
            {
                continue;
            }

            Destroy(button.gameObject);
        }
    }

    private void CleanupButtonList()
    {
        for (
            int i = createdButtons.Count - 1;
            i >= 0;
            i--
        )
        {
            if (createdButtons[i] == null)
                createdButtons.RemoveAt(i);
        }
    }

    private void StopPageAnimation()
    {
        if (pageRoutine == null)
            return;

        StopCoroutine(pageRoutine);
        pageRoutine = null;

        if (levelButtonsContainer != null)
        {
            levelButtonsContainer.anchoredPosition =
                containerStartPosition;
        }

        if (containerGroup != null)
            containerGroup.alpha = 1f;
    }

    private void SwitchPanels(
        GameObject panelToHide,
        GameObject panelToShow
    )
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
        levelsPerPage =
            Mathf.Max(1, levelsPerPage);

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
                        "Duplicate level number found: " +
                        levels[i].levelNumber,
                        this
                    );
                }
            }
        }
    }

    private static float EaseOutCubic(
        float value
    )
    {
        float inverse =
            1f - Mathf.Clamp01(value);

        return
            1f -
            inverse *
            inverse *
            inverse;
    }

    private void OpenLatestUnlockedPage()
    {
        int unlockedLevel = PlayerPrefs.GetInt(
            "UnlockedLevel",
            1
        );

        int safeUnlockedLevel = Mathf.Max(
            1,
            unlockedLevel
        );

        currentPageIndex =
            (safeUnlockedLevel - 1) /
            levelsPerPage;

        currentPageIndex = Mathf.Clamp(
            currentPageIndex,
            0,
            totalPageCount - 1
        );
    }
}