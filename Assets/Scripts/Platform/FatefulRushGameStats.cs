using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

// Matches the published run_completed / progressUpdate Console schemas.
public static class FatefulRushGameStats
{
    private static bool runStarted;
    private static bool tracking;
    private static int level;
    private static int nearMisses;
    private static int highestCombo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        runStarted = false;
        tracking = false;
    }

    public static void BeginRun(int levelNumber)
    {
        level = levelNumber;
        runStarted = level >= 1 && level <= 40;
        tracking =
            FatefulRushFirebaseServices.GetBool(
                FatefulRushFirebaseServices.GameStatsEnabledKey,
                true
            ) &&
            level >= 1 &&
            level <= 40;
        nearMisses = 0;
        highestCombo = 1;

        if (tracking)
        {
            FatefulRushFirebaseServices.SetCustomKey(
                "current_level",
                level.ToString()
            );
        }
    }

    public static void RecordNearMiss()
    {
        if (tracking) nearMisses = Math.Min(int.MaxValue - 1, nearMisses) + 1;
    }

    public static void RecordCombo(int multiplier)
    {
        if (tracking) highestCombo = Mathf.Max(highestCombo, Mathf.Clamp(multiplier, 1, 6));
    }

    public static void EndRun(bool won, int score, float seconds, int coins)
    {
        if (!runStarted)
        {
            tracking = false;
            return;
        }

        bool shouldRecordGameStats = tracking;
        runStarted = false;
        tracking = false; // Duplicate result callbacks must not count twice.
        FatefulRushReviewPrompt.NotifyRunFinished();

        if (!shouldRecordGameStats)
            return;

        if (!FatefulRushFirebaseServices.GetBool(
                FatefulRushFirebaseServices.GameStatsEnabledKey,
                true
            ))
        {
            return;
        }

        FatefulRushFirebaseServices.SetCustomKey(
            "last_run_result",
            won ? "win" : "loss"
        );
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!GooglePlayGamesManager.IsAuthenticated) return;
        try
        {
            double duration = float.IsNaN(seconds) || float.IsInfinity(seconds)
                ? 0d : Math.Max(0d, seconds);
            var result = new PlayerGameEvent.Builder("run_completed")
                .AddProperty("level_number", (long)level)
                .AddProperty("is_win", won ? 1L : 0L)
                .AddProperty("coins_collected", (long)Mathf.Max(0, coins))
                .AddProperty("near_misses", (long)nearMisses)
                .AddProperty("highest_combo", (long)highestCombo)
                .AddProperty("score", (long)Mathf.Max(0, score))
                .AddProperty("time_taken", duration)
                .Build();
            PlayGamesPlatform.Instance.RecordEvent(result);
            RecordProgress();
            PlayGamesPlatform.Instance.RequestEventsUpload();
            Log("Run and progression handed to PGS. Check device logs for server submission.");
        }
        catch (Exception e)
        {
            FatefulRushFirebaseServices.RecordNonFatal(
                e,
                "Google Play Game Stats run event failed"
            );
        }
#endif
    }

    // Called after authentication AND cloud restore, so a reinstall reports restored progress.
    public static void SyncProgress()
    {
        if (!FatefulRushFirebaseServices.GetBool(
                FatefulRushFirebaseServices.GameStatsEnabledKey,
                true
            ))
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!GooglePlayGamesManager.IsAuthenticated) return;
        try
        {
            RecordProgress();
            PlayGamesPlatform.Instance.RequestEventsUpload();
            Log("Progression handed to PGS.");
        }
        catch (Exception e)
        {
            FatefulRushFirebaseServices.RecordNonFatal(
                e,
                "Google Play Game Stats progress sync failed"
            );
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void RecordProgress()
    {
        var progress = new PlayerGameEvent.Builder("progressUpdate")
            .AddProperty("currentProgress", (long)StatsManager.GetCompletedLevelCount())
            .Build();
        PlayGamesPlatform.Instance.RecordEvent(progress);
    }
#endif
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void Log(string text) { Debug.Log("[GameStats] " + text); }
}
