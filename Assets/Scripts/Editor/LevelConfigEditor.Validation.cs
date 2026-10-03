using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in LevelConfigEditor.cs.
public partial class LevelConfigEditor
{
    private void DrawGlobalWarnings()
    {
        if (serializedObject.isEditingMultipleObjects)
            return;

        LevelConfig config = target as LevelConfig;

        if (config == null)
            return;

        bool hasThreat =
            config.normalEnemyCount > 0 ||
            config.projectileEnemyCount > 0 ||
            config.hunterEnemyCount > 0 ||
            config.beaconEnemyCount > 0 ||
            config.bossEnabled ||
            config.verticalLaserEnabled ||
            config.horizontalLaserEnabled ||
            config.bombTrapEnabled;

        if (hasThreat && !config.HasDangerProfile)
        {
            EditorGUILayout.HelpBox(
                "Danger Balance Profile is not assigned. The game will remain functional by using the old hidden LevelConfig values, but danger tier selection will not change behaviour until a profile is assigned.",
                MessageType.Warning
            );
        }

        if (!hasThreat)
        {
            Warning("This level contains no enemies, boss or traps.");
        }

        if (config.UsesScore &&
            !config.normalCoinEnabled &&
            !config.goldCoinEnabled &&
            !config.rareCoinEnabled)
        {
            Warning("This objective requires score, but every coin type is disabled.");
        }

        if (config.UsesScore && config.maxCoinCount <= 0)
        {
            Warning("This objective requires score, but Max Coin Count is 0.");
        }

        float enabledChance = 0f;

        if (config.normalCoinEnabled)
            enabledChance += config.normalCoinChance;
        if (config.goldCoinEnabled)
            enabledChance += config.goldCoinChance;
        if (config.rareCoinEnabled)
            enabledChance += config.rareCoinChance;

        if (config.UsesScore &&
            enabledChance > 0f &&
            !Mathf.Approximately(enabledChance, 100f))
        {
            Help($"Enabled coin chances total {enabledChance:0.##}%. A total of 100% is recommended.");
        }

        if (config.beaconEnemyCount > 0 &&
            config.normalEnemyCount <= 0 &&
            config.projectileEnemyCount <= 0 &&
            config.hunterEnemyCount <= 0)
        {
            Help("Beacon is enabled without Normal, Projectile or Hunter enemies. Its buff will have very few useful targets.");
        }

        if (config.bossEnabled)
        {
            if (config.EffectiveBossSpawnCondition == BossSpawnCondition.Score &&
                config.bossSpawnScore >= config.winScore)
            {
                Warning("Boss Spawn Score should be lower than Win Score.");
            }

            if (config.EffectiveBossSpawnCondition == BossSpawnCondition.Time &&
                config.bossSpawnTime >= config.timeLimit)
            {
                Warning("Boss Spawn Time should be lower than the level Time Limit.");
            }
        }

        int extremeCount = CountExtremeThreats(config);

        if (extremeCount >= 3)
        {
            Warning(
                $"This level combines {extremeCount} D4/D5 threats. Test reaction windows carefully, especially when their spawn timings overlap."
            );
        }

        if (HasAnyCustomOverride(config))
        {
            Help("This level contains custom danger overrides. Future balance-profile changes will not affect those overridden threats.");
        }
    }

    private void DrawValidation()
    {
        FoldoutBox(
            "LEVEL VALIDATION",
            ref validationExpanded,
            () =>
            {
                LevelConfig config = GetSingleConfig();

                if (config == null)
                {
                    Help("Validation is available when a single LevelConfig is selected.");
                    return;
                }

                List<ValidationIssue> issues =
                    BuildValidationIssues(config);

                int errors = CountIssues(
                    issues,
                    ValidationSeverity.Error
                );

                int warnings = CountIssues(
                    issues,
                    ValidationSeverity.Warning
                );

                int suggestions = CountIssues(
                    issues,
                    ValidationSeverity.Suggestion
                );

                EditorGUILayout.BeginHorizontal();
                DrawValidationCount("ERRORS", errors);
                DrawValidationCount("WARNINGS", warnings);
                DrawValidationCount("SUGGESTIONS", suggestions);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                if (issues.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No level-design problems were detected.",
                        MessageType.Info
                    );
                    return;
                }

                DrawValidationGroup(
                    issues,
                    ValidationSeverity.Error,
                    "ERRORS"
                );

                DrawValidationGroup(
                    issues,
                    ValidationSeverity.Warning,
                    "WARNINGS"
                );

                DrawValidationGroup(
                    issues,
                    ValidationSeverity.Suggestion,
                    "DESIGN SUGGESTIONS"
                );

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(
                    "Validation also checks duplicate mechanic introductions across all LevelConfig assets.",
                    EditorStyles.wordWrappedMiniLabel
                );
            }
        );
    }

    private static void DrawValidationCount(
        string label,
        int count)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            label,
            EditorStyles.centeredGreyMiniLabel
        );
        EditorGUILayout.LabelField(
            count.ToString(),
            new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            }
        );
        EditorGUILayout.EndVertical();
    }

    private void DrawValidationGroup(
        List<ValidationIssue> issues,
        ValidationSeverity severity,
        string title)
    {
        bool hasAny = false;

        foreach (ValidationIssue issue in issues)
        {
            if (issue.severity == severity)
            {
                hasAny = true;
                break;
            }
        }

        if (!hasAny)
            return;

        MiniTitle(title);

        foreach (ValidationIssue issue in issues)
        {
            if (issue.severity != severity)
                continue;

            MessageType messageType =
                severity == ValidationSeverity.Error
                    ? MessageType.Error
                    : severity == ValidationSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info;

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );

            EditorGUILayout.HelpBox(
                issue.message,
                messageType
            );

            if (issue.fix != null ||
                !string.IsNullOrWhiteSpace(
                    issue.propertyPath))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (!string.IsNullOrWhiteSpace(
                        issue.propertyPath) &&
                    GUILayout.Button(
                        focusedValidationPropertyPath ==
                        issue.propertyPath
                            ? "HIDE FIELD"
                            : "EDIT FIELD",
                        GUILayout.Width(100f)))
                {
                    focusedValidationPropertyPath =
                        focusedValidationPropertyPath ==
                        issue.propertyPath
                            ? null
                            : issue.propertyPath;
                }

                if (issue.fix != null &&
                    GUILayout.Button(
                        issue.fixLabel,
                        GUILayout.Width(110f)))
                {
                    ApplyValidationFix(issue);
                }

                EditorGUILayout.EndHorizontal();
            }

            if (!string.IsNullOrWhiteSpace(
                    issue.propertyPath) &&
                focusedValidationPropertyPath ==
                issue.propertyPath)
            {
                SerializedProperty focusedProperty =
                    serializedObject.FindProperty(
                        issue.propertyPath
                    );

                if (focusedProperty != null)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.PropertyField(
                        focusedProperty,
                        true
                    );
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "The linked field could not be found: " +
                        issue.propertyPath,
                        MessageType.Error
                    );
                }
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void ApplyValidationFix(
        ValidationIssue issue)
    {
        LevelConfig config = GetSingleConfig();

        if (config == null || issue.fix == null)
            return;

        serializedObject.ApplyModifiedProperties();

        Undo.RecordObject(
            config,
            "Fix Level Validation Issue"
        );

        issue.fix(config);

        EditorUtility.SetDirty(config);
        serializedObject.Update();
        GUI.changed = true;
    }

    private static int CountIssues(
        List<ValidationIssue> issues,
        ValidationSeverity severity)
    {
        int count = 0;

        foreach (ValidationIssue issue in issues)
        {
            if (issue.severity == severity)
                count++;
        }

        return count;
    }

    private List<ValidationIssue> BuildValidationIssues(
        LevelConfig config)
    {
        List<ValidationIssue> issues =
            new List<ValidationIssue>();

        bool hasThreat = HasAnyThreat(config);

        if (hasThreat && !config.HasDangerProfile)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Danger Balance Profile is not assigned. Danger tiers will use legacy hidden fallback values.",
                    null,
                    "AUTO FIX",
                    "dangerBalanceProfile"
                )
            );
        }

        if (!hasThreat)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Suggestion,
                    "This level contains no enemies, boss or traps. That is fine for a pure introduction, but confirm that the level still has meaningful pressure."
                )
            );
        }

        if (config.UsesScore &&
            !config.normalCoinEnabled &&
            !config.goldCoinEnabled &&
            !config.rareCoinEnabled)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Error,
                    "This objective requires score, but every coin type is disabled.",
                    level =>
                    {
                        level.normalCoinEnabled = true;
                        level.normalCoinChance = 100f;
                    },
                    "AUTO FIX",
                    "normalCoinEnabled"
                )
            );
        }

        if (config.UsesScore &&
            config.maxCoinCount <= 0)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Error,
                    "This objective requires score, but Max Coin Count is 0.",
                    level => level.maxCoinCount = 8,
                    "AUTO FIX",
                    "maxCoinCount"
                )
            );
        }

        float enabledChance =
            GetEnabledCoinChance(config);

        if (config.UsesScore &&
            enabledChance > 0f &&
            !Mathf.Approximately(enabledChance, 100f))
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Warning,
                    $"Enabled coin chances total {enabledChance:0.##}%. A total of 100% keeps the distribution predictable.",
                    NormalizeCoinChances,
                    "AUTO FIX",
                    "normalCoinChance"
                )
            );
        }

        if (config.beaconEnemyCount > 0 &&
            config.normalEnemyCount <= 0 &&
            config.projectileEnemyCount <= 0 &&
            config.hunterEnemyCount <= 0 &&
            !config.bossEnabled)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Beacon is enabled without another enemy type or boss. Its buff has no useful combat target.",
                    null,
                    "AUTO FIX",
                    "beaconEnemyCount"
                )
            );
        }

        ValidateBoss(config, issues);
        ValidateSpawnWindows(config, issues);

        int extremeCount =
            CountExtremeThreats(config);

        if (extremeCount >= 3)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Warning,
                    $"This level combines {extremeCount} D4/D5 threats. Test reaction windows carefully, especially where spawn timings overlap."
                )
            );
        }

        int openingPressure =
            CountOpeningPressure(config, 5f);

        if (openingPressure >= 3)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Warning,
                    $"{openingPressure} threat systems can become active during the first 5 seconds. The opening may not give the player enough time to read the arena."
                )
            );
        }

        float dangerAverage =
            config.GetActiveDangerAverage();

        if (dangerAverage >= 3.5f &&
            config.SafeMissionDifficulty <= 1)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Suggestion,
                    $"Estimated combat pressure is D{dangerAverage:0.0}, but the displayed mission difficulty is only {config.SafeMissionDifficulty}/5.",
                    null,
                    "AUTO FIX",
                    "missionDifficulty"
                )
            );
        }

        if (HasAnyCustomOverride(config))
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Suggestion,
                    "This level contains custom danger overrides. Future shared profile changes will not affect those overridden threats."
                )
            );
        }

        ValidateMechanicProgression(
            config,
            issues
        );

        if (!HasUsefulBriefingPages(config))
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Suggestion,
                    "The briefing has no useful extra page or still contains the default placeholder.",
                    PopulateBriefingDraft,
                    "GENERATE DRAFT",
                    "briefingPages"
                )
            );
        }

        ValidateObstaclePool(config, issues);

        return issues;
    }

    private static void ValidateBoss(
        LevelConfig config,
        List<ValidationIssue> issues)
    {
        if (!config.bossEnabled)
            return;

        if (config.EffectiveBossSpawnCondition ==
            BossSpawnCondition.Score)
        {
            if (config.bossSpawnScore >=
                config.SafeWinScore)
            {
                issues.Add(
                    new ValidationIssue(
                        ValidationSeverity.Error,
                        "Boss Spawn Score must be lower than the mission Win Score or the boss may never appear.",
                        level =>
                        {
                            level.bossSpawnScore =
                                Mathf.Max(
                                    0,
                                    Mathf.CeilToInt(
                                        level.SafeWinScore *
                                        0.75f
                                    ) - 1
                                );
                        },
                        "AUTO FIX",
                        "bossSpawnScore"
                    )
                );
            }
        }
        else if (config.bossSpawnTime >=
                 config.SafeTimeLimit)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Error,
                    "Boss Spawn Time must be lower than the mission Time Limit or the boss may never appear.",
                    level =>
                    {
                        level.bossSpawnTime =
                            Mathf.Max(
                                0f,
                                level.SafeTimeLimit *
                                0.75f
                            );
                    },
                    "AUTO FIX",
                    "bossSpawnTime"
                )
            );
        }
    }

    private static void ValidateSpawnWindows(
        LevelConfig config,
        List<ValidationIssue> issues)
    {
        ValidateWindow(
            issues,
            "Beacon Spawn Time",
            config.beaconEnemyCount > 0,
            config.beaconMinSpawnTime,
            config.beaconMaxSpawnTime,
            level =>
            {
                FitWindowToDuration(
                    ref level.beaconMinSpawnTime,
                    ref level.beaconMaxSpawnTime,
                    level.UsesTime
                        ? level.SafeTimeLimit
                        : Mathf.Max(
                            level.beaconMaxSpawnTime,
                            20f
                        )
                );
            }
        );

        ValidateWindow(
            issues,
            "Armor Spawn Time",
            config.armorEnabled,
            config.armorMinSpawnTime,
            config.armorMaxSpawnTime,
            level =>
            {
                FitWindowToDuration(
                    ref level.armorMinSpawnTime,
                    ref level.armorMaxSpawnTime,
                    level.UsesTime
                        ? level.SafeTimeLimit
                        : Mathf.Max(
                            level.armorMaxSpawnTime,
                            20f
                        )
                );
            }
        );

        ValidateWindow(
            issues,
            "Slow Spawn Time",
            config.slowEnabled,
            config.slowMinSpawnTime,
            config.slowMaxSpawnTime,
            level =>
            {
                FitWindowToDuration(
                    ref level.slowMinSpawnTime,
                    ref level.slowMaxSpawnTime,
                    level.UsesTime
                        ? level.SafeTimeLimit
                        : Mathf.Max(
                            level.slowMaxSpawnTime,
                            20f
                        )
                );
            }
        );

        if (!config.UsesTime)
            return;

        float duration = config.SafeTimeLimit;

        ValidateFirstSpawnBeforeEnd(
            issues,
            "Normal Enemy",
            config.normalEnemyCount > 0,
            config.normalEnemySpawnInterval,
            duration,
            level =>
                level.normalEnemySpawnInterval =
                    Mathf.Max(
                        0.1f,
                        level.SafeTimeLimit * 0.20f
                    )
        );

        ValidateFirstSpawnBeforeEnd(
            issues,
            "Projectile Enemy",
            config.projectileEnemyCount > 0,
            config.projectileEnemySpawnInterval,
            duration,
            level =>
                level.projectileEnemySpawnInterval =
                    Mathf.Max(
                        0.1f,
                        level.SafeTimeLimit * 0.20f
                    )
        );

        ValidateFirstSpawnBeforeEnd(
            issues,
            "Hunter Enemy",
            config.hunterEnemyCount > 0,
            config.hunterEnemySpawnInterval,
            duration,
            level =>
                level.hunterEnemySpawnInterval =
                    Mathf.Max(
                        0.1f,
                        level.SafeTimeLimit * 0.25f
                    )
        );

        ValidateFirstSpawnBeforeEnd(
            issues,
            "Beacon Enemy",
            config.beaconEnemyCount > 0,
            config.beaconMinSpawnTime,
            duration,
            level =>
            {
                level.beaconMinSpawnTime =
                    level.SafeTimeLimit * 0.30f;
                level.beaconMaxSpawnTime =
                    level.SafeTimeLimit * 0.60f;
            }
        );

        ValidateFirstSpawnBeforeEnd(
            issues,
            "Armor",
            config.armorEnabled,
            config.armorMinSpawnTime,
            duration,
            level =>
            {
                level.armorMinSpawnTime =
                    level.SafeTimeLimit * 0.30f;
                level.armorMaxSpawnTime =
                    level.SafeTimeLimit * 0.65f;
            }
        );

        ValidateFirstSpawnBeforeEnd(
            issues,
            "Slow",
            config.slowEnabled,
            config.slowMinSpawnTime,
            duration,
            level =>
            {
                level.slowMinSpawnTime =
                    level.SafeTimeLimit * 0.30f;
                level.slowMaxSpawnTime =
                    level.SafeTimeLimit * 0.65f;
            }
        );

        if (config.verticalLaserEnabled)
        {
            LaserDangerSettings settings =
                config.ResolveVerticalLaserDanger();

            if (settings.minSpawnTime >= duration)
            {
                issues.Add(
                    new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Vertical Laser first-spawn window begins after the mission ends. Choose a faster danger tier or create a per-level override."
                    )
                );
            }
        }

        if (config.horizontalLaserEnabled)
        {
            LaserDangerSettings settings =
                config.ResolveHorizontalLaserDanger();

            if (settings.minSpawnTime >= duration)
            {
                issues.Add(
                    new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Horizontal Laser first-spawn window begins after the mission ends. Choose a faster danger tier or create a per-level override."
                    )
                );
            }
        }

        if (config.bombTrapEnabled)
        {
            BombDangerSettings settings =
                config.ResolveBombDanger();

            if (settings.minSpawnTime >= duration)
            {
                issues.Add(
                    new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Space Bomb first-spawn window begins after the mission ends. Choose a faster danger tier or create a per-level override."
                    )
                );
            }
        }
    }

    private static void ValidateWindow(
        List<ValidationIssue> issues,
        string displayName,
        bool active,
        float min,
        float max,
        Action<LevelConfig> fix)
    {
        if (!active || min <= max)
            return;

        issues.Add(
            new ValidationIssue(
                ValidationSeverity.Error,
                $"{displayName}: minimum value cannot be greater than maximum value.",
                fix
            )
        );
    }

    private static void ValidateFirstSpawnBeforeEnd(
        List<ValidationIssue> issues,
        string displayName,
        bool active,
        float firstSpawn,
        float duration,
        Action<LevelConfig> fix)
    {
        if (!active || firstSpawn < duration)
            return;

        issues.Add(
            new ValidationIssue(
                ValidationSeverity.Warning,
                $"{displayName} is enabled, but its first spawn occurs at or after the mission ends.",
                fix
            )
        );
    }

    private static void FitWindowToDuration(
        ref float min,
        ref float max,
        float duration)
    {
        float safeDuration =
            Mathf.Max(0.2f, duration);

        float lower = Mathf.Min(min, max);
        float upper = Mathf.Max(min, max);

        if (lower >= safeDuration)
            lower = safeDuration * 0.30f;

        if (upper >= safeDuration)
            upper = safeDuration * 0.70f;

        min = Mathf.Max(0f, lower);
        max = Mathf.Max(min, upper);
    }

    private void ValidateMechanicProgression(
        LevelConfig config,
        List<ValidationIssue> issues)
    {
        int introducedCount = 0;

        foreach (MechanicDescriptor descriptor in
                 MechanicDescriptors)
        {
            bool active =
                descriptor.isActive(config);

            MechanicProgressionStatus status =
                GetMechanicStatus(
                    config,
                    descriptor.id
                );

            if (!active &&
                status !=
                MechanicProgressionStatus.AlreadyKnown)
            {
                MechanicId id = descriptor.id;

                issues.Add(
                    new ValidationIssue(
                        ValidationSeverity.Warning,
                        $"{descriptor.label} is tagged as {GetMechanicStatusLabel(status)}, but the mechanic is inactive in this level.",
                        level =>
                            SetMechanicStatus(
                                level,
                                id,
                                MechanicProgressionStatus.AlreadyKnown
                            ),
                        "AUTO FIX",
                        descriptor.propertyPath
                    )
                );

                continue;
            }

            if (!active)
                continue;

            if (status ==
                MechanicProgressionStatus.IntroducedHere)
            {
                introducedCount++;

                DangerLevel danger;

                if (TryGetThreatDanger(
                    config,
                    descriptor.id,
                    out danger) &&
                    (int)danger >=
                    (int)DangerLevel.Danger4)
                {
                    issues.Add(
                        new ValidationIssue(
                            ValidationSeverity.Warning,
                            $"{descriptor.label} is introduced at {DangerLevelUtility.GetDisplayName(danger)}. First appearances usually need a more readable D1–D2 setup."
                        )
                    );
                }

                List<LevelConfig> duplicates =
                    FindDuplicateIntroductions(
                        config,
                        descriptor
                    );

                if (duplicates.Count > 0)
                {
                    List<string> labels =
                        new List<string>();

                    foreach (LevelConfig duplicate in
                             duplicates)
                    {
                        labels.Add(
                            $"Level {duplicate.levelNumber}"
                        );
                    }

                    issues.Add(
                        new ValidationIssue(
                            ValidationSeverity.Warning,
                            $"{descriptor.label} is also marked as introduced in {string.Join(", ", labels)}.",
                            null,
                            "AUTO FIX",
                            descriptor.propertyPath
                        )
                    );
                }
            }
        }

        if (introducedCount > 2)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Suggestion,
                    $"This level introduces {introducedCount} mechanics at once. Consider limiting major introductions to one or two so the player can read each system."
                )
            );
        }
    }

    private static void ValidateObstaclePool(
        LevelConfig config,
        List<ValidationIssue> issues)
    {
        if (config.obstacleSpawnMode !=
            ObstacleSpawnMode.Random ||
            config.randomObstacleCount <= 0)
        {
            return;
        }

        bool hasEnabledPrefab = false;

        if (config.levelObstacles != null)
        {
            foreach (LevelObstacleOption option in
                     config.levelObstacles)
            {
                if (option != null &&
                    option.enabled &&
                    option.prefab != null)
                {
                    hasEnabledPrefab = true;
                    break;
                }
            }
        }

        if (!hasEnabledPrefab)
        {
            issues.Add(
                new ValidationIssue(
                    ValidationSeverity.Error,
                    "Random obstacle spawning is enabled, but the obstacle pool contains no enabled prefab.",
                    null,
                    "AUTO FIX",
                    "levelObstacles"
                )
            );
        }
    }

    private static int CountOpeningPressure(
        LevelConfig config,
        float openingSeconds)
    {
        int count = 0;

        if (config.normalEnemyCount > 0 &&
            config.normalEnemySpawnInterval <= openingSeconds)
            count++;

        if (config.projectileEnemyCount > 0 &&
            config.projectileEnemySpawnInterval <= openingSeconds)
            count++;

        if (config.hunterEnemyCount > 0 &&
            config.hunterEnemySpawnInterval <= openingSeconds)
            count++;

        if (config.beaconEnemyCount > 0 &&
            config.beaconMinSpawnTime <= openingSeconds)
            count++;

        if (config.bossEnabled &&
            config.EffectiveBossSpawnCondition ==
            BossSpawnCondition.Time &&
            config.bossSpawnTime <= openingSeconds)
            count++;

        if (config.verticalLaserEnabled &&
            config.ResolveVerticalLaserDanger()
                .minSpawnTime <= openingSeconds)
            count++;

        if (config.horizontalLaserEnabled &&
            config.ResolveHorizontalLaserDanger()
                .minSpawnTime <= openingSeconds)
            count++;

        if (config.bombTrapEnabled &&
            config.ResolveBombDanger()
                .minSpawnTime <= openingSeconds)
            count++;

        return count;
    }

    private static bool HasAnyThreat(
        LevelConfig config)
    {
        return
            config.normalEnemyCount > 0 ||
            config.projectileEnemyCount > 0 ||
            config.hunterEnemyCount > 0 ||
            config.beaconEnemyCount > 0 ||
            config.bossEnabled ||
            config.verticalLaserEnabled ||
            config.horizontalLaserEnabled ||
            config.bombTrapEnabled;
    }

    private static float GetEnabledCoinChance(
        LevelConfig config)
    {
        float total = 0f;

        if (config.normalCoinEnabled)
            total += config.normalCoinChance;

        if (config.goldCoinEnabled)
            total += config.goldCoinChance;

        if (config.rareCoinEnabled)
            total += config.rareCoinChance;

        return total;
    }

    private static void NormalizeCoinChances(
        LevelConfig config)
    {
        float total =
            GetEnabledCoinChance(config);

        if (total <= 0f)
        {
            config.normalCoinEnabled = true;
            config.normalCoinChance = 100f;
            config.goldCoinChance = 0f;
            config.rareCoinChance = 0f;
            return;
        }

        float multiplier = 100f / total;

        if (config.normalCoinEnabled)
            config.normalCoinChance *= multiplier;

        if (config.goldCoinEnabled)
            config.goldCoinChance *= multiplier;

        if (config.rareCoinEnabled)
            config.rareCoinChance *= multiplier;
    }

    private static bool HasActiveObstacles(
        LevelConfig config)
    {
        if (config == null)
            return false;

        bool hasEnabledPrefab = false;

        if (config.levelObstacles != null)
        {
            foreach (LevelObstacleOption option in
                     config.levelObstacles)
            {
                if (option != null &&
                    option.enabled &&
                    option.prefab != null)
                {
                    hasEnabledPrefab = true;
                    break;
                }
            }
        }

        if (!hasEnabledPrefab)
            return false;

        return config.obstacleSpawnMode ==
               ObstacleSpawnMode.Fixed ||
               config.randomObstacleCount > 0;
    }

    private static List<LevelConfig>
        FindDuplicateIntroductions(
            LevelConfig current,
            MechanicDescriptor descriptor)
    {
        List<LevelConfig> duplicates =
            new List<LevelConfig>();

        foreach (LevelConfig config in
                 GetAllLevelConfigs())
        {
            if (config == null ||
                config == current ||
                config.mechanicProgression == null ||
                !descriptor.isActive(config))
            {
                continue;
            }

            if (GetMechanicStatus(
                    config,
                    descriptor.id) ==
                MechanicProgressionStatus.IntroducedHere)
            {
                duplicates.Add(config);
            }
        }

        return duplicates;
    }

    private void ValidateMinMax(
        string minPropertyName,
        string maxPropertyName,
        string displayName)
    {
        SerializedProperty minProperty =
            serializedObject.FindProperty(minPropertyName);

        SerializedProperty maxProperty =
            serializedObject.FindProperty(maxPropertyName);

        if (minProperty == null ||
            maxProperty == null ||
            minProperty.hasMultipleDifferentValues ||
            maxProperty.hasMultipleDifferentValues)
        {
            return;
        }

        if (GetNumericValue(minProperty) <= GetNumericValue(maxProperty))
            return;

        EditorGUILayout.HelpBox(
            displayName + ": minimum value cannot be greater than maximum value.",
            MessageType.Error
        );
    }

    private static int CountExtremeThreats(LevelConfig config)
    {
        int count = 0;

        AddExtreme(config.normalEnemyCount > 0, config.normalEnemyDanger, ref count);
        AddExtreme(config.projectileEnemyCount > 0, config.projectileEnemyDanger, ref count);
        AddExtreme(config.hunterEnemyCount > 0, config.hunterEnemyDanger, ref count);
        AddExtreme(config.beaconEnemyCount > 0, config.beaconEnemyDanger, ref count);
        AddExtreme(config.bossEnabled, config.bossDanger, ref count);
        AddExtreme(config.verticalLaserEnabled, config.verticalLaserDanger, ref count);
        AddExtreme(config.horizontalLaserEnabled, config.horizontalLaserDanger, ref count);
        AddExtreme(config.bombTrapEnabled, config.bombDanger, ref count);

        return count;
    }

    private static void AddExtreme(
        bool active,
        DangerLevel level,
        ref int count)
    {
        if (active && (int)level >= (int)DangerLevel.Danger4)
            count++;
    }
}
