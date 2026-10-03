using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelConfig))]
public partial class LevelConfigEditor : Editor
{
    private static readonly int[] DefaultComboCoins =
    {
        3, 7, 14, 18, 22
    };

    private static readonly float[] DefaultComboSpeedMultipliers =
    {
        1.03f, 1.06f, 1.09f, 1.12f, 1.15f
    };

    private enum EditorViewMode
    {
        Basic,
        Advanced
    }

    private enum MechanicId
    {
        ReachScoreMode,
        SurviveTimeMode,
        TimedScoreMode,
        Dash,
        Clone,
        Combo,
        NormalCoin,
        GoldCoin,
        RareCoin,
        StaticObstacles,
        NormalEnemy,
        ProjectileEnemy,
        HunterEnemy,
        Boss,
        BeaconEnemy,
        Armor,
        Slow,
        VerticalLaser,
        HorizontalLaser,
        SpaceBomb
    }

    private enum ValidationSeverity
    {
        Error,
        Warning,
        Suggestion
    }

    private sealed class MechanicDescriptor
    {
        public readonly MechanicId id;
        public readonly string category;
        public readonly string label;
        public readonly string propertyPath;
        public readonly Func<LevelConfig, bool> isActive;
        public readonly string briefingText;

        public MechanicDescriptor(
            MechanicId id,
            string category,
            string label,
            string propertyPath,
            Func<LevelConfig, bool> isActive,
            string briefingText)
        {
            this.id = id;
            this.category = category;
            this.label = label;
            this.propertyPath = propertyPath;
            this.isActive = isActive;
            this.briefingText = briefingText;
        }
    }

    private sealed class ValidationIssue
    {
        public readonly ValidationSeverity severity;
        public readonly string message;
        public readonly Action<LevelConfig> fix;
        public readonly string fixLabel;
        public readonly string propertyPath;

        public ValidationIssue(
            ValidationSeverity severity,
            string message,
            Action<LevelConfig> fix = null,
            string fixLabel = "AUTO FIX",
            string propertyPath = null)
        {
            this.severity = severity;
            this.message = message;
            this.fix = fix;
            this.fixLabel = fixLabel;
            this.propertyPath = propertyPath;
        }
    }

    private sealed class TimelineRow
    {
        public string label;
        public string tooltip;
        public Color color;
        public bool hasWindow;
        public float windowStart;
        public float windowEnd;
        public readonly List<float> markers = new List<float>();
    }

    private static readonly MechanicDescriptor[] MechanicDescriptors =
    {
        new MechanicDescriptor(
            MechanicId.ReachScoreMode,
            "MISSION MODE",
            "Reach Score",
            "mechanicProgression.reachScoreMode",
            config => config.winCondition == WinConditionType.ReachScore,
            "This mission is completed by collecting the required score."
        ),
        new MechanicDescriptor(
            MechanicId.SurviveTimeMode,
            "MISSION MODE",
            "Survive Time",
            "mechanicProgression.surviveTimeMode",
            config => config.winCondition == WinConditionType.SurviveTime,
            "This mission removes score pressure. Stay alive until the countdown reaches zero."
        ),
        new MechanicDescriptor(
            MechanicId.TimedScoreMode,
            "MISSION MODE",
            "Timed Score",
            "mechanicProgression.timedScoreMode",
            config => config.winCondition == WinConditionType.ReachScoreWithinTime,
            "Collect the required score before the countdown reaches zero."
        ),
        new MechanicDescriptor(
            MechanicId.Dash,
            "PLAYER TOOLS",
            "Dash",
            "mechanicProgression.dash",
            config => config.dashEnabled,
            "Dash gives you a short burst of movement. Use it to escape danger and create a safer route."
        ),
        new MechanicDescriptor(
            MechanicId.Clone,
            "PLAYER TOOLS",
            "Clone",
            "mechanicProgression.clone",
            config => config.cloneEnabled,
            "Deploy the clone to redirect enemy attention. After use, it becomes available again when its cooldown ends."
        ),
        new MechanicDescriptor(
            MechanicId.Combo,
            "SCORING / ARENA",
            "Combo",
            "mechanicProgression.combo",
            config => config.UsesScore && config.comboEnabled,
            "Collect coins quickly to build your combo and increase your movement speed."
        ),
        new MechanicDescriptor(
            MechanicId.NormalCoin,
            "SCORING / ARENA",
            "1-Point Coin",
            "mechanicProgression.normalCoin",
            config => config.UsesScore && config.normalCoinEnabled,
            "Standard coins are worth 1 point and form the foundation of the scoring route."
        ),
        new MechanicDescriptor(
            MechanicId.GoldCoin,
            "SCORING / ARENA",
            "3-Point Coin",
            "mechanicProgression.goldCoin",
            config => config.UsesScore && config.goldCoinEnabled,
            "Gold coins are worth 3 points. Reaching them can shorten the mission, but may require a riskier route."
        ),
        new MechanicDescriptor(
            MechanicId.RareCoin,
            "SCORING / ARENA",
            "5-Point Coin",
            "mechanicProgression.rareCoin",
            config => config.UsesScore && config.rareCoinEnabled,
            "Rare coins are worth 5 points. Treat them as high-value opportunities rather than safe pickups."
        ),
        new MechanicDescriptor(
            MechanicId.StaticObstacles,
            "SCORING / ARENA",
            "Static Obstacles",
            "mechanicProgression.staticObstacles",
            HasActiveObstacles,
            "Static obstacles break direct routes and force you to plan movement around the arena."
        ),
        new MechanicDescriptor(
            MechanicId.NormalEnemy,
            "ENEMIES",
            "Normal Enemy",
            "mechanicProgression.normalEnemy",
            config => config.normalEnemyCount > 0,
            "Normal enemies pursue you continuously. Keep moving and avoid letting several enemies close in at once."
        ),
        new MechanicDescriptor(
            MechanicId.ProjectileEnemy,
            "ENEMIES",
            "Projectile Enemy",
            "mechanicProgression.projectileEnemy",
            config => config.projectileEnemyCount > 0,
            "Projectile enemies attack from range. Keep moving and watch the firing line before committing to a route."
        ),
        new MechanicDescriptor(
            MechanicId.HunterEnemy,
            "ENEMIES",
            "Hunter Enemy",
            "mechanicProgression.hunterEnemy",
            config => config.hunterEnemyCount > 0,
            "Hunters reposition, warn, then charge. Read the warning and move out of the attack path."
        ),
        new MechanicDescriptor(
            MechanicId.Boss,
            "ENEMIES",
            "Boss",
            "mechanicProgression.boss",
            config => config.bossEnabled,
            "The boss enters after the mission trigger is reached. Preserve space and prepare for its stronger pressure."
        ),
        new MechanicDescriptor(
            MechanicId.BeaconEnemy,
            "ENEMIES",
            "Beacon Enemy",
            "mechanicProgression.beaconEnemy",
            config => config.beaconEnemyCount > 0,
            "The beacon strengthens nearby enemies. Reposition before its buff turns an ordinary encounter into heavy pressure."
        ),
        new MechanicDescriptor(
            MechanicId.Armor,
            "POWER UPS",
            "Armor",
            "mechanicProgression.armor",
            config => config.armorEnabled,
            "Armor absorbs one lethal hit. It is protection, not permission to stop moving."
        ),
        new MechanicDescriptor(
            MechanicId.Slow,
            "POWER UPS",
            "Slow",
            "mechanicProgression.slow",
            config => config.slowEnabled,
            "The slow power-up temporarily reduces enemy and hazard pressure."
        ),
        new MechanicDescriptor(
            MechanicId.VerticalLaser,
            "TRAPS",
            "Vertical Laser",
            "mechanicProgression.verticalLaser",
            config => config.verticalLaserEnabled,
            "Vertical lasers announce their position before activating. Leave the marked lane before the warning ends."
        ),
        new MechanicDescriptor(
            MechanicId.HorizontalLaser,
            "TRAPS",
            "Horizontal Laser",
            "mechanicProgression.horizontalLaser",
            config => config.horizontalLaserEnabled,
            "Horizontal lasers cut across the arena after a warning. Read the safe side and move early."
        ),
        new MechanicDescriptor(
            MechanicId.SpaceBomb,
            "TRAPS",
            "Space Bomb",
            "mechanicProgression.spaceBomb",
            config => config.bombTrapEnabled,
            "Space bombs create lethal zones inside the arena. Do not let them block your next escape route."
        )
    };

    private static readonly string[] MechanicStatusLabels =
    {
        "Already Known",
        "Introduced In This Level",
        "Final Challenge"
    };

    private static List<LevelConfig> cachedLevelConfigs;
    private static double cachedLevelConfigTime;

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
    private bool timelineExpanded = true;
    private bool progressionExpanded = true;
    private bool validationExpanded = true;
    private bool showInactiveMechanics;
    private string focusedValidationPropertyPath;

    private bool normalEnemyExpanded = true;
    private bool projectileEnemyExpanded = true;
    private bool hunterEnemyExpanded = true;
    private bool bossExpanded = true;
    private bool beaconExpanded = true;

    private bool IsAdvanced => true;

    private WinConditionType SelectedWinCondition =>
        (WinConditionType)EnumValue("winCondition");

    private bool UsesScore =>
        SelectedWinCondition == WinConditionType.ReachScore ||
        SelectedWinCondition == WinConditionType.ReachScoreWithinTime;

    private void OnEnable()
    {
        EnsureProgressionMetadata();
    }

    public override void OnInspectorGUI()
    {
        EnsureProgressionMetadata();
        serializedObject.Update();

        DrawMainHeader();
        DrawSummary();

        // Core gameplay composition comes first.
        DrawCore();
        DrawPlayer();
        DrawAbilities();
        DrawCombo();
        DrawBackground();
        DrawCoins();
        DrawObstacles();
        DrawEnemies();
        DrawTraps();
        DrawPowerUps();

        // Design analysis and presentation come after gameplay is composed.
        DrawPacingTimeline();
        DrawMechanicProgression();
        DrawMissionBriefing();
        DrawMusic();
        DrawValidation();

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
            "Complete level composition and behaviour settings",
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
                SummaryRow("New Mechanics", GetProgressionSummary(
                    config,
                    MechanicProgressionStatus.IntroducedHere
                ));
                SummaryRow("Final Challenges", GetProgressionSummary(
                    config,
                    MechanicProgressionStatus.FinalChallenge
                ));
                SummaryRow("Music", config.gameplayMusic != null
                    ? config.gameplayMusic.name
                    : "Not Assigned");
            }
        );
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

    private string GetFoldoutPrefsKey(
        string foldoutType,
        string title)
    {
        LevelConfig config = target as LevelConfig;

        string assetId = "GLOBAL";

        if (config != null)
        {
            string assetPath =
                AssetDatabase.GetAssetPath(config);

            if (!string.IsNullOrEmpty(assetPath))
            {
                string guid =
                    AssetDatabase.AssetPathToGUID(assetPath);

                if (!string.IsNullOrEmpty(guid))
                    assetId = guid;
            }
            else
            {
                assetId =
                    config.GetEntityId().ToString();
            }
        }

        return
            $"VoidRush.LevelConfigEditor." +
            $"{assetId}.{foldoutType}.{title}";
    }

    private void FoldoutBox(
        string title,
        ref bool expanded,
        Action content)
    {
        string prefsKey =
            GetFoldoutPrefsKey("Main", title);

        expanded = EditorPrefs.GetBool(
            prefsKey,
            expanded
        );

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        EditorGUI.BeginChangeCheck();

        bool newExpanded =
            EditorGUILayout.Foldout(
                expanded,
                title,
                true,
                EditorStyles.foldoutHeader
            );

        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(
                prefsKey,
                newExpanded
            );
        }

        expanded = newExpanded;

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
        string prefsKey =
            GetFoldoutPrefsKey("Nested", title);

        expanded = EditorPrefs.GetBool(
            prefsKey,
            expanded
        );

        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();

        bool newExpanded =
            EditorGUILayout.Foldout(
                expanded,
                title,
                true
            );

        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(
                prefsKey,
                newExpanded
            );
        }

        expanded = newExpanded;

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

    private void PropWithLabel(
        string name,
        string label,
        string tooltip = null)
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

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(label, tooltip)
        );
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
