using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in LevelConfigEditor.cs.
public partial class LevelConfigEditor
{
    private void DrawPacingTimeline()
    {
        FoldoutBox(
            "PACING TIMELINE",
            ref timelineExpanded,
            () =>
            {
                LevelConfig config = GetSingleConfig();

                if (config == null)
                {
                    Help("The pacing timeline is available when a single LevelConfig is selected.");
                    return;
                }

                bool estimated;
                float duration = GetTimelineDuration(config, out estimated);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    estimated
                        ? $"Estimated Mission Length: {duration:0.#}s"
                        : $"Mission Length: {duration:0.#}s",
                    EditorStyles.boldLabel
                );

                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField(
                    estimated ? "ESTIMATED" : "EXACT",
                    EditorStyles.miniBoldLabel,
                    GUILayout.Width(72f)
                );
                EditorGUILayout.EndHorizontal();

                if (estimated)
                {
                    Help(
                        "Reach Score has no fixed duration. This estimate assumes the player collects spawned coins consistently and is used only for pacing analysis."
                    );
                }

                List<TimelineRow> rows =
                    BuildTimelineRows(config, duration);

                if (rows.Count == 0)
                {
                    Help("No timed spawns or mission events are active.");
                    return;
                }

                DrawTimelineAxis(duration);

                foreach (TimelineRow row in rows)
                    DrawTimelineRow(row, duration);

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(
                    "Bars = first random window    Markers = repeated/expected spawns through mission end",
                    EditorStyles.centeredGreyMiniLabel
                );
            }
        );
    }

    private static float GetTimelineDuration(
        LevelConfig config,
        out bool estimated)
    {
        if (config.UsesTime)
        {
            estimated = false;
            return Mathf.Max(1f, config.SafeTimeLimit);
        }

        estimated = true;

        float totalChance = 0f;
        float weightedValue = 0f;

        if (config.normalCoinEnabled)
        {
            totalChance += config.normalCoinChance;
            weightedValue +=
                config.normalCoinChance *
                Mathf.Max(1, config.normalCoinValue);
        }

        if (config.goldCoinEnabled)
        {
            totalChance += config.goldCoinChance;
            weightedValue +=
                config.goldCoinChance *
                Mathf.Max(1, config.goldCoinValue);
        }

        if (config.rareCoinEnabled)
        {
            totalChance += config.rareCoinChance;
            weightedValue +=
                config.rareCoinChance *
                Mathf.Max(1, config.rareCoinValue);
        }

        float expectedCoinValue =
            totalChance > 0f
                ? weightedValue / totalChance
                : 1f;

        float requiredCoins =
            config.SafeWinScore /
            Mathf.Max(0.01f, expectedCoinValue);

        float estimatedDuration =
            requiredCoins *
            Mathf.Max(0.01f, config.coinSpawnInterval);

        return Mathf.Clamp(estimatedDuration, 8f, 180f);
    }

    private static List<TimelineRow> BuildTimelineRows(
        LevelConfig config,
        float duration)
    {
        List<TimelineRow> rows = new List<TimelineRow>();

        Color coinColor = GetTimelineColor(
            new Color(0.35f, 0.72f, 0.95f, 1f),
            new Color(0.10f, 0.45f, 0.70f, 1f)
        );

        Color enemyColor = GetTimelineColor(
            new Color(0.95f, 0.42f, 0.42f, 1f),
            new Color(0.72f, 0.15f, 0.15f, 1f)
        );

        Color bossColor = GetTimelineColor(
            new Color(0.95f, 0.35f, 0.82f, 1f),
            new Color(0.65f, 0.10f, 0.52f, 1f)
        );

        Color supportColor = GetTimelineColor(
            new Color(0.35f, 0.88f, 0.62f, 1f),
            new Color(0.10f, 0.58f, 0.32f, 1f)
        );

        Color hazardColor = GetTimelineColor(
            new Color(1f, 0.68f, 0.28f, 1f),
            new Color(0.82f, 0.38f, 0.05f, 1f)
        );

        if (config.UsesScore &&
            (config.normalCoinEnabled ||
             config.goldCoinEnabled ||
             config.rareCoinEnabled))
        {
            TimelineRow coins = new TimelineRow
            {
                label = "Coins",
                tooltip =
                    $"A coin spawn attempt occurs every {config.coinSpawnInterval:0.##} seconds.",
                color = coinColor
            };

            AddRepeatedMarkers(
                coins.markers,
                Mathf.Max(0.01f, config.coinSpawnInterval),
                Mathf.Min(
                    24,
                    Mathf.CeilToInt(
                        duration /
                        Mathf.Max(0.01f, config.coinSpawnInterval)
                    )
                )
            );

            rows.Add(coins);
        }

        AddEnemyTimelineRow(
            rows,
            "Normal Enemy",
            config.normalEnemyCount,
            config.normalEnemySpawnInterval,
            enemyColor
        );

        AddEnemyTimelineRow(
            rows,
            "Projectile Enemy",
            config.projectileEnemyCount,
            config.projectileEnemySpawnInterval,
            enemyColor
        );

        AddEnemyTimelineRow(
            rows,
            "Hunter Enemy",
            config.hunterEnemyCount,
            config.hunterEnemySpawnInterval,
            enemyColor
        );

        if (config.beaconEnemyCount > 0)
        {
            rows.Add(
                CreateWindowRow(
                    "Beacon Enemy",
                    config.beaconMinSpawnTime,
                    config.beaconMaxSpawnTime,
                    enemyColor,
                    $"First beacon spawn window. Maximum count: {config.beaconEnemyCount}."
                )
            );
        }

        if (config.bossEnabled)
        {
            float bossTime;
            string tooltip;

            if (config.EffectiveBossSpawnCondition ==
                BossSpawnCondition.Time)
            {
                bossTime = config.bossSpawnTime;
                tooltip =
                    $"Boss triggers after {config.bossSpawnTime:0.##} seconds.";
            }
            else
            {
                float progress =
                    config.SafeBossSpawnScore /
                    (float)Mathf.Max(1, config.SafeWinScore);

                bossTime = duration * Mathf.Clamp01(progress);
                tooltip =
                    $"Boss triggers at {config.SafeBossSpawnScore} score. " +
                    "Its timeline position is estimated from score progress.";
            }

            TimelineRow boss = new TimelineRow
            {
                label = "Boss",
                tooltip = tooltip,
                color = bossColor
            };

            boss.markers.Add(bossTime);
            rows.Add(boss);
        }

        if (config.armorEnabled)
        {
            rows.Add(
                CreateWindowRow(
                    "Armor",
                    config.armorMinSpawnTime,
                    config.armorMaxSpawnTime,
                    supportColor,
                    "Possible armor spawn window."
                )
            );
        }

        if (config.slowEnabled)
        {
            rows.Add(
                CreateWindowRow(
                    "Slow",
                    config.slowMinSpawnTime,
                    config.slowMaxSpawnTime,
                    supportColor,
                    "Possible slow power-up spawn window."
                )
            );
        }

        if (config.verticalLaserEnabled)
        {
            LaserDangerSettings settings =
                config.ResolveVerticalLaserDanger();

            rows.Add(
                CreateRepeatingRandomSpawnRow(
                    "Vertical Laser",
                    settings.minSpawnTime,
                    settings.maxSpawnTime,
                    duration,
                    settings.warningDuration,
                    hazardColor,
                    $"First spawn occurs between {settings.minSpawnTime:0.##}-{settings.maxSpawnTime:0.##}s. " +
                    $"It then repeats with the same random delay until the mission ends. " +
                    $"Warning: {settings.warningDuration:0.##}s."
                )
            );
        }

        if (config.horizontalLaserEnabled)
        {
            LaserDangerSettings settings =
                config.ResolveHorizontalLaserDanger();

            rows.Add(
                CreateRepeatingRandomSpawnRow(
                    "Horizontal Laser",
                    settings.minSpawnTime,
                    settings.maxSpawnTime,
                    duration,
                    settings.warningDuration,
                    hazardColor,
                    $"First spawn occurs between {settings.minSpawnTime:0.##}-{settings.maxSpawnTime:0.##}s. " +
                    $"It then repeats with the same random delay until the mission ends. " +
                    $"Warning: {settings.warningDuration:0.##}s."
                )
            );
        }

        if (config.bombTrapEnabled)
        {
            BombDangerSettings settings =
                config.ResolveBombDanger();

            rows.Add(
                CreateRepeatingRandomSpawnRow(
                    "Space Bomb",
                    settings.minSpawnTime,
                    settings.maxSpawnTime,
                    duration,
                    0f,
                    hazardColor,
                    $"First spawn occurs between {settings.minSpawnTime:0.##}-{settings.maxSpawnTime:0.##}s. " +
                    $"It keeps attempting spawns until the mission ends while below the active-bomb limit. " +
                    $"Maximum active bombs: {settings.maxBombCount}."
                )
            );
        }

        return rows;
    }

    private static void AddEnemyTimelineRow(
        List<TimelineRow> rows,
        string label,
        int count,
        float interval,
        Color color)
    {
        if (count <= 0)
            return;

        TimelineRow row = new TimelineRow
        {
            label = label,
            tooltip =
                $"{count} total spawn(s), one every {interval:0.##} seconds.",
            color = color
        };

        AddRepeatedMarkers(
            row.markers,
            Mathf.Max(0.01f, interval),
            Mathf.Min(count, 24)
        );

        rows.Add(row);
    }

    private static TimelineRow CreateWindowRow(
        string label,
        float start,
        float end,
        Color color,
        string tooltip)
    {
        return new TimelineRow
        {
            label = label,
            tooltip = tooltip,
            color = color,
            hasWindow = true,
            windowStart = Mathf.Max(0f, start),
            windowEnd = Mathf.Max(start, end)
        };
    }

    private static TimelineRow CreateRepeatingRandomSpawnRow(
        string label,
        float minimumDelay,
        float maximumDelay,
        float duration,
        float cycleExtraTime,
        Color color,
        string tooltip)
    {
        float safeMinimum = Mathf.Max(0f, minimumDelay);
        float safeMaximum = Mathf.Max(safeMinimum, maximumDelay);

        TimelineRow row = CreateWindowRow(
            label,
            safeMinimum,
            safeMaximum,
            color,
            tooltip
        );

        float averageDelay =
            Mathf.Max(0.01f, (safeMinimum + safeMaximum) * 0.5f);

        float repeatInterval =
            averageDelay + Mathf.Max(0f, cycleExtraTime);

        float expectedSpawnTime = averageDelay;
        int markerCount = 0;

        while (expectedSpawnTime <= duration && markerCount < 24)
        {
            row.markers.Add(expectedSpawnTime);
            expectedSpawnTime += repeatInterval;
            markerCount++;
        }

        return row;
    }

    private static void AddRepeatedMarkers(
        List<float> markers,
        float interval,
        int count)
    {
        float safeInterval = Mathf.Max(0.01f, interval);

        for (int i = 1; i <= count; i++)
            markers.Add(safeInterval * i);
    }

    private static void DrawTimelineAxis(float duration)
    {
        Rect rect = GUILayoutUtility.GetRect(
            0f,
            28f,
            GUILayout.ExpandWidth(true)
        );

        const float labelWidth = 132f;
        Rect track = new Rect(
            rect.x + labelWidth,
            rect.y + 3f,
            Mathf.Max(20f, rect.width - labelWidth - 4f),
            rect.height - 6f
        );

        DrawTimelineBackground(track);

        GUIStyle labelStyle = new GUIStyle(
            EditorStyles.centeredGreyMiniLabel
        );

        for (int i = 0; i <= 4; i++)
        {
            float normalized = i / 4f;
            float x = track.x + track.width * normalized;

            EditorGUI.DrawRect(
                new Rect(x, track.y, 1f, track.height),
                GetTimelineGridColor()
            );

            Rect labelRect = new Rect(
                x - 25f,
                track.y,
                50f,
                track.height
            );

            GUI.Label(
                labelRect,
                $"{duration * normalized:0.#}s",
                labelStyle
            );
        }
    }

    private static void DrawTimelineRow(
        TimelineRow row,
        float duration)
    {
        Rect rect = GUILayoutUtility.GetRect(
            0f,
            24f,
            GUILayout.ExpandWidth(true)
        );

        const float labelWidth = 132f;

        Rect labelRect = new Rect(
            rect.x,
            rect.y + 3f,
            labelWidth - 6f,
            rect.height - 6f
        );

        Rect track = new Rect(
            rect.x + labelWidth,
            rect.y + 4f,
            Mathf.Max(20f, rect.width - labelWidth - 4f),
            rect.height - 8f
        );

        GUI.Label(
            labelRect,
            new GUIContent(row.label, row.tooltip),
            EditorStyles.miniLabel
        );

        DrawTimelineBackground(track);

        for (int i = 1; i < 4; i++)
        {
            float x =
                track.x +
                track.width *
                (i / 4f);

            EditorGUI.DrawRect(
                new Rect(x, track.y, 1f, track.height),
                GetTimelineGridColor()
            );
        }

        if (row.hasWindow)
        {
            float startNormalized =
                Mathf.Clamp01(
                    row.windowStart /
                    Mathf.Max(0.01f, duration)
                );

            float endNormalized =
                Mathf.Clamp01(
                    row.windowEnd /
                    Mathf.Max(0.01f, duration)
                );

            float x = track.x + track.width * startNormalized;
            float width = Mathf.Max(
                3f,
                track.width *
                Mathf.Max(0f, endNormalized - startNormalized)
            );

            EditorGUI.DrawRect(
                new Rect(
                    x,
                    track.y + 2f,
                    width,
                    track.height - 4f
                ),
                row.color
            );
        }

        foreach (float markerTime in row.markers)
        {
            float normalized =
                Mathf.Clamp01(
                    markerTime /
                    Mathf.Max(0.01f, duration)
                );

            float x =
                track.x +
                track.width *
                normalized;

            EditorGUI.DrawRect(
                new Rect(
                    x - 2f,
                    track.y + 1f,
                    4f,
                    track.height - 2f
                ),
                row.color
            );
        }

        GUI.Label(
            track,
            new GUIContent(string.Empty, row.tooltip)
        );
    }

    private static void DrawTimelineBackground(Rect rect)
    {
        EditorGUI.DrawRect(
            rect,
            EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.12f, 0.12f, 0.65f)
                : new Color(0.76f, 0.76f, 0.76f, 0.7f)
        );
    }

    private static Color GetTimelineGridColor()
    {
        return EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.08f)
            : new Color(0f, 0f, 0f, 0.10f);
    }

    private static Color GetTimelineColor(
        Color darkSkinColor,
        Color lightSkinColor)
    {
        return EditorGUIUtility.isProSkin
            ? darkSkinColor
            : lightSkinColor;
    }
}
