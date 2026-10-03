using System.Text;
using UnityEngine;

/// <summary>Shared localized mission text; presentation remains owned by its panel.</summary>
public static class MissionTextFormatter
{
    public static string BriefingDescription(LevelConfig level, bool tr)
    {
        StringBuilder builder = new StringBuilder(256);

        builder.Append(BuildObjective(level, tr));

        string briefing = BuildConfiguredBriefing(level, tr);
        if (!string.IsNullOrWhiteSpace(briefing))
        {
            builder.Append("\n\n");
            builder.Append(briefing.Trim());
        }

        return builder.ToString().Trim();
    }

    private static string BuildConfiguredBriefing(LevelConfig level, bool tr)
    {
        if (level.briefingPages == null || level.briefingPages.Length == 0)
            return string.Empty;

        StringBuilder builder = new StringBuilder(192);

        for (int i = 0; i < level.briefingPages.Length; i++)
        {
            string configured = level.briefingPages[i];

            if (string.IsNullOrWhiteSpace(configured))
                continue;

            string text = configured.Trim();

            // English always uses the exact text stored in the LevelConfig.
            // For Turkish, use the existing per-level localized briefing entry.
            if (tr && i == 0)
                text = FatefulRushLocalization.BriefingTip(level.levelNumber, text);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (builder.Length > 0)
                builder.Append("\n\n");

            builder.Append(text.Trim());
        }

        return builder.ToString();
    }

    private static string BuildObjective(LevelConfig level, bool tr)
    {
        int score = level.SafeWinScore;
        string time = FormatNumber(level.SafeTimeLimit);

        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                return tr
                    ? $"Görevi tamamlamak için {score} puana ulaş."
                    : $"Reach {score} points to complete the mission.";

            case WinConditionType.SurviveTime:
                return tr
                    ? $"{time} saniye hayatta kal. Bu görevde coinler ve kombolar devre dışı."
                    : $"Survive for {time} seconds. Coins and combos are disabled in this mission.";

            case WinConditionType.ReachScoreWithinTime:
                return tr
                    ? $"Süre bitmeden {score} puana ulaş. Toplam süren {time} saniye."
                    : $"Reach {score} points before the {time}-second timer reaches zero.";

            default:
                return tr ? "Görev hedefini tamamla." : "Complete the mission objective.";
        }
    }

    private static string FormatNumber(float value)
    {
        float safe = Mathf.Max(0f, value);

        return Mathf.Approximately(safe, Mathf.Round(safe))
            ? Mathf.RoundToInt(safe).ToString()
            : safe.ToString("0.#");
    }

    public static string BriefingMode(LevelConfig level)
    {
        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                return FatefulRushLocalization.Text("briefing.mode.score", "SCORE MISSION");
            case WinConditionType.SurviveTime:
                return FatefulRushLocalization.Text("briefing.mode.survival", "SURVIVAL MISSION");
            case WinConditionType.ReachScoreWithinTime:
                return FatefulRushLocalization.Text("briefing.mode.timed_score", "TIMED SCORE MISSION");
            default:
                return FatefulRushLocalization.Text("briefing.mode.mission", "MISSION");
        }
    }

    public static string PauseObjective(LevelConfig level)
    {
        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                return FatefulRushLocalization.Text(
                    "objective.pause_reach_score",
                    "REACH SCORE: {0}",
                    level.SafeWinScore
                );

            case WinConditionType.SurviveTime:
                return FatefulRushLocalization.Text(
                    "objective.pause_survive",
                    "SURVIVE: {0}",
                    FormatSeconds(level.SafeTimeLimit)
                );

            case WinConditionType.ReachScoreWithinTime:
                return FatefulRushLocalization.Text(
                    "objective.pause_reach_score_time",
                    "REACH SCORE: {0}  ·  {1}",
                    level.SafeWinScore,
                    FormatSeconds(level.SafeTimeLimit)
                );

            default:
                return FatefulRushLocalization.Text(
                    "objective.complete_mission",
                    "COMPLETE THE MISSION"
                );
        }
    }

    public static string FormatSeconds(float seconds)
    {
        float safe = Mathf.Max(0f, seconds);
        int rounded = Mathf.RoundToInt(safe);
        return Mathf.Approximately(safe, rounded) ? $"{rounded}s" : $"{safe:0.#}s";
    }

    public static string IntroObjective(
        LevelConfig level)
    {
        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                return FatefulRushLocalization.Text(
                    "objective.reach_score",
                    FatefulRushLocalization.IsTurkish
                        ? "{0} SKORA ULAŞ"
                        : "REACH {0} SCORE",
                    level.SafeWinScore
                );

            case WinConditionType.SurviveTime:
                return FatefulRushLocalization.Text(
                    "objective.survive_time",
                    FatefulRushLocalization.IsTurkish
                        ? "{0} SANİYE HAYATTA KAL"
                        : "SURVIVE FOR {0} SECONDS",
                    FormatNumber(level.SafeTimeLimit)
                );

            case WinConditionType.ReachScoreWithinTime:
                return FatefulRushLocalization.Text(
                    "objective.reach_score_in_time",
                    FatefulRushLocalization.IsTurkish
                        ? "{1} SANİYE İÇİNDE {0} SKORA ULAŞ"
                        : "REACH {0} SCORE IN {1} SECONDS",
                    level.SafeWinScore,
                    FormatNumber(level.SafeTimeLimit)
                );

            default:
                return FatefulRushLocalization.Text(
                    "objective.complete_mission",
                    FatefulRushLocalization.IsTurkish
                        ? "GÖREVİ TAMAMLA"
                        : "COMPLETE THE MISSION"
                );
        }
    }
}
