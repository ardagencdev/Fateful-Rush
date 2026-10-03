using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class GameStateManager : MonoBehaviour
{
    public static bool IsGameplayStarted { get; private set; }
    public static bool IsGameplayEnded { get; private set; }

    [Header("References")]
    public PlayerMovement playerMovement;
    public PlayerDash playerDash;
    public PlayerCoinCollector playerCoinCollector;
    public SoundManager soundManager;
    public GameResultUI gameResultUI;

    public LaserWallSpawner laserWallSpawner;
    public HorizontalLaserWallSpawner horizontalLaserWallSpawner;
    public ObstacleSpawner obstacleSpawner;

    [Header("Gameplay Music")]
    [SerializeField]
    private GameplayMusicFade gameplayMusic;

    [SerializeField]
    private DynamicMusicTension dynamicMusicTension;

    [Header("Win Condition Intro")]
    [SerializeField]
    private bool showWinConditionIntro = true;

    [SerializeField, Min(0.5f)]
    private float winConditionIntroDuration = 2.5f;

    [SerializeField, Min(0f)]
    private float winConditionIntroFadeDuration = 0.3f;

    [SerializeField]
    private bool allowWinConditionIntroSkip = true;

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
    private WinConditionIntroUI winConditionIntroUI;
    private CurrentLevelHUD currentLevelHUD;
    private HUDPlayerOcclusionController hudPlayerOcclusion;

    private bool gameFrozen;
    private bool gameEnded;
    private float gameTimer;

    public float ElapsedGameTime => gameTimer;

    private LevelConfig CurrentLevel =>
        levelManager != null
            ? levelManager.currentLevel
            : null;

    private int CurrentScore =>
        playerCoinCollector != null
            ? playerCoinCollector.Score
            : 0;

    private void Awake()
    {
        FindMissingReferences();
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1f;

        IsGameplayStarted = false;
        IsGameplayEnded = false;
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

            playerMovement.PrepareForGameplay();
            playerMovement.gameObject.SetActive(false);
        }

        SetHUD(false);

        LevelConfig currentLevel = null;

        if (levelManager != null)
        {
            levelManager.InitializeLevel();
            currentLevel = levelManager.currentLevel;
        }

        EnsureCurrentLevelHUD(currentLevel);
        EnsureHUDPlayerOcclusion();

        gameplayMusic?.PlayClipAndFadeIn(
            currentLevel != null
                ? currentLevel.gameplayMusic
                : null
        );

        EnsureDynamicMusicTension(currentLevel);

        yield return null;

        SetHUD(false);

        Coroutine winConditionRoutine =
            StartWinConditionIntro(currentLevel);

        if (hudIntroAnimator != null)
        {
            RegisterCurrentLevelHUDForIntro();
            hudIntroAnimator.HideInstant();

            yield return
                hudIntroAnimator.PlayAndWait(
                    ShouldAnimateHUDItem
                );
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
            playerMovement.PrepareForGameplay();
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

        if (winConditionRoutine != null)
            yield return winConditionRoutine;

        yield return
            new WaitForSecondsRealtime(0.05f);

        FatefulRushGameStats.BeginRun(
            currentLevel != null ? currentLevel.levelNumber : 0
        );
        IsGameplayStarted = true;
    }

    private Coroutine StartWinConditionIntro(
        LevelConfig currentLevel)
    {
        if (!showWinConditionIntro ||
            currentLevel == null)
        {
            return null;
        }

        if (winConditionIntroUI == null)
        {
            winConditionIntroUI =
                GetComponent<WinConditionIntroUI>();
        }

        if (winConditionIntroUI == null)
        {
            winConditionIntroUI =
                gameObject.AddComponent
                    <WinConditionIntroUI>();
        }

        return StartCoroutine(
            winConditionIntroUI.PlayAndWait(
                currentLevel,
                winConditionIntroDuration,
                winConditionIntroFadeDuration,
                allowWinConditionIntroSkip
            )
        );
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

        if (levelManager != null &&
            levelManager.enemySpawner != null &&
            CurrentLevel != null &&
            CurrentLevel.UsesTime)
        {
            /*
             * bossSpawnTime, gameplay başladıktan sonra geçmesi gereken süreyi
             * temsil eder. EnemySpawner da doğrudan geçen oyun süresini bekler.
             */
            levelManager.enemySpawner
                .TrySpawnBossByTime(gameTimer);
        }

        CheckTimeObjective();
    }

    private void EnsureGameResultUIReference()
    {
        if (gameResultUI != null)
            return;

        gameResultUI =
            FindAnyObjectByType<GameResultUI>(
                FindObjectsInactive.Include
            );

        if (gameResultUI == null)
        {
            Debug.LogError(
                "[GameStateManager] GameResultUI bulunamadı. " +
                "Win/Lose ekranı gösterilemez.",
                this
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

        if (playerCoinCollector == null)
        {
            playerCoinCollector =
                FindAnyObjectByType<PlayerCoinCollector>();
        }

        if (soundManager == null)
        {
            soundManager =
                FindAnyObjectByType<SoundManager>();
        }

        EnsureGameResultUIReference();

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

        if (gameplayMusic == null)
        {
            gameplayMusic =
                FindAnyObjectByType<GameplayMusicFade>();
        }

        if (dynamicMusicTension == null)
        {
            dynamicMusicTension =
                GetComponent<DynamicMusicTension>();
        }

        levelManager =
            FindAnyObjectByType<LevelManager>();

        gameTimerComponent =
            FindAnyObjectByType<GameTimer>();

        winConditionIntroUI =
            GetComponent<WinConditionIntroUI>();
    }

    private void OnValidate()
    {
        winConditionIntroDuration =
            Mathf.Max(
                0.5f,
                winConditionIntroDuration
            );

        winConditionIntroFadeDuration =
            Mathf.Max(
                0f,
                winConditionIntroFadeDuration
            );
    }

    private void OnDestroy()
    {
        dynamicMusicTension = null;

        IsGameplayStarted = false;
        IsGameplayEnded = false;
    }
}
