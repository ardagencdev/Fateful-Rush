using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DangerBalanceProfile))]
public class DangerBalanceProfileEditor : Editor
{
    private bool normalExpanded = true;
    private bool projectileExpanded = true;
    private bool hunterExpanded = true;
    private bool bossExpanded = true;
    private bool beaconExpanded = true;
    private bool verticalLaserExpanded = true;
    private bool horizontalLaserExpanded = true;
    private bool bombExpanded = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDangerHeader();
        DrawProfileActions();
        DrawSection("NORMAL ENEMY", "normalEnemyLevels", ref normalExpanded);
        DrawSection("PROJECTILE ENEMY", "projectileEnemyLevels", ref projectileExpanded);
        DrawSection("HUNTER ENEMY", "hunterEnemyLevels", ref hunterExpanded);
        DrawSection("BOSS", "bossLevels", ref bossExpanded);
        DrawSection("BEACON ENEMY", "beaconEnemyLevels", ref beaconExpanded);
        DrawSection("VERTICAL LASER", "verticalLaserLevels", ref verticalLaserExpanded);
        DrawSection("HORIZONTAL LASER", "horizontalLaserLevels", ref horizontalLaserExpanded);
        DrawSection("SPACE BOMB", "bombLevels", ref bombExpanded);

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawDangerHeader()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(
            "VOID RUSH — DANGER BALANCE",
            titleStyle,
            GUILayout.Height(28f)
        );

        EditorGUILayout.HelpBox(
            "D1 introduces a mechanic, D2 preserves the original baseline, D3 adds pressure, D4 is severe and D5 is reserved for carefully tested endgame encounters.",
            MessageType.Info
        );
    }

    private void DrawProfileActions()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        SerializedProperty version =
            serializedObject.FindProperty("profileVersion");

        if (version != null)
            EditorGUILayout.PropertyField(version);

        if (GUILayout.Button("RESET ALL TIERS TO BALANCED DEFAULTS"))
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Reset Danger Balance",
                "This will replace every danger tier in this profile with the balanced default values.",
                "Reset",
                "Cancel"
            );

            if (confirmed)
            {
                DangerBalanceProfile profile =
                    target as DangerBalanceProfile;

                Undo.RecordObject(profile, "Reset Danger Balance");
                profile.ResetToBalancedDefaults();
                EditorUtility.SetDirty(profile);
                serializedObject.Update();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSection(
        string title,
        string propertyName,
        ref bool expanded)
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
            SerializedProperty array =
                serializedObject.FindProperty(propertyName);

            if (array == null)
            {
                EditorGUILayout.HelpBox(
                    "Missing serialized array: " + propertyName,
                    MessageType.Error
                );
            }
            else
            {
                EnsureTierCount(array);

                for (int i = 0; i < DangerLevelUtility.TierCount; i++)
                {
                    DrawTier(
                        array.GetArrayElementAtIndex(i),
                        (DangerLevel)(i + 1)
                    );
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static void DrawTier(
        SerializedProperty tierProperty,
        DangerLevel level)
    {
        EditorGUILayout.Space(3);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(
            DangerLevelUtility.GetDisplayName(level),
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            DangerLevelUtility.GetDescription(level),
            EditorStyles.wordWrappedMiniLabel
        );

        EditorGUILayout.Space(2);
        EditorGUILayout.PropertyField(
            tierProperty,
            GUIContent.none,
            true
        );

        EditorGUILayout.EndVertical();
    }

    private static void EnsureTierCount(SerializedProperty array)
    {
        if (!array.isArray)
            return;

        if (array.arraySize != DangerLevelUtility.TierCount)
            array.arraySize = DangerLevelUtility.TierCount;
    }
}
