using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
    [SerializeField] private AudioSource gameplayMusicSource;
    [SerializeField] private float gameplayMusicFadeInDuration = 0.45f;
    [SerializeField] private float gameplayMusicBaseVolume = 1f;

    [Header("HUD")]
    public GameObject scoreHUD;
    public GameObject timeHUD;
    public GameObject joystickHUD;
    public GameObject dashHUD;
    public GameObject cloneHUD;
    public GameObject pauseButtonHUD;
    public HUDIntroAnimator hudIntroAnimator;

    private bool gameFrozen = false;
    private float gameTimer = 0f;

    private Coroutine gameplayMusicRoutine;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (playerDash == null)
            playerDash = FindAnyObjectByType<PlayerDash>();

        if (laserWallSpawner == null)
            laserWallSpawner = FindAnyObjectByType<LaserWallSpawner>();

        if (horizontalLaserWallSpawner == null)
            horizontalLaserWallSpawner = FindAnyObjectByType<HorizontalLaserWallSpawner>();

        if (obstacleSpawner == null)
            obstacleSpawner = FindAnyObjectByType<ObstacleSpawner>();

        if (tutorialPanelUI == null)
            tutorialPanelUI = FindAnyObjectByType<TutorialPanelUI>();

        CacheGameplayMusic();
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        IsGameplayStarted = false;
        gameTimer = 0f;

        CacheGameplayMusic();
        StopGameplayMusicImmediately();

        yield return null;

        StopGameplayMusicImmediately();

        Vector3 playerTargetScale = Vector3.one;

        if (playerMovement != null)
        {
            playerTargetScale = playerMovement.transform.localScale;
            playerMovement.StopMovement();
            playerMovement.gameObject.SetActive(false);
        }

        SetHUD(false);

        LevelConfig currentLevel = null;
        LevelManager levelManager = FindAnyObjectByType<LevelManager>();

        if (levelManager != null)
        {
            levelManager.InitializeLevel();
            currentLevel = levelManager.currentLevel;
        }

        bool levelAlreadyCompleted = false;

        if (currentLevel != null && SelectedLevelData.isLevelMode)
        {
            levelAlreadyCompleted =
                PlayerPrefs.GetInt("CompletedLevel_" + currentLevel.levelNumber, 0) == 1;
        }

        bool shouldShowTutorial =
            currentLevel != null &&
            currentLevel.showTutorial &&
            tutorialPanelUI != null &&
            !levelAlreadyCompleted;

        if (shouldShowTutorial)
        {
            StopGameplayMusicImmediately();

            yield return new WaitForSecondsRealtime(0.25f);

            bool tutorialClosed = false;

            tutorialPanelUI.ShowTutorial(
                currentLevel.tutorialTitle,
                currentLevel.tutorialPages,
                () => tutorialClosed = true
            );

            yield return new WaitUntil(() => tutorialClosed);
        }

        StartGameplayMusicFadeIn();

        yield return null;

        SetHUD(false);

        if (hudIntroAnimator != null)
        {
            hudIntroAnimator.HideInstant();
            yield return hudIntroAnimator.PlayAndWait();
        }
        else
        {
            SetHUD(true);
        }

        if (obstacleSpawner != null)
            yield return obstacleSpawner.PlaySpawnedObstaclePopupsAndWait();

        if (playerMovement != null)
        {
            playerMovement.gameObject.SetActive(true);
            playerMovement.transform.localScale = Vector3.zero;

            float timer = 0f;
            float duration = 0.18f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);

                float scale;

                if (t < 0.75f)
                {
                    float p = t / 0.75f;
                    scale = Mathf.Lerp(0f, 1.15f, p);
                }
                else
                {
                    float p = (t - 0.75f) / 0.25f;
                    scale = Mathf.Lerp(1.15f, 1f, p);
                }

                playerMovement.transform.localScale = playerTargetScale * scale;
                yield return null;
            }

            playerMovement.transform.localScale = playerTargetScale;
        }

        yield return new WaitForSecondsRealtime(0.05f);

        IsGameplayStarted = true;
    }

    private void Update()
    {
        if (!IsGameplayStarted)
            return;

        if (playerMovement != null && playerMovement.IsGameOver)
            return;

        gameTimer += Time.deltaTime;
    }

    public void WinGame(int score)
    {
        Time.timeScale = 1f;

        if (TimeSlowController.Instance != null)
            TimeSlowController.Instance.ForceStopForGameEnd();

        StatsManager.AddRun();
        StatsManager.AddWin();
        StatsManager.AddPlayTime(gameTimer);

        IsGameplayStarted = false;

        if (playerMovement != null)
            playerMovement.SetGameOver(true);

        LevelManager levelManager = FindAnyObjectByType<LevelManager>();
        string bestTimeKey = GetBestTimeKey(levelManager);

        float bestTime = PlayerPrefs.GetFloat(bestTimeKey, Mathf.Infinity);

        if (gameTimer < bestTime)
            PlayerPrefs.SetFloat(bestTimeKey, gameTimer);

        if (levelManager != null && levelManager.currentLevel != null && SelectedLevelData.isLevelMode)
        {
            int levelNumber = levelManager.currentLevel.levelNumber;
            int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

            if (levelNumber >= unlockedLevel)
                PlayerPrefs.SetInt("UnlockedLevel", levelNumber + 1);

            PlayerPrefs.SetInt("CompletedLevel_" + levelNumber, 1);
        }

        PlayerPrefs.Save();

        SetHUD(false);
        StopLaserSystems();

        if (gameResultUI != null)
            gameResultUI.ShowWin(score, gameTimer);

        FindAnyObjectByType<GameTimer>()?.StopTimer();
        FindAnyObjectByType<BossScreenEffect>()?.StopEffect();

        StopMusic();

        if (soundManager != null)
            soundManager.PlayWinSound();

        StartCoroutine(FreezeGameRoutine());
    }

    public void GameOver(int score)
    {
        Time.timeScale = 1f;

        if (TimeSlowController.Instance != null)
            TimeSlowController.Instance.ForceStopForGameEnd();

        StatsManager.AddRun();
        StatsManager.AddDeath();
        StatsManager.AddPlayTime(gameTimer);

        IsGameplayStarted = false;

        if (playerMovement != null)
            playerMovement.SetGameOver(true);

        if (playerDash != null)
            playerDash.StopDash();

        SetHUD(false);
        StopLaserSystems();

        if (gameResultUI != null)
            gameResultUI.ShowLose(score, gameTimer);

        FindAnyObjectByType<GameTimer>()?.StopTimer();
        FindAnyObjectByType<BossScreenEffect>()?.StopEffect();

        StopMusic();

        if (soundManager != null)
            soundManager.PlayLoseSound();

        StartCoroutine(FreezeGameRoutine());
    }

    private string GetBestTimeKey(LevelManager levelManager)
    {
        if (SelectedLevelData.isLevelMode && levelManager != null && levelManager.currentLevel != null)
            return "BestTime_Level_" + levelManager.currentLevel.levelNumber;

        return "BestTime_DevRoom";
    }

    private IEnumerator FreezeGameRoutine()
    {
        if (gameFrozen) yield break;
        gameFrozen = true;

        yield return new WaitForSecondsRealtime(0.35f);

        Time.timeScale = 0f;

        Rigidbody2D[] bodies = FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Exclude);

        foreach (Rigidbody2D rb in bodies)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    private void StopLaserSystems()
    {
        if (laserWallSpawner != null)
            laserWallSpawner.StopLaserSystem();

        if (horizontalLaserWallSpawner != null)
            horizontalLaserWallSpawner.StopLaserSystem();
    }

    private void SetHUD(bool state)
    {
        if (scoreHUD != null) scoreHUD.SetActive(state);
        if (timeHUD != null) timeHUD.SetActive(state);
        if (joystickHUD != null) joystickHUD.SetActive(state);
        if (dashHUD != null) dashHUD.SetActive(state);
        if (cloneHUD != null) cloneHUD.SetActive(state);
        if (pauseButtonHUD != null) pauseButtonHUD.SetActive(state);
    }

    private void CacheGameplayMusic()
    {
        if (gameplayMusicSource != null)
            return;

        GameplayMusicFade gameplayMusic = FindAnyObjectByType<GameplayMusicFade>();

        if (gameplayMusic != null)
            gameplayMusicSource = gameplayMusic.GetComponent<AudioSource>();

        if (gameplayMusicSource == null)
        {
            GameObject musicManager = GameObject.Find("MusicManager");

            if (musicManager != null)
                gameplayMusicSource = musicManager.GetComponent<AudioSource>();
        }
    }

    private void StopGameplayMusicImmediately()
    {
        CacheGameplayMusic();

        if (gameplayMusicRoutine != null)
            StopCoroutine(gameplayMusicRoutine);

        if (gameplayMusicSource == null)
            return;

        gameplayMusicSource.Stop();
        gameplayMusicSource.volume = 0f;
    }

    private void StartGameplayMusicFadeIn()
    {
        CacheGameplayMusic();

        if (gameplayMusicSource == null)
            return;

        if (gameplayMusicRoutine != null)
            StopCoroutine(gameplayMusicRoutine);

        gameplayMusicRoutine = StartCoroutine(GameplayMusicFadeInRoutine());
    }

    private IEnumerator GameplayMusicFadeInRoutine()
    {
        float targetVolume = PlayerPrefs.GetInt("SoundOn", 1) == 1
            ? PlayerPrefs.GetFloat("MusicVolume", 1f) * gameplayMusicBaseVolume
            : 0f;

        gameplayMusicSource.volume = 0f;
        gameplayMusicSource.Play();

        float timer = 0f;
        float duration = Mathf.Max(0.01f, gameplayMusicFadeInDuration);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            gameplayMusicSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        gameplayMusicSource.volume = targetVolume;
        gameplayMusicRoutine = null;
    }

    private void StopMusic()
    {
        if (gameplayMusicRoutine != null)
            StopCoroutine(gameplayMusicRoutine);

        StopGameplayMusicImmediately();
    }

    public void RestartGame()
    {
        IsGameplayStarted = false;
        Time.timeScale = 0f;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}