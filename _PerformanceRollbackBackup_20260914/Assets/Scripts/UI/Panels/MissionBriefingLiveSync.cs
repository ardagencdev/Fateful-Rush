using System;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the mission briefing synchronized with the selected LevelConfig.
/// The LevelConfig briefing text is the source of truth; this component must
/// never replace it with an auto-generated enemy/hazard summary.
/// </summary>
[DefaultExecutionOrder(20000)]
public sealed class MissionBriefingLiveSync : MonoBehaviour
{
    private static MissionBriefingLiveSync instance;

    private static readonly FieldInfo SelectedLevelField =
        typeof(MissionBriefingPanelUI).GetField(
            "selectedLevel",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

    private static readonly FieldInfo DescriptionTextField =
        typeof(MissionBriefingPanelUI).GetField(
            "pageDescriptionText",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

    private MissionBriefingPanelUI[] panels = Array.Empty<MissionBriefingPanelUI>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("Mission Briefing Live Sync");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<MissionBriefingLiveSync>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshPanels();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshPanels();
    }

    private void LateUpdate()
    {
        // Event-driven now. MissionBriefingPanelUI.Show calls RequestRefresh().
    }

    private void RefreshPanels()
    {
#if UNITY_2023_1_OR_NEWER
        panels = FindObjectsByType<MissionBriefingPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
#else
        panels = FindObjectsOfType<MissionBriefingPanelUI>(true);
#endif
    }

    private void ApplyToPanel(MissionBriefingPanelUI panel)
    {
        if (panel == null || !panel.IsOpen)
            return;

        LevelConfig level = SelectedLevelField?.GetValue(panel) as LevelConfig;
        TMP_Text description = DescriptionTextField?.GetValue(panel) as TMP_Text;

        if (level == null || description == null)
            return;

        bool turkish = IsTurkish();
        string desired = BuildDescription(level, turkish);

        if (!string.Equals(description.text, desired, StringComparison.Ordinal))
            description.text = desired;
    }

    private static bool IsTurkish()
    {
        string code = LocalizationSettings.SelectedLocale?.Identifier.Code;

        if (string.IsNullOrWhiteSpace(code))
            code = PlayerPrefs.GetString("SelectedLocaleCode", string.Empty);

        if (string.IsNullOrWhiteSpace(code))
            code = PlayerPrefs.GetString("Language", "EN");

        return code.StartsWith("tr", StringComparison.OrdinalIgnoreCase) ||
               code.Equals("TR", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDescription(LevelConfig level, bool tr)
    {
        StringBuilder builder = new StringBuilder(256);

        builder.Append(BuildObjective(level, tr));

        string briefing = BuildConfiguredBriefing(level, tr);
        if (!string.IsNullOrWhiteSpace(briefing))
        {
            builder.Append("\n\n");
            builder.Append(briefing.Trim());
        }

        return builder.ToString().Trim();
    }

    private static string BuildConfiguredBriefing(LevelConfig level, bool tr)
    {
        if (level.briefingPages == null || level.briefingPages.Length == 0)
            return string.Empty;

        StringBuilder builder = new StringBuilder(192);

        for (int i = 0; i < level.briefingPages.Length; i++)
        {
            string configured = level.briefingPages[i];

            if (string.IsNullOrWhiteSpace(configured))
                continue;

            string text = configured.Trim();

            // English always uses the exact text stored in the LevelConfig.
            // For Turkish, use the existing per-level localized briefing entry.
            if (tr && i == 0)
                text = FatefulRushLocalization.BriefingTip(level.levelNumber, text);

            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (builder.Length > 0)
                builder.Append("\n\n");

            builder.Append(text.Trim());
        }

        return builder.ToString();
    }

    private static string BuildObjective(LevelConfig level, bool tr)
    {
        int score = level.SafeWinScore;
        string time = FormatNumber(level.SafeTimeLimit);

        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                return tr
                    ? $"Görevi tamamlamak için {score} puana ulaş."
                    : $"Reach {score} points to complete the mission.";

            case WinConditionType.SurviveTime:
                return tr
                    ? $"{time} saniye hayatta kal. Bu görevde coinler ve kombolar devre dışı."
                    : $"Survive for {time} seconds. Coins and combos are disabled in this mission.";

            case WinConditionType.ReachScoreWithinTime:
                return tr
                    ? $"Süre bitmeden {score} puana ulaş. Toplam süren {time} saniye."
                    : $"Reach {score} points before the {time}-second timer reaches zero.";

            default:
                return tr ? "Görev hedefini tamamla." : "Complete the mission objective.";
        }
    }

    private static string FormatNumber(float value)
    {
        float safe = Mathf.Max(0f, value);

        return Mathf.Approximately(safe, Mathf.Round(safe))
            ? Mathf.RoundToInt(safe).ToString()
            : safe.ToString("0.#");
    }


    public static void RequestRefresh()
    {
        if (instance == null)
            return;

        instance.RefreshPanels();

        for (int i = 0; i < instance.panels.Length; i++)
            instance.ApplyToPanel(instance.panels[i]);
    }
}
