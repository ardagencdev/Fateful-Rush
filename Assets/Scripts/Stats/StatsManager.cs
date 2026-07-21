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
        /*
         * TotalCoins şu anda coin değerini değil,
         * toplanan coin adedini temsil ediyor.
         *
         * value parametresi mevcut çağrıları bozmamak
         * için korunuyor ancak istatistikte kullanılmıyor.
         */
        AddInt(TotalCoinsKey);

        if (value > 0)
        {
            AddInt(TotalCoinValueKey, value);
        }

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
                    $"Unknown coin type received: {coinType}"
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
                "Best time could not be saved because the key was empty."
            );

            return;
        }

        if (time <= 0f ||
            float.IsNaN(time) ||
            float.IsInfinity(time))
        {
            Debug.LogWarning(
                $"Invalid best time value received: {time}"
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
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (amount == 0)
            return;

        int currentValue =
            PlayerPrefs.GetInt(key, 0);

        long newValue =
            (long)currentValue + amount;

        int safeValue = (int)Mathf.Clamp(
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
            PlayerPrefs.DeleteKey(keys[i]);
        }
    }
}