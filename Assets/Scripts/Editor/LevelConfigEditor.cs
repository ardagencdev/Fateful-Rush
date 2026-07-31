using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelConfig))]
[CanEditMultipleObjects]
public class LevelConfigEditor : Editor
{
    private enum EditorViewMode
    {
        Basic,
        Advanced
    }

    private EditorViewMode viewMode;

    private bool summaryExpanded = true;
    private bool coreExpanded = true;
    private bool briefingExpanded = true;
    private bool musicExpanded = true;
    private bool playerExpanded = true;
    private bool abilitiesExpanded;
    private bool comboExpanded;
    private bool backgroundExpanded;
    private bool coinsExpanded = true;
    private bool obstaclesExpanded = true;
    private bool balanceExpanded = true;
    private bool enemiesExpanded = true;
    private bool powerUpsExpanded;
    private bool trapsExpanded = true;

    private bool normalEnemyExpanded = true;
    private bool projectileEnemyExpanded = true;
    private bool hunterEnemyExpanded = true;
    private bool bossExpanded = true;
    private bool beaconExpanded = true;

    private bool IsAdvanced =>
        viewMode == EditorViewMode.Advanced;

    private WinConditionType SelectedWinCondition =>
        (WinConditionType)EnumValue("winCondition");

    private bool UsesScore =>
        SelectedWinCondition == WinConditionType.ReachScore ||
        SelectedWinCondition == WinConditionType.ReachScoreWithinTime;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawMainHeader();
        DrawViewModeToolbar();
        DrawSummary();
        DrawGlobalWarnings();

        DrawCore();
        DrawMissionBriefing();
        DrawMusic();
        DrawPlayer();
        DrawAbilities();
        DrawCombo();
        DrawBackground();
        DrawCoins();
        DrawObstacles();
        DrawDangerBalance();
        DrawEnemies();
        DrawPowerUps();
        DrawTraps();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMainHeader()
    {
        EditorGUILayout.Space(6);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField(
            "VOID RUSH — LEVEL DESIGN",
            titleStyle,
            GUILayout.Height(28f)
        );

        EditorGUILayout.LabelField(
            "Composition in LevelConfig • Behaviour in Danger Balance Profile",
            EditorStyles.centeredGreyMiniLabel
        );

        EditorGUILayout.Space(4);
    }

    private void DrawViewModeToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        viewMode = (EditorViewMode)GUILayout.Toolbar(
            (int)viewMode,
            new[] { "BASIC DESIGN", "ADVANCED / OVERRIDES" }
        );

        EditorGUILayout.Space(3);
        EditorGUILayout.HelpBox(
            IsAdvanced
                ? "Advanced mode exposes technical player settings and optional per-level danger overrides. Presets remain the recommended default."
                : "Basic mode contains the fields required for fast level design. Enemy and trap behaviour comes from the selected danger tiers.",
            MessageType.Info
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawSummary()
    {
        FoldoutBox(
            "LEVEL SUMMARY",
            ref summaryExpanded,
            () =>
            {
                if (serializedObject.isEditingMultipleObjects)
                {
                    Help("Detailed summary is available when a single LevelConfig is selected.");
                    return;
                }

                LevelConfig config = target as LevelConfig;

                if (config == null)
                    return;

                SummaryRow("Level", $"{config.levelNumber} — {config.levelName}");
                SummaryRow("Objective", GetWinConditionSummary(config));
                SummaryRow("Mission Stars", $"{config.SafeMissionDifficulty}/5");
                SummaryRow("Danger Profile", config.dangerBalanceProfile != null
                    ? config.dangerBalanceProfile.name
                    : "LEGACY FALLBACK");

                float dangerAverage = config.GetActiveDangerAverage();
                SummaryRow(
                    "Danger Average",
                    dangerAverage > 0f
                        ? $"D{dangerAverage:0.0}"
                        : "No active threats"
                );

                SummaryRow("Enemies", GetEnemySummary(config));
                SummaryRow("Hazards", GetHazardSummary(config));
                SummaryRow("Boss", GetBossSummary(config));
                SummaryRow("Music", config.gameplayMusic != null
                    ? config.gameplayMusic.name
                    : "Not Assigned");
            }
        );
    }

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

    private void DrawCore()
    {
        FoldoutBox(
            "LEVEL / WIN CONDITION",
            ref coreExpanded,
            () =>
            {
                Prop("levelNumber");
                Prop("levelName");
                Space();
                Prop("winCondition");

                switch (SelectedWinCondition)
                {
                    case WinConditionType.ReachScore:
                        Prop("winScore");
                        Help("Collect the target score. Timer HUD and survival countdown are disabled.");
                        break;

                    case WinConditionType.SurviveTime:
                        Prop("timeLimit");
                        Help("Survive until the countdown reaches zero. Coins, score and combo are disabled at runtime.");
                        break;

                    case WinConditionType.ReachScoreWithinTime:
                        Prop("winScore");
                        Prop("timeLimit");
                        Help("Reach the score before the countdown reaches zero.");
                        break;
                }
            }
        );
    }

    private void DrawMissionBriefing()
    {
        FoldoutBox(
            "MISSION BRIEFING",
            ref briefingExpanded,
            () =>
            {
                Prop("briefingTitle");
                Prop("missionDifficulty");
                Prop("briefingModeDescription");
                Prop("briefingObjectiveDescription");
                Prop("briefingPages", true);
            }
        );
    }

    private void DrawMusic()
    {
        FoldoutBox(
            "GAMEPLAY MUSIC",
            ref musicExpanded,
            () => Prop("gameplayMusic")
        );
    }

    private void DrawPlayer()
    {
        FoldoutBox(
            "PLAYER",
            ref playerExpanded,
            () =>
            {
                Prop("playerMoveSpeed");

                if (IsAdvanced)
                    Prop("playerComboSpeedBonus");
            }
        );
    }

    private void DrawAbilities()
    {
        FoldoutBox(
            "PLAYER ABILITIES",
            ref abilitiesExpanded,
            () =>
            {
                Prop("dashEnabled");

                if (BoolValue("dashEnabled") && IsAdvanced)
                {
                    Prop("dashDistance");
                    Prop("dashDuration");
                    Prop("dashCooldown");
                }

                Space();
                Prop("cloneEnabled");

                if (BoolValue("cloneEnabled"))
                {
                    Prop("cloneUses");

                    if (IsAdvanced)
                    {
                        Prop("cloneDuration");
                        Prop("cloneCooldown");
                    }
                }
            }
        );
    }

    private void DrawCombo()
    {
        FoldoutBox(
            "COMBO / HUD",
            ref comboExpanded,
            () =>
            {
                if (!UsesScore)
                {
                    Help("Combo is automatically disabled for Survive Time missions.");
                    return;
                }

                Prop("comboEnabled");

                if (!BoolValue("comboEnabled"))
                    return;

                Prop("comboTimeLimit");
                Prop("maxCombo");

                if (IsAdvanced)
                    Prop("comboSpeedStages", true);
            }
        );
    }

    private void DrawBackground()
    {
        FoldoutBox(
            "BACKGROUND / NEAR STARS",
            ref backgroundExpanded,
            () =>
            {
                Prop("randomizeNearStarsColor");

                if (!BoolValue("randomizeNearStarsColor"))
                    Prop("nearStarsColor");

                if (IsAdvanced)
                {
                    Prop("nearStarsSpeedMultiplier");
                    Prop("nearStarsSizeMultiplier");
                    Prop("nearStarsEmissionRate");
                }
            }
        );
    }

    private void DrawCoins()
    {
        FoldoutBox(
            "COINS",
            ref coinsExpanded,
            () =>
            {
                if (!UsesScore)
                {
                    Help("Coins are automatically disabled for this win condition.");
                    return;
                }

                Prop("coinSpawnInterval");
                Prop("maxCoinCount");
                Space();
                DrawCoin("Normal Coin", "normalCoinEnabled", "normalCoinChance", "normalCoinValue");
                DrawCoin("Gold Coin", "goldCoinEnabled", "goldCoinChance", "goldCoinValue");
                DrawCoin("Rare Coin", "rareCoinEnabled", "rareCoinChance", "rareCoinValue");
            }
        );
    }

    private void DrawObstacles()
    {
        FoldoutBox(
            "STATIC OBSTACLES",
            ref obstaclesExpanded,
            () =>
            {
                Prop("obstacleSpawnMode");

                if (EnumValue("obstacleSpawnMode") == (int)ObstacleSpawnMode.Random)
                    Prop("randomObstacleCount");
                else
                    Prop("levelObstacles", true);
            }
        );
    }

    private void DrawDangerBalance()
    {
        FoldoutBox(
            "DANGER BALANCE PROFILE",
            ref balanceExpanded,
            () =>
            {
                Prop("dangerBalanceProfile");

                SerializedProperty profileProperty =
                    serializedObject.FindProperty("dangerBalanceProfile");

                DangerBalanceProfile profile =
                    profileProperty != null
                        ? profileProperty.objectReferenceValue as DangerBalanceProfile
                        : null;

                EditorGUILayout.BeginHorizontal();

                using (new EditorGUI.DisabledScope(profile == null))
                {
                    if (GUILayout.Button("SELECT PROFILE"))
                    {
                        Selection.activeObject = profile;
                        EditorGUIUtility.PingObject(profile);
                    }
                }

                if (GUILayout.Button("CREATE BALANCED PROFILE"))
                    CreateBalancedProfile(profileProperty);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);
                Help(
                    "Counts, enemy spawn intervals and boss trigger timing stay in this LevelConfig. Movement, attacks, reaction windows, buffs and trap intensity come from the selected D1–D5 tiers."
                );
            }
        );
    }

    private void DrawEnemies()
    {
        FoldoutBox(
            "ENEMIES",
            ref enemiesExpanded,
            () =>
            {
                DrawNormalEnemy();
                DrawProjectileEnemy();
                DrawHunterEnemy();
                DrawBoss();
                DrawBeacon();
            }
        );
    }

    private void DrawNormalEnemy()
    {
        NestedFoldout(
            "NORMAL ENEMY",
            ref normalEnemyExpanded,
            () =>
            {
                Prop("normalEnemyCount");

                if (IntValue("normalEnemyCount") <= 0)
                    return;

                Prop("normalEnemySpawnInterval");
                DrawDangerSelection(
                    "normalEnemyDanger",
                    "normalEnemyCustomOverride",
                    "normalEnemyOverride"
                );

                DrawNormalPreview();
            }
        );
    }

    private void DrawProjectileEnemy()
    {
        NestedFoldout(
            "PROJECTILE ENEMY",
            ref projectileEnemyExpanded,
            () =>
            {
                Prop("projectileEnemyCount");

                if (IntValue("projectileEnemyCount") <= 0)
                    return;

                Prop("projectileEnemySpawnInterval");
                DrawDangerSelection(
                    "projectileEnemyDanger",
                    "projectileEnemyCustomOverride",
                    "projectileEnemyOverride"
                );

                DrawProjectilePreview();
            }
        );
    }

    private void DrawHunterEnemy()
    {
        NestedFoldout(
            "HUNTER ENEMY",
            ref hunterEnemyExpanded,
            () =>
            {
                Prop("hunterEnemyCount");

                if (IntValue("hunterEnemyCount") <= 0)
                    return;

                Prop("hunterEnemySpawnInterval");
                DrawDangerSelection(
                    "hunterEnemyDanger",
                    "hunterEnemyCustomOverride",
                    "hunterEnemyOverride"
                );

                DrawHunterPreview();
            }
        );
    }

    private void DrawBoss()
    {
        NestedFoldout(
            "BOSS",
            ref bossExpanded,
            () =>
            {
                Prop("bossEnabled");

                if (!BoolValue("bossEnabled"))
                    return;

                switch (SelectedWinCondition)
                {
                    case WinConditionType.ReachScore:
                        ForceBossCondition(BossSpawnCondition.Score);
                        Prop("bossSpawnScore");
                        break;

                    case WinConditionType.SurviveTime:
                        ForceBossCondition(BossSpawnCondition.Time);
                        Prop("bossSpawnTime");
                        break;

                    case WinConditionType.ReachScoreWithinTime:
                        Prop("bossSpawnCondition");

                        if ((BossSpawnCondition)EnumValue("bossSpawnCondition") ==
                            BossSpawnCondition.Score)
                        {
                            Prop("bossSpawnScore");
                        }
                        else
                        {
                            Prop("bossSpawnTime");
                        }
                        break;
                }

                DrawDangerSelection(
                    "bossDanger",
                    "bossCustomOverride",
                    "bossOverride"
                );

                DrawBossPreview();
            }
        );
    }

    private void DrawBeacon()
    {
        NestedFoldout(
            "BEACON ENEMY",
            ref beaconExpanded,
            () =>
            {
                Prop("beaconEnemyCount");

                if (IntValue("beaconEnemyCount") <= 0)
                    return;

                Prop("beaconMinSpawnTime");
                Prop("beaconMaxSpawnTime");
                ValidateMinMax("beaconMinSpawnTime", "beaconMaxSpawnTime", "Beacon Spawn Time");

                DrawDangerSelection(
                    "beaconEnemyDanger",
                    "beaconEnemyCustomOverride",
                    "beaconEnemyOverride"
                );

                DrawBeaconPreview();
            }
        );
    }

    private void DrawPowerUps()
    {
        FoldoutBox(
            "POWER UPS",
            ref powerUpsExpanded,
            () =>
            {
                Prop("armorEnabled");

                if (BoolValue("armorEnabled"))
                {
                    Prop("armorMinSpawnTime");
                    Prop("armorMaxSpawnTime");
                    ValidateMinMax("armorMinSpawnTime", "armorMaxSpawnTime", "Armor Spawn Time");

                    if (IsAdvanced)
                        Prop("armorImmuneDuration");
                }

                Space();
                Prop("slowEnabled");

                if (BoolValue("slowEnabled"))
                {
                    Prop("slowMinSpawnTime");
                    Prop("slowMaxSpawnTime");
                    ValidateMinMax("slowMinSpawnTime", "slowMaxSpawnTime", "Slow Spawn Time");

                    if (IsAdvanced)
                    {
                        Prop("slowMultiplier");
                        Prop("slowDuration");
                    }
                }
            }
        );
    }

    private void DrawTraps()
    {
        FoldoutBox(
            "TRAPS / LASERS",
            ref trapsExpanded,
            () =>
            {
                DrawTrap(
                    "VERTICAL LASER",
                    "verticalLaserEnabled",
                    "verticalLaserDanger",
                    "verticalLaserCustomOverride",
                    "verticalLaserOverride",
                    DrawVerticalLaserPreview
                );

                Space();

                DrawTrap(
                    "HORIZONTAL LASER",
                    "horizontalLaserEnabled",
                    "horizontalLaserDanger",
                    "horizontalLaserCustomOverride",
                    "horizontalLaserOverride",
                    DrawHorizontalLaserPreview
                );

                Space();

                DrawTrap(
                    "SPACE BOMB",
                    "bombTrapEnabled",
                    "bombDanger",
                    "bombCustomOverride",
                    "bombOverride",
                    DrawBombPreview
                );
            }
        );
    }

    private void DrawTrap(
        string title,
        string enabledProperty,
        string dangerProperty,
        string overrideToggleProperty,
        string overrideProperty,
        Action preview)
    {
        MiniTitle(title);
        Prop(enabledProperty);

        if (!BoolValue(enabledProperty))
            return;

        DrawDangerSelection(
            dangerProperty,
            overrideToggleProperty,
            overrideProperty
        );

        preview?.Invoke();
    }

    private void DrawDangerSelection(
        string dangerProperty,
        string overrideToggleProperty,
        string overrideProperty)
    {
        Prop(dangerProperty);

        DangerLevel level = DangerValue(dangerProperty);

        EditorGUILayout.HelpBox(
            $"{DangerLevelUtility.GetDisplayName(level)}\n" +
            DangerLevelUtility.GetDescription(level),
            MessageType.None
        );

        bool overrideEnabled = BoolValue(overrideToggleProperty);

        if (IsAdvanced)
        {
            Prop(overrideToggleProperty);

            if (BoolValue(overrideToggleProperty))
            {
                EditorGUILayout.HelpBox(
                    "Custom override disconnects this threat from the shared profile for this level only.",
                    MessageType.Warning
                );

                Prop(overrideProperty, true);
            }
        }
        else if (overrideEnabled)
        {
            Warning("CUSTOM OVERRIDE is active. Switch to Advanced mode to edit it.");
        }
    }

    private void DrawNormalPreview()
    {
        LevelConfig config = GetSingleConfig();
        if (config == null) return;

        NormalEnemyDangerSettings settings = config.ResolveNormalEnemyDanger();

        DrawResolvedBox(
            "RESOLVED NORMAL ENEMY",
            new[]
            {
                $"Start Speed: {settings.minStartSpeed:0.##} – {settings.maxStartSpeed:0.##}",
                $"Max Speed: {settings.maxSpeed:0.##}",
                $"Acceleration: {settings.speedIncreaseRate:0.###}/s",
                $"Prediction: {(settings.predictionEnabled ? "ON" : "OFF")}",
                $"Separation: {(settings.separationEnabled ? "ON" : "OFF")}"
            }
        );
    }

    private void DrawProjectilePreview()
    {
        LevelConfig config = GetSingleConfig();
        if (config == null) return;

        ProjectileEnemyDangerSettings settings = config.ResolveProjectileEnemyDanger();

        DrawResolvedBox(
            "RESOLVED PROJECTILE ENEMY",
            new[]
            {
                $"Move Speed: {settings.moveSpeed:0.##}",
                $"Fire Interval: {settings.fireRate:0.##}s",
                $"Projectile Speed: {settings.projectileSpeed:0.##}",
                $"Combat Range: {settings.retreatDistance:0.##} – {settings.stoppingDistance:0.##}",
                $"Predictive Aim: {(settings.predictiveAimEnabled ? "ON" : "OFF")}"
            }
        );
    }

    private void DrawHunterPreview()
    {
        LevelConfig config = GetSingleConfig();
        if (config == null) return;

        HunterEnemyDangerSettings settings = config.ResolveHunterEnemyDanger();

        DrawResolvedBox(
            "RESOLVED HUNTER",
            new[]
            {
                $"Reposition: {settings.repositionTime:0.##}s",
                $"Warning: {settings.warningDuration:0.##}s",
                $"Charge Speed: {settings.chargeSpeed:0.##}",
                $"Max Charge: {settings.maxChargeTime:0.##}s",
                $"Stun: {settings.stunDuration:0.##}s"
            }
        );
    }

    private void DrawBossPreview()
    {
        LevelConfig config = GetSingleConfig();
        if (config == null) return;

        BossDangerSettings settings = config.ResolveBossDanger();

        DrawResolvedBox(
            "RESOLVED BOSS",
            new[]
            {
                $"Move Speed: {settings.speed:0.##}",
                $"Direction Smoothness: {settings.directionSmoothness:0.##}",
                $"Can Split: {(settings.canSplit ? "YES" : "NO")}",
                $"Split Delay: {settings.splitDelay:0.##}s",
                $"Mini Boss Speed: {settings.miniBossSpeed:0.##}"
            }
        );
    }

    private void DrawBeaconPreview()
    {
        LevelConfig config = GetSingleConfig();
        if (config == null) return;

        BeaconEnemyDangerSettings settings = config.ResolveBeaconEnemyDanger();

        DrawResolvedBox(
            "RESOLVED BEACON",
            new[]
            {
                $"Activation Delay: {settings.activationDelay:0.##}s",
                $"Buff Duration: {settings.buffDuration:0.##}s",
                $"Normal Speed Buff: ×{settings.normalSpeedMultiplier:0.##}",
                $"Projectile Fire Buff: ×{settings.projectileFireMultiplier:0.##}",
                $"Hunter Warning Multiplier: ×{settings.hunterWarningMultiplier:0.##}"
            }
        );
    }

    private void DrawVerticalLaserPreview()
    {
        LevelConfig config = GetSingleConfig();
        if (config == null) return;
        DrawLaserPreview("RESOLVED VERTICAL LASER", config.ResolveVerticalLaserDanger());
    }

    private void DrawHorizontalLaserPreview()
    {
        LevelConfig config = GetSingleConfig();
        if (config == null) return;
        DrawLaserPreview("RESOLVED HORIZONTAL LASER", config.ResolveHorizontalLaserDanger());
    }

    private void DrawLaserPreview(string title, LaserDangerSettings settings)
    {
        DrawResolvedBox(
            title,
            new[]
            {
                $"Spawn Window: {settings.minSpawnTime:0.##} – {settings.maxSpawnTime:0.##}s",
                $"Warning: {settings.warningDuration:0.##}s",
                $"Lifetime: {settings.lifeTime:0.##}s",
                $"Width: {settings.width:0.##}",
                $"Size Extra: {settings.sizeExtra:0.##}"
            }
        );
    }

    private void DrawBombPreview()
    {
        LevelConfig config = GetSingleConfig();
        if (config == null) return;

        BombDangerSettings settings = config.ResolveBombDanger();

        DrawResolvedBox(
            "RESOLVED SPACE BOMB",
            new[]
            {
                $"Spawn Window: {settings.minSpawnTime:0.##} – {settings.maxSpawnTime:0.##}s",
                $"Maximum Active Bombs: {settings.maxBombCount}",
                $"Spawn Safety: {settings.spawnSafeTime:0.##}s"
            }
        );
    }

    private void DrawResolvedBox(string title, IEnumerable<string> rows)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);

        foreach (string row in rows)
            EditorGUILayout.LabelField(row, EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void DrawCoin(
        string title,
        string enabledProperty,
        string chanceProperty,
        string valueProperty)
    {
        MiniTitle(title);
        Prop(enabledProperty);

        if (!BoolValue(enabledProperty))
            return;

        if (IsAdvanced)
        {
            Prop(chanceProperty);
            Prop(valueProperty);
        }
    }

    private void CreateBalancedProfile(SerializedProperty profileProperty)
    {
        string defaultFolder = "Assets/Balance";

        if (!AssetDatabase.IsValidFolder(defaultFolder))
            AssetDatabase.CreateFolder("Assets", "Balance");

        string path = AssetDatabase.GenerateUniqueAssetPath(
            defaultFolder + "/Default_Danger_Balance.asset"
        );

        DangerBalanceProfile profile =
            ScriptableObject.CreateInstance<DangerBalanceProfile>();

        profile.ResetToBalancedDefaults();
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();

        if (profileProperty != null)
            profileProperty.objectReferenceValue = profile;

        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);
    }

    private void ForceBossCondition(BossSpawnCondition condition)
    {
        SerializedProperty property =
            serializedObject.FindProperty("bossSpawnCondition");

        if (property == null || property.hasMultipleDifferentValues)
            return;

        property.enumValueIndex = (int)condition;
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

    private static float GetNumericValue(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.intValue;
            case SerializedPropertyType.Float:
                return property.floatValue;
            default:
                return 0f;
        }
    }

    private LevelConfig GetSingleConfig()
    {
        return serializedObject.isEditingMultipleObjects
            ? null
            : target as LevelConfig;
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

    private static bool HasAnyCustomOverride(LevelConfig config)
    {
        return config.normalEnemyCustomOverride ||
               config.projectileEnemyCustomOverride ||
               config.hunterEnemyCustomOverride ||
               config.bossCustomOverride ||
               config.beaconEnemyCustomOverride ||
               config.verticalLaserCustomOverride ||
               config.horizontalLaserCustomOverride ||
               config.bombCustomOverride;
    }

    private static string GetWinConditionSummary(LevelConfig config)
    {
        switch (config.winCondition)
        {
            case WinConditionType.ReachScore:
                return $"Reach {config.SafeWinScore} Score";
            case WinConditionType.SurviveTime:
                return $"Survive {config.SafeTimeLimit:0.##} Seconds";
            case WinConditionType.ReachScoreWithinTime:
                return $"Reach {config.SafeWinScore} in {config.SafeTimeLimit:0.##} Seconds";
            default:
                return "Unknown";
        }
    }

    private static string GetBossSummary(LevelConfig config)
    {
        if (!config.bossEnabled)
            return "Disabled";

        string trigger = config.EffectiveBossSpawnCondition == BossSpawnCondition.Score
            ? $"at {config.SafeBossSpawnScore} score"
            : $"after {config.SafeBossSpawnTime:0.##}s";

        return $"{trigger} • {DangerLevelUtility.GetShortLabel(config.bossDanger)}";
    }

    private static string GetEnemySummary(LevelConfig config)
    {
        List<string> values = new List<string>();

        if (config.normalEnemyCount > 0)
            values.Add($"{config.normalEnemyCount} Normal {DangerLevelUtility.GetShortLabel(config.normalEnemyDanger)}");
        if (config.projectileEnemyCount > 0)
            values.Add($"{config.projectileEnemyCount} Projectile {DangerLevelUtility.GetShortLabel(config.projectileEnemyDanger)}");
        if (config.hunterEnemyCount > 0)
            values.Add($"{config.hunterEnemyCount} Hunter {DangerLevelUtility.GetShortLabel(config.hunterEnemyDanger)}");
        if (config.beaconEnemyCount > 0)
            values.Add($"{config.beaconEnemyCount} Beacon {DangerLevelUtility.GetShortLabel(config.beaconEnemyDanger)}");

        return values.Count > 0
            ? string.Join(", ", values)
            : "None";
    }

    private static string GetHazardSummary(LevelConfig config)
    {
        List<string> values = new List<string>();

        if (config.verticalLaserEnabled)
            values.Add($"Vertical {DangerLevelUtility.GetShortLabel(config.verticalLaserDanger)}");
        if (config.horizontalLaserEnabled)
            values.Add($"Horizontal {DangerLevelUtility.GetShortLabel(config.horizontalLaserDanger)}");
        if (config.bombTrapEnabled)
            values.Add($"Bomb {DangerLevelUtility.GetShortLabel(config.bombDanger)}");

        return values.Count > 0
            ? string.Join(", ", values)
            : "None";
    }

    private void FoldoutBox(
        string title,
        ref bool expanded,
        Action content)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        expanded = EditorGUILayout.Foldout(
            expanded,
            title,
            true,
            EditorStyles.foldoutHeader
        );

        if (expanded)
        {
            EditorGUILayout.Space(3);
            content?.Invoke();
        }

        EditorGUILayout.EndVertical();
    }

    private void NestedFoldout(
        string title,
        ref bool expanded,
        Action content)
    {
        EditorGUILayout.Space(4);

        expanded = EditorGUILayout.Foldout(
            expanded,
            title,
            true
        );

        if (!expanded)
            return;

        EditorGUI.indentLevel++;
        content?.Invoke();
        EditorGUI.indentLevel--;
    }

    private static void SummaryRow(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(110f));
        EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
    }

    private static void MiniTitle(string title)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
    }

    private static void Help(string text)
    {
        EditorGUILayout.HelpBox(text, MessageType.Info);
    }

    private static void Warning(string text)
    {
        EditorGUILayout.HelpBox(text, MessageType.Warning);
    }

    private static void Space()
    {
        EditorGUILayout.Space(6);
    }

    private void Prop(string name, bool includeChildren = false)
    {
        SerializedProperty property =
            serializedObject.FindProperty(name);

        if (property == null)
        {
            EditorGUILayout.HelpBox(
                "Missing serialized property: " + name,
                MessageType.Error
            );

            return;
        }

        EditorGUILayout.PropertyField(property, includeChildren);
    }

    private bool BoolValue(string name)
    {
        SerializedProperty property =
            serializedObject.FindProperty(name);

        return property != null && property.boolValue;
    }

    private int IntValue(string name)
    {
        SerializedProperty property =
            serializedObject.FindProperty(name);

        return property != null ? property.intValue : 0;
    }


    private DangerLevel DangerValue(string name)
    {
        SerializedProperty property =
            serializedObject.FindProperty(name);

        if (property == null)
            return DangerLevel.Danger2;

        return DangerLevelUtility.Sanitize(
            (DangerLevel)property.intValue
        );
    }

    private int EnumValue(string name)
    {
        SerializedProperty property =
            serializedObject.FindProperty(name);

        return property != null ? property.enumValueIndex : 0;
    }
}
