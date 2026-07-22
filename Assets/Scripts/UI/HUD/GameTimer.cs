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

    private float elapsedTime;
    private float uiRefreshTimer;

    private bool showHUDTimer;

    public bool IsTiming { get; private set; }
    public float ElapsedTime => elapsedTime;

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

        // Pause sırasında timer ilerlemez.
        if (Time.timeScale <= 0f)
            return;

        // Slow etkisinden bağımsız gerçek oyun süresini sayar.
        elapsedTime += Time.unscaledDeltaTime;
        uiRefreshTimer += Time.unscaledDeltaTime;

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

    private void ApplyLevelConfig()
    {
        LevelManager levelManager =
            FindAnyObjectByType<LevelManager>();

        LevelConfig levelConfig =
            levelManager != null
                ? levelManager.currentLevel
                : null;

        showHUDTimer =
            levelConfig != null &&
            levelConfig.showGameTimerHUD;

        if (timerText != null)
            timerText.gameObject.SetActive(showHUDTimer);
    }

    private void UpdateUI()
    {
        if (!showHUDTimer || timerText == null)
            return;

        timerText.text =
            $"Time: {elapsedTime:F1}";
    }

    private void RefreshReferences()
    {
        if (timerText == null)
            timerText = GetComponent<TextMeshProUGUI>();

        if (timerText == null)
        {
            Debug.LogWarning(
                "GameTimer could not find a TextMeshProUGUI reference.",
                this
            );
        }
    }

    private void OnValidate()
    {
        uiRefreshInterval =
            Mathf.Max(0f, uiRefreshInterval);
    }
}