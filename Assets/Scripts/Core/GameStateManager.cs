using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static bool IsGameplayStarted { get; private set; }

    [Header("References")]
    public PlayerMovement playerMovement;
    public PlayerDash playerDash;
    public SoundManager soundManager;
    public GameResultUI gameResultUI;

    public LaserWallSpawner laserWallSpawner;
    public HorizontalLaserWallSpawner horizontalLaserWallSpawner;
    public ObstacleSpawner obstacleSpawner;

    [Header("Tutorial")]
    public TutorialPanelUI tutorialPanelUI;

    [Header("Gameplay Music")]
    [SerializeField]
    private GameplayMusicFade gameplayMusic;

    [Header("HUD")]
    public GameObject scoreHUD;
    public GameObject timeHUD;
    public GameObject joystickHUD;
    public GameObject dashHUD;
    public GameObject cloneHUD;
    public GameObject pauseButtonHUD;
    public HUDIntroAnimator hudIntroAnimator;

    private LevelManager levelManager;
    private GameTimer gameTimerComponent;
    private BossScreenEffect bossScreenEffect;

    private bool gameFrozen;
    private bool gameEnded;
    private float gameTimer;

    private void Awake()
    {
        FindMissingReferences();
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1f;

        IsGameplayStarted = false;
        gameFrozen = false;
        gameEnded = false;
        gameTimer = 0f;

        gameplayMusic?.StopImmediately();

        yield return null;

        Vector3 playerTargetScale = Vector3.one;

        if (playerMovement != null)
        {
            playerTargetScale =
                playerMovement.transform.localScale;

            playerMovement.StopMovement();
            playerMovement.gameObject.SetActive(false);
        }

        SetHUD(false);

        LevelConfig currentLevel = null;

        if (levelManager != null)
        {
            levelManager.InitializeLevel();
            currentLevel = levelManager.currentLevel;
        }

        bool levelAlreadyCompleted = false;

        if (currentLevel != null &&
            SelectedLevelData.isLevelMode)
        {
            string completedKey =
                "CompletedLevel_" +
                currentLevel.levelNumber;

            levelAlreadyCompleted =
                PlayerPrefs.GetInt(
                    completedKey,
                    0
                ) == 1;
        }

        bool shouldShowTutorial =
            currentLevel != null &&
            currentLevel.showTutorial &&
            tutorialPanelUI != null &&
            !levelAlreadyCompleted;

        if (shouldShowTutorial)
        {
            gameplayMusic?.StopImmediately();

            yield return
                new WaitForSecondsRealtime(0.25f);

            bool tutorialClosed = false;

            tutorialPanelUI.ShowTutorial(
                currentLevel.tutorialTitle,
                currentLevel.tutorialPages,
                () => tutorialClosed = true
            );

            yield return new WaitUntil(
                () => tutorialClosed
            );
        }

        gameplayMusic?.PlayAndFadeIn();

        yield return null;

        SetHUD(false);

        if (hudIntroAnimator != null)
        {
            hudIntroAnimator.HideInstant();

            yield return
                hudIntroAnimator.PlayAndWait();
        }
        else
        {
            SetHUD(true);
        }

        if (obstacleSpawner != null)
        {
            yield return
                obstacleSpawner
                    .PlaySpawnedObstaclePopupsAndWait();
        }

        if (playerMovement != null)
        {
            playerMovement.gameObject.SetActive(true);

            playerMovement.transform.localScale =
                Vector3.zero;

            float timer = 0f;
            const float duration = 0.18f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(timer / duration);

                float scale;

                if (progress < 0.75f)
                {
                    float firstPhase =
                        progress / 0.75f;

                    scale = Mathf.Lerp(
                        0f,
                        1.15f,
                        firstPhase
                    );
                }
                else
                {
                    float secondPhase =
                        (progress - 0.75f) / 0.25f;

                    scale = Mathf.Lerp(
                        1.15f,
                        1f,
                        secondPhase
                    );
                }

                playerMovement.transform.localScale =
                    playerTargetScale * scale;

                yield return null;
            }

            playerMovement.transform.localScale =
                playerTargetScale;
        }

        yield return
            new WaitForSecondsRealtime(0.05f);

        IsGameplayStarted = true;
    }

    private void Update()
    {
        if (!IsGameplayStarted)
            return;

        if (gameEnded)
            return;

        if (playerMovement != null &&
            playerMovement.IsGameOver)
        {
            return;
        }

        if (Time.timeScale <= 0f)
            return;

        gameTimer += Time.unscaledDeltaTime;
    }

    public void WinGame(int score)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        IsGameplayStarted = false;

        Time.timeScale = 1f;

        if (TimeSlowController.Instance != null)
        {
            TimeSlowController.Instance
                .ForceStopForGameEnd();
        }

        StatsManager.AddRun();
        StatsManager.AddWin();
        StatsManager.AddPlayTime(gameTimer);

        if (playerMovement != null)
            playerMovement.SetGameOver(true);

        string bestTimeKey =
            GetBestTimeKey();

        float bestTime =
            PlayerPrefs.GetFloat(
                bestTimeKey,
                Mathf.Infinity
            );

        if (gameTimer < bestTime)
        {
            PlayerPrefs.SetFloat(
                bestTimeKey,
                gameTimer
            );
        }

        if (levelManager != null &&
            levelManager.currentLevel != null &&
            SelectedLevelData.isLevelMode)
        {
            int levelNumber =
                levelManager.currentLevel.levelNumber;

            int unlockedLevel =
                PlayerPrefs.GetInt(
                    "UnlockedLevel",
                    1
                );

            if (levelNumber >= unlockedLevel)
            {
                PlayerPrefs.SetInt(
                    "UnlockedLevel",
                    levelNumber + 1
                );
            }

            PlayerPrefs.SetInt(
                "CompletedLevel_" + levelNumber,
                1
            );
        }

        PlayerPrefs.Save();

        SetHUD(false);
        StopLaserSystems();

        if (gameResultUI != null)
        {
            gameResultUI.ShowWin(
                score,
                gameTimer
            );
        }

        gameTimerComponent?.StopTimer();
        bossScreenEffect?.StopEffect();

        StopMusic();

        if (soundManager != null)
            soundManager.PlayWinSound();

        StartCoroutine(FreezeGameRoutine());
    }

    public void GameOver(int score)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        IsGameplayStarted = false;

        Time.timeScale = 1f;

        if (TimeSlowController.Instance != null)
        {
            TimeSlowController.Instance
                .ForceStopForGameEnd();
        }

        StatsManager.AddRun();
        StatsManager.AddDeath();
        StatsManager.AddPlayTime(gameTimer);

        if (playerMovement != null)
            playerMovement.SetGameOver(true);

        if (playerDash != null)
            playerDash.StopDash();

        SetHUD(false);
        StopLaserSystems();

        if (gameResultUI != null)
        {
            gameResultUI.ShowLose(
                score,
                gameTimer
            );
        }

        gameTimerComponent?.StopTimer();
        bossScreenEffect?.StopEffect();

        StopMusic();

        if (soundManager != null)
            soundManager.PlayLoseSound();

        StartCoroutine(FreezeGameRoutine());
    }

    private string GetBestTimeKey()
    {
        if (SelectedLevelData.isLevelMode &&
            levelManager != null &&
            levelManager.currentLevel != null)
        {
            return "BestTime_Level_" +
                   levelManager.currentLevel.levelNumber;
        }

        return "BestTime_DevRoom";
    }

    private IEnumerator FreezeGameRoutine()
    {
        if (gameFrozen)
            yield break;

        gameFrozen = true;

        yield return
            new WaitForSecondsRealtime(0.35f);

        Time.timeScale = 0f;

        Rigidbody2D[] bodies =
            FindObjectsByType<Rigidbody2D>(
                FindObjectsInactive.Exclude
            );

        foreach (Rigidbody2D body in bodies)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
    }

    private void StopLaserSystems()
    {
        if (laserWallSpawner != null)
        {
            laserWallSpawner
                .StopLaserSystem();
        }

        if (horizontalLaserWallSpawner != null)
        {
            horizontalLaserWallSpawner
                .StopLaserSystem();
        }
    }

    private void SetHUD(bool state)
    {
        if (scoreHUD != null)
            scoreHUD.SetActive(state);

        if (timeHUD != null)
            timeHUD.SetActive(state);

        if (joystickHUD != null)
            joystickHUD.SetActive(state);

        if (dashHUD != null)
            dashHUD.SetActive(state);

        if (cloneHUD != null)
            cloneHUD.SetActive(state);

        if (pauseButtonHUD != null)
            pauseButtonHUD.SetActive(state);
    }

    private void StopMusic()
    {
        gameplayMusic?.StopImmediately();
    }

    public void RestartGame()
    {
        IsGameplayStarted = false;
        Time.timeScale = 0f;

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance
                .LoadSceneWithFade(
                    SceneManager
                        .GetActiveScene()
                        .name
                );
        }
        else
        {
            SceneManager.LoadScene(
                SceneManager
                    .GetActiveScene()
                    .buildIndex
            );
        }
    }

    private void FindMissingReferences()
    {
        if (playerMovement == null)
        {
            playerMovement =
                FindAnyObjectByType<PlayerMovement>();
        }

        if (playerDash == null)
        {
            playerDash =
                FindAnyObjectByType<PlayerDash>();
        }

        if (soundManager == null)
        {
            soundManager =
                FindAnyObjectByType<SoundManager>();
        }

        if (gameResultUI == null)
        {
            gameResultUI =
                FindAnyObjectByType<GameResultUI>();
        }

        if (laserWallSpawner == null)
        {
            laserWallSpawner =
                FindAnyObjectByType<LaserWallSpawner>();
        }

        if (horizontalLaserWallSpawner == null)
        {
            horizontalLaserWallSpawner =
                FindAnyObjectByType
                    <HorizontalLaserWallSpawner>();
        }

        if (obstacleSpawner == null)
        {
            obstacleSpawner =
                FindAnyObjectByType<ObstacleSpawner>();
        }

        if (tutorialPanelUI == null)
        {
            tutorialPanelUI =
                FindAnyObjectByType<TutorialPanelUI>();
        }

        if (gameplayMusic == null)
        {
            gameplayMusic =
                FindAnyObjectByType<GameplayMusicFade>();
        }

        levelManager =
            FindAnyObjectByType<LevelManager>();

        gameTimerComponent =
            FindAnyObjectByType<GameTimer>();

        bossScreenEffect =
            FindAnyObjectByType<BossScreenEffect>();
    }

    private void OnDestroy()
    {
        IsGameplayStarted = false;
    }
}