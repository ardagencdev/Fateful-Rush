using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

// Same Unity component. Inspector data and lifecycle entry points remain in GooglePlayGamesManager.cs.
public sealed partial class GooglePlayGamesManager
{
    private string BuildDiagnosticSummary()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bool platformAuthenticated = false;

        try
        {
            platformAuthenticated =
                PlayGamesPlatform.Instance.IsAuthenticated();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "[GPGS-DIAG] Platform auth query failed: " +
                exception.Message
            );
        }

        return
            "localAuth=" + authenticated +
            " | platformAuth=" + platformAuthenticated +
            " | lastAuth=" + lastAuthenticationStatus +
            " | lastUI=" + lastAchievementsUiStatus;
#else
        return
            "localAuth=" + authenticated +
            " | lastAuth=" + lastAuthenticationStatus +
            " | lastUI=" + lastAchievementsUiStatus;
#endif
    }

    private void SaveDiagnosticState()
    {
        if (!DiagnosticsEnabled)
            return;

        try
        {
            PlayerPrefs.SetString(
                "GPGS_DIAG_LastAuth",
                lastAuthenticationStatus
            );

            PlayerPrefs.SetString(
                "GPGS_DIAG_LastUI",
                lastAchievementsUiStatus
            );

            PlayerPrefs.SetInt(
                "GPGS_DIAG_LocalAuthenticated",
                authenticated ? 1 : 0
            );

#if UNITY_ANDROID && !UNITY_EDITOR
            bool platformAuthenticated = false;

            try
            {
                platformAuthenticated =
                    PlayGamesPlatform.Instance.IsAuthenticated();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    "[GPGS-DIAG] Platform auth save query failed: " +
                    exception.Message
                );
            }

            PlayerPrefs.SetInt(
                "GPGS_DIAG_PlatformAuthenticated",
                platformAuthenticated ? 1 : 0
            );
#endif

            PlayerPrefs.Save();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "[GPGS-DIAG] Saving diagnostic state failed: " +
                exception.Message
            );
        }
    }

    private void LogDiagnostic(
        string message)
    {
        if (!DiagnosticsEnabled)
            return;

        Debug.Log(
            "[GPGS-DIAG] " + message
        );
    }

    private static string ResolveAchievementId(
        AchievementKey key)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        switch (key)
        {
            case AchievementKey.FirstRush:
                return GPGSIds.achievement_first_rush;
            case AchievementKey.WarmingUp:
                return GPGSIds.achievement_warming_up;
            case AchievementKey.HalfwayThere:
                return GPGSIds.achievement_halfway_there;
            case AchievementKey.NoTurningBack:
                return GPGSIds.achievement_no_turning_back;
            case AchievementKey.FateDefied:
                return GPGSIds.achievement_fate_defied;
            case AchievementKey.CloseCall:
                return GPGSIds.achievement_close_call;
            case AchievementKey.ThreadTheNeedle:
                return GPGSIds.achievement_thread_the_needle;
            case AchievementKey.LivingOnTheEdge:
                return GPGSIds.achievement_living_on_the_edge;
            case AchievementKey.Untouchable:
                return GPGSIds.achievement_untouchable;
            case AchievementKey.ComboMaster:
                return GPGSIds.achievement_combo_master;
            case AchievementKey.MagneticAttraction:
                return GPGSIds.achievement_magnetic_attraction;
            case AchievementKey.FirstContact:
                return GPGSIds.achievement_first_contact;
            case AchievementKey.DivideAndConquer:
                return GPGSIds.achievement_divide_and_conquer;
            case AchievementKey.BehindCover:
                return GPGSIds.achievement_behind_cover;
            case AchievementKey.DarkFate:
                return GPGSIds.achievement_dark_fate;
            case AchievementKey.GoldenFate:
                return GPGSIds.achievement_golden_fate;
            case AchievementKey.StillStanding:
                return GPGSIds.achievement_still_standing;
            case AchievementKey.TooStubbornToQuit:
                return GPGSIds.achievement_too_stubborn_to_quit;
            case AchievementKey.EchoInitiate:
                return GPGSIds.achievement_echo_initiate;
            case AchievementKey.DoubleTrouble:
                return GPGSIds.achievement_double_trouble;
            case AchievementKey.ShadowArmy:
                return GPGSIds.achievement_shadow_army;
            case AchievementKey.QuickReflexes:
                return GPGSIds.achievement_quick_reflexes;
            case AchievementKey.BlinkAndYouMissMe:
                return GPGSIds.achievement_blink_and_you_miss_me;
            case AchievementKey.BornToRush:
                return GPGSIds.achievement_born_to_rush;
            case AchievementKey.Counterattack:
                return GPGSIds.achievement_counterattack;
            case AchievementKey.Payback:
                return GPGSIds.achievement_payback;
            case AchievementKey.Reaper:
                return GPGSIds.achievement_reaper;
            case AchievementKey.PocketChange:
                return GPGSIds.achievement_pocket_change;
            case AchievementKey.TreasureHunter:
                return GPGSIds.achievement_treasure_hunter;
            case AchievementKey.FortuneFavorsTheFast:
                return GPGSIds.achievement_fortune_favors_the_fast;
            case AchievementKey.SuitUp:
                return GPGSIds.achievement_suit_up;
            case AchievementKey.IronResolve:
                return GPGSIds.achievement_iron_resolve;
            case AchievementKey.TimeBender:
                return GPGSIds.achievement_time_bender;
            case AchievementKey.MasterOfTime:
                return GPGSIds.achievement_master_of_time;
            case AchievementKey.BadStep:
                return GPGSIds.achievement_bad_step;
            case AchievementKey.BombMagnet:
                return GPGSIds.achievement_bomb_magnet;
            case AchievementKey.GroundZero:
                return GPGSIds.achievement_ground_zero;
            case AchievementKey.BurnedOnce:
                return GPGSIds.achievement_burned_once;
            case AchievementKey.LightShowCasualty:
                return GPGSIds.achievement_light_show_casualty;
            case AchievementKey.Laserproof:
                return GPGSIds.achievement_laserproof;
            default:
                return string.Empty;
        }
#else
        return string.Empty;
#endif
    }
}
