using System;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps mission briefing copy factual by deriving it directly from the active
/// LevelConfig instead of relying on hand-written numbers that can go stale
/// after balance changes. Runs after FatefulRushLocalizationRuntime so the
/// dynamic EN/TR text is the final text displayed to the player.
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
        // FatefulRushLocalizationRuntime also refreshes briefing text in LateUpdate.
        // This component runs later (execution order 20000) and applies the
        // LevelConfig-derived copy every frame, so only one final text reaches
        // the rendered frame and there is no EN/TR flicker.
        if (panels == null || panels.Length == 0)
            RefreshPanels();

        for (int i = 0; i < panels.Length; i++)
            ApplyToPanel(panels[i]);
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
        if (tr && (level.levelNumber == 30 || level.levelNumber == 34 || level.levelNumber == 40))
            return BuildCompactTurkishDescription(level);

        StringBuilder builder = new StringBuilder(320);
        builder.Append(BuildObjective(level, tr));

        string tactical = BuildTacticalSummary(level, tr);
        if (!string.IsNullOrWhiteSpace(tactical))
        {
            builder.Append("\n\n");
            builder.Append(tactical);
        }

        return builder.ToString().Trim();
    }

    private static string BuildCompactTurkishDescription(LevelConfig level)
    {
        StringBuilder builder = new StringBuilder(220);
        builder.Append(BuildObjective(level, true));

        StringBuilder tip = new StringBuilder(150);

        if (level.bossEnabled)
        {
            if (level.EffectiveBossSpawnCondition == BossSpawnCondition.Time)
                AppendSentence(tip, $"Boss {FormatNumber(level.SafeBossSpawnTime)}. saniyede gelir.");
            else
                AppendSentence(tip, $"Boss {level.SafeBossSpawnScore} puanda gelir.");
        }

        if (level.beaconEnemyCount > 0)
        {
            AppendSentence(
                tip,
                $"Beacon {FormatNumber(level.beaconMinSpawnTime)}-{FormatNumber(level.beaconMaxSpawnTime)}. saniyeler arasında çıkar."
            );
        }

        switch (level.levelNumber)
        {
            case 30:
                if (level.beaconEnemyCount > 0)
                    AppendSentence(tip, "Uzun skor rotasından önce Beacon'ı yok et.");
                break;

            case 34:
                if (level.beaconEnemyCount > 0)
                    AppendSentence(tip, "Önce Beacon'ı temizle.");
                if (level.slowEnabled)
                    AppendSentence(tip, "Slow'u yoğun baskıya sakla.");
                break;

            case 40:
                if (level.armorEnabled || level.slowEnabled || level.cloneEnabled)
                    AppendSentence(tip, "Armor, Slow ve Clone'u sırayla kullan.");
                break;
        }

        if (tip.Length > 0)
        {
            builder.Append("\n\n");
            builder.Append(tip);
        }

        return builder.ToString().Trim();
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

    private static string BuildTacticalSummary(LevelConfig level, bool tr)
    {
        StringBuilder builder = new StringBuilder(240);

        if (level.bossEnabled)
        {
            string trigger;
            if (level.EffectiveBossSpawnCondition == BossSpawnCondition.Time)
            {
                string time = FormatNumber(level.SafeBossSpawnTime);
                trigger = tr
                    ? $"Boss {time}. saniyede gelir."
                    : $"The Boss arrives at {time} seconds.";
            }
            else
            {
                int score = level.SafeBossSpawnScore;
                trigger = tr
                    ? $"Boss {score} puanda gelir."
                    : $"The Boss enters at {score} score.";
            }

            AppendSentence(builder, trigger);
        }

        if (level.beaconEnemyCount > 0)
        {
            string min = FormatNumber(level.beaconMinSpawnTime);
            string max = FormatNumber(level.beaconMaxSpawnTime);

            AppendSentence(
                builder,
                tr
                    ? $"Beacon tehdidi {min}-{max}. saniye aralığında devreye girebilir; Dash ile hızlıca yok et."
                    : $"Beacons can enter between {min}-{max} seconds; destroy them quickly with Dash."
            );
        }

        string enemies = BuildEnemySummary(level, tr);
        AppendSentence(builder, enemies);

        string hazards = BuildHazardSummary(level, tr);
        AppendSentence(builder, hazards);

        string abilities = BuildAbilitySummary(level, tr);
        AppendSentence(builder, abilities);

        return builder.ToString().Trim();
    }

    private static string BuildEnemySummary(LevelConfig level, bool tr)
    {
        int activeTypes = 0;
        if (level.normalEnemyCount > 0) activeTypes++;
        if (level.projectileEnemyCount > 0) activeTypes++;
        if (level.hunterEnemyCount > 0) activeTypes++;

        if (activeTypes == 0)
            return string.Empty;

        if (tr)
        {
            StringBuilder list = new StringBuilder();
            AppendListItem(list, level.normalEnemyCount > 0 ? $"{level.normalEnemyCount} Stalker" : null);
            AppendListItem(list, level.projectileEnemyCount > 0 ? $"{level.projectileEnemyCount} Blaster" : null);
            AppendListItem(list, level.hunterEnemyCount > 0 ? $"{level.hunterEnemyCount} Hunter" : null);
            return $"Aynı anda {list} tehdidiyle karşılaşabilirsin.";
        }
        else
        {
            StringBuilder list = new StringBuilder();
            AppendListItem(list, level.normalEnemyCount > 0 ? $"{level.normalEnemyCount} Stalker" : null);
            AppendListItem(list, level.projectileEnemyCount > 0 ? $"{level.projectileEnemyCount} Blaster" : null);
            AppendListItem(list, level.hunterEnemyCount > 0 ? $"{level.hunterEnemyCount} Hunter" : null);
            return $"Expect up to {list} active threats at once.";
        }
    }

    private static string BuildHazardSummary(LevelConfig level, bool tr)
    {
        StringBuilder list = new StringBuilder();

        AppendListItem(list, level.verticalLaserEnabled ? (tr ? "dikey lazer" : "vertical lasers") : null);
        AppendListItem(list, level.horizontalLaserEnabled ? (tr ? "yatay lazer" : "horizontal lasers") : null);
        AppendListItem(list, level.bombTrapEnabled ? (tr ? "uzay bombası" : "space bombs") : null);

        if (list.Length == 0)
            return string.Empty;

        return tr
            ? $"Arena tehlikeleri: {list}."
            : $"Arena hazards: {list}.";
    }

    private static string BuildAbilitySummary(LevelConfig level, bool tr)
    {
        if (!level.cloneEnabled && !level.slowEnabled && !level.armorEnabled)
            return string.Empty;

        StringBuilder list = new StringBuilder();
        AppendListItem(list, level.cloneEnabled ? "Clone" : null);
        AppendListItem(list, level.slowEnabled ? "Slow" : null);
        AppendListItem(list, level.armorEnabled ? "Armor" : null);

        return tr
            ? $"Kullanılabilir destekler: {list}."
            : $"Available support: {list}.";
    }

    private static void AppendSentence(StringBuilder builder, string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
            return;

        if (builder.Length > 0)
            builder.Append(' ');

        builder.Append(sentence.Trim());
    }

    private static void AppendListItem(StringBuilder builder, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (builder.Length > 0)
            builder.Append(", ");

        builder.Append(value);
    }

    private static string FormatNumber(float value)
    {
        float safe = Mathf.Max(0f, value);
        return Mathf.Approximately(safe, Mathf.Round(safe))
            ? Mathf.RoundToInt(safe).ToString()
            : safe.ToString("0.#");
    }
}
