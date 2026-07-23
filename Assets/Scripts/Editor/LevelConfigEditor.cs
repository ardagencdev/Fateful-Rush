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
    private bool tutorialExpanded;
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

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader("VOID RUSH LEVEL CONFIG");
        DrawViewModeToolbar();
        DrawSummary();
        DrawGlobalWarnings();

        DrawCore();
        DrawTutorial();
        DrawMusic();
        DrawPlayer();
        DrawAbilities();
        DrawHUD();
        DrawCombo();
        DrawBackground();
        DrawCoins();
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
            config.bossEnabled
                ? $"Enabled at {config.bossSpawnScore} Score"
                : "Disabled"
        );

        SummaryRow(
            "Tutorial",
            config.showTutorial
                ? "Enabled"
                : "Disabled"
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

        bool requiresScore =
    config.winCondition ==
        WinConditionType.ReachScore ||
    config.winCondition ==
        WinConditionType.ReachScoreWithinTime;

        if (requiresScore &&
            !config.normalCoinEnabled &&
            !config.goldCoinEnabled &&
            !config.rareCoinEnabled)
        {
            Warning(
                "Bu win condition skor gerektiriyor fakat bütün coin türleri kapalı. " +
                "Oyuncu levelı tamamlayamaz."
            );
        }

        if (requiresScore &&
            config.maxCoinCount <= 0)
        {
            Warning(
                "Bu win condition skor gerektiriyor fakat Max Coin Count 0. " +
                "Coin spawn olmayacağı için level tamamlanamaz."
            );
        }

        if (requiresScore &&
        config.bossEnabled &&
        config.bossSpawnScore >= config.winScore)
        {
            Warning(
                "Boss Spawn Score, Win Score değerine eşit veya daha büyük. " +
                "Oyuncu boss görünmeden levelı bitirebilir."
            );
        }

        bool requiresTimeLimit =
        config.winCondition ==
        WinConditionType.SurviveTime ||
        config.winCondition ==
        WinConditionType.ReachScoreWithinTime;

        if (requiresTimeLimit &&
            config.timeLimit <= 0f)
        {
            Warning(
                "Bu win condition süre gerektiriyor fakat Time Limit 0 veya daha düşük."
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
                "Bu levelda enemy veya hazard bulunmuyor."
            );
        }

        float enabledCoinChance = 0f;

        if (config.normalCoinEnabled)
            enabledCoinChance += config.normalCoinChance;

        if (config.goldCoinEnabled)
            enabledCoinChance += config.goldCoinChance;

        if (config.rareCoinEnabled)
            enabledCoinChance += config.rareCoinChance;

        if (enabledCoinChance > 0f &&
            !Mathf.Approximately(enabledCoinChance, 100f))
        {
            EditorGUILayout.HelpBox(
                $"Aktif coin ihtimallerinin toplamı " +
                $"{enabledCoinChance:0.##}%. " +
                "Toplamın 100 olması tavsiye edilir.",
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

    private void DrawTutorial()
    {
        FoldoutBox(
            "TUTORIAL",
            ref tutorialExpanded,
            () =>
            {
                Prop("showTutorial");

                if (!Bool("showTutorial"))
                    return;

                Prop("tutorialTitle");
                Prop("tutorialPages", true);

                Help(
                    "Tutorial Music yalnızca tutorial panel açıkken çalar. " +
                    "START'a basılınca levelın Gameplay Music parçasına geçilir."
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

    private void DrawHUD()
    {
        FoldoutBox(
            "UI / HUD",
            ref hudExpanded,
            () =>
            {
                Prop("showGameTimerHUD");

                if (IsAdvanced)
                {
                    Help(
                        "Sadece oyun içindeki HUD timer yazısını açar/kapatır. " +
                        "Result paneldeki survived time ve record sistemine dokunmaz."
                    );
                }
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

                Prop("bossSpawnScore");

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