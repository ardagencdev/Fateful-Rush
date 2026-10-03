using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public static class StatsTextFormatter
{
    private static readonly Dictionary<string, string> StatsLabelToKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "GENERAL", "stats.section.general" },
            { "PROGRESSION", "stats.section.progression" },
            { "MISSION MODES", "stats.section.mission_modes" },
            { "PERFORMANCE", "stats.section.performance" },
            { "NEAR MISS", "stats.section.near_miss" },
            { "COLLECTIBLES", "stats.section.collectibles" },
            { "ABILITIES & POWER-UPS", "stats.section.abilities" },
            { "DANGER MASTERY", "stats.section.danger_mastery" },
            { "DEATH ANALYSIS", "stats.section.death_analysis" },
            { "LEVEL BEST TIMES", "stats.section.level_best_times" },
            { "Total Runs", "stats.total_runs" },
            { "Total Wins", "stats.total_wins" },
            { "Total Deaths", "stats.total_deaths" },
            { "Win Rate", "stats.win_rate" },
            { "Current Win Streak", "stats.current_win_streak" },
            { "Best Win Streak", "stats.best_win_streak" },
            { "Total Play Time", "stats.total_play_time" },
            { "Average Run Time", "stats.average_run_time" },
            { "Longest Run", "stats.longest_run" },
            { "Levels Completed", "stats.levels_completed" },
            { "Completion", "stats.completion" },
            { "Highest Level Completed", "stats.highest_level_completed" },
            { "Skins Unlocked", "stats.skins_unlocked" },
            { "Score Missions", "stats.score_missions" },
            { "Survival Missions", "stats.survival_missions" },
            { "Timed Score Missions", "stats.timed_score_missions" },
            { "Total Score Earned", "stats.total_score_earned" },
            { "Best Run Score", "stats.best_run_score" },
            { "Average Score / Score Run", "stats.average_score_per_run" },
            { "Highest Combo", "stats.highest_combo" },
            { "Longest Combo Chain", "stats.longest_combo_chain" },
            { "6x Combo Reached", "stats.combo_6x_reached" },
            { "Combo Bonus Points", "stats.combo_bonus_points" },
            { "Total Near Misses", "stats.total_near_misses" },
            { "Best Near Miss Streak", "stats.best_near_miss_streak" },
            { "Total Coins", "stats.total_coins" },
            { "Normal Coins", "stats.normal_coins" },
            { "Gold Coins", "stats.gold_coins" },
            { "Rare Coins", "stats.rare_coins" },
            { "Most Coins in a Run", "stats.most_coins_run" },
            { "Base Coin Value Collected", "stats.base_coin_value" },
            { "Magnet Coins", "stats.magnet_coins" },
            { "Dash Uses", "stats.dash_uses" },
            { "Clone Uses", "stats.clone_uses" },
            { "Slow Buff Uses", "stats.slow_uses" },
            { "Armor Buff Uses", "stats.armor_uses" },
            { "Armor Saves", "stats.armor_saves" },
            { "Armor Save Rate", "stats.armor_save_rate" },
            { "Beacons Destroyed", "stats.beacons_destroyed" },
            { "Hunters Stunned", "stats.hunters_stunned" },
            { "Boss Encounters", "stats.boss_encounters" },
            { "Boss Splits Triggered", "stats.boss_splits" },
            { "Boss AOE Evades", "stats.boss_aoe_evades" },
            { "Mini-Boss AOE Evades", "stats.mini_boss_aoe_evades" },
            { "Nemesis", "stats.nemesis" },
            { "Stalker Deaths", "stats.death_stalker" },
            { "Hunter Deaths", "stats.death_hunter" },
            { "Blaster Deaths", "stats.death_blaster" },
            { "Boss Deaths", "stats.death_boss" },
            { "Laser Bullet Deaths", "stats.death_laser_bullet" },
            { "Laser Wall Deaths", "stats.death_laser_wall" },
            { "Mini-Boss Deaths", "stats.death_mini_boss" },
            { "Space Bomb Deaths", "stats.death_space_bomb" },
            { "Time Expired", "stats.death_time_expired" },
            { "Unknown Deaths", "stats.death_unknown" }
        };

    public static string TranslateStats(string source)
    {
        string[] lines = source.Replace("\r\n", "\n").Split('\n');
        StringBuilder builder = new StringBuilder(source.Length + 128);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string translated = TranslateStatsLine(line);
            builder.Append(translated);
            if (i < lines.Length - 1)
                builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string TranslateStatsLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return line;

        string trimmed = line.Trim();

        if (StatsLabelToKey.TryGetValue(trimmed, out string exactKey))
            return FatefulRushLocalization.Text(exactKey, trimmed);

        if (trimmed.Equals("No recorded death causes yet.", StringComparison.OrdinalIgnoreCase))
            return FatefulRushLocalization.Text("stats.no_death_causes", trimmed);

        if (trimmed.Equals("No best-time records yet.", StringComparison.OrdinalIgnoreCase))
            return FatefulRushLocalization.Text("stats.no_best_times", trimmed);

        if (trimmed.StartsWith("Level ", StringComparison.OrdinalIgnoreCase))
        {
            int colon = trimmed.IndexOf(':');
            if (colon > 6)
            {
                string levelNumber = trimmed.Substring(6, colon - 6).Trim();
                string rest = trimmed.Substring(colon + 1).Trim();
                return FatefulRushLocalization.Text("stats.level", "Level") + " " + levelNumber + ": " + rest;
            }
        }

        int separator = line.IndexOf(':');
        if (separator > 0)
        {
            string label = line.Substring(0, separator).Trim();
            string value = line.Substring(separator + 1).TrimStart();

            if (StatsLabelToKey.TryGetValue(label, out string labelKey))
            {
                value = TranslateStatsValue(value);
                return FatefulRushLocalization.Text(labelKey, label) + ": " + value;
            }
        }

        return line;
    }

    private static string TranslateDeathCauseValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        string[] causes =
        {
            "LASER BULLET", "LASER WALL", "MINI BOSS", "SPACE BOMB",
            "TIME EXPIRED", "STALKER", "HUNTER", "BLASTER", "BOSS", "UNKNOWN"
        };

        for (int i = 0; i < causes.Length; i++)
        {
            string cause = causes[i];
            if (!value.StartsWith(cause, StringComparison.OrdinalIgnoreCase))
                continue;

            string suffix = value.Substring(cause.Length);
            return LocalizeDeathCause(cause) + suffix;
        }

        return value;
    }

    private static string LocalizeDeathCause(string cause) { return FatefulRushLocalization.DeathCause(cause); }

    private static string TranslateStatsValue(string value)
    {
        if (string.Equals(value, "NONE", StringComparison.OrdinalIgnoreCase))
            return FatefulRushLocalization.Text("stats.none", "NONE");

        value = TranslateDeathCauseValue(value);

        value = Regex.Replace(
            value,
            @"\bwins\b",
            FatefulRushLocalization.Text("stats.unit.wins", "wins"),
            RegexOptions.IgnoreCase
        );

        value = Regex.Replace(
            value,
            @"\bcoins\b",
            FatefulRushLocalization.Text("stats.unit.coins", "coins"),
            RegexOptions.IgnoreCase
        );

        value = Regex.Replace(value, @"(?<=\d)h\b", FatefulRushLocalization.Text("stats.unit.hours_short", "h"));
        value = Regex.Replace(value, @"(?<=\d)m\b", FatefulRushLocalization.Text("stats.unit.minutes_short", "m"));
        value = Regex.Replace(value, @"(?<=\d)s\b", FatefulRushLocalization.Text("stats.unit.seconds_short", "s"));

        return value;
    }
}
