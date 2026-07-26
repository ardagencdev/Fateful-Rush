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

    private EditorViewMode viewMode = EditorViewMode.Basic;

    private bool coreExpanded = true;
    private bool briefingExpanded = true;
    private bool musicExpanded = true;
    private bool playerExpanded = true;
    private bool abilitiesExpanded;
    private bool hudExpanded;
    private bool comboExpanded;
    private bool backgroundExpanded;
    private bool coinsExpanded = true;
    private bool obstaclesExpanded = true;
    private bool enemiesExpanded = true;
    private bool powerUpsExpanded;
    private bool trapsExpanded;

    private bool normalEnemyExpanded = true;
    private bool projectileEnemyExpanded = true;
    private bool hunterEnemyExpanded = true;
    private bool bossExpanded = true;
    private bool beaconExpanded = true;

    private bool IsAdvanced =>
        viewMode == EditorViewMode.Advanced;

    private WinConditionType SelectedWinCondition =>
        (WinConditionType)Enum("winCondition");

    private bool UsesScore =>
        SelectedWinCondition ==
            WinConditionType.ReachScore ||
        SelectedWinCondition ==
            WinConditionType.ReachScoreWithinTime;

    private bool UsesTime =>
        SelectedWinCondition ==
            WinConditionType.SurviveTime ||
        SelectedWinCondition ==
            WinConditionType.ReachScoreWithinTime;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader("VOID RUSH LEVEL CONFIG");
        DrawViewModeToolbar();
        DrawSummary();
        DrawGlobalWarnings();

        DrawCore();
        DrawMissionBriefing();
        DrawMusic();
        DrawPlayer();
        DrawAbilities();
        DrawComboSection();
        DrawBackground();
        DrawCoinsSection();

        DrawObstacles();
        DrawEnemies();
        DrawPowerUps();
        DrawTraps();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawViewModeToolbar()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(
            "EDITOR VIEW",
            EditorStyles.boldLabel
        );

        viewMode = (EditorViewMode)GUILayout.Toolbar(
            (int)viewMode,
            new[]
            {
                "BASIC",
                "ADVANCED"
            }
        );

        EditorGUILayout.Space(3);

        EditorGUILayout.HelpBox(
            IsAdvanced
                ? "Advanced mod: bütün teknik gameplay ayarları gösterilir."
                : "Basic mod: level tasarımında en sık kullanılan ayarlar gösterilir.",
            MessageType.Info
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawSummary()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(
            "LEVEL SUMMARY",
            EditorStyles.boldLabel
        );

        if (serializedObject.isEditingMultipleObjects)
        {
            EditorGUILayout.HelpBox(
                "Birden fazla LevelConfig seçili. " +
                "Detaylı özet yalnızca tek asset seçildiğinde gösterilir.",
                MessageType.Info
            );

            EditorGUILayout.EndVertical();
            return;
        }

        LevelConfig config = target as LevelConfig;

        if (config == null)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        SummaryRow(
            "Level",
            $"{config.levelNumber} - {config.levelName}"
        );

        SummaryRow(
            "Win Condition",
            GetWinConditionSummary(config)
         );

        SummaryRow(
            "Difficulty",
            $"{config.SafeMissionDifficulty}/5"
        );

        SummaryRow(
            "Briefing Pages",
            GetBriefingPageCount(config).ToString()
        );

        SummaryRow(
            "Enemies",
            GetEnemySummary(config)
        );

        SummaryRow(
            "Obstacles",
            GetObstacleSummary(config)
        );

        SummaryRow(
            "Power Ups",
            GetPowerUpSummary(config)
        );

        SummaryRow(
            "Hazards",
            GetHazardSummary(config)
        );

        SummaryRow(
            "Boss",
            GetBossSummary(config)
        );

        SummaryRow(
    "Gameplay Music",
    config.gameplayMusic != null
        ? config.gameplayMusic.name
        : "Not Assigned"
        );

        EditorGUILayout.EndVertical();
    }

    private string GetWinConditionSummary(
    LevelConfig config)
    {
        switch (config.winCondition)
        {
            case WinConditionType.ReachScore:
                return $"Reach {config.winScore} Score";

            case WinConditionType.SurviveTime:
                return $"Survive {config.timeLimit:0.##} Seconds";

            case WinConditionType.ReachScoreWithinTime:
                return
                    $"Reach {config.winScore} Score " +
                    $"in {config.timeLimit:0.##} Seconds";

            default:
                return "Unknown";
        }
    }

    private string GetBossSummary(LevelConfig config)
    {
        if (!config.bossEnabled)
            return "Disabled";

        switch (config.EffectiveBossSpawnCondition)
        {
            case BossSpawnCondition.Score:
                return
                    $"Enabled at {config.bossSpawnScore} Score";

            case BossSpawnCondition.Time:
                return
                    $"Enabled after {config.bossSpawnTime:0.##} Seconds";

            default:
                return "Enabled";
        }
    }

    private string GetEnemySummary(LevelConfig config)
    {
        List<string> enemies = new List<string>();

        if (config.normalEnemyCount > 0)
        {
            enemies.Add(
                $"{config.normalEnemyCount} Normal"
            );
        }

        if (config.projectileEnemyCount > 0)
        {
            enemies.Add(
                $"{config.projectileEnemyCount} Projectile"
            );
        }

        if (config.hunterEnemyCount > 0)
        {
            enemies.Add(
                $"{config.hunterEnemyCount} Hunter"
            );
        }

        if (config.beaconEnemyCount > 0)
        {
            enemies.Add(
                $"{config.beaconEnemyCount} Beacon"
            );
        }

        return enemies.Count > 0
            ? string.Join(", ", enemies)
            : "None";
    }

    private string GetObstacleSummary(LevelConfig config)
    {
        if (config.obstacleSpawnMode ==
            ObstacleSpawnMode.Random)
        {
            return $"{config.randomObstacleCount} Random";
        }

        if (config.levelObstacles == null ||
            config.levelObstacles.Length == 0)
        {
            return "None";
        }

        int enabledCount = 0;

        foreach (LevelObstacleOption option
                 in config.levelObstacles)
        {
            if (option != null &&
                option.enabled &&
                option.prefab != null)
            {
                enabledCount++;
            }
        }

        return enabledCount > 0
            ? $"{enabledCount} Fixed"
            : "None";
    }

    private string GetPowerUpSummary(LevelConfig config)
    {
        List<string> powerUps = new List<string>();

        if (config.armorEnabled)
            powerUps.Add("Armor");

        if (config.slowEnabled)
            powerUps.Add("Slow");

        return powerUps.Count > 0
            ? string.Join(", ", powerUps)
            : "None";
    }

    private string GetHazardSummary(LevelConfig config)
    {
        List<string> hazards = new List<string>();

        if (config.verticalLaserEnabled)
            hazards.Add("Vertical Laser");

        if (config.horizontalLaserEnabled)
            hazards.Add("Horizontal Laser");

        if (config.bombTrapEnabled)
            hazards.Add("Bombs");

        return hazards.Count > 0
            ? string.Join(", ", hazards)
            : "None";
    }

    private void DrawGlobalWarnings()
    {
        if (serializedObject.isEditingMultipleObjects)
            return;

        LevelConfig config = target as LevelConfig;

        if (config == null)
            return;

        if (config.UsesScore &&
            !config.normalCoinEnabled &&
            !config.goldCoinEnabled &&
            !config.rareCoinEnabled)
        {
            Warning(
                "This win condition requires score, but every coin type is disabled. " +
                "The player cannot complete the level."
            );
        }

        if (config.UsesScore &&
            config.maxCoinCount <= 0)
        {
            Warning(
                "This win condition requires score, but Max Coin Count is 0. " +
                "No coins can spawn, so the level cannot be completed."
            );
        }

        if (config.bossEnabled &&
            config.EffectiveBossSpawnCondition ==
                BossSpawnCondition.Score &&
            config.bossSpawnScore >= config.winScore)
        {
            Warning(
                "Boss Spawn Score is equal to or greater than Win Score. " +
                "The level may end before the boss becomes relevant."
            );
        }

        if (config.bossEnabled &&
            config.EffectiveBossSpawnCondition ==
                BossSpawnCondition.Time)
        {
            if (config.bossSpawnTime <= 0f)
            {
                Warning(
                    "Boss Spawn Time is 0. The boss will appear immediately when gameplay starts."
                );
            }
            else if (config.bossSpawnTime >= config.timeLimit)
            {
                Warning(
                    "Boss Spawn Time is equal to or greater than the Time Limit. " +
                    "The boss would spawn after the level has already ended."
                );
            }
        }

        if (config.UsesTime &&
            config.timeLimit <= 0f)
        {
            Warning(
                "This win condition requires a timer, but Time Limit is 0 or lower."
            );
        }

        if (config.normalEnemyCount <= 0 &&
            config.projectileEnemyCount <= 0 &&
            config.hunterEnemyCount <= 0 &&
            config.beaconEnemyCount <= 0 &&
            !config.bossEnabled &&
            !config.verticalLaserEnabled &&
            !config.horizontalLaserEnabled &&
            !config.bombTrapEnabled)
        {
            Warning(
                "This level contains no enemies or hazards."
            );
        }

        float enabledCoinChance = 0f;

        if (config.normalCoinEnabled)
            enabledCoinChance += config.normalCoinChance;

        if (config.goldCoinEnabled)
            enabledCoinChance += config.goldCoinChance;

        if (config.rareCoinEnabled)
            enabledCoinChance += config.rareCoinChance;

        if (config.UsesScore &&
            enabledCoinChance > 0f &&
            !Mathf.Approximately(enabledCoinChance, 100f))
        {
            EditorGUILayout.HelpBox(
                $"The enabled coin chances total {enabledCoinChance:0.##}%. " +
                "A total of 100% is recommended.",
                MessageType.Info
            );
        }
    }

    private void DrawCore()
    {
        FoldoutBox(
            "LEVEL / WIN",
            ref coreExpanded,
            () =>
            {
                Prop("levelNumber");
                Prop("levelName");

                Space();

                Prop("winCondition");

                switch ((WinConditionType)Enum("winCondition"))
                {
                    case WinConditionType.ReachScore:
                        Prop("winScore");

                        Help(
                            "Oyuncu belirlenen skora ulaştığında level kazanılır."
                        );
                        break;

                    case WinConditionType.SurviveTime:
                        Prop("timeLimit");

                        Help(
                            "Oyuncu süre dolana kadar hayatta kalırsa level kazanılır."
                        );
                        break;

                    case WinConditionType.ReachScoreWithinTime:
                        Prop("winScore");
                        Prop("timeLimit");

                        Help(
                            "Oyuncu süre dolmadan belirlenen skora ulaşmalıdır. " +
                            "Süre dolarsa level kaybedilir."
                        );
                        break;
                }
            }
        );
    }

    private int GetBriefingPageCount(LevelConfig config)
    {
        int pageCount = 1;

        if (config.briefingPages == null)
            return pageCount;

        foreach (string page in config.briefingPages)
        {
            if (!string.IsNullOrWhiteSpace(page))
                pageCount++;
        }

        return pageCount;
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

                Help(
                    "Difficulty uses whole stars only: 0/5, 1/5, 2/5, " +
                    "3/5, 4/5 or 5/5."
                );

                Space();

                Prop("briefingModeDescription");
                Prop("briefingObjectiveDescription");
                Prop("briefingPages", true);

                Help(
                    "The first page is generated automatically from the mission objective. " +
                    "Briefing Pages are added after it in the order shown here."
                );
            }
        );
    }

    private void DrawMusic()
    {
        FoldoutBox(
            "MUSIC",
            ref musicExpanded,
            () =>
            {
                Prop("gameplayMusic");

                Help(
                    "Bu müzik level başladığında çalar. " +
                    "Her LevelConfig için farklı bir AudioClip seçebilirsin."
                );
            }
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

                if (Bool("dashEnabled"))
                {
                    MiniTitle("Dash");

                    if (IsAdvanced)
                    {
                        Prop("dashDistance");
                        Prop("dashDuration");
                    }

                    Prop("dashCooldown");
                }

                Space();

                Prop("cloneEnabled");

                if (Bool("cloneEnabled"))
                {
                    MiniTitle("Void Clone");

                    Prop("cloneDuration");
                    Prop("cloneCooldown");
                    Prop("cloneUses");
                }
            }
        );
    }

    private void DrawComboSection()
    {
        if (UsesScore)
        {
            DrawCombo();
            return;
        }

        FoldoutBox(
            "UI / COMBO",
            ref comboExpanded,
            () =>
            {
                EditorGUILayout.HelpBox(
                    "Combo settings are unavailable for Survive Time. " +
                    "This objective does not use coins or score progression. " +
                    "Select a score-based win condition to configure the combo system.",
                    MessageType.Info
                );
            }
        );
    }

    private void DrawCombo()
    {
        FoldoutBox(
            "UI / COMBO",
            ref comboExpanded,
            () =>
            {
                Prop("comboEnabled");

                if (!Bool("comboEnabled"))
                    return;

                Prop("comboTimeLimit");
                Prop("maxCombo");

                if (!IsAdvanced)
                    return;

                Prop("playerComboSpeedBonus");
                Prop("comboSpeedStages", true);

                Help(
                    "Player Combo Speed, Combo Speed Stages boşsa kullanılan " +
                    "eski sabit hız bonusudur."
                );

                Help(
                    "Her stage: combo kaç X olacak, kaç coin chain ile açılacak, " +
                    "player speed kaçla çarpılacak."
                );
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

                if (Bool("randomizeNearStarsColor"))
                {
                    Help(
                        "Level her açıldığında Near Stars rengi rastgele seçilir."
                    );
                }
                else
                {
                    Prop("nearStarsColor");

                    if (IsAdvanced)
                    {
                        Help(
                            "Random kapalıysa her zaman bu renk kullanılır."
                        );
                    }
                }

                Space();

                Prop("nearStarsSpeedMultiplier");
                Prop("nearStarsSizeMultiplier");

                if (IsAdvanced)
                    Prop("nearStarsEmissionRate");
            }
        );
    }

    private void DrawCoinsSection()
    {
        if (UsesScore)
        {
            DrawCoins();
            return;
        }

        FoldoutBox(
            "COINS",
            ref coinsExpanded,
            () =>
            {
                EditorGUILayout.HelpBox(
                    "Coin settings are unavailable for Survive Time. " +
                    "The player only needs to remain alive until the countdown ends. " +
                    "Select a score-based win condition to configure coin spawning and values.",
                    MessageType.Info
                );
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
                Prop("coinSpawnInterval");
                Prop("maxCoinCount");

                Space();

                DrawCoin(
                    "Normal Coin",
                    "normalCoinEnabled",
                    "normalCoinChance",
                    "normalCoinValue"
                );

                DrawCoin(
                    "Gold Coin",
                    "goldCoinEnabled",
                    "goldCoinChance",
                    "goldCoinValue"
                );

                DrawCoin(
                    "Rare Coin",
                    "rareCoinEnabled",
                    "rareCoinChance",
                    "rareCoinValue"
                );
            }
        );
    }

    private void DrawObstacles()
    {
        FoldoutBox(
            "OBSTACLES",
            ref obstaclesExpanded,
            () =>
            {
                Prop("obstacleSpawnMode");

                if (Enum("obstacleSpawnMode") == 1)
                {
                    Prop("randomObstacleCount");

                    if (IsAdvanced)
                    {
                        Help(
                            "Random modda aynı obstacle birden fazla seçilmez. " +
                            "Liste içindeki prefablar arasından seçim yapılır."
                        );
                    }
                }

                Prop("levelObstacles", true);
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

                if (Int("normalEnemyCount") <= 0)
                    return;

                Prop("normalEnemySpawnInterval");

                if (!IsAdvanced)
                    return;

                Prop("normalMinStartSpeed");
                Prop("normalMaxStartSpeed");

                ValidateMinMax(
                    "normalMinStartSpeed",
                    "normalMaxStartSpeed",
                    "Normal Enemy Start Speed"
                );

                Prop("normalMaxSpeed");
                Prop("normalSpeedIncreaseRate");

                Space();

                MiniTitle("Prediction");

                Prop("normalPredictionEnabled");

                if (Bool("normalPredictionEnabled"))
                {
                    Prop("normalPredictionDistanceThreshold");
                    Prop("normalPredictionTime");
                    Prop("normalMaxPredictionDistance");
                }

                Space();

                MiniTitle("Separation");

                Prop("normalSeparationEnabled");

                if (Bool("normalSeparationEnabled"))
                {
                    Prop("normalSeparationRadius");
                    Prop("normalSeparationStrength");
                }
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

                if (Int("projectileEnemyCount") <= 0)
                    return;

                Prop("projectileEnemySpawnInterval");

                if (!IsAdvanced)
                    return;

                Prop("projectileMoveSpeed");
                Prop("projectileStoppingDistance");
                Prop("projectileRetreatDistance");
                Prop("projectileFireRate");
                Prop("projectileSpeed");
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

                if (Int("hunterEnemyCount") <= 0)
                    return;

                Prop("hunterEnemySpawnInterval");

                if (!IsAdvanced)
                    return;

                Prop("hunterRepositionTime");
                Prop("hunterWarningDuration");
                Prop("hunterChargeSpeed");
                Prop("hunterStunDuration");
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

                if (!Bool("bossEnabled"))
                    return;

                switch (SelectedWinCondition)
                {
                    case WinConditionType.ReachScore:
                        SetBossSpawnCondition(
                            BossSpawnCondition.Score
                        );

                        Prop("bossSpawnScore");

                        Help(
                            "This level uses Reach Score, so the boss automatically spawns " +
                            "when the player's score reaches this value."
                        );
                        break;

                    case WinConditionType.SurviveTime:
                        SetBossSpawnCondition(
                            BossSpawnCondition.Time
                        );

                        Prop("bossSpawnTime");

                        Help(
    "This level uses Survive Time. Boss Spawn Time defines how many seconds " +
    "after gameplay begins the boss will appear. " +
    "Example: with a value of 15, the boss spawns 15 seconds after the level starts."
);
                        break;

                    case WinConditionType.ReachScoreWithinTime:
                        Prop("bossSpawnCondition");

                        switch ((BossSpawnCondition)
                            Enum("bossSpawnCondition"))
                        {
                            case BossSpawnCondition.Score:
                                Prop("bossSpawnScore");

                                Help(
                                    "The boss spawns when the player's score reaches this value."
                                );
                                break;

                            case BossSpawnCondition.Time:
                                Prop("bossSpawnTime");

                                Help(
                                    "Boss Spawn Time defines how many seconds after gameplay begins " +
    "the boss will appear."
                                );
                                break;
                        }
                        break;
                }

                DrawBossSpawnValidation();

                if (!IsAdvanced)
                    return;

                Prop("bossSpeed");
                Prop("bossCanSplit");

                if (Bool("bossCanSplit"))
                {
                    Prop("bossSplitDelay");
                    Prop("bossSplitDistance");
                    Prop("miniBossSpeed");
                }
            }
        );
    }

    private void SetBossSpawnCondition(
        BossSpawnCondition condition)
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                "bossSpawnCondition"
            );

        if (property == null ||
            property.hasMultipleDifferentValues)
        {
            return;
        }

        int targetValue = (int)condition;

        if (property.enumValueIndex != targetValue)
            property.enumValueIndex = targetValue;
    }

    private void DrawBossSpawnValidation()
    {
        BossSpawnCondition condition;

        switch (SelectedWinCondition)
        {
            case WinConditionType.ReachScore:
                condition = BossSpawnCondition.Score;
                break;

            case WinConditionType.SurviveTime:
                condition = BossSpawnCondition.Time;
                break;

            default:
                condition = (BossSpawnCondition)
                    Enum("bossSpawnCondition");
                break;
        }

        if (condition == BossSpawnCondition.Score)
        {
            int spawnScore = Int("bossSpawnScore");
            int targetScore = Int("winScore");

            if (spawnScore >= targetScore)
            {
                Warning(
                    "Boss Spawn Score should be lower than Win Score. " +
                    "Otherwise the level can end before the boss has a meaningful role."
                );
            }

            return;
        }

        SerializedProperty spawnTimeProperty =
            serializedObject.FindProperty("bossSpawnTime");

        SerializedProperty timeLimitProperty =
            serializedObject.FindProperty("timeLimit");

        if (spawnTimeProperty == null ||
            timeLimitProperty == null ||
            spawnTimeProperty.hasMultipleDifferentValues ||
            timeLimitProperty.hasMultipleDifferentValues)
        {
            return;
        }

        float spawnTime =
    spawnTimeProperty.floatValue;
        float timeLimit = timeLimitProperty.floatValue;

        if (spawnTime <= 0f)
        {
            Warning(
                "A value of 0 makes the boss spawn immediately when gameplay starts."
            );
        }
        else if (spawnTime >= timeLimit)
        {
            Warning(
                "Boss Spawn Time must be lower than the Time Limit. " +
"Otherwise the level will finish before the boss can spawn."
            );
        }
    }

    private void DrawBeacon()
    {
        NestedFoldout(
            "BEACON ENEMY",
            ref beaconExpanded,
            () =>
            {
                Prop("beaconEnemyCount");

                if (Int("beaconEnemyCount") <= 0)
                    return;

                Prop("beaconMinSpawnTime");
                Prop("beaconMaxSpawnTime");

                ValidateMinMax(
                    "beaconMinSpawnTime",
                    "beaconMaxSpawnTime",
                    "Beacon Spawn Time"
                );

                if (!IsAdvanced)
                    return;

                MiniTitle("Buff Settings");

                Prop("beaconBuffDuration");
                Prop("beaconBuffSizeMultiplier");
                Prop("beaconNormalSpeedMultiplier");
                Prop("beaconNormalMaxSpeedMultiplier");
                Prop("beaconProjectileMoveMultiplier");
                Prop("beaconProjectileShotMultiplier");
                Prop("beaconProjectileFireMultiplier");
                Prop("beaconHunterRepositionMultiplier");
                Prop("beaconHunterWarningMultiplier");
                Prop("beaconHunterChargeMultiplier");
                Prop("beaconHunterStunMultiplier");
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

                if (Bool("armorEnabled"))
                {
                    MiniTitle("Armor");

                    Prop("armorMinSpawnTime");
                    Prop("armorMaxSpawnTime");

                    ValidateMinMax(
                        "armorMinSpawnTime",
                        "armorMaxSpawnTime",
                        "Armor Spawn Time"
                    );

                    if (IsAdvanced)
                        Prop("armorImmuneDuration");
                }

                Space();

                Prop("slowEnabled");

                if (Bool("slowEnabled"))
                {
                    MiniTitle("Slow");

                    Prop("slowMinSpawnTime");
                    Prop("slowMaxSpawnTime");

                    ValidateMinMax(
                        "slowMinSpawnTime",
                        "slowMaxSpawnTime",
                        "Slow Spawn Time"
                    );

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
                DrawVerticalLaser();
                Space();
                DrawHorizontalLaser();
                Space();
                DrawBombTrap();
            }
        );
    }

    private void DrawVerticalLaser()
    {
        Prop("verticalLaserEnabled");

        if (!Bool("verticalLaserEnabled"))
            return;

        MiniTitle("Vertical Laser");

        Prop("verticalLaserMinSpawnTime");
        Prop("verticalLaserMaxSpawnTime");

        ValidateMinMax(
            "verticalLaserMinSpawnTime",
            "verticalLaserMaxSpawnTime",
            "Vertical Laser Spawn Time"
        );

        if (!IsAdvanced)
            return;

        Prop("verticalLaserWarningDuration");
        Prop("verticalLaserLifeTime");
        Prop("verticalLaserWidth");
        Prop("verticalLaserHeightExtra");
    }

    private void DrawHorizontalLaser()
    {
        Prop("horizontalLaserEnabled");

        if (!Bool("horizontalLaserEnabled"))
            return;

        MiniTitle("Horizontal Laser");

        Prop("horizontalLaserMinSpawnTime");
        Prop("horizontalLaserMaxSpawnTime");

        ValidateMinMax(
            "horizontalLaserMinSpawnTime",
            "horizontalLaserMaxSpawnTime",
            "Horizontal Laser Spawn Time"
        );

        if (!IsAdvanced)
            return;

        Prop("horizontalLaserWarningDuration");
        Prop("horizontalLaserLifeTime");
        Prop("horizontalLaserWidth");
        Prop("horizontalLaserWidthExtra");
    }

    private void DrawBombTrap()
    {
        Prop("bombTrapEnabled");

        if (!Bool("bombTrapEnabled"))
            return;

        MiniTitle("Bomb Trap");

        Prop("bombMinSpawnTime");
        Prop("bombMaxSpawnTime");
        Prop("maxBombCount");

        ValidateMinMax(
            "bombMinSpawnTime",
            "bombMaxSpawnTime",
            "Bomb Spawn Time"
        );
    }

    private void DrawCoin(
        string title,
        string enabledProp,
        string chanceProp,
        string valueProp)
    {
        MiniTitle(title);
        Prop(enabledProp);

        if (!Bool(enabledProp))
            return;

        if (IsAdvanced)
        {
            Prop(chanceProp);
            Prop(valueProp);
        }
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
            maxProperty == null)
        {
            return;
        }

        if (minProperty.hasMultipleDifferentValues ||
            maxProperty.hasMultipleDifferentValues)
        {
            return;
        }

        float minValue =
            GetNumericValue(minProperty);

        float maxValue =
            GetNumericValue(maxProperty);

        if (minValue <= maxValue)
            return;

        EditorGUILayout.HelpBox(
            displayName +
            ": Minimum değer maksimum değerden büyük olamaz.",
            MessageType.Error
        );
    }

    private float GetNumericValue(
        SerializedProperty property)
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

    private void FoldoutBox(
        string title,
        ref bool expanded,
        Action content)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical("box");

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
        EditorGUILayout.Space(3);

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

    private void SummaryRow(
        string label,
        string value)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            label,
            GUILayout.Width(90f)
        );

        EditorGUILayout.LabelField(
            value,
            EditorStyles.boldLabel
        );

        EditorGUILayout.EndHorizontal();
    }

    private void DrawHeader(string title)
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(4);
    }

    private void MiniTitle(string title)
    {
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            title,
            EditorStyles.miniBoldLabel
        );
    }

    private void Help(string text)
    {
        EditorGUILayout.HelpBox(
            text,
            MessageType.Info
        );
    }

    private void Warning(string text)
    {
        EditorGUILayout.HelpBox(
            text,
            MessageType.Warning
        );
    }

    private void Space()
    {
        EditorGUILayout.Space(6);
    }

    private void Prop(
        string name,
        bool includeChildren = false)
    {
        SerializedProperty property =
            serializedObject.FindProperty(name);

        if (property != null)
        {
            EditorGUILayout.PropertyField(
                property,
                includeChildren
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Missing property: " + name,
                MessageType.Warning
            );
        }
    }

    private bool Bool(string name)
    {
        SerializedProperty property =
            serializedObject.FindProperty(name);

        return property != null &&
               property.boolValue;
    }

    private int Int(string name)
    {
        SerializedProperty property =
            serializedObject.FindProperty(name);

        return property != null
            ? property.intValue
            : 0;
    }

    private int Enum(string name)
    {
        SerializedProperty property =
            serializedObject.FindProperty(name);

        return property != null
            ? property.enumValueIndex
            : 0;
    }
}