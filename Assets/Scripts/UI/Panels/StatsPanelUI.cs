using TMPro;
using UnityEngine;

public class StatsPanelUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject statsPanel;

    [Header("Fade")]
    public UIPanelFadeSwitcher fadeSwitcher;

    [Header("Texts")]
    public TextMeshProUGUI generalText;
    public TextMeshProUGUI gameplayText;
    public TextMeshProUGUI bestTimesLeftText;
    public TextMeshProUGUI bestTimesRightText;

    private void Awake()
    {
        if (fadeSwitcher == null)
            fadeSwitcher = GetComponent<UIPanelFadeSwitcher>();
    }

    public void OpenStats()
    {
        RefreshStats();
        Switch(mainMenuPanel, statsPanel);
    }

    public void CloseStats()
    {
        Switch(statsPanel, mainMenuPanel);
    }

    public void ResetStats()
    {
        StatsManager.ResetAllStats();
        RefreshStats();
    }

    private void Switch(GameObject fromPanel, GameObject toPanel)
    {
        if (fadeSwitcher != null)
            fadeSwitcher.SwitchPanel(fromPanel, toPanel);
        else
        {
            if (fromPanel != null) fromPanel.SetActive(false);
            if (toPanel != null) toPanel.SetActive(true);
        }
    }

    private void RefreshStats()
    {
        if (generalText == null || gameplayText == null || bestTimesLeftText == null || bestTimesRightText == null)
            return;

        int runs = StatsManager.GetInt("Stats_TotalRuns");
        int wins = StatsManager.GetInt("Stats_TotalWins");
        int deaths = StatsManager.GetInt("Stats_TotalDeaths");

        float winRate = runs > 0 ? (wins / (float)runs) * 100f : 0f;
        float playTime = StatsManager.GetFloat("Stats_TotalPlayTime");

        generalText.text =
            "GENERAL\n" +
            $"Total Runs: {runs}\n" +
            $"Total Wins: {wins}\n" +
            $"Total Deaths: {deaths}\n" +
            $"Win Rate: {winRate:F1}%\n" +
            $"Total Play Time:\n{FormatTime(playTime)}";

        gameplayText.text =
            "GAMEPLAY\n" +
            $"Total Coins: {StatsManager.GetInt("Stats_TotalCoins")}\n" +
            $"Normal Coins: {StatsManager.GetInt("Stats_NormalCoins")}\n" +
            $"Gold Coins: {StatsManager.GetInt("Stats_GoldCoins")}\n" +
            $"Rare Coins: {StatsManager.GetInt("Stats_RareCoins")}\n\n" +
            $"Dash Uses: {StatsManager.GetInt("Stats_DashUses")}\n" +
            $"Clone Uses: {StatsManager.GetInt("Stats_CloneUses")}\n\n" +
            $"Slow Buff Uses: {StatsManager.GetInt("Stats_SlowBuffUses")}\n" +
            $"Armor Buff Uses: {StatsManager.GetInt("Stats_ArmorBuffUses")}\n" +
            $"Armor Kills: {StatsManager.GetInt("Stats_ArmorKills")}";

        bestTimesLeftText.text =
            "BEST TIMES\n" +
            $"Dev Room: {FormatBestTime("BestTime_DevRoom")}\n" +
            GetBestTimesText(1, 10);

        bestTimesRightText.text =
            "\n\n" +
            GetBestTimesText(11, 20);
    }

    private string GetBestTimesText(int startLevel, int endLevel)
    {
        string result = "";

        for (int i = startLevel; i <= endLevel; i++)
            result += $"Level {i}: {FormatBestTime("BestTime_Level_" + i)}\n";

        return result;
    }

    private string FormatBestTime(string key)
    {
        float time = PlayerPrefs.GetFloat(key, -1f);
        return time < 0f ? "--" : time.ToString("F1") + "s";
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;

        if (hours > 0)
            return $"{hours}h {minutes}m {secs}s";

        return $"{minutes}m {secs}s";
    }
}