using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in LevelConfigEditor.cs.
public partial class LevelConfigEditor
{
    private void DrawMissionBriefing()
    {
        FoldoutBox(
            "MISSION PRESENTATION",
            ref briefingExpanded,
            () =>
            {
                Help(
                    "Build the gameplay first, then describe it here. The draft generator uses the objective, active mechanics and progression tags."
                );

                Prop("briefingTitle");
                Prop("missionDifficulty");
                Prop("briefingModeDescription");
                Prop("briefingObjectiveDescription");
                Prop("briefingPages", true);

                EditorGUILayout.Space(5);

                using (new EditorGUI.DisabledScope(
                    serializedObject.isEditingMultipleObjects))
                {
                    if (GUILayout.Button(
                        "GENERATE BRIEFING DRAFT",
                        GUILayout.Height(26f)))
                    {
                        GenerateBriefingDraftWithConfirmation();
                    }
                }

                LevelConfig config = GetSingleConfig();

                if (config == null)
                    return;

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    "BRIEFING PREVIEW",
                    EditorStyles.miniBoldLabel
                );

                EditorGUILayout.LabelField(
                    config.GetEffectiveModeDescription(),
                    EditorStyles.boldLabel
                );

                EditorGUILayout.LabelField(
                    config.GetEffectiveObjectiveDescription(),
                    EditorStyles.wordWrappedMiniLabel
                );

                int extraPageCount =
                    CountNonEmptyBriefingPages(config.briefingPages);

                EditorGUILayout.LabelField(
                    $"Total Pages: {1 + extraPageCount} " +
                    $"(1 objective + {extraPageCount} extra)",
                    EditorStyles.miniLabel
                );

                EditorGUILayout.EndVertical();
            }
        );
    }

    private void DrawMechanicProgression()
    {
        FoldoutBox(
            "MECHANIC PROGRESSION",
            ref progressionExpanded,
            () =>
            {
                LevelConfig config = GetSingleConfig();

                if (config == null)
                {
                    Help("Mechanic progression is available when a single LevelConfig is selected.");
                    return;
                }

                Help(
                    "Mark only the mechanics the player meets for the first time or must master here. These tags are used by validation and the briefing draft generator."
                );

                string introduced = GetProgressionSummary(
                    config,
                    MechanicProgressionStatus.IntroducedHere
                );

                string finalChallenges = GetProgressionSummary(
                    config,
                    MechanicProgressionStatus.FinalChallenge
                );

                SummaryRow("Introduced", introduced);
                SummaryRow("Final Challenge", finalChallenges);

                EditorGUILayout.Space(4);

                showInactiveMechanics = EditorGUILayout.ToggleLeft(
                    "Show inactive mechanics",
                    showInactiveMechanics
                );

                string lastCategory = null;

                foreach (MechanicDescriptor descriptor in
                         MechanicDescriptors)
                {
                    bool active = descriptor.isActive(config);

                    if (!active && !showInactiveMechanics)
                        continue;

                    if (lastCategory != descriptor.category)
                    {
                        MiniTitle(descriptor.category);
                        lastCategory = descriptor.category;
                    }

                    DrawMechanicStatusRow(
                        descriptor,
                        active
                    );
                }
            }
        );
    }

    private void DrawMechanicStatusRow(
        MechanicDescriptor descriptor,
        bool active)
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                descriptor.propertyPath
            );

        if (property == null)
        {
            EditorGUILayout.HelpBox(
                "Missing serialized property: " +
                descriptor.propertyPath,
                MessageType.Error
            );
            return;
        }

        EditorGUILayout.BeginHorizontal();

        GUIContent label = new GUIContent(
            descriptor.label,
            descriptor.briefingText
        );

        EditorGUILayout.LabelField(
            label,
            GUILayout.Width(155f)
        );

        using (new EditorGUI.DisabledScope(!active))
        {
            property.enumValueIndex =
                EditorGUILayout.Popup(
                    property.enumValueIndex,
                    MechanicStatusLabels
                );
        }

        GUIStyle stateStyle =
            new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };

        EditorGUILayout.LabelField(
            active ? "ACTIVE" : "OFF",
            stateStyle,
            GUILayout.Width(48f)
        );

        EditorGUILayout.EndHorizontal();

        if (!active &&
            property.enumValueIndex !=
            (int)MechanicProgressionStatus.AlreadyKnown)
        {
            Warning(
                $"{descriptor.label} is inactive but still has a progression tag."
            );
        }
    }

    private void GenerateBriefingDraftWithConfirmation()
    {
        LevelConfig config = GetSingleConfig();

        if (config == null)
            return;

        serializedObject.ApplyModifiedProperties();

        if (HasCustomBriefingContent(config))
        {
            bool confirmed =
                EditorUtility.DisplayDialog(
                    "Generate Briefing Draft",
                    "Existing custom briefing text will be replaced by a new draft based on this level's current design.",
                    "Generate",
                    "Cancel"
                );

            if (!confirmed)
            {
                serializedObject.Update();
                return;
            }
        }

        Undo.RecordObject(
            config,
            "Generate Briefing Draft"
        );

        PopulateBriefingDraft(config);

        EditorUtility.SetDirty(config);
        serializedObject.Update();
        GUI.changed = true;
    }

    private static void PopulateBriefingDraft(
        LevelConfig config)
    {
        if (config == null)
            return;

        if (string.IsNullOrWhiteSpace(
            config.briefingTitle))
        {
            config.briefingTitle =
                "MISSION BRIEFING";
        }

        config.briefingModeDescription =
            config.GetDefaultModeDescription();

        switch (config.winCondition)
        {
            case WinConditionType.ReachScore:
                config.briefingObjectiveDescription =
                    $"Collect {config.SafeWinScore} points and complete the mission.";
                break;

            case WinConditionType.SurviveTime:
                config.briefingObjectiveDescription =
                    $"Survive for {FormatSecondsForEditor(config.SafeTimeLimit)}. Keep moving until the countdown reaches zero.";
                break;

            case WinConditionType.ReachScoreWithinTime:
                config.briefingObjectiveDescription =
                    $"Collect {config.SafeWinScore} points before the {FormatSecondsForEditor(config.SafeTimeLimit)} countdown reaches zero.";
                break;
        }

        List<string> pages = new List<string>();
        List<MechanicDescriptor> introduced =
            GetActiveMechanicsWithStatus(
                config,
                MechanicProgressionStatus.IntroducedHere
            );

        for (int i = 0;
             i < introduced.Count && i < 3;
             i++)
        {
            pages.Add(
                introduced[i].briefingText
            );
        }

        if (introduced.Count > 3)
        {
            List<string> remaining =
                new List<string>();

            for (int i = 3;
                 i < introduced.Count;
                 i++)
            {
                remaining.Add(
                    introduced[i].label
                );
            }

            pages.Add(
                "This mission also introduces: " +
                string.Join(", ", remaining) +
                ". Take time to read how these systems interact."
            );
        }

        List<MechanicDescriptor> finalChallenges =
            GetActiveMechanicsWithStatus(
                config,
                MechanicProgressionStatus.FinalChallenge
            );

        if (finalChallenges.Count > 0)
        {
            List<string> names =
                new List<string>();

            foreach (MechanicDescriptor descriptor in
                     finalChallenges)
            {
                names.Add(descriptor.label);
            }

            pages.Add(
                "Mastery test: " +
                string.Join(", ", names) +
                ". Expect little room for mistakes and use every tool deliberately."
            );
        }

        if (pages.Count == 0)
        {
            string enemyPage =
                BuildEnemyBriefingPage(config);

            if (!string.IsNullOrWhiteSpace(enemyPage))
                pages.Add(enemyPage);

            string hazardPage =
                BuildHazardBriefingPage(config);

            if (!string.IsNullOrWhiteSpace(hazardPage))
                pages.Add(hazardPage);

            string supportPage =
                BuildSupportBriefingPage(config);

            if (!string.IsNullOrWhiteSpace(supportPage))
                pages.Add(supportPage);
        }

        if (pages.Count == 0)
        {
            pages.Add(
                "Read the arena, keep moving and complete the objective without wasting your escape routes."
            );
        }

        config.briefingPages =
            pages.ToArray();
    }

    private static string BuildEnemyBriefingPage(
        LevelConfig config)
    {
        List<string> enemies =
            new List<string>();

        if (config.normalEnemyCount > 0)
            enemies.Add("normal enemies");

        if (config.projectileEnemyCount > 0)
            enemies.Add("projectile enemies");

        if (config.hunterEnemyCount > 0)
            enemies.Add("hunters");

        if (config.beaconEnemyCount > 0)
            enemies.Add("beacons");

        if (config.bossEnabled)
            enemies.Add("a boss encounter");

        if (enemies.Count == 0)
            return string.Empty;

        return
            "Active threats: " +
            string.Join(", ", enemies) +
            ". Keep enough space to react when their pressure overlaps.";
    }

    private static string BuildHazardBriefingPage(
        LevelConfig config)
    {
        List<string> hazards =
            new List<string>();

        if (config.verticalLaserEnabled)
            hazards.Add("vertical lasers");

        if (config.horizontalLaserEnabled)
            hazards.Add("horizontal lasers");

        if (config.bombTrapEnabled)
            hazards.Add("space bombs");

        if (hazards.Count == 0)
            return string.Empty;

        return
            "Arena hazards: " +
            string.Join(", ", hazards) +
            ". Watch their warnings and protect your next escape route.";
    }

    private static string BuildSupportBriefingPage(
        LevelConfig config)
    {
        List<string> tools =
            new List<string>();

        if (config.dashEnabled)
            tools.Add("Dash");

        if (config.cloneEnabled)
            tools.Add("Clone");

        if (config.armorEnabled)
            tools.Add("Armor");

        if (config.slowEnabled)
            tools.Add("Slow");

        if (tools.Count == 0)
            return string.Empty;

        return
            "Available tools: " +
            string.Join(", ", tools) +
            ". Use them deliberately instead of waiting until the arena is already closed around you.";
    }

    private static bool HasCustomBriefingContent(
        LevelConfig config)
    {
        if (!string.IsNullOrWhiteSpace(
                config.briefingModeDescription) ||
            !string.IsNullOrWhiteSpace(
                config.briefingObjectiveDescription))
        {
            return true;
        }

        if (config.briefingPages == null)
            return false;

        foreach (string page in
                 config.briefingPages)
        {
            if (!string.IsNullOrWhiteSpace(page) &&
                !IsPlaceholderBriefingPage(page))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUsefulBriefingPages(
        LevelConfig config)
    {
        if (config.briefingPages == null ||
            config.briefingPages.Length == 0)
        {
            return false;
        }

        foreach (string page in
                 config.briefingPages)
        {
            if (!string.IsNullOrWhiteSpace(page) &&
                !IsPlaceholderBriefingPage(page))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPlaceholderBriefingPage(
        string page)
    {
        if (string.IsNullOrWhiteSpace(page))
            return true;

        string value =
            page.Trim();

        return string.Equals(
                   value,
                   "Mission information...",
                   StringComparison.OrdinalIgnoreCase
               ) ||
               string.Equals(
                   value,
                   "Exclusive for Tester",
                   StringComparison.OrdinalIgnoreCase
               );
    }

    private static int CountNonEmptyBriefingPages(
        string[] pages)
    {
        if (pages == null)
            return 0;

        int count = 0;

        foreach (string page in pages)
        {
            if (!string.IsNullOrWhiteSpace(page))
                count++;
        }

        return count;
    }

    private static string FormatSecondsForEditor(
        float seconds)
    {
        float safeSeconds =
            Mathf.Max(0f, seconds);

        if (Mathf.Approximately(
            safeSeconds,
            Mathf.Round(safeSeconds)))
        {
            return
                $"{Mathf.RoundToInt(safeSeconds)} seconds";
        }

        return $"{safeSeconds:0.#} seconds";
    }

    private static List<MechanicDescriptor>
        GetActiveMechanicsWithStatus(
            LevelConfig config,
            MechanicProgressionStatus status)
    {
        List<MechanicDescriptor> result =
            new List<MechanicDescriptor>();

        foreach (MechanicDescriptor descriptor in
                 MechanicDescriptors)
        {
            if (descriptor.isActive(config) &&
                GetMechanicStatus(
                    config,
                    descriptor.id) == status)
            {
                result.Add(descriptor);
            }
        }

        return result;
    }

    private static string GetProgressionSummary(
        LevelConfig config,
        MechanicProgressionStatus status)
    {
        if (config == null ||
            config.mechanicProgression == null)
        {
            return "None";
        }

        List<string> labels =
            new List<string>();

        foreach (MechanicDescriptor descriptor in
                 MechanicDescriptors)
        {
            if (!descriptor.isActive(config))
                continue;

            if (GetMechanicStatus(
                    config,
                    descriptor.id) == status)
            {
                labels.Add(descriptor.label);
            }
        }

        return labels.Count > 0
            ? string.Join(", ", labels)
            : "None";
    }

    private static string GetMechanicStatusLabel(
        MechanicProgressionStatus status)
    {
        switch (status)
        {
            case MechanicProgressionStatus.IntroducedHere:
                return "Introduced In This Level";

            case MechanicProgressionStatus.FinalChallenge:
                return "Final Challenge";

            default:
                return "Already Known";
        }
    }

    private static MechanicProgressionStatus
        GetMechanicStatus(
            LevelConfig config,
            MechanicId id)
    {
        if (config == null ||
            config.mechanicProgression == null)
        {
            return
                MechanicProgressionStatus.AlreadyKnown;
        }

        LevelMechanicProgression progression =
            config.mechanicProgression;

        switch (id)
        {
            case MechanicId.ReachScoreMode:
                return progression.reachScoreMode;
            case MechanicId.SurviveTimeMode:
                return progression.surviveTimeMode;
            case MechanicId.TimedScoreMode:
                return progression.timedScoreMode;
            case MechanicId.Dash:
                return progression.dash;
            case MechanicId.Clone:
                return progression.clone;
            case MechanicId.Combo:
                return progression.combo;
            case MechanicId.NormalCoin:
                return progression.normalCoin;
            case MechanicId.GoldCoin:
                return progression.goldCoin;
            case MechanicId.RareCoin:
                return progression.rareCoin;
            case MechanicId.StaticObstacles:
                return progression.staticObstacles;
            case MechanicId.NormalEnemy:
                return progression.normalEnemy;
            case MechanicId.ProjectileEnemy:
                return progression.projectileEnemy;
            case MechanicId.HunterEnemy:
                return progression.hunterEnemy;
            case MechanicId.Boss:
                return progression.boss;
            case MechanicId.BeaconEnemy:
                return progression.beaconEnemy;
            case MechanicId.Armor:
                return progression.armor;
            case MechanicId.Slow:
                return progression.slow;
            case MechanicId.VerticalLaser:
                return progression.verticalLaser;
            case MechanicId.HorizontalLaser:
                return progression.horizontalLaser;
            case MechanicId.SpaceBomb:
                return progression.spaceBomb;
            default:
                return
                    MechanicProgressionStatus.AlreadyKnown;
        }
    }

    private static void SetMechanicStatus(
        LevelConfig config,
        MechanicId id,
        MechanicProgressionStatus status)
    {
        if (config.mechanicProgression == null)
        {
            config.mechanicProgression =
                new LevelMechanicProgression();
        }

        LevelMechanicProgression progression =
            config.mechanicProgression;

        switch (id)
        {
            case MechanicId.ReachScoreMode:
                progression.reachScoreMode = status;
                break;
            case MechanicId.SurviveTimeMode:
                progression.surviveTimeMode = status;
                break;
            case MechanicId.TimedScoreMode:
                progression.timedScoreMode = status;
                break;
            case MechanicId.Dash:
                progression.dash = status;
                break;
            case MechanicId.Clone:
                progression.clone = status;
                break;
            case MechanicId.Combo:
                progression.combo = status;
                break;
            case MechanicId.NormalCoin:
                progression.normalCoin = status;
                break;
            case MechanicId.GoldCoin:
                progression.goldCoin = status;
                break;
            case MechanicId.RareCoin:
                progression.rareCoin = status;
                break;
            case MechanicId.StaticObstacles:
                progression.staticObstacles = status;
                break;
            case MechanicId.NormalEnemy:
                progression.normalEnemy = status;
                break;
            case MechanicId.ProjectileEnemy:
                progression.projectileEnemy = status;
                break;
            case MechanicId.HunterEnemy:
                progression.hunterEnemy = status;
                break;
            case MechanicId.Boss:
                progression.boss = status;
                break;
            case MechanicId.BeaconEnemy:
                progression.beaconEnemy = status;
                break;
            case MechanicId.Armor:
                progression.armor = status;
                break;
            case MechanicId.Slow:
                progression.slow = status;
                break;
            case MechanicId.VerticalLaser:
                progression.verticalLaser = status;
                break;
            case MechanicId.HorizontalLaser:
                progression.horizontalLaser = status;
                break;
            case MechanicId.SpaceBomb:
                progression.spaceBomb = status;
                break;
        }
    }

    private static bool TryGetThreatDanger(
        LevelConfig config,
        MechanicId id,
        out DangerLevel danger)
    {
        switch (id)
        {
            case MechanicId.NormalEnemy:
                danger = config.normalEnemyDanger;
                return true;
            case MechanicId.ProjectileEnemy:
                danger = config.projectileEnemyDanger;
                return true;
            case MechanicId.HunterEnemy:
                danger = config.hunterEnemyDanger;
                return true;
            case MechanicId.Boss:
                danger = config.bossDanger;
                return true;
            case MechanicId.BeaconEnemy:
                danger = config.beaconEnemyDanger;
                return true;
            case MechanicId.VerticalLaser:
                danger = config.verticalLaserDanger;
                return true;
            case MechanicId.HorizontalLaser:
                danger = config.horizontalLaserDanger;
                return true;
            case MechanicId.SpaceBomb:
                danger = config.bombDanger;
                return true;
            default:
                danger = DangerLevel.Danger2;
                return false;
        }
    }

    private static List<LevelConfig>
        GetAllLevelConfigs()
    {
        double now =
            EditorApplication.timeSinceStartup;

        if (cachedLevelConfigs != null &&
            now - cachedLevelConfigTime < 5d)
        {
            return cachedLevelConfigs;
        }

        cachedLevelConfigTime = now;
        cachedLevelConfigs =
            new List<LevelConfig>();

        string[] guids =
            AssetDatabase.FindAssets(
                "t:LevelConfig"
            );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            LevelConfig config =
                AssetDatabase.LoadAssetAtPath<LevelConfig>(
                    path
                );

            if (config != null)
                cachedLevelConfigs.Add(config);
        }

        return cachedLevelConfigs;
    }

    private void EnsureProgressionMetadata()
    {
        if (targets == null)
            return;

        foreach (UnityEngine.Object item in
                 targets)
        {
            LevelConfig config =
                item as LevelConfig;

            if (config == null ||
                config.mechanicProgression != null)
            {
                continue;
            }

            config.mechanicProgression =
                new LevelMechanicProgression();

            EditorUtility.SetDirty(config);
        }
    }
}
