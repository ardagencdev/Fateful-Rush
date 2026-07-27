using TMPro;
using UnityEngine;

public class StatsPanelUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject resetConfirmationPanel;

    [Header("Fade")]
    [SerializeField] private UIPanelFadeSwitcher fadeSwitcher;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI generalText;
    [SerializeField] private TextMeshProUGUI gameplayText;

    private void Awake()
    {
        if (fadeSwitcher == null)
            fadeSwitcher = GetComponent<UIPanelFadeSwitcher>();

        if (resetConfirmationPanel != null)
        {
            if (fadeSwitcher != null)
                fadeSwitcher.SetInstant(
                    resetConfirmationPanel,
                    false
                );
            else
                resetConfirmationPanel.SetActive(false);
        }
    }

    public void OpenStats()
    {
        HideResetConfirmation();
        RefreshStats();

        MainMenuStarColorRandomizer.Instance?
            .ShowStatsColor();

        Switch(
            optionsPanel,
            statsPanel
        );
    }

    public void CloseStats()
    {
        HideResetConfirmation();

        MainMenuStarColorRandomizer.Instance?
            .ShowOptionsColor();

        Switch(
            statsPanel,
            optionsPanel
        );
    }

    public void ShowResetConfirmation()
    {
        if (resetConfirmationPanel == null)
            return;

        if (fadeSwitcher != null)
        {
            fadeSwitcher.ShowPanel(resetConfirmationPanel);
            return;
        }

        resetConfirmationPanel.SetActive(true);
    }

    public void HideResetConfirmation()
    {
        if (resetConfirmationPanel == null)
            return;

        if (fadeSwitcher != null &&
            resetConfirmationPanel.activeSelf)
        {
            fadeSwitcher.HidePanel(resetConfirmationPanel);
            return;
        }

        resetConfirmationPanel.SetActive(false);
    }

    public void ConfirmResetStats()
    {
        StatsManager.ResetAllStats();

        RefreshStats();
        HideResetConfirmation();
    }

    private void Switch(
        GameObject fromPanel,
        GameObject toPanel
    )
    {
        if (fadeSwitcher != null)
        {
            fadeSwitcher.SwitchPanel(
                fromPanel,
                toPanel
            );

            return;
        }

        if (fromPanel != null)
            fromPanel.SetActive(false);

        if (toPanel != null)
            toPanel.SetActive(true);
    }

    private void RefreshStats()
    {
        int runs =
            StatsManager.GetTotalRuns();

        int wins =
            StatsManager.GetTotalWins();

        int deaths =
            StatsManager.GetTotalDeaths();

        float winRate =
            runs > 0
                ? wins / (float)runs * 100f
                : 0f;

        if (generalText != null)
        {
            generalText.text =
                "GENERAL\n" +
                $"Total Runs: {runs}\n" +
                $"Total Wins: {wins}\n" +
                $"Total Deaths: {deaths}\n" +
                $"Win Rate: {winRate:F1}%\n" +
                $"Total Play Time:\n" +
                FormatTime(
                    StatsManager.GetTotalPlayTime()
                );
        }

        if (gameplayText != null)
        {
            gameplayText.text =
                "GAMEPLAY\n" +
                $"Total Coins: {StatsManager.GetTotalCoins()}\n" +
                $"Coins Earned: {StatsManager.GetTotalCoinValue()}\n" +
                $"Normal Coins: {StatsManager.GetNormalCoins()}\n" +
                $"Gold Coins: {StatsManager.GetGoldCoins()}\n" +
                $"Rare Coins: {StatsManager.GetRareCoins()}\n\n" +
                $"Dash Uses: {StatsManager.GetDashUses()}\n" +
                $"Clone Uses: {StatsManager.GetCloneUses()}\n\n" +
                $"Slow Buff Uses: {StatsManager.GetSlowBuffUses()}\n" +
                $"Armor Buff Uses: {StatsManager.GetArmorBuffUses()}\n" +
                $"Armor Kills: {StatsManager.GetArmorKills()}";
        }
    }

    private static string FormatTime(
        float seconds
    )
    {
        int totalSeconds =
            Mathf.FloorToInt(
                Mathf.Max(0f, seconds)
            );

        int hours =
            totalSeconds / 3600;

        int minutes =
            totalSeconds % 3600 / 60;

        int secs =
            totalSeconds % 60;

        if (hours > 0)
            return $"{hours}h {minutes}m {secs}s";

        return $"{minutes}m {secs}s";
    }
}