using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
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
    private BossScreenEffect bossScreenEffect;
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

    public void CheckScoreObjective(int currentScore)
    {
        if (!IsGameplayStarted)
            return;

        if (gameEnded)
            return;

        LevelConfig currentLevel = CurrentLevel;

        if (currentLevel == null)
            return;

        if (!currentLevel.UsesScore)
            return;

        switch (currentLevel.winCondition)
        {
            case WinConditionType.ReachScore:

                if (currentScore >=
                    currentLevel.SafeWinScore)
                {
                    WinGame(currentScore);
                }

                break;

            case WinConditionType.ReachScoreWithinTime:

                if (gameTimer >
                    currentLevel.SafeTimeLimit)
                {
                    return;
                }

                if (currentScore >=
                    currentLevel.SafeWinScore)
                {
                    WinGame(currentScore);
                }

                break;
        }
    }

    private void CheckTimeObjective()
    {
        LevelConfig currentLevel = CurrentLevel;

        if (currentLevel == null)
            return;

        if (!currentLevel.UsesTime)
            return;

        if (gameTimer < currentLevel.SafeTimeLimit)
            return;

        gameTimer = currentLevel.SafeTimeLimit;

        switch (currentLevel.winCondition)
        {
            case WinConditionType.SurviveTime:
                WinGame(CurrentScore);
                break;

            case WinConditionType.ReachScoreWithinTime:
                GameOver(
                    CurrentScore,
                    "TIME EXPIRED"
                );
                break;
        }
    }

    public void WinGame(int score)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        IsGameplayStarted = false;
        IsGameplayEnded = true;

        Time.timeScale = 1f;

        if (TimeSlowController.Instance != null)
        {
            TimeSlowController.Instance
                .ForceStopForGameEnd();
        }

        // Results/win/lose audio must start from a clean mixer state even
        // if the run ended during Slow + BossDanger.
        GameAudioMixerController.ResetTransientState(0.12f);

        StatsManager.AddRun();
        StatsManager.AddWin();
        StatsManager.AddPlayTime(gameTimer);
        StatsManager.RecordRunDetails(
            true,
            score,
            gameTimer,
            CurrentLevel != null
                ? CurrentLevel.winCondition
                : WinConditionType.ReachScore,
            playerCoinCollector != null
                ? playerCoinCollector.CoinsCollectedThisRun
                : 0
        );

        int completedLevelNumber = 0;
        bool isFirstCompletion = false;

        if (playerMovement != null)
            playerMovement.SetGameOver(true);

        if (CurrentLevel != null &&
            CurrentLevel.CanSaveBestTime)
        {
            SaveBestTime();
        }

        if (levelManager != null &&
            levelManager.currentLevel != null &&
            SelectedLevelData.isLevelMode)
        {
            int levelNumber =
                levelManager.currentLevel.levelNumber;

            completedLevelNumber =
                levelNumber;

            isFirstCompletion =
                PlayerPrefs.GetInt(
                    "CompletedLevel_" +
                    levelNumber,
                    0
                ) == 0;

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

        StatsManager.SaveIfDirty();

        SetHUD(false);
        StopGameplayImmediately();

        if (gameResultUI != null)
        {
            gameResultUI.ShowWin(
                score,
                gameTimer,
                completedLevelNumber,
                isFirstCompletion
            );
        }

        gameTimerComponent?.StopTimer();
        bossScreenEffect?.StopEffect();

        gameplayMusic?.ResetTension(true);
        StopMusic();

        if (soundManager != null)
            soundManager.PlayWinSound();

        VibrationManager.Instance?.VibrateSuccess();

    }

    public void GameOver(int score)
    {
        GameOver(
        score,
        LastDeathInfo.Cause
    );
    }

    public void GameOver(
        int score,
        string cause)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        IsGameplayStarted = false;
        IsGameplayEnded = true;

        Time.timeScale = 1f;

        if (TimeSlowController.Instance != null)
        {
            TimeSlowController.Instance
                .ForceStopForGameEnd();
        }

        // Results/win/lose audio must start from a clean mixer state even
        // if the run ended during Slow + BossDanger.
        GameAudioMixerController.ResetTransientState(0.12f);

        StatsManager.AddRun();
        StatsManager.AddDeath();
        StatsManager.AddPlayTime(gameTimer);
        StatsManager.RecordRunDetails(
            false,
            score,
            gameTimer,
            CurrentLevel != null
                ? CurrentLevel.winCondition
                : WinConditionType.ReachScore,
            playerCoinCollector != null
                ? playerCoinCollector.CoinsCollectedThisRun
                : 0,
            cause
        );
        StatsManager.SaveIfDirty();

        if (playerMovement != null)
            playerMovement.SetGameOver(true);

        if (playerDash != null)
            playerDash.StopDash();

        // One authoritative death impact. PlayerMovement used to trigger a
        // second shake immediately before this one, causing the two effects
        // to fight each other.
        CameraShake.Instance?.Shake(
            0.44f,
            0.34f
        );

        SetHUD(false);
        StopGameplayImmediately();

        if (gameResultUI != null)
        {
            gameResultUI.ShowLose(
                score,
                gameTimer,
                cause
            );
        }

        gameTimerComponent?.StopTimer();
        bossScreenEffect?.StopEffect();

        gameplayMusic?.ResetTension(true);
        StopMusic();

        if (soundManager != null)
            soundManager.PlayLoseSound();

        VibrationManager.Instance?.VibrateFailure();

    }

    private void SaveBestTime()
    {
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

    private void StopGameplayImmediately()
    {
        if (gameFrozen)
            return;

        gameFrozen = true;

        // Result ekranı görünür görünmez gameplay'in yeni frame üretmesini durdur.
        // UI/scene geçişleri unscaled time kullandığı için çalışmaya devam eder.
        Time.timeScale = 0f;

        StopLaserSystems();
        StopBossAoeSystems();
        StopActiveGameplayAudio();
        FreezeActiveRigidbodies();
    }

    private static void StopBossAoeSystems()
    {
        BossEnemyFollow[] bosses =
            FindObjectsByType<BossEnemyFollow>(
                FindObjectsInactive.Exclude
            );

        for (int i = 0; i < bosses.Length; i++)
        {
            if (bosses[i] != null)
                bosses[i].StopForGameEnd();
        }

        MiniBossFollow[] miniBosses =
            FindObjectsByType<MiniBossFollow>(
                FindObjectsInactive.Exclude
            );

        for (int i = 0; i < miniBosses.Length; i++)
        {
            if (miniBosses[i] != null)
                miniBosses[i].StopForGameEnd();
        }
    }

    private void StopActiveGameplayAudio()
    {
        soundManager?.StopAllSfx();

        // Hunter/projectile/laser gibi kendi AudioSource'unu kullanan gameplay
        // objeleri de sonuç ekranından sonra ses üretmeye devam etmesin.
        AudioSource[] activeSources =
            FindObjectsByType<AudioSource>(
                FindObjectsInactive.Exclude
            );

        for (int i = 0; i < activeSources.Length; i++)
        {
            AudioSource source = activeSources[i];

            if (source != null && source.isPlaying)
                source.Stop();
        }
    }

    private static void FreezeActiveRigidbodies()
    {
        Rigidbody2D[] bodies =
            FindObjectsByType<Rigidbody2D>(
                FindObjectsInactive.Exclude
            );

        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody2D body = bodies[i];

            if (body == null)
                continue;

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
        LevelConfig level = CurrentLevel;

        if (!state || level == null)
        {
            SetHUDObject(scoreHUD, false);
            SetHUDObject(timeHUD, false);
            SetHUDObject(joystickHUD, false);
            SetHUDObject(dashHUD, false);
            SetHUDObject(cloneHUD, false);
            SetHUDObject(pauseButtonHUD, false);
            SetCurrentLevelHUDVisible(false);
            return;
        }

        SetHUDObject(
            scoreHUD,
            level.ScoreHUDEnabled
        );

        SetHUDObject(
            timeHUD,
            level.TimerHUDEnabled
        );

        SetHUDObject(joystickHUD, true);

        SetHUDObject(
            dashHUD,
            level.dashEnabled
        );

        SetHUDObject(
            cloneHUD,
            level.cloneEnabled
        );

        SetHUDObject(pauseButtonHUD, true);
        SetCurrentLevelHUDVisible(true);
    }

    private void EnsureCurrentLevelHUD(
        LevelConfig level)
    {
        if (currentLevelHUD != null ||
            level == null ||
            level.levelNumber <= 0)
        {
            return;
        }

        Canvas hudCanvas = FindHUDCanvas();

        if (hudCanvas == null)
        {
            Debug.LogWarning(
                "[GameStateManager] Current Level HUD için Canvas bulunamadı.",
                this
            );
            return;
        }

        Color appliedNearStarsColor =
            level.nearStarsColor;

        if (levelManager != null &&
            levelManager.starfieldController != null)
        {
            // StarfieldController already resolved the level color before the
            // HUD is created, so this is the exact color visible in NearStars.
            appliedNearStarsColor =
                levelManager.starfieldController.CurrentNearStarsColor;
        }

        int siblingIndex =
            GetHUDInsertSiblingIndex(hudCanvas);

        currentLevelHUD =
            CurrentLevelHUD.Create(
                level,
                appliedNearStarsColor,
                hudCanvas,
                siblingIndex
            );
    }

    private void EnsureHUDPlayerOcclusion()
    {
        if (playerMovement == null)
            return;

        Canvas hudCanvas = FindHUDCanvas();

        if (hudCanvas == null)
            return;

        if (hudPlayerOcclusion == null)
        {
            hudPlayerOcclusion =
                GetComponent<HUDPlayerOcclusionController>();

            if (hudPlayerOcclusion == null)
            {
                hudPlayerOcclusion =
                    gameObject.AddComponent
                        <HUDPlayerOcclusionController>();
            }
        }

        hudPlayerOcclusion.Configure(
            playerMovement.transform,
            hudCanvas,
            hudIntroAnimator,
            scoreHUD,
            timeHUD,
            joystickHUD,
            dashHUD,
            cloneHUD,
            pauseButtonHUD,
            currentLevelHUD != null
                ? currentLevelHUD.gameObject
                : null
        );

        // Root referanslarının yanında gerçek TMP componentlerini de doğrudan
        // kaydet. Böylece sahne hiyerarşisi değişse bile Score / Timer kesin
        // olarak aynı occlusion sistemine girer.
        if (playerCoinCollector != null &&
            playerCoinCollector.scoreText != null)
        {
            hudPlayerOcclusion.RegisterHUDRoot(
                playerCoinCollector.scoreText.gameObject
            );
        }

        if (gameTimerComponent != null &&
            gameTimerComponent.timerText != null)
        {
            hudPlayerOcclusion.RegisterHUDRoot(
                gameTimerComponent.timerText.gameObject
            );
        }

        if (currentLevelHUD != null)
        {
            hudPlayerOcclusion.RegisterHUDRoot(
                currentLevelHUD.gameObject
            );
        }
    }

    private void RegisterCurrentLevelHUDForIntro()
    {
        if (hudIntroAnimator == null ||
            currentLevelHUD == null)
        {
            return;
        }

        hudIntroAnimator.RegisterRuntimeItem(
            currentLevelHUD.gameObject
        );
    }

    private Canvas FindHUDCanvas()
    {
        GameObject[] hudObjects =
        {
            scoreHUD,
            timeHUD,
            joystickHUD,
            dashHUD,
            cloneHUD,
            pauseButtonHUD
        };

        for (int i = 0;
             i < hudObjects.Length;
             i++)
        {
            GameObject hudObject = hudObjects[i];

            if (hudObject == null)
                continue;

            Canvas canvas =
                hudObject.GetComponentInParent
                    <Canvas>(true);

            if (canvas != null)
                return canvas;
        }

        return FindAnyObjectByType<Canvas>();
    }

    private int GetHUDInsertSiblingIndex(
        Canvas hudCanvas)
    {
        if (hudCanvas == null)
            return 0;

        GameObject[] hudObjects =
        {
            scoreHUD,
            timeHUD,
            joystickHUD,
            dashHUD,
            cloneHUD,
            pauseButtonHUD
        };

        int highestHudSiblingIndex = -1;

        for (int i = 0;
             i < hudObjects.Length;
             i++)
        {
            Transform topLevelHudTransform =
                GetTopLevelChildUnderCanvas(
                    hudObjects[i],
                    hudCanvas.transform
                );

            if (topLevelHudTransform == null)
                continue;

            highestHudSiblingIndex =
                Mathf.Max(
                    highestHudSiblingIndex,
                    topLevelHudTransform.GetSiblingIndex()
                );
        }

        if (highestHudSiblingIndex < 0)
            return hudCanvas.transform.childCount;

        return Mathf.Min(
            highestHudSiblingIndex + 1,
            hudCanvas.transform.childCount
        );
    }

    private static Transform GetTopLevelChildUnderCanvas(
        GameObject hudObject,
        Transform canvasTransform)
    {
        if (hudObject == null ||
            canvasTransform == null)
        {
            return null;
        }

        Transform current = hudObject.transform;

        while (current != null &&
               current.parent != null &&
               current.parent != canvasTransform)
        {
            current = current.parent;
        }

        return current != null &&
               current.parent == canvasTransform
            ? current
            : null;
    }

    private void SetCurrentLevelHUDVisible(
        bool visible)
    {
        if (currentLevelHUD != null)
            currentLevelHUD.SetVisible(visible);
    }

    private static void SetHUDObject(
        GameObject target,
        bool state)
    {
        if (target != null)
            target.SetActive(state);
    }

    private bool ShouldAnimateHUDItem(
        GameObject target)
    {
        if (target == null)
            return false;

        LevelConfig level = CurrentLevel;

        if (level == null)
            return false;

        if (target == scoreHUD)
            return level.ScoreHUDEnabled;

        if (target == timeHUD)
            return level.TimerHUDEnabled;

        if (target == dashHUD)
            return level.dashEnabled;

        if (target == cloneHUD)
            return level.cloneEnabled;

        return true;
    }

    private void EnsureDynamicMusicTension(
        LevelConfig currentLevel)
    {
        if (currentLevel == null ||
            gameplayMusic == null)
        {
            return;
        }

        if (dynamicMusicTension == null)
        {
            dynamicMusicTension =
                GetComponent<DynamicMusicTension>();
        }

        if (dynamicMusicTension == null)
        {
            dynamicMusicTension =
                gameObject.AddComponent<DynamicMusicTension>();
        }

        dynamicMusicTension.Configure(
            this,
            playerCoinCollector,
            gameplayMusic,
            currentLevel
        );
    }

    private void StopMusic()
    {
        gameplayMusic?.StopImmediately();
    }

    public void RestartGame()
    {
        IsGameplayStarted = false;
        IsGameplayEnded = false;
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

        bossScreenEffect =
            FindAnyObjectByType<BossScreenEffect>();

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