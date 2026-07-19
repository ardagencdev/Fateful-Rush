using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelConfig))]
public class LevelConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader("VOID RUSH LEVEL CONFIG");

        DrawCore();
        DrawTutorial();
        DrawPlayer();
        DrawUIAndBackground();
        DrawCoins();
        DrawObstacles();
        DrawEnemies();
        DrawBoss();
        DrawBeacon();
        DrawPowerUps();
        DrawTraps();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCore()
    {
        Box("LEVEL / WIN", () =>
        {
            Prop("levelNumber");
            Prop("levelName");
            Prop("winScore");
        });
    }

    private void DrawTutorial()
    {
        Box("TUTORIAL", () =>
        {
            Prop("showTutorial");

            if (!Bool("showTutorial")) return;

            Prop("tutorialTitle");
            Prop("tutorialPages", true);

            Help("Bu level başlarken tutorial panel açılır. START'a basılana kadar gameplay başlamaz.");
        });
    }

    private void DrawPlayer()
    {
        Box("PLAYER", () =>
        {
            Prop("playerMoveSpeed");
        });

        Box("ABILITIES", () =>
        {
            Prop("dashEnabled");

            if (Bool("dashEnabled"))
            {
                MiniTitle("Dash");
                Prop("dashDistance");
                Prop("dashDuration");
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
        });
    }

    private void DrawUIAndBackground()
    {
        Box("UI / HUD", () =>
        {
            Prop("showGameTimerHUD");
            Help("Sadece oyun içindeki HUD timer yazısını açar/kapatır. Result paneldeki survived time ve record sistemine dokunmaz.");
        });

        Box("UI / COMBO", () =>
        {
            if (Bool("comboEnabled"))
            {
                Prop("comboTimeLimit");
                Prop("maxCombo");
                Prop("playerComboSpeedBonus");
                Prop("comboSpeedStages", true);

                Help("Player Combo Speed, Combo Speed Stages boşsa kullanılan eski sabit hız bonusudur.");
                Help("Her stage: combo kaç X olacak, kaç coin chain ile açılacak, player speed kaçla çarpılacak. Örnek: 3x / 5 coin / 1.5 speed.");
            }
        });

        Box("BACKGROUND / NEAR STARS", () =>
        {
            Prop("nearStarsColor");
            Prop("nearStarsSpeedMultiplier");
            Prop("nearStarsSizeMultiplier");
            Prop("nearStarsEmissionRate");
        });
    }

    private void DrawCoins()
    {
        Box("COINS", () =>
        {
            Prop("coinSpawnInterval");
            Prop("maxCoinCount");

            Space();
            DrawCoin("Normal Coin", "normalCoinEnabled", "normalCoinChance", "normalCoinValue");
            DrawCoin("Gold Coin", "goldCoinEnabled", "goldCoinChance", "goldCoinValue");
            DrawCoin("Rare Coin", "rareCoinEnabled", "rareCoinChance", "rareCoinValue");
        });
    }

    private void DrawObstacles()
    {
        Box("OBSTACLES", () =>
        {
            Prop("obstacleSpawnMode");

            if (Enum("obstacleSpawnMode") == 1)
            {
                Prop("randomObstacleCount");
                Help("Random modda aynı obstacle birden fazla seçilmez. Listeye koyduğun enabled obstaclelar arasından seçer.");
            }

            Prop("levelObstacles", true);
        });
    }

    private void DrawEnemies()
    {
        Box("NORMAL ENEMY", () =>
        {
            Prop("normalEnemyCount");

            if (Int("normalEnemyCount") <= 0)
                return;

            Prop("normalEnemySpawnInterval");
            Prop("normalMinStartSpeed");
            Prop("normalMaxStartSpeed");
            Prop("normalMaxSpeed");
            Prop("normalSpeedIncreaseRate");

            EditorGUILayout.Space();

            MiniTitle("AI");

            Prop("normalPredictionEnabled");

            if (Bool("normalPredictionEnabled"))
            {
                Prop("normalPredictionDistanceThreshold");
                Prop("normalPredictionTime");
                Prop("normalMaxPredictionDistance");
            }

            EditorGUILayout.Space();

            Prop("normalSeparationEnabled");

            if (Bool("normalSeparationEnabled"))
            {
                Prop("normalSeparationRadius");
                Prop("normalSeparationStrength");
            }
        });

        Box("PROJECTILE ENEMY", () =>
        {
            Prop("projectileEnemyCount");

            if (Int("projectileEnemyCount") <= 0) return;

            Prop("projectileEnemySpawnInterval");
            Prop("projectileMoveSpeed");
            Prop("projectileStoppingDistance");
            Prop("projectileRetreatDistance");
            Prop("projectileFireRate");
            Prop("projectileSpeed");
        });

        Box("HUNTER ENEMY", () =>
        {
            Prop("hunterEnemyCount");

            if (Int("hunterEnemyCount") <= 0) return;

            Prop("hunterEnemySpawnInterval");
            Prop("hunterRepositionTime");
            Prop("hunterWarningDuration");
            Prop("hunterChargeSpeed");
            Prop("hunterStunDuration");
        });
    }

    private void DrawBoss()
    {
        Box("BOSS", () =>
        {
            Prop("bossEnabled");

            if (!Bool("bossEnabled")) return;

            Prop("bossSpawnScore");
            Prop("bossSpeed");
            Prop("bossCanSplit");

            if (Bool("bossCanSplit"))
            {
                Prop("bossSplitDelay");
                Prop("bossSplitDistance");
                Prop("miniBossSpeed");
            }
        });
    }

    private void DrawBeacon()
    {
        Box("BEACON ENEMY", () =>
        {
            Prop("beaconEnemyCount");

            if (Int("beaconEnemyCount") <= 0) return;

            Prop("beaconMinSpawnTime");
            Prop("beaconMaxSpawnTime");

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
        });
    }

    private void DrawPowerUps()
    {
        Box("POWER UPS", () =>
        {
            Prop("armorEnabled");

            if (Bool("armorEnabled"))
            {
                MiniTitle("Armor");
                Prop("armorMinSpawnTime");
                Prop("armorMaxSpawnTime");
                Prop("armorImmuneDuration");
            }

            Space();

            Prop("slowEnabled");

            if (Bool("slowEnabled"))
            {
                MiniTitle("Slow");
                Prop("slowMinSpawnTime");
                Prop("slowMaxSpawnTime");
                Prop("slowMultiplier");
                Prop("slowDuration");
            }
        });
    }

    private void DrawTraps()
    {
        Box("TRAPS / LASERS", () =>
        {
            Prop("verticalLaserEnabled");

            if (Bool("verticalLaserEnabled"))
            {
                MiniTitle("Vertical Laser");
                Prop("verticalLaserMinSpawnTime");
                Prop("verticalLaserMaxSpawnTime");
                Prop("verticalLaserWarningDuration");
                Prop("verticalLaserLifeTime");
                Prop("verticalLaserWidth");
                Prop("verticalLaserHeightExtra");
            }

            Space();

            Prop("horizontalLaserEnabled");

            if (Bool("horizontalLaserEnabled"))
            {
                MiniTitle("Horizontal Laser");
                Prop("horizontalLaserMinSpawnTime");
                Prop("horizontalLaserMaxSpawnTime");
                Prop("horizontalLaserWarningDuration");
                Prop("horizontalLaserLifeTime");
                Prop("horizontalLaserWidth");
                Prop("horizontalLaserWidthExtra");
            }

            Space();

            Prop("bombTrapEnabled");

            if (Bool("bombTrapEnabled"))
            {
                MiniTitle("Bomb Trap");
                Prop("bombMinSpawnTime");
                Prop("bombMaxSpawnTime");
                Prop("maxBombCount");
            }
        });
    }

    private void DrawCoin(string title, string enabledProp, string chanceProp, string valueProp)
    {
        MiniTitle(title);
        Prop(enabledProp);

        if (Bool(enabledProp))
        {
            Prop(chanceProp);
            Prop(valueProp);
        }
    }

    private void DrawHeader(string title)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(4);
    }

    private void Box(string title, System.Action content)
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        content?.Invoke();

        EditorGUILayout.EndVertical();
    }

    private void MiniTitle(string title)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
    }

    private void Help(string text)
    {
        EditorGUILayout.HelpBox(text, MessageType.Info);
    }

    private void Space()
    {
        EditorGUILayout.Space(6);
    }

    private void Prop(string name, bool includeChildren = false)
    {
        SerializedProperty property = serializedObject.FindProperty(name);

        if (property != null)
            EditorGUILayout.PropertyField(property, includeChildren);
        else
            EditorGUILayout.HelpBox("Missing property: " + name, MessageType.Warning);
    }

    private bool Bool(string name)
    {
        SerializedProperty property = serializedObject.FindProperty(name);
        return property != null && property.boolValue;
    }

    private int Int(string name)
    {
        SerializedProperty property = serializedObject.FindProperty(name);
        return property != null ? property.intValue : 0;
    }

    private int Enum(string name)
    {
        SerializedProperty property = serializedObject.FindProperty(name);
        return property != null ? property.enumValueIndex : 0;
    }
}