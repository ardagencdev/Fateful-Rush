using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in LevelConfigEditor.cs.
public partial class LevelConfigEditor
{
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
            () => Prop("playerMoveSpeed")
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

                if (BoolValue("cloneEnabled") && IsAdvanced)
                {
                    Prop("cloneDuration");
                    Prop("cloneCooldown");
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

                if (IsAdvanced)
                {
                    Space();
                    MiniTitle("COMBO SPEED SETTINGS");
                    DrawComboSpeedStages();
                }
            }
        );
    }

    private void DrawComboSpeedStages()
    {
        SerializedProperty stages =
            serializedObject.FindProperty("comboSpeedStages");

        if (stages == null || !stages.isArray)
        {
            EditorGUILayout.HelpBox(
                "Missing serialized array: comboSpeedStages",
                MessageType.Error
            );
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            "CUSTOM COMBO SPEED STAGES",
            EditorStyles.miniBoldLabel
        );

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(
            stages.arraySize == 1
                ? "1 stage"
                : $"{stages.arraySize} stages",
            EditorStyles.miniLabel,
            GUILayout.Width(58f)
        );
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            "Each stage defines which combo multiplier appears, how many chained coins unlock it, and how fast the player moves while it is active.",
            EditorStyles.wordWrappedMiniLabel
        );

        EditorGUILayout.Space(4);

        for (int i = 0; i < stages.arraySize; i++)
        {
            SerializedProperty stage = stages.GetArrayElementAtIndex(i);
            SerializedProperty comboMultiplier =
                stage.FindPropertyRelative("comboMultiplier");
            SerializedProperty coinsRequired =
                stage.FindPropertyRelative("coinsRequired");
            SerializedProperty speedMultiplier =
                stage.FindPropertyRelative("playerSpeedMultiplier");

            int comboValue = comboMultiplier != null
                ? comboMultiplier.intValue
                : i + 2;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"STAGE {i + 1}  •  COMBO ×{comboValue}",
                EditorStyles.boldLabel
            );

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(i == 0))
            {
                if (GUILayout.Button("▲", GUILayout.Width(26f)))
                    stages.MoveArrayElement(i, i - 1);
            }

            using (new EditorGUI.DisabledScope(i >= stages.arraySize - 1))
            {
                if (GUILayout.Button("▼", GUILayout.Width(26f)))
                    stages.MoveArrayElement(i, i + 1);
            }

            if (GUILayout.Button("REMOVE", GUILayout.Width(68f)))
            {
                stages.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (comboMultiplier != null)
            {
                EditorGUILayout.PropertyField(
                    comboMultiplier,
                    new GUIContent(
                        "Displayed Combo",
                        "The combo multiplier shown for this stage, such as ×2 or ×3."
                    )
                );
            }

            if (coinsRequired != null)
            {
                EditorGUILayout.PropertyField(
                    coinsRequired,
                    new GUIContent(
                        "Coins Needed",
                        "Number of coins that must be collected within the combo window to unlock this stage."
                    )
                );
            }

            if (speedMultiplier != null)
            {
                EditorGUILayout.PropertyField(
                    speedMultiplier,
                    new GUIContent(
                        "Player Speed Multiplier",
                        "Movement speed multiplier applied while this combo stage is active. 1.25 means 25% faster."
                    )
                );
            }

            if (coinsRequired != null &&
                speedMultiplier != null)
            {
                float percentage =
                    Mathf.Max(0f, speedMultiplier.floatValue - 1f) * 100f;

                EditorGUILayout.LabelField(
                    $"Unlocks after {coinsRequired.intValue} chained coins • Player moves {percentage:0.#}% faster",
                    EditorStyles.wordWrappedMiniLabel
                );
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        if (GUILayout.Button("+ ADD COMBO SPEED STAGE"))
        {
            int index = stages.arraySize;
            stages.InsertArrayElementAtIndex(index);

            SerializedProperty newStage =
                stages.GetArrayElementAtIndex(index);

            SerializedProperty comboMultiplier =
                newStage.FindPropertyRelative("comboMultiplier");
            SerializedProperty coinsRequired =
                newStage.FindPropertyRelative("coinsRequired");
            SerializedProperty speedMultiplier =
                newStage.FindPropertyRelative("playerSpeedMultiplier");

            if (comboMultiplier != null)
                comboMultiplier.intValue = index + 2;

            if (coinsRequired != null)
            {
                coinsRequired.intValue =
                    index < DefaultComboCoins.Length
                        ? DefaultComboCoins[index]
                        : DefaultComboCoins[DefaultComboCoins.Length - 1] +
                          (index - DefaultComboCoins.Length + 1) * 4;
            }

            if (speedMultiplier != null)
            {
                speedMultiplier.floatValue =
                    index < DefaultComboSpeedMultipliers.Length
                        ? DefaultComboSpeedMultipliers[index]
                        : DefaultComboSpeedMultipliers[
                            DefaultComboSpeedMultipliers.Length - 1
                          ];
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBackground()
    {
        FoldoutBox(
            "BACKGROUND / STARFIELD",
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
                    Prop("starfieldDensityMultiplier");
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
                Space();

                if (EnumValue("obstacleSpawnMode") ==
                    (int)ObstacleSpawnMode.Random)
                {
                    Prop("randomObstacleCount");

                    Space();

                    EditorGUILayout.LabelField(
                        "RANDOM OBSTACLE POOL",
                        EditorStyles.boldLabel
                    );

                    Prop("levelObstacles", true);

                    Help(
                        "Random mode randomly selects unique prefabs from this pool. " +
                        "Only entries with Enabled turned on and a valid prefab assigned can spawn."
                    );
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "FIXED OBSTACLES",
                        EditorStyles.boldLabel
                    );

                    Prop("levelObstacles", true);

                    Help(
                        "Every enabled prefab in this list spawns once."
                    );
                }
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
                $"Burst: {settings.shotsPerBurst} shots",
                $"Reload: {settings.reloadDuration:0.##}s",
                $"Reload Retreat: {settings.reloadRetreatDistance:0.##}",
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
}
