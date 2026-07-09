using UnityEngine;
using TMPro;
using System.Collections;

public class GameTimer : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timerText;

    [Header("Optimization")]
    public float uiRefreshInterval = 0.05f;

    private float elapsedTime;
    private float uiRefreshTimer;

    private bool showHUDTimer = false;

    public bool IsTiming { get; private set; }

    private void Awake()
    {
        if (timerText == null)
            timerText = GetComponent<TextMeshProUGUI>();
    }

    private IEnumerator Start()
    {
        ApplyLevelConfig();

        elapsedTime = 0f;
        uiRefreshTimer = 0f;
        IsTiming = false;

        UpdateUI();

        yield return new WaitUntil(() => GameStateManager.IsGameplayStarted);

        StartTimer();
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplayStarted) return;
        if (!IsTiming) return;

        if (Time.timeScale <= 0f)
            return;

        elapsedTime += Time.unscaledDeltaTime;
        uiRefreshTimer += Time.unscaledDeltaTime;

        if (uiRefreshTimer >= uiRefreshInterval)
        {
            uiRefreshTimer = 0f;
            UpdateUI();
        }
    }

    private void ApplyLevelConfig()
    {
        LevelManager levelManager = FindAnyObjectByType<LevelManager>();
        LevelConfig levelConfig = levelManager != null ? levelManager.currentLevel : null;

        showHUDTimer = levelConfig != null && levelConfig.showGameTimerHUD;

        if (timerText != null)
            timerText.gameObject.SetActive(showHUDTimer);
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
        IsTiming = false;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (!showHUDTimer) return;

        if (timerText != null)
            timerText.text = "Time: " + elapsedTime.ToString("F2");
    }
}