using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

// Same Unity component. Inspector data and lifecycle entry points remain in FatefulRushLocalizationRuntime.cs.
public sealed partial class FatefulRushLocalizationRuntime
{
    private void ApplyStaticRepairs()
    {
        for (int i = 0; i < sceneTexts.Length; i++)
        {
            TMP_Text text = sceneTexts[i];
            if (text == null)
                continue;

            string name = text.gameObject.name;
            string path = BuildPath(text.transform);

            ApplyCompactTurkishButtonLabel(text);

            if (path.IndexOf("OptionsPanel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                switch (name)
                {
                    case "AudioTitle": SetText(text, FatefulRushLocalization.Text("options.audio", "AUDIO")); break;
                    case "SoundText": SetText(text, FatefulRushLocalization.Text("options.sound", "SOUND")); break;
                    case "MenuMusicText": SetText(text, FatefulRushLocalization.Text("options.menu_music", "MENU MUSIC")); break;
                    case "MusicText": SetText(text, FatefulRushLocalization.Text("options.music", "MUSIC")); break;
                    case "SFXText": SetText(text, FatefulRushLocalization.Text("options.sfx", "SFX")); break;
                    case "GameTitle": SetText(text, FatefulRushLocalization.Text("options.game", "GAME")); break;
                    case "JoystickText": SetText(text, FatefulRushLocalization.Text("options.joystick", "JOYSTICK")); break;
                    case "VibrationText": SetText(text, FatefulRushLocalization.Text("options.vibration", "VIBRATION")); break;
                    case "FPSText": SetText(text, FatefulRushLocalization.Text("options.fps", "FPS")); break;
                    case "HUDOpacityText": SetText(text, FatefulRushLocalization.Text("options.hud_opacity", "HUD OPACITY")); break;
                    case "LanguageTitle": SetText(text, FatefulRushLocalization.Text("options.language", "LANGUAGE")); break;
                    case "Title": SetText(text, FatefulRushLocalization.Text("options.title", "OPTIONS")); break;
                }
            }


            if (IsMainMenuButtonLabel(path, text.text))
                SetText(text, FatefulRushLocalization.Text("ui.main_menu", "MAIN MENU"));
        }
    }

    private static void ApplyCompactTurkishButtonLabel(TMP_Text text)
    {
        if (text == null || !FatefulRushLocalization.IsTurkish)
            return;

        // Only compact labels that are too wide for the existing button art.
        // Keep the wording natural while avoiding TMP overflow / edge touching.
        string value = (text.text ?? string.Empty).Trim().ToUpperInvariant();

        switch (value)
        {
            case "GÖRÜNÜMLER":
            case "SKINS":
                SetText(text, "GÖRÜNÜM");
                break;

            case "İSTATİSTİKLER":
            case "STATS":
                SetText(text, "İSTATİSTİK");
                break;

            case "LİDERLİK TABLOLARI":
            case "LEADERBOARDS":
                SetText(text, "SIRALAMA");
                break;
        }
    }

    private static bool IsMainMenuButtonLabel(string path, string currentText)
    {
        if (path.IndexOf(
        "/MenuConfirmationPanel/",
        StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        if (string.IsNullOrEmpty(path))
            return false;

        bool isKnownButton =
            path.IndexOf("/MenuButton/", StringComparison.OrdinalIgnoreCase) >= 0 ||
            path.IndexOf("/MainMenuButton/", StringComparison.OrdinalIgnoreCase) >= 0;

        if (!isKnownButton)
            return false;

        string normalized = (currentText ?? string.Empty).Trim().ToUpperInvariant();
        return normalized.Contains("MAIN MENU") ||
               normalized.Contains("MAΙN MENU") ||
               normalized.Contains("MAİN MENU") ||
               normalized.Contains("ANA MENÜ") ||
               path.IndexOf("ResultsUIController", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("PausePanel", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string LocalizeDeathCause(string cause)
    {
        return FatefulRushLocalization.DeathCause(cause);
    }

    private static string GetSkinKey(string skinId)
    {
        string id = (skinId ?? string.Empty).Trim().ToLowerInvariant();
        switch (id)
        {
            case "white": return "skin.name.white";
            case "blue": return "skin.name.blue";
            case "orange": return "skin.name.orange";
            case "red": return "skin.name.red";
            case "green": return "skin.name.green";
            case "pink": return "skin.name.pink";
            case "yellow": return "skin.name.yellow";
            case "cyan": return "skin.name.cyan";
            case "purple": return "skin.name.purple";
            case "dark": return "skin.name.dark";
            case "gold":
            case "golden": return "skin.name.golden";
            default: return "skin.name." + id;
        }
    }

    private static LevelConfig ResolveCurrentLevel()
    {
        LevelManager[] managers = FindSceneObjects<LevelManager>();
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null && managers[i].currentLevel != null)
                return managers[i].currentLevel;
        }

        return SelectedLevelData.selectedLevel;
    }

    private static string FormatSeconds(float seconds)
    {
        float safe = Mathf.Max(0f, seconds);
        int rounded = Mathf.RoundToInt(safe);
        return Mathf.Approximately(safe, rounded) ? $"{rounded}s" : $"{safe:0.#}s";
    }

    private static T[] FindSceneObjects<T>() where T : Component
    {
        T[] all = Resources.FindObjectsOfTypeAll<T>();
        List<T> result = new List<T>(all.Length);

        for (int i = 0; i < all.Length; i++)
        {
            T item = all[i];
            if (item == null)
                continue;

            Scene scene = item.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            result.Add(item);
        }

        return result.ToArray();
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        if (target == null || string.IsNullOrEmpty(fieldName))
            return default;

        FieldInfo field = GetField(target.GetType(), fieldName);
        if (field == null)
            return default;

        object value = field.GetValue(target);
        return value is T typed ? typed : default;
    }

    private static FieldInfo GetField(Type type, string fieldName)
    {
        string key = type.FullName + "|" + fieldName;
        if (FieldCache.TryGetValue(key, out FieldInfo cached))
            return cached;

        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        FieldCache[key] = field;
        return field;
    }

    private static string BuildPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        Stack<string> names = new Stack<string>();
        Transform current = transform;

        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names.ToArray());
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target == null)
            return;

        if (value == null)
            value = string.Empty;
        if (!string.Equals(target.text, value, StringComparison.Ordinal))
            target.text = value;
    }
}
