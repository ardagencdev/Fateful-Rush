using System.Collections;
using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timerText;

    [Header("Optimization")]
    [Min(0f)]
    public float uiRefreshInterval = 0.05f;

    private GameStateManager gameStateManager;
    private LevelConfig levelConfig;

    private float elapsedTime;
    private float uiRefreshTimer;

    private bool showHUDTimer;
    private bool useCountdown;

    public bool IsTiming { get; private set; }

    public float ElapsedTime => elapsedTime;

    public float RemainingTime
    {
        get
        {
            if (!useCountdown ||
                levelConfig == null)
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                levelConfig.timeLimit - elapsedTime
            );
        }
    }

    private void Awake()
    {
        RefreshReferences();
    }

    private IEnumerator Start()
    {
        ApplyLevelConfig();

        ResetTimerState();
        UpdateUI();

        yield return new WaitUntil(
            () => GameStateManager.IsGameplayStarted
        );

        StartTimer();
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted)
            return;

        if (!IsTiming)
            return;

        if (Time.timeScale <= 0f)
            return;

        UpdateElapsedTime();

        uiRefreshTimer +=
            Time.unscaledDeltaTime;

        if (uiRefreshInterval <= 0f ||
            uiRefreshTimer >= uiRefreshInterval)
        {
            uiRefreshTimer = 0f;
            UpdateUI();
        }
    }

    public void StartTimer()
    {
        elapsedTime = 0f;
        uiRefreshTimer = 0f;
        IsTiming = true;

        UpdateUI();
    }

    public void StopTimer()
    {
        if (!IsTiming)
            return;

        UpdateElapsedTime();

        IsTiming = false;
        UpdateUI();
    }

    public void ResetTimer()
    {
        ResetTimerState();
        UpdateUI();
    }

    private void ResetTimerState()
    {
        elapsedTime = 0f;
        uiRefreshTimer = 0f;
        IsTiming = false;
    }

    private void UpdateElapsedTime()
    {
        if (gameStateManager != null)
        {
            elapsedTime =
                gameStateManager.ElapsedGameTime;

            return;
        }

        elapsedTime +=
            Time.unscaledDeltaTime;
    }

    private void ApplyLevelConfig()
    {
        LevelManager levelManager =
            FindAnyObjectByType<LevelManager>();

        levelConfig =
            levelManager != null
                ? levelManager.currentLevel
                : null;

        showHUDTimer =
            levelConfig != null &&
            levelConfig.showGameTimerHUD;

        useCountdown =
            levelConfig != null &&
            (
                levelConfig.winCondition ==
                    WinConditionType.SurviveTime ||
                levelConfig.winCondition ==
                    WinConditionType.ReachScoreWithinTime
            );

        if (timerText != null)
        {
            timerText.gameObject.SetActive(
                showHUDTimer
            );
        }
    }

    private void UpdateUI()
    {
        if (!showHUDTimer ||
            timerText == null)
        {
            return;
        }

        if (useCountdown &&
            levelConfig != null)
        {
            float remainingTime =
                Mathf.Max(
                    0f,
                    levelConfig.timeLimit -
                    elapsedTime
                );

            timerText.text =
                $"Time: {remainingTime:F1}";

            return;
        }

        timerText.text =
            $"Time: {elapsedTime:F1}";
    }

    private void RefreshReferences()
    {
        if (timerText == null)
        {
            timerText =
                GetComponent<TextMeshProUGUI>();
        }

        if (gameStateManager == null)
        {
            gameStateManager =
                FindAnyObjectByType<GameStateManager>();
        }

        if (timerText == null)
        {
            Debug.LogWarning(
                "GameTimer could not find a TextMeshProUGUI reference.",
                this
            );
        }

        if (gameStateManager == null)
        {
            Debug.LogWarning(
                "GameTimer could not find GameStateManager. " +
                "Timer will use its internal fallback time.",
                this
            );
        }
    }

    private void OnValidate()
    {
        uiRefreshInterval =
            Mathf.Max(
                0f,
                uiRefreshInterval
            );
    }
}
