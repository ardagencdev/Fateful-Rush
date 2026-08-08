using UnityEngine;

public static class StatsManager
{
    private const int FirstLevelNumber = 1;
    private const int LastLevelNumber = 40;

    private const string TotalRunsKey = "Stats_TotalRuns";
    private const string TotalWinsKey = "Stats_TotalWins";
    private const string TotalDeathsKey = "Stats_TotalDeaths";
    private const string TotalCoinsKey = "Stats_TotalCoins";
    private const string TotalCoinValueKey = "Stats_TotalCoinValue";
    private const string NormalCoinsKey = "Stats_NormalCoins";
    private const string GoldCoinsKey = "Stats_GoldCoins";
    private const string RareCoinsKey = "Stats_RareCoins";
    private const string DashUsesKey = "Stats_DashUses";
    private const string CloneUsesKey = "Stats_CloneUses";
    private const string SlowBuffUsesKey = "Stats_SlowBuffUses";
    private const string ArmorBuffUsesKey = "Stats_ArmorBuffUses";

    // Key name is kept for backward compatibility with existing saves.
    // In the UI this statistic is presented as Armor Saves.
    private const string ArmorSavesKey = "Stats_ArmorKills";

    private const string TotalPlayTimeKey = "Stats_TotalPlayTime";
    private const string BestTimeLevelPrefix = "BestTime_Level_";
    private const string BestTimeDevRoomKey = "BestTime_DevRoom";

    private static bool dirty;

    public static bool HasUnsavedChanges => dirty;

    public static void AddRun() => AddInt(TotalRunsKey);
    public static void AddWin() => AddInt(TotalWinsKey);
    public static void AddDeath() => AddInt(TotalDeathsKey);
    public static void AddDashUse() => AddInt(DashUsesKey);
    public static void AddCloneUse() => AddInt(CloneUsesKey);
    public static void AddSlowBuffUse() => AddInt(SlowBuffUsesKey);
    public static void AddArmorBuffUse() => AddInt(ArmorBuffUsesKey);

    // Kept so existing call sites and serialized code remain compatible.
    public static void AddArmorKill() => AddArmorSave();
    public static void AddArmorSave() => AddInt(ArmorSavesKey);

    public static void AddCoin(int value, CoinType coinType)
    {
        AddInt(TotalCoinsKey);

        if (value > 0)
            AddInt(TotalCoinValueKey, value);

        switch (coinType)
        {
            case CoinType.Normal:
                AddInt(NormalCoinsKey);
                break;

            case CoinType.Gold:
                AddInt(GoldCoinsKey);
                break;

            case CoinType.Rare:
                AddInt(RareCoinsKey);
                break;

            default:
                Debug.LogWarning(
                    $"[StatsManager] Unknown coin type: {coinType}"
                );
                break;
        }
    }

    public static void AddPlayTime(float seconds)
    {
        if (seconds <= 0f ||
            float.IsNaN(seconds) ||
            float.IsInfinity(seconds))
        {
            return;
        }

        float currentPlayTime =
            PlayerPrefs.GetFloat(TotalPlayTimeKey, 0f);

        PlayerPrefs.SetFloat(
            TotalPlayTimeKey,
            currentPlayTime + seconds
        );

        dirty = true;
    }

    public static void SetBestTime(string key, float time)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning(
                "[StatsManager] Best time key is empty."
            );
            return;
        }

        if (time <= 0f ||
            float.IsNaN(time) ||
            float.IsInfinity(time))
        {
            Debug.LogWarning(
                $"[StatsManager] Invalid best time: {time}"
            );
            return;
        }

        float currentBestTime =
            PlayerPrefs.GetFloat(key, Mathf.Infinity);

        if (time >= currentBestTime)
            return;

        PlayerPrefs.SetFloat(key, time);
        dirty = true;
    }

    public static void SaveIfDirty()
    {
        if (!dirty)
            return;

        PlayerPrefs.Save();
        dirty = false;
    }

    public static int GetTotalRuns() => GetInt(TotalRunsKey);
    public static int GetTotalWins() => GetInt(TotalWinsKey);
    public static int GetTotalDeaths() => GetInt(TotalDeathsKey);
    public static int GetTotalCoins() => GetInt(TotalCoinsKey);
    public static int GetTotalCoinValue() => GetInt(TotalCoinValueKey);
    public static int GetNormalCoins() => GetInt(NormalCoinsKey);
    public static int GetGoldCoins() => GetInt(GoldCoinsKey);
    public static int GetRareCoins() => GetInt(RareCoinsKey);
    public static int GetDashUses() => GetInt(DashUsesKey);
    public static int GetCloneUses() => GetInt(CloneUsesKey);
    public static int GetSlowBuffUses() => GetInt(SlowBuffUsesKey);
    public static int GetArmorBuffUses() => GetInt(ArmorBuffUsesKey);
    public static int GetArmorKills() => GetInt(ArmorSavesKey);
    public static int GetArmorSaves() => GetInt(ArmorSavesKey);
    public static float GetTotalPlayTime() => GetFloat(TotalPlayTimeKey);

    public static float GetLevelBestTime(int levelNumber)
    {
        if (levelNumber < FirstLevelNumber ||
            levelNumber > LastLevelNumber)
        {
            return -1f;
        }

        return PlayerPrefs.GetFloat(
            BestTimeLevelPrefix + levelNumber,
            -1f
        );
    }

    public static float GetDevRoomBestTime()
    {
        return PlayerPrefs.GetFloat(BestTimeDevRoomKey, -1f);
    }

    public static int GetInt(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return 0;

        return PlayerPrefs.GetInt(key, 0);
    }

    public static float GetFloat(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return 0f;

        return PlayerPrefs.GetFloat(key, 0f);
    }

    private static void AddInt(string key, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(key) || amount == 0)
            return;

        int currentValue = PlayerPrefs.GetInt(key, 0);
        long newValue = (long)currentValue + amount;

        int safeValue;

        if (newValue <= 0L)
            safeValue = 0;
        else if (newValue >= int.MaxValue)
            safeValue = int.MaxValue;
        else
            safeValue = (int)newValue;

        PlayerPrefs.SetInt(key, safeValue);
        dirty = true;
    }

    public static void ResetAllStats()
    {
        DeleteKeys(
            TotalRunsKey,
            TotalWinsKey,
            TotalDeathsKey,
            TotalCoinsKey,
            TotalCoinValueKey,
            NormalCoinsKey,
            GoldCoinsKey,
            RareCoinsKey,
            DashUsesKey,
            CloneUsesKey,
            SlowBuffUsesKey,
            ArmorBuffUsesKey,
            ArmorSavesKey,
            TotalPlayTimeKey
        );

        for (int levelNumber = FirstLevelNumber;
             levelNumber <= LastLevelNumber;
             levelNumber++)
        {
            PlayerPrefs.DeleteKey(
                BestTimeLevelPrefix + levelNumber
            );
        }

        PlayerPrefs.DeleteKey(BestTimeDevRoomKey);

        PlayerPrefs.Save();
        dirty = false;
    }

    private static void DeleteKeys(params string[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(keys[i]))
                PlayerPrefs.DeleteKey(keys[i]);
        }
    }
}
