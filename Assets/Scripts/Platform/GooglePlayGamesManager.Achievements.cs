using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

// Same Unity component. Inspector data and lifecycle entry points remain in GooglePlayGamesManager.cs.
public sealed partial class GooglePlayGamesManager
{
    public static void NotifyLevelCompleted(
        int levelNumber)
    {
        GooglePlayGamesManager manager =
            EnsureInstance();

        switch (levelNumber)
        {
            case 1:
                manager.Unlock(AchievementKey.FirstRush);
                break;

            case 10:
                manager.Unlock(AchievementKey.WarmingUp);
                break;

            case 20:
                manager.Unlock(AchievementKey.HalfwayThere);
                break;

            case 30:
                manager.Unlock(AchievementKey.NoTurningBack);
                break;

            case 40:
                manager.Unlock(AchievementKey.FateDefied);
                break;
        }
    }

    public static void NotifyNearMissTotal(
        int totalNearMisses)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalNearMisses);

        if (safeTotal >= 1)
            manager.Unlock(AchievementKey.CloseCall);

        manager.PushProgressIfUseful(
            AchievementKey.ThreadTheNeedle,
            safeTotal,
            NearMiss10Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.LivingOnTheEdge,
            safeTotal,
            NearMiss50Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.Untouchable,
            safeTotal,
            NearMiss250Target
        );
    }

    public static void NotifyComboReached(
        int comboMultiplier)
    {
        if (comboMultiplier < 6)
            return;

        EnsureInstance().Unlock(
            AchievementKey.ComboMaster
        );
    }

    public static void NotifyMagnetCoinTotal(
        int totalMagnetCoins)
    {
        EnsureInstance().PushProgressIfUseful(
            AchievementKey.MagneticAttraction,
            Mathf.Max(0, totalMagnetCoins),
            MagnetCoinTarget
        );
    }

    public static void NotifyTotalDeaths(
        int totalDeaths)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalDeaths);

        manager.PushProgressIfUseful(
            AchievementKey.StillStanding,
            safeTotal,
            Death50Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.TooStubbornToQuit,
            safeTotal,
            Death100Target
        );
    }

    public static void NotifyCloneUseTotal(
        int totalUses)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalUses);

        manager.PushProgressIfUseful(
            AchievementKey.EchoInitiate,
            safeTotal,
            Ability10Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.DoubleTrouble,
            safeTotal,
            Ability50Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.ShadowArmy,
            safeTotal,
            Ability250Target
        );
    }

    public static void NotifyDashUseTotal(
        int totalUses)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalUses);

        manager.PushProgressIfUseful(
            AchievementKey.QuickReflexes,
            safeTotal,
            Ability10Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.BlinkAndYouMissMe,
            safeTotal,
            Ability50Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.BornToRush,
            safeTotal,
            Ability250Target
        );
    }

    public static void NotifyArmorEnemyKillTotal(
        int totalKills)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalKills);

        manager.PushProgressIfUseful(
            AchievementKey.Counterattack,
            safeTotal,
            ArmorKill10Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.Payback,
            safeTotal,
            ArmorKill50Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.Reaper,
            safeTotal,
            ArmorKill100Target
        );
    }

    public static void NotifyTotalCoins(
        int totalCoins)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalCoins);

        manager.PushProgressIfUseful(
            AchievementKey.PocketChange,
            safeTotal,
            Coin1000Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.TreasureHunter,
            safeTotal,
            Coin5000Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.FortuneFavorsTheFast,
            safeTotal,
            Coin10000Target
        );
    }

    public static void NotifyArmorUseTotal(
        int totalUses)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalUses);

        manager.PushProgressIfUseful(
            AchievementKey.SuitUp,
            safeTotal,
            ArmorUse50Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.IronResolve,
            safeTotal,
            ArmorUse100Target
        );
    }

    public static void NotifySlowUseTotal(
        int totalUses)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalUses);

        manager.PushProgressIfUseful(
            AchievementKey.TimeBender,
            safeTotal,
            SlowUse50Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.MasterOfTime,
            safeTotal,
            SlowUse100Target
        );
    }

    public static void NotifySpaceBombTriggerTotal(
        int totalTriggers)
    {
        if (totalTriggers < 1)
            return;

        EnsureInstance().Unlock(
            AchievementKey.BadStep
        );
    }

    public static void NotifySpaceBombDeathTotal(
        int totalDeaths)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalDeaths);

        manager.PushProgressIfUseful(
            AchievementKey.BombMagnet,
            safeTotal,
            BombDeath10Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.GroundZero,
            safeTotal,
            BombDeath25Target
        );
    }

    public static void NotifyLaserDeathTotal(
        int totalDeaths)
    {
        GooglePlayGamesManager manager = EnsureInstance();
        int safeTotal = Mathf.Max(0, totalDeaths);

        if (safeTotal >= 1)
            manager.Unlock(AchievementKey.BurnedOnce);

        manager.PushProgressIfUseful(
            AchievementKey.LightShowCasualty,
            safeTotal,
            LaserDeath10Target
        );

        manager.PushProgressIfUseful(
            AchievementKey.Laserproof,
            safeTotal,
            LaserDeath25Target
        );
    }

    public static void NotifyBossEncounter()
    {
        EnsureInstance().Unlock(
            AchievementKey.FirstContact
        );
    }

    public static void NotifyBossSplit()
    {
        EnsureInstance().Unlock(
            AchievementKey.DivideAndConquer
        );
    }

    public static void NotifyBossAoeEvade()
    {
        EnsureInstance().Unlock(
            AchievementKey.BehindCover
        );
    }

    public static void NotifySkinEquipped(
        string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
            return;

        string normalized = skinId
            .Trim()
            .ToLowerInvariant()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);

        if (normalized == "dark" ||
            normalized == "black")
        {
            EnsureInstance().Unlock(
                AchievementKey.DarkFate
            );

            return;
        }

        if (normalized == "gold" ||
            normalized == "golden")
        {
            EnsureInstance().Unlock(
                AchievementKey.GoldenFate
            );
        }
    }

    private void BeginCloudSyncThenSyncAchievements(
        System.Action onFinished = null)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        FatefulRushCloudSave.SyncAfterAuthentication(
            () =>
            {
                SyncProgressFromLocalSave();
                FatefulRushGameStats.SyncProgress();
                GooglePlayGamesLeaderboards.SyncLocalBestTimes();
                onFinished?.Invoke();
            }
        );
#else
        SyncProgressFromLocalSave();
        FatefulRushGameStats.SyncProgress();
        onFinished?.Invoke();
#endif
    }

    private void SyncProgressFromLocalSave()
    {
        if (!IsReady())
            return;

        SyncLevelAchievement(1, AchievementKey.FirstRush);
        SyncLevelAchievement(10, AchievementKey.WarmingUp);
        SyncLevelAchievement(20, AchievementKey.HalfwayThere);
        SyncLevelAchievement(30, AchievementKey.NoTurningBack);
        SyncLevelAchievement(40, AchievementKey.FateDefied);

        int nearMisses = StatsManager.GetNearMisses();

        if (nearMisses >= 1)
            Unlock(AchievementKey.CloseCall);

        SetStepsAtLeast(
            AchievementKey.ThreadTheNeedle,
            nearMisses,
            NearMiss10Target
        );

        SetStepsAtLeast(
            AchievementKey.LivingOnTheEdge,
            nearMisses,
            NearMiss50Target
        );

        SetStepsAtLeast(
            AchievementKey.Untouchable,
            nearMisses,
            NearMiss250Target
        );

        if (StatsManager.GetHighestCombo() >= 6)
            Unlock(AchievementKey.ComboMaster);

        SetStepsAtLeast(
            AchievementKey.MagneticAttraction,
            StatsManager.GetMagnetCoins(),
            MagnetCoinTarget
        );

        SetStepsAtLeast(
            AchievementKey.StillStanding,
            StatsManager.GetActualDeaths(),
            Death50Target
        );

        SetStepsAtLeast(
            AchievementKey.TooStubbornToQuit,
            StatsManager.GetActualDeaths(),
            Death100Target
        );

        SyncAbilityProgress(
            StatsManager.GetCloneUses(),
            AchievementKey.EchoInitiate,
            AchievementKey.DoubleTrouble,
            AchievementKey.ShadowArmy
        );

        SyncAbilityProgress(
            StatsManager.GetDashUses(),
            AchievementKey.QuickReflexes,
            AchievementKey.BlinkAndYouMissMe,
            AchievementKey.BornToRush
        );

        int armorEnemyKills = StatsManager.GetArmorEnemyKills();

        SetStepsAtLeast(
            AchievementKey.Counterattack,
            armorEnemyKills,
            ArmorKill10Target
        );

        SetStepsAtLeast(
            AchievementKey.Payback,
            armorEnemyKills,
            ArmorKill50Target
        );

        SetStepsAtLeast(
            AchievementKey.Reaper,
            armorEnemyKills,
            ArmorKill100Target
        );

        int totalCoins = StatsManager.GetTotalCoins();

        SetStepsAtLeast(
            AchievementKey.PocketChange,
            totalCoins,
            Coin1000Target
        );

        SetStepsAtLeast(
            AchievementKey.TreasureHunter,
            totalCoins,
            Coin5000Target
        );

        SetStepsAtLeast(
            AchievementKey.FortuneFavorsTheFast,
            totalCoins,
            Coin10000Target
        );

        int armorUses = StatsManager.GetArmorBuffUses();

        SetStepsAtLeast(
            AchievementKey.SuitUp,
            armorUses,
            ArmorUse50Target
        );

        SetStepsAtLeast(
            AchievementKey.IronResolve,
            armorUses,
            ArmorUse100Target
        );

        int slowUses = StatsManager.GetSlowBuffUses();

        SetStepsAtLeast(
            AchievementKey.TimeBender,
            slowUses,
            SlowUse50Target
        );

        SetStepsAtLeast(
            AchievementKey.MasterOfTime,
            slowUses,
            SlowUse100Target
        );

        int bombDeaths = StatsManager.GetDeathCauseCount("SPACE BOMB");
        int bombTriggers = StatsManager.GetSpaceBombTriggers();

        if (bombTriggers > 0 || bombDeaths > 0)
            Unlock(AchievementKey.BadStep);

        SetStepsAtLeast(
            AchievementKey.BombMagnet,
            bombDeaths,
            BombDeath10Target
        );

        SetStepsAtLeast(
            AchievementKey.GroundZero,
            bombDeaths,
            BombDeath25Target
        );

        int laserDeaths = StatsManager.GetLaserDeaths();

        if (laserDeaths > 0)
            Unlock(AchievementKey.BurnedOnce);

        SetStepsAtLeast(
            AchievementKey.LightShowCasualty,
            laserDeaths,
            LaserDeath10Target
        );

        SetStepsAtLeast(
            AchievementKey.Laserproof,
            laserDeaths,
            LaserDeath25Target
        );

        if (StatsManager.GetBossEncounters() > 0)
            Unlock(AchievementKey.FirstContact);

        if (StatsManager.GetBossSplits() > 0)
            Unlock(AchievementKey.DivideAndConquer);

        if (StatsManager.GetBossAoeEvades() > 0)
            Unlock(AchievementKey.BehindCover);

        string selectedSkinId =
            PlayerPrefs.GetString(
                PlayerSkinCatalog.SelectedSkinKey,
                string.Empty
            );

        NotifySkinEquipped(selectedSkinId);
    }

    private void SyncAbilityProgress(
        int totalUses,
        AchievementKey tenKey,
        AchievementKey fiftyKey,
        AchievementKey twoHundredFiftyKey)
    {
        SetStepsAtLeast(
            tenKey,
            totalUses,
            Ability10Target
        );

        SetStepsAtLeast(
            fiftyKey,
            totalUses,
            Ability50Target
        );

        SetStepsAtLeast(
            twoHundredFiftyKey,
            totalUses,
            Ability250Target
        );
    }

    private void SyncLevelAchievement(
        int levelNumber,
        AchievementKey key)
    {
        bool completed =
            PlayerPrefs.GetInt(
                "CompletedLevel_" + levelNumber,
                0
            ) == 1;

        if (completed)
            Unlock(key);
    }

    private void PushProgressIfUseful(
        AchievementKey key,
        int currentSteps,
        int targetSteps)
    {
        int safeSteps = Mathf.Max(0, currentSteps);
        int safeTarget = Mathf.Max(1, targetSteps);

        if (!ShouldPushProgress(safeSteps, safeTarget))
            return;

        SetStepsAtLeast(
            key,
            safeSteps,
            safeTarget
        );
    }

    private static bool ShouldPushProgress(
        int currentSteps,
        int targetSteps)
    {
        if (currentSteps <= 0)
            return false;

        if (currentSteps == 1 || currentSteps >= targetSteps)
            return true;

        int interval;

        if (targetSteps <= 10)
            interval = 2;
        else if (targetSteps <= 50)
            interval = 5;
        else if (targetSteps <= 100)
            interval = 10;
        else if (targetSteps <= 250)
            interval = 25;
        else if (targetSteps <= 1000)
            interval = 100;
        else if (targetSteps <= 5000)
            interval = 500;
        else
            interval = 1000;

        return currentSteps % interval == 0;
    }

    private bool IsReady()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!authenticated &&
                PlayGamesPlatform.Instance.IsAuthenticated())
            {
                authenticated = true;
            }

            return authenticated;
        }
        catch (System.Exception exception)
        {
            authenticated = false;
            lastAuthenticationStatus = "RuntimeError";

            Debug.LogError(
                "[GooglePlayGames] Runtime authentication check failed. " +
                "Gameplay will continue without achievement sync.",
                this
            );
            Debug.LogException(exception, this);
            return false;
        }
#else
        return false;
#endif
    }

    private void Unlock(
        AchievementKey key)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsReady())
            return;

        string achievementId =
            ResolveAchievementId(key);

        if (string.IsNullOrWhiteSpace(achievementId))
            return;

        try
        {
            PlayGamesPlatform.Instance.UnlockAchievement(
                achievementId,
                success =>
                {
                    if (!success)
                    {
                        Debug.LogWarning(
                            "[GooglePlayGames] Achievement unlock failed: " +
                            key
                        );
                        return;
                    }

                    LogDiagnostic(
                        "Achievement unlock succeeded: " + key
                    );
                }
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "[GooglePlayGames] Achievement unlock call threw an exception. " +
                "Gameplay is protected and will continue.",
                this
            );
            Debug.LogException(exception, this);
        }
#endif
    }

    private void SetStepsAtLeast(
        AchievementKey key,
        int currentSteps,
        int targetSteps)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsReady())
            return;

        int safeTarget = Mathf.Max(1, targetSteps);
        int safeSteps = Mathf.Clamp(currentSteps, 0, safeTarget);

        if (safeSteps <= 0)
            return;

        string achievementId =
            ResolveAchievementId(key);

        if (string.IsNullOrWhiteSpace(achievementId))
            return;

        try
        {
            PlayGamesPlatform.Instance.SetStepsAtLeast(
                achievementId,
                safeSteps,
                success =>
                {
                    if (!success)
                    {
                        Debug.LogWarning(
                            "[GooglePlayGames] Achievement progress failed: " +
                            key + " | " +
                            safeSteps + "/" + safeTarget
                        );
                        return;
                    }

                    LogDiagnostic(
                        "Achievement progress succeeded: " +
                        key + " | " +
                        safeSteps + "/" + safeTarget
                    );
                }
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "[GooglePlayGames] Achievement progress call threw an exception. " +
                "Gameplay is protected and will continue.",
                this
            );
            Debug.LogException(exception, this);
        }
#endif
    }
}
