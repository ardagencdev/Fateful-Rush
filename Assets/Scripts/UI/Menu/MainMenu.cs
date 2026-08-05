using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "a";

    [Header("UI")]
    [SerializeField] private UIPanelFadeSwitcher fadeSwitcher;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Continue")]
    [Tooltip("LevelSelectPanel üzerindeki 40 LevelConfig kaynağı.")]
    [SerializeField] private LevelSelectPanel levelSelectPanel;

    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueLevelText;

    [SerializeField, Range(0.1f, 1f)]
    private float unavailableContinueAlpha = 0.25f;

    [Header("Dev Room")]
    [SerializeField] private LevelConfig devRoomConfig;
    [SerializeField] private GameObject devRoomButton;
    [SerializeField] private Key devRoomRevealKey = Key.F8;

    [Header("Quit")]
    [SerializeField, Min(0f)]
    private float fallbackQuitDelay = 0.35f;

    private Coroutine quitRoutine;
    private CanvasGroup continueButtonGroup;
    private LevelConfig continueTargetLevel;

    private bool isStartingGame;
    private bool isQuitting;
    private bool isDevRoomButtonVisible;
    private bool isDesktopDevRoomAllowed;

    public bool IsContinueAvailable =>
        continueTargetLevel != null;

    private void Awake()
    {
        Time.timeScale = 1f;

        isDesktopDevRoomAllowed =
            IsDesktopPlatform();

        FindDevRoomButtonIfNeeded();
        SetDevRoomButtonVisible(false);

        FindLevelSelectPanelIfNeeded();
        PrepareContinueButton();
        RefreshContinueState();
    }

    private void OnEnable()
    {
        isStartingGame = false;
        isQuitting = false;

        RefreshContinueState();
    }

    private void OnDisable()
    {
        StopQuitRoutine();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(
                ContinueGame
            );
        }
    }

    private void Update()
    {
        if (!isDesktopDevRoomAllowed)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (!keyboard[devRoomRevealKey].wasPressedThisFrame)
            return;

        SetDevRoomButtonVisible(
            !isDevRoomButtonVisible
        );
    }

    public void ContinueGame()
    {
        if (isStartingGame || isQuitting)
            return;

        // Butona basıldığı anda kayıt durumunu bir kez daha doğrula.
        RefreshContinueState();

        if (continueTargetLevel == null ||
            continueButton == null)
        {
            return;
        }

        if (!CanLoadGameScene())
            return;

        isStartingGame = true;
        Time.timeScale = 1f;

        SelectedLevelData.SetMission(
            continueTargetLevel
        );

        LoadGameScene();
    }

    public void RefreshContinueState()
    {
        FindLevelSelectPanelIfNeeded();
        PrepareContinueButton();

        bool canContinue =
            TryFindContinueTarget(
                out continueTargetLevel
            );

        SetContinueVisualState(canContinue);
    }

    public void StartGame()
    {
        if (!isDesktopDevRoomAllowed)
            return;

        if (isStartingGame || isQuitting)
            return;

        if (devRoomConfig == null)
        {
            Debug.LogError(
                "MainMenu devRoomConfig reference is missing.",
                this
            );

            return;
        }

        if (!CanLoadGameScene())
            return;

        isStartingGame = true;
        Time.timeScale = 1f;

        SelectedLevelData.SetDevRoom(devRoomConfig);

        LoadGameScene();
    }

    public void QuitGame()
    {
        if (isQuitting || isStartingGame)
            return;

        isQuitting = true;
        Time.timeScale = 1f;

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.QuitGameWithFade();
            return;
        }

        StopQuitRoutine();
        quitRoutine = StartCoroutine(QuitRoutine());
    }

    private bool TryFindContinueTarget(
        out LevelConfig targetLevel
    )
    {
        targetLevel = null;

        if (levelSelectPanel == null)
            return false;

        IReadOnlyList<LevelConfig> configuredLevels =
            levelSelectPanel.GetConfiguredLevels();

        if (configuredLevels == null ||
            configuredLevels.Count == 0)
        {
            return false;
        }

        int highestCompletedLevel = 0;

        foreach (LevelConfig level in configuredLevels)
        {
            if (level == null)
                continue;

            bool isCompleted =
                PlayerPrefs.GetInt(
                    $"CompletedLevel_{level.levelNumber}",
                    0
                ) == 1;

            if (!isCompleted)
                continue;

            highestCompletedLevel = Mathf.Max(
                highestCompletedLevel,
                level.levelNumber
            );
        }

        // Oyuncu henüz hiçbir görevi tamamlamadıysa Continue kapalı kalır.
        if (highestCompletedLevel <= 0)
            return false;

        int unlockedLevel =
            PlayerPrefs.GetInt(
                "UnlockedLevel",
                1
            );

        // Eski veya bozulmuş kayıtta UnlockedLevel geride kalmışsa
        // tamamlanan en yüksek bölümün bir sonrasını esas al.
        int desiredLevelNumber = Mathf.Max(
            unlockedLevel,
            highestCompletedLevel + 1
        );

        // Level 40 tamamlandığında UnlockedLevel 41 olabilir.
        // Listede bulunan en ileri geçerli LevelConfig seçilerek taşma önlenir.
        foreach (LevelConfig level in configuredLevels)
        {
            if (level == null)
                continue;

            if (level.levelNumber > desiredLevelNumber)
                break;

            targetLevel = level;
        }

        return targetLevel != null;
    }

    private void PrepareContinueButton()
    {
        if (continueButton == null)
            return;

        if (continueButtonGroup == null)
        {
            continueButtonGroup =
                continueButton.GetComponent<CanvasGroup>();

            if (continueButtonGroup == null)
            {
                continueButtonGroup =
                    continueButton.gameObject
                        .AddComponent<CanvasGroup>();
            }
        }

        continueButton.onClick.RemoveListener(
            ContinueGame
        );
        continueButton.onClick.AddListener(
            ContinueGame
        );

        UIButtonSound buttonSound =
            continueButton.GetComponent<UIButtonSound>();

        if (buttonSound != null)
        {
            buttonSound.ConfigureAsContinue(this);
        }
    }

    private void SetContinueVisualState(
        bool canContinue
    )
    {
        // Continue her zaman tıklama alır. Kayıt yoksa ContinueGame
        // hiçbir aksiyon gerçekleştirmez; UIButtonSound Locked sesi çalar.
        if (continueButton != null)
            continueButton.interactable = true;

        if (continueButtonGroup != null)
        {
            continueButtonGroup.alpha =
                canContinue
                    ? 1f
                    : unavailableContinueAlpha;

            continueButtonGroup.interactable = true;
            continueButtonGroup.blocksRaycasts = true;
        }

        if (continueLevelText == null)
            return;

        continueLevelText.gameObject.SetActive(
            canContinue
        );

        if (canContinue &&
            continueTargetLevel != null)
        {
            continueLevelText.text =
                $"LEVEL {continueTargetLevel.levelNumber}";

            // Continue yazısı, hedef bölümün Near Stars tema rengini kullanır.
            continueLevelText.color =
                continueTargetLevel.nearStarsColor;
        }
    }

    private void FindLevelSelectPanelIfNeeded()
    {
        if (levelSelectPanel != null)
            return;

        LevelSelectPanel[] candidates =
            Resources.FindObjectsOfTypeAll<LevelSelectPanel>();

        foreach (LevelSelectPanel candidate in candidates)
        {
            if (candidate == null ||
                !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            levelSelectPanel = candidate;
            return;
        }
    }

    private void LoadGameScene()
    {
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

    private IEnumerator QuitRoutine()
    {
        if (fadeSwitcher != null &&
            mainMenuPanel != null)
        {
            fadeSwitcher.HidePanel(mainMenuPanel);

            if (fallbackQuitDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    fallbackQuitDelay
                );
            }
        }

        quitRoutine = null;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void FindDevRoomButtonIfNeeded()
    {
        if (devRoomButton != null)
            return;

        Transform[] sceneTransforms =
            Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform == null)
                continue;

            GameObject candidate =
                sceneTransform.gameObject;

            if (!candidate.scene.IsValid())
                continue;

            if (candidate.name != "DevRoomButton")
                continue;

            devRoomButton = candidate;
            return;
        }

        Debug.LogWarning(
            "MainMenu could not find DevRoomButton. " +
            "Assign it in the Inspector or keep its name as DevRoomButton.",
            this
        );
    }

    private void SetDevRoomButtonVisible(
        bool visible
    )
    {
        bool shouldShow =
            isDesktopDevRoomAllowed && visible;

        isDevRoomButtonVisible = shouldShow;

        if (devRoomButton != null)
            devRoomButton.SetActive(shouldShow);
    }

    private static bool IsDesktopPlatform()
    {
        if (Application.isEditor)
            return true;

        return
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer;
    }

    private bool CanLoadGameScene()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "MainMenu game scene name is empty.",
                this
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError(
                $"Scene '{gameSceneName}' could not be loaded. " +
                "Make sure it is included in Build Profiles.",
                this
            );

            return false;
        }

        return true;
    }

    private void StopQuitRoutine()
    {
        if (quitRoutine == null)
            return;

        StopCoroutine(quitRoutine);
        quitRoutine = null;
    }

    private void OnValidate()
    {
        fallbackQuitDelay =
            Mathf.Max(0f, fallbackQuitDelay);

        unavailableContinueAlpha =
            Mathf.Clamp(
                unavailableContinueAlpha,
                0.1f,
                1f
            );
    }
}