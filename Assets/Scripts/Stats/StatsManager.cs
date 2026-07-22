using UnityEngine;

public static class StatsManager
{
    private const int FirstLevelNumber = 1;
    private const int LastLevelNumber = 20;

    private const string NormalCoinType = "Normal";
    private const string GoldCoinType = "Gold";
    private const string RareCoinType = "Rare";

    private const string TotalRunsKey =
        "Stats_TotalRuns";

    private const string TotalWinsKey =
        "Stats_TotalWins";

    private const string TotalDeathsKey =
        "Stats_TotalDeaths";

    private const string TotalCoinsKey =
        "Stats_TotalCoins";

    private const string TotalCoinValueKey =
        "Stats_TotalCoinValue";

    private const string NormalCoinsKey =
        "Stats_NormalCoins";

    private const string GoldCoinsKey =
        "Stats_GoldCoins";

    private const string RareCoinsKey =
        "Stats_RareCoins";

    private const string DashUsesKey =
        "Stats_DashUses";

    private const string CloneUsesKey =
        "Stats_CloneUses";

    private const string SlowBuffUsesKey =
        "Stats_SlowBuffUses";

    private const string ArmorBuffUsesKey =
        "Stats_ArmorBuffUses";

    private const string ArmorKillsKey =
        "Stats_ArmorKills";

    private const string TotalPlayTimeKey =
        "Stats_TotalPlayTime";

    private const string BestTimeLevelPrefix =
        "BestTime_Level_";

    private const string BestTimeDevRoomKey =
        "BestTime_DevRoom";

    public static void AddRun()
    {
        AddInt(TotalRunsKey);
    }

    public static void AddWin()
    {
        AddInt(TotalWinsKey);
    }

    public static void AddDeath()
    {
        AddInt(TotalDeathsKey);
    }

    public static void AddDashUse()
    {
        AddInt(DashUsesKey);
    }

    public static void AddCloneUse()
    {
        AddInt(CloneUsesKey);
    }

    public static void AddSlowBuffUse()
    {
        AddInt(SlowBuffUsesKey);
    }

    public static void AddArmorBuffUse()
    {
        AddInt(ArmorBuffUsesKey);
    }

    public static void AddArmorKill()
    {
        AddInt(ArmorKillsKey);
    }

    public static void AddCoin(
        int value,
        string coinType
    )
    {
        AddInt(TotalCoinsKey);

        if (value > 0)
            AddInt(TotalCoinValueKey, value);

        switch (coinType)
        {
            case NormalCoinType:
                AddInt(NormalCoinsKey);
                break;

            case GoldCoinType:
                AddInt(GoldCoinsKey);
                break;

            case RareCoinType:
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
            PlayerPrefs.GetFloat(
                TotalPlayTimeKey,
                0f
            );

        PlayerPrefs.SetFloat(
            TotalPlayTimeKey,
            currentPlayTime + seconds
        );

        PlayerPrefs.Save();
    }

    public static void SetBestTime(
        string key,
        float time
    )
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
            PlayerPrefs.GetFloat(
                key,
                Mathf.Infinity
            );

        if (time >= currentBestTime)
            return;

        PlayerPrefs.SetFloat(key, time);
        PlayerPrefs.Save();
    }

    public static int GetTotalRuns()
    {
        return GetInt(TotalRunsKey);
    }

    public static int GetTotalWins()
    {
        return GetInt(TotalWinsKey);
    }

    public static int GetTotalDeaths()
    {
        return GetInt(TotalDeathsKey);
    }

    public static int GetTotalCoins()
    {
        return GetInt(TotalCoinsKey);
    }

    public static int GetTotalCoinValue()
    {
        return GetInt(TotalCoinValueKey);
    }

    public static int GetNormalCoins()
    {
        return GetInt(NormalCoinsKey);
    }

    public static int GetGoldCoins()
    {
        return GetInt(GoldCoinsKey);
    }

    public static int GetRareCoins()
    {
        return GetInt(RareCoinsKey);
    }

    public static int GetDashUses()
    {
        return GetInt(DashUsesKey);
    }

    public static int GetCloneUses()
    {
        return GetInt(CloneUsesKey);
    }

    public static int GetSlowBuffUses()
    {
        return GetInt(SlowBuffUsesKey);
    }

    public static int GetArmorBuffUses()
    {
        return GetInt(ArmorBuffUsesKey);
    }

    public static int GetArmorKills()
    {
        return GetInt(ArmorKillsKey);
    }

    public static float GetTotalPlayTime()
    {
        return GetFloat(TotalPlayTimeKey);
    }

    public static float GetLevelBestTime(
        int levelNumber
    )
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
        return PlayerPrefs.GetFloat(
            BestTimeDevRoomKey,
            -1f
        );
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

    private static void AddInt(
        string key,
        int amount = 1
    )
    {
        if (string.IsNullOrWhiteSpace(key) ||
            amount == 0)
        {
            return;
        }

        int currentValue =
            PlayerPrefs.GetInt(key, 0);

        long newValue =
            (long)currentValue + amount;

        int safeValue =
            (int)Mathf.Clamp(
                newValue,
                0L,
                int.MaxValue
            );

        PlayerPrefs.SetInt(key, safeValue);
        PlayerPrefs.Save();
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
            ArmorKillsKey,
            TotalPlayTimeKey
        );

        for (int levelNumber = FirstLevelNumber;
             levelNumber <= LastLevelNumber;
             levelNumber++)
        {
            PlayerPrefs.DeleteKey(
                BestTimeLevelPrefix +
                levelNumber
            );
        }

        PlayerPrefs.DeleteKey(
            BestTimeDevRoomKey
        );

        PlayerPrefs.Save();
    }

    private static void DeleteKeys(
        params string[] keys
    )
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(keys[i]))
                PlayerPrefs.DeleteKey(keys[i]);
        }
    }
}