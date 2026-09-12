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

[DefaultExecutionOrder(10000)]
public sealed class FatefulRushLocalizationRuntime : MonoBehaviour
{
    private const float RefreshInterval = 0.05f;
    private const float CacheRefreshInterval = 0.75f;

    private static FatefulRushLocalizationRuntime instance;

    private float refreshTimer;
    private float cacheRefreshTimer;
    private bool localizationReady;
    private bool forceRefresh = true;

    private MainMenu[] mainMenus = Array.Empty<MainMenu>();
    private PausePanelTransition[] pausePanels = Array.Empty<PausePanelTransition>();
    private PlayerSkinPanelUI[] skinPanels = Array.Empty<PlayerSkinPanelUI>();
    private MissionBriefingPanelUI[] briefingPanels = Array.Empty<MissionBriefingPanelUI>();
    private StatsPanelUI[] statsPanels = Array.Empty<StatsPanelUI>();
    private GameResultUI[] resultUIs = Array.Empty<GameResultUI>();
    private LoseDeathMessageUI[] deathMessageUIs = Array.Empty<LoseDeathMessageUI>();
    private MenuFloatingText[] floatingTexts = Array.Empty<MenuFloatingText>();
    private NearMissStreakUI[] nearMissUIs = Array.Empty<NearMissStreakUI>();
    private TMP_Text[] sceneTexts = Array.Empty<TMP_Text>();

    private readonly Dictionary<EntityId, string> floatingKeys = new Dictionary<EntityId, string>();
    private readonly Dictionary<EntityId, string[]> floatingSourceMessages = new Dictionary<EntityId, string[]>();
    private readonly Dictionary<EntityId, string> deathMessageKeys = new Dictionary<EntityId, string>();
    private readonly Dictionary<EntityId, string> statsSources = new Dictionary<EntityId, string>();
    private readonly Dictionary<EntityId, string> statsApplied = new Dictionary<EntityId, string>();

    private static readonly Dictionary<string, FieldInfo> FieldCache =
        new Dictionary<string, FieldInfo>(StringComparer.Ordinal);

    private static readonly Dictionary<string, string> AmbientEnglishToKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "SIGNAL LOST", "ambient.signal_lost" },
            { "THREAT: UNKNOWN", "ambient.threat_unknown" },
            { "NO RETURN VECTOR", "ambient.no_return_vector" },
            { "Fate Is Closing In", "ambient.fate_is_closing_in" },
            { "The Stars Have Gone Silent", "ambient.stars_gone_silent" },
            { "Every Second Seals Your Path", "ambient.every_second_seals_path" },
            { "No Signal Reaches Home", "ambient.no_signal_reaches_home" },
            { "Something Is Following You", "ambient.something_following_you" },
            { "The Future Is Collapsing", "ambient.future_collapsing" },
            { "Time Refuses To Wait", "ambient.time_refuses_to_wait" },
            { "Your Next Move Is Final", "ambient.next_move_is_final" },
            { "The Dark Remembers Your Name", "ambient.dark_remembers_name" },
            { "Survival Was Never Promised", "ambient.survival_never_promised" },
            { "There Is No Safe Vector", "ambient.no_safe_vector" },
            { "The End Is Gaining Ground", "ambient.end_gaining_ground" },
            { "Run Before Fate Finds You", "ambient.run_before_fate" },
            { "Fate Moves Faster Than You", "ambient.fate_moves_faster" },
            { "The Countdown Has Begun", "ambient.countdown_begun" },
            { "Only Motion Keeps You Alive", "ambient.motion_keeps_alive" }
        };

    private static readonly Dictionary<string, string> DeathEnglishToKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "YOU LET IT GET TOO CLOSE.", "death.message.stalker.1" },
            { "THE STALKER FOUND ITS OPENING.", "death.message.stalker.2" },
            { "YOU COULDN'T SHAKE IT.", "death.message.stalker.3" },
            { "IT NEVER STOPPED CHASING.", "death.message.stalker.4" },
            { "THE HUNTER CAUGHT ITS PREY.", "death.message.hunter.1" },
            { "YOU COULDN'T OUTRUN THE HUNT.", "death.message.hunter.2" },
            { "THE HUNTER CLOSED THE DISTANCE.", "death.message.hunter.3" },
            { "THERE WAS NOWHERE LEFT TO RUN.", "death.message.hunter.4" },
            { "YOU CROSSED THE BLASTER'S PATH.", "death.message.blaster.1" },
            { "THE BLASTER HAD YOU LINED UP.", "death.message.blaster.2" },
            { "YOU STAYED TOO CLOSE FOR TOO LONG.", "death.message.blaster.3" },
            { "THE BLASTER WON THE STANDOFF.", "death.message.blaster.4" },
            { "ONE SHOT WAS ALL IT TOOK.", "death.message.laser_bullet.1" },
            { "YOU NEVER SAW THAT SHOT COMING.", "death.message.laser_bullet.2" },
            { "THE LASER FOUND ITS MARK.", "death.message.laser_bullet.3" },
            { "ONE HIT ENDED THE RUN.", "death.message.laser_bullet.4" },
            { "THERE WAS NO GAP TO ESCAPE.", "death.message.laser_wall.1" },
            { "THE LASER WALL CLOSED YOU IN.", "death.message.laser_wall.2" },
            { "YOU RAN OUT OF ROOM.", "death.message.laser_wall.3" },
            { "THE WALL LEFT NO WAY THROUGH.", "death.message.laser_wall.4" },
            { "THE VOID CLAIMED ANOTHER RUN.", "death.message.boss.1" },
            { "THE BOSS OVERWHELMED YOU.", "death.message.boss.2" },
            { "YOU COULDN'T SURVIVE ITS ATTACK.", "death.message.boss.3" },
            { "THE BOSS ENDED YOUR RUN.", "death.message.boss.4" },
            { "YOU UNDERESTIMATED THE THREAT.", "death.message.mini_boss.1" },
            { "THE MINI-BOSS CAUGHT YOU OFF GUARD.", "death.message.mini_boss.2" },
            { "YOU DIDN'T CLEAR THE DANGER ZONE.", "death.message.mini_boss.3" },
            { "THE THREAT WAS SMALLER. NOT WEAKER.", "death.message.mini_boss.4" },
            { "YOU WERE CAUGHT IN THE BLAST.", "death.message.space_bomb.1" },
            { "THE EXPLOSION LEFT NO ESCAPE.", "death.message.space_bomb.2" },
            { "YOU STAYED TOO CLOSE TO THE BOMB.", "death.message.space_bomb.3" },
            { "THE BLAST RADIUS GOT YOU.", "death.message.space_bomb.4" },
            { "TIME RAN OUT.", "death.message.time_expired.1" },
            { "YOU NEEDED A FEW MORE SECONDS.", "death.message.time_expired.2" },
            { "THE CLOCK WON THIS ROUND.", "death.message.time_expired.3" },
            { "YOU RAN OUT OF TIME.", "death.message.time_expired.4" },
            { "THE RUN ENDED HERE.", "death.message.unknown.1" },
            { "SOMETHING WENT VERY WRONG.", "death.message.unknown.2" },
            { "THE VOID HAD OTHER PLANS.", "death.message.unknown.3" },
            { "THIS RUN WASN'T MEANT TO LAST.", "death.message.unknown.4" }
        };

    private static readonly Dictionary<string, string> StatsLabelToKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "GENERAL", "stats.section.general" },
            { "PROGRESSION", "stats.section.progression" },
            { "MISSION MODES", "stats.section.mission_modes" },
            { "PERFORMANCE", "stats.section.performance" },
            { "NEAR MISS", "stats.section.near_miss" },
            { "COLLECTIBLES", "stats.section.collectibles" },
            { "ABILITIES & POWER-UPS", "stats.section.abilities" },
            { "DANGER MASTERY", "stats.section.danger_mastery" },
            { "DEATH ANALYSIS", "stats.section.death_analysis" },
            { "LEVEL BEST TIMES", "stats.section.level_best_times" },
            { "Total Runs", "stats.total_runs" },
            { "Total Wins", "stats.total_wins" },
            { "Total Deaths", "stats.total_deaths" },
            { "Win Rate", "stats.win_rate" },
            { "Current Win Streak", "stats.current_win_streak" },
            { "Best Win Streak", "stats.best_win_streak" },
            { "Total Play Time", "stats.total_play_time" },
            { "Average Run Time", "stats.average_run_time" },
            { "Longest Run", "stats.longest_run" },
            { "Levels Completed", "stats.levels_completed" },
            { "Completion", "stats.completion" },
            { "Highest Level Completed", "stats.highest_level_completed" },
            { "Skins Unlocked", "stats.skins_unlocked" },
            { "Score Missions", "stats.score_missions" },
            { "Survival Missions", "stats.survival_missions" },
            { "Timed Score Missions", "stats.timed_score_missions" },
            { "Total Score Earned", "stats.total_score_earned" },
            { "Best Run Score", "stats.best_run_score" },
            { "Average Score / Score Run", "stats.average_score_per_run" },
            { "Highest Combo", "stats.highest_combo" },
            { "Longest Combo Chain", "stats.longest_combo_chain" },
            { "6x Combo Reached", "stats.combo_6x_reached" },
            { "Combo Bonus Points", "stats.combo_bonus_points" },
            { "Total Near Misses", "stats.total_near_misses" },
            { "Best Near Miss Streak", "stats.best_near_miss_streak" },
            { "Total Coins", "stats.total_coins" },
            { "Normal Coins", "stats.normal_coins" },
            { "Gold Coins", "stats.gold_coins" },
            { "Rare Coins", "stats.rare_coins" },
            { "Most Coins in a Run", "stats.most_coins_run" },
            { "Base Coin Value Collected", "stats.base_coin_value" },
            { "Magnet Coins", "stats.magnet_coins" },
            { "Dash Uses", "stats.dash_uses" },
            { "Clone Uses", "stats.clone_uses" },
            { "Slow Buff Uses", "stats.slow_uses" },
            { "Armor Buff Uses", "stats.armor_uses" },
            { "Armor Saves", "stats.armor_saves" },
            { "Armor Save Rate", "stats.armor_save_rate" },
            { "Beacons Destroyed", "stats.beacons_destroyed" },
            { "Hunters Stunned", "stats.hunters_stunned" },
            { "Boss Encounters", "stats.boss_encounters" },
            { "Boss Splits Triggered", "stats.boss_splits" },
            { "Boss AOE Evades", "stats.boss_aoe_evades" },
            { "Mini-Boss AOE Evades", "stats.mini_boss_aoe_evades" },
            { "Nemesis", "stats.nemesis" },
            { "Stalker Deaths", "stats.death_stalker" },
            { "Hunter Deaths", "stats.death_hunter" },
            { "Blaster Deaths", "stats.death_blaster" },
            { "Boss Deaths", "stats.death_boss" },
            { "Laser Bullet Deaths", "stats.death_laser_bullet" },
            { "Laser Wall Deaths", "stats.death_laser_wall" },
            { "Mini-Boss Deaths", "stats.death_mini_boss" },
            { "Space Bomb Deaths", "stats.death_space_bomb" },
            { "Time Expired", "stats.death_time_expired" },
            { "Unknown Deaths", "stats.death_unknown" }
        };


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("Fateful Rush Localization Runtime");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<FatefulRushLocalizationRuntime>();
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
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        PlayerSkinCatalog.SelectedSkinChanged += HandleSkinChanged;

        // Cache scene objects immediately. This lets runtime-created intro UI
        // be localized before Unity Localization finishes its async startup.
        RefreshSceneCache();
    }

    private IEnumerator Start()
    {
        var operation = LocalizationSettings.InitializationOperation;

        while (!operation.IsDone)
            yield return null;

        localizationReady = true;
        SyncLanguagePrefs(LocalizationSettings.SelectedLocale);
        RefreshSceneCache();
        ApplyEverything();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        PlayerSkinCatalog.SelectedSkinChanged -= HandleSkinChanged;
        instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        forceRefresh = true;
        cacheRefreshTimer = CacheRefreshInterval;

        RefreshSceneCache();
        ApplyThemeColorGuards();

        StartCoroutine(RefreshNextFrames());
    }

    private IEnumerator RefreshNextFrames()
    {
        yield return null;
        RefreshSceneCache();
        ApplyEverything();
        yield return null;
        ApplyEverything();
    }

    private void HandleLocaleChanged(Locale locale)
    {
        FatefulRushLocalization.ClearCache();
        SyncLanguagePrefs(locale);
        forceRefresh = true;
        StartCoroutine(RefreshNextFrames());
    }

    private void HandleSkinChanged()
    {
        forceRefresh = true;
    }

    private void LateUpdate()
    {
        refreshTimer += Time.unscaledDeltaTime;
        cacheRefreshTimer += Time.unscaledDeltaTime;

        if (cacheRefreshTimer >= CacheRefreshInterval)
        {
            cacheRefreshTimer = 0f;
            RefreshSceneCache();
        }

        // Theme colors are independent from String Table readiness.
        ApplyThemeColorGuards();

        if (!localizationReady)
            return;

        if (!forceRefresh && refreshTimer < RefreshInterval)
            return;

        refreshTimer = 0f;
        forceRefresh = false;
        ApplyEverything();
    }

    private static void SyncLanguagePrefs(Locale locale)
    {
        if (locale == null)
            return;

        string code = locale.Identifier.Code;

        if (string.IsNullOrWhiteSpace(code))
            return;

        PlayerPrefs.SetString("SelectedLocaleCode", code);

        string legacyCode = code;
        int separatorIndex = legacyCode.IndexOf('-');
        if (separatorIndex > 0)
            legacyCode = legacyCode.Substring(0, separatorIndex);

        PlayerPrefs.SetString("Language", legacyCode.ToUpperInvariant());
        PlayerPrefs.Save();
    }

    private void RefreshSceneCache()
    {
        mainMenus = FindSceneObjects<MainMenu>();
        pausePanels = FindSceneObjects<PausePanelTransition>();
        skinPanels = FindSceneObjects<PlayerSkinPanelUI>();
        briefingPanels = FindSceneObjects<MissionBriefingPanelUI>();
        statsPanels = FindSceneObjects<StatsPanelUI>();
        resultUIs = FindSceneObjects<GameResultUI>();
        deathMessageUIs = FindSceneObjects<LoseDeathMessageUI>();
        floatingTexts = FindSceneObjects<MenuFloatingText>();
        nearMissUIs = FindSceneObjects<NearMissStreakUI>();
        sceneTexts = FindSceneObjects<TMP_Text>();
    }

    private void ApplyEverything()
    {
        ApplyStaticRepairs();
        ApplyMainMenus();
        ApplyPauseObjectives();
        ApplySkinPanels();
        ApplyMissionBriefings();
        ApplyStatsPanels();
        ApplyGameResults();
        ApplyDeathMessages();
        ApplyFloatingTexts();
        ApplyGameplayHUD();
        ApplyNearMissTexts();

        // Final visual pass. Localization may rewrite TMP rich text/content,
        // but skin-theme colors and the Continue level's own NearStars color
        // must be authoritative for the rendered frame.
        ApplyThemeColorGuards();
    }

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

            if (path.IndexOf("MainMenuPanel/Header/TitleText", StringComparison.OrdinalIgnoreCase) >= 0)
                ApplyThemedMainTitle(text);

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

    private static void ApplyThemedMainTitle(TMP_Text title)
    {
        if (title == null)
            return;

        Color theme = Color.white;
        PlayerSkinCatalog catalog = PlayerSkinCatalog.LoadedInstance;
        if (catalog != null)
            theme = catalog.GetSelectedUIThemeColor();

        theme.a = 1f;
        string hex = ColorUtility.ToHtmlStringRGB(theme);
        SetText(title, $"FATEFUL <color=#{hex}>RUSH</color>");
    }

    private void ApplyMainMenus()
    {
        for (int i = 0; i < mainMenus.Length; i++)
        {
            MainMenu menu = mainMenus[i];
            if (menu == null)
                continue;

            TMP_Text signal = GetPrivate<TMP_Text>(menu, "signalStatusText");
            if (signal != null)
            {
                string key = SignalStatusState.IsStable
                    ? "menu.signal_stable"
                    : "menu.signal_unstable";

                string fallback = SignalStatusState.IsStable
                    ? "SIGNAL // STABLE"
                    : "SIGNAL // UNSTABLE";

                SetText(signal, FatefulRushLocalization.Text(key, fallback));
            }

            TMP_Text continueText = GetPrivate<TMP_Text>(menu, "continueLevelText");
            LevelConfig target = GetPrivate<LevelConfig>(menu, "continueTargetLevel");

            if (continueText != null && target != null && continueText.gameObject.activeSelf)
            {
                SetText(
                    continueText,
                    FatefulRushLocalization.Text("menu.level", "LEVEL {0}", target.levelNumber)
                );

                // This label intentionally previews the target level's own
                // NearStars color. Localization/theme refreshes must never
                // replace it with the selected skin UI theme color.
                menu.RefreshContinueLevelColor();
            }
        }
    }

    private void ApplyPauseObjectives()
    {
        LevelConfig level = ResolveCurrentLevel();
        if (level == null)
            return;

        for (int i = 0; i < pausePanels.Length; i++)
        {
            PausePanelTransition panel = pausePanels[i];
            if (panel == null)
                continue;

            TMP_Text title = GetPrivate<TMP_Text>(panel, "objectiveTitleText");
            TMP_Text value = GetPrivate<TMP_Text>(panel, "objectiveValueText");

            if (title != null)
                SetText(title, FatefulRushLocalization.Text("common.objective", "OBJECTIVE"));

            if (value != null)
                SetText(value, BuildPauseObjective(level));
        }
    }

    private static string BuildPauseObjective(LevelConfig level)
    {
        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                return FatefulRushLocalization.Text(
                    "objective.pause_reach_score",
                    "REACH SCORE: {0}",
                    level.SafeWinScore
                );

            case WinConditionType.SurviveTime:
                return FatefulRushLocalization.Text(
                    "objective.pause_survive",
                    "SURVIVE: {0}",
                    FormatSeconds(level.SafeTimeLimit)
                );

            case WinConditionType.ReachScoreWithinTime:
                return FatefulRushLocalization.Text(
                    "objective.pause_reach_score_time",
                    "REACH SCORE: {0}  ·  {1}",
                    level.SafeWinScore,
                    FormatSeconds(level.SafeTimeLimit)
                );

            default:
                return FatefulRushLocalization.Text(
                    "objective.complete_mission",
                    "COMPLETE THE MISSION"
                );
        }
    }

    private void ApplySkinPanels()
    {
        for (int i = 0; i < skinPanels.Length; i++)
        {
            PlayerSkinPanelUI panel = skinPanels[i];
            if (panel == null)
                continue;

            PlayerSkinCatalog catalog = GetPrivate<PlayerSkinCatalog>(panel, "skinCatalog");
            if (catalog == null || catalog.Skins == null || catalog.Skins.Count == 0)
                continue;

            int index = GetPrivate<int>(panel, "currentSkinIndex");
            index = Mathf.Clamp(index, 0, catalog.Skins.Count - 1);
            PlayerSkinCatalog.SkinEntry skin = catalog.Skins[index];
            if (skin == null)
                continue;

            bool unlocked = catalog.IsUnlocked(skin);
            bool selected = catalog.IsSelected(skin);
            string skinKey = GetSkinKey(skin.id);

            TMP_Text name = GetPrivate<TMP_Text>(panel, "skinNameText");
            TMP_Text status = GetPrivate<TMP_Text>(panel, "skinStatusText");
            TMP_Text requirement = GetPrivate<TMP_Text>(panel, "skinRequirementText");
            TMP_Text equip = GetPrivate<TMP_Text>(panel, "equipButtonText");

            if (name != null)
            {
                string fallbackName = string.IsNullOrWhiteSpace(skin.displayName)
                    ? skin.id.ToUpperInvariant()
                    : skin.displayName.ToUpperInvariant();

                SetText(name, FatefulRushLocalization.Text(skinKey, fallbackName));
            }

            if (status != null)
            {
                string statusKey = selected
                    ? "skin.status.equipped"
                    : unlocked
                        ? "skin.status.available"
                        : "skin.status.locked";

                string fallback = selected ? "EQUIPPED" : unlocked ? "AVAILABLE" : "LOCKED";
                SetText(status, FatefulRushLocalization.Text(statusKey, fallback));
            }

            if (requirement != null)
            {
                string requirementText = unlocked
                    ? string.Empty
                    : FatefulRushLocalization.Text(
                        "skin.requirement.complete_level_to_unlock",
                        "COMPLETE LEVEL {0} TO UNLOCK",
                        skin.requiredCompletedLevel
                    );

                SetText(requirement, requirementText);
            }

            if (equip != null)
            {
                string equipKey = selected
                    ? "skin.status.equipped"
                    : unlocked
                        ? "skin.status.equip"
                        : "skin.status.locked";

                string fallback = selected ? "EQUIPPED" : unlocked ? "EQUIP" : "LOCKED";
                SetText(equip, FatefulRushLocalization.Text(equipKey, fallback));
            }
        }
    }

    private void ApplyMissionBriefings()
    {
        for (int i = 0; i < briefingPanels.Length; i++)
        {
            MissionBriefingPanelUI panel = briefingPanels[i];
            if (panel == null)
                continue;

            LevelConfig level = GetPrivate<LevelConfig>(panel, "selectedLevel");
            if (level == null)
                continue;

            TMP_Text title = GetPrivate<TMP_Text>(panel, "levelTitleText");
            TMP_Text mode = GetPrivate<TMP_Text>(panel, "modeText");
            TMP_Text description = GetPrivate<TMP_Text>(panel, "pageDescriptionText");

            string originalName = string.IsNullOrWhiteSpace(level.levelName)
                ? string.Empty
                : level.levelName.Trim();

            string localizedName = FatefulRushLocalization.LevelName(level.levelNumber, originalName);

            if (title != null)
            {
                if (string.IsNullOrWhiteSpace(localizedName))
                {
                    SetText(title, FatefulRushLocalization.Text("menu.level", "LEVEL {0}", level.levelNumber));
                }
                else
                {
                    SetText(
                        title,
                        FatefulRushLocalization.Text(
                            "briefing.level_title",
                            "LEVEL {0} — {1}",
                            level.levelNumber,
                            localizedName.ToUpperInvariant()
                        )
                    );
                }
            }

            if (mode != null)
                SetText(mode, BuildBriefingMode(level));

            if (description != null)
                SetText(description, BuildBriefingDescription(level));
        }
    }

    private static string BuildBriefingMode(LevelConfig level)
    {
        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                return FatefulRushLocalization.Text("briefing.mode.score", "SCORE MISSION");
            case WinConditionType.SurviveTime:
                return FatefulRushLocalization.Text("briefing.mode.survival", "SURVIVAL MISSION");
            case WinConditionType.ReachScoreWithinTime:
                return FatefulRushLocalization.Text("briefing.mode.timed_score", "TIMED SCORE MISSION");
            default:
                return FatefulRushLocalization.Text("briefing.mode.mission", "MISSION");
        }
    }

    private static string BuildBriefingDescription(LevelConfig level)
    {
        string objective;

        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                objective = FatefulRushLocalization.Text(
                    "briefing.objective.score",
                    "Collect {0} points to complete the mission.",
                    level.SafeWinScore
                );
                break;

            case WinConditionType.SurviveTime:
                objective = FatefulRushLocalization.Text(
                    "briefing.objective.survive",
                    "Stay alive for {0} seconds. Coins and combos are disabled in this mission.",
                    Mathf.RoundToInt(level.SafeTimeLimit)
                );
                break;

            case WinConditionType.ReachScoreWithinTime:
                objective = FatefulRushLocalization.Text(
                    "briefing.objective.timed_score",
                    "Collect {0} points before the {1} seconds countdown reaches zero.",
                    level.SafeWinScore,
                    Mathf.RoundToInt(level.SafeTimeLimit)
                );
                break;

            default:
                objective = FatefulRushLocalization.Text(
                    "briefing.objective.default",
                    "Complete the mission objective."
                );
                break;
        }

        string fallbackTip = string.Empty;
        if (level.briefingPages != null && level.briefingPages.Length > 0)
            fallbackTip = level.briefingPages[0] ?? string.Empty;

        string tip = FatefulRushLocalization.BriefingTip(level.levelNumber, fallbackTip);

        if (string.IsNullOrWhiteSpace(tip))
            return objective;

        return objective + "\n\n" + tip.Trim();
    }

    private void ApplyStatsPanels()
    {
        for (int i = 0; i < statsPanels.Length; i++)
        {
            StatsPanelUI panel = statsPanels[i];
            if (panel == null)
                continue;

            TMP_Text statsText = GetPrivate<TMP_Text>(panel, "statsText");
            if (statsText == null || string.IsNullOrWhiteSpace(statsText.text))
                continue;

            EntityId id = statsText.GetEntityId();
            string current = statsText.text;

            if (!statsApplied.TryGetValue(id, out string lastApplied) ||
                !string.Equals(current, lastApplied, StringComparison.Ordinal))
            {
                statsSources[id] = current;
            }

            if (!statsSources.TryGetValue(id, out string source) || string.IsNullOrWhiteSpace(source))
                continue;

            string localized = TranslateStats(source);
            SetText(statsText, localized);
            statsApplied[id] = localized;
        }
    }

    private static string TranslateStats(string source)
    {
        string[] lines = source.Replace("\r\n", "\n").Split('\n');
        StringBuilder builder = new StringBuilder(source.Length + 128);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string translated = TranslateStatsLine(line);
            builder.Append(translated);
            if (i < lines.Length - 1)
                builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string TranslateStatsLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return line;

        string trimmed = line.Trim();

        if (StatsLabelToKey.TryGetValue(trimmed, out string exactKey))
            return FatefulRushLocalization.Text(exactKey, trimmed);

        if (trimmed.Equals("No recorded death causes yet.", StringComparison.OrdinalIgnoreCase))
            return FatefulRushLocalization.Text("stats.no_death_causes", trimmed);

        if (trimmed.Equals("No best-time records yet.", StringComparison.OrdinalIgnoreCase))
            return FatefulRushLocalization.Text("stats.no_best_times", trimmed);

        if (trimmed.StartsWith("Level ", StringComparison.OrdinalIgnoreCase))
        {
            int colon = trimmed.IndexOf(':');
            if (colon > 6)
            {
                string levelNumber = trimmed.Substring(6, colon - 6).Trim();
                string rest = trimmed.Substring(colon + 1).Trim();
                return FatefulRushLocalization.Text("stats.level", "Level") + " " + levelNumber + ": " + rest;
            }
        }

        if (trimmed.StartsWith("Dev Room:", StringComparison.OrdinalIgnoreCase))
        {
            string rest = trimmed.Substring("Dev Room:".Length).Trim();
            return FatefulRushLocalization.Text("stats.dev_room", "Dev Room") + ": " + rest;
        }

        int separator = line.IndexOf(':');
        if (separator > 0)
        {
            string label = line.Substring(0, separator).Trim();
            string value = line.Substring(separator + 1).TrimStart();

            if (StatsLabelToKey.TryGetValue(label, out string labelKey))
            {
                value = TranslateStatsValue(value);
                return FatefulRushLocalization.Text(labelKey, label) + ": " + value;
            }
        }

        return line;
    }

    private static string TranslateStatsValue(string value)
    {
        if (string.Equals(value, "NONE", StringComparison.OrdinalIgnoreCase))
            return FatefulRushLocalization.Text("stats.none", "NONE");

        value = TranslateDeathCauseValue(value);

        value = Regex.Replace(
            value,
            @"\bwins\b",
            FatefulRushLocalization.Text("stats.unit.wins", "wins"),
            RegexOptions.IgnoreCase
        );

        value = Regex.Replace(
            value,
            @"\bcoins\b",
            FatefulRushLocalization.Text("stats.unit.coins", "coins"),
            RegexOptions.IgnoreCase
        );

        value = Regex.Replace(value, @"(?<=\d)h\b", FatefulRushLocalization.Text("stats.unit.hours_short", "h"));
        value = Regex.Replace(value, @"(?<=\d)m\b", FatefulRushLocalization.Text("stats.unit.minutes_short", "m"));
        value = Regex.Replace(value, @"(?<=\d)s\b", FatefulRushLocalization.Text("stats.unit.seconds_short", "s"));

        return value;
    }

    private static string TranslateDeathCauseValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        string[] causes =
        {
            "LASER BULLET", "LASER WALL", "MINI BOSS", "SPACE BOMB",
            "TIME EXPIRED", "STALKER", "HUNTER", "BLASTER", "BOSS", "UNKNOWN"
        };

        for (int i = 0; i < causes.Length; i++)
        {
            string cause = causes[i];
            if (!value.StartsWith(cause, StringComparison.OrdinalIgnoreCase))
                continue;

            string suffix = value.Substring(cause.Length);
            return LocalizeDeathCause(cause) + suffix;
        }

        return value;
    }

    private void ApplyGameResults()
    {
        LevelConfig level = ResolveCurrentLevel();

        for (int i = 0; i < resultUIs.Length; i++)
        {
            GameResultUI ui = resultUIs[i];
            if (ui == null)
                continue;

            TMP_Text destroyedBy = GetPrivate<TMP_Text>(ui, "destroyedByText");
            if (destroyedBy != null && destroyedBy.gameObject.activeInHierarchy)
                SetText(destroyedBy, LocalizeDeathCause(LastDeathInfo.Cause));

            GameObject winUI = GetPrivate<GameObject>(ui, "winUI");
            GameObject loseUI = GetPrivate<GameObject>(ui, "loseUI");
            TMP_Text winTimeLabel = GetPrivate<TMP_Text>(ui, "winTimeLabel");
            TMP_Text winTimeValue = GetPrivate<TMP_Text>(ui, "winTimeValue");
            TMP_Text loseSurvivedLabel = GetPrivate<TMP_Text>(ui, "loseSurvivedLabel");

            bool isSurviveTime =
                level != null &&
                level.winCondition == WinConditionType.SurviveTime;

            // GameResultUI restores its Inspector-authored English defaults
            // when the result panel opens. Re-localize those runtime labels.
            if (!isSurviveTime)
            {
                if (winTimeLabel != null)
                {
                    SetText(
                        winTimeLabel,
                        FatefulRushLocalization.Text(
                            "scene.gamescene.game_0.ui_3.canvas_0.resultsuicontroller_7.resultspanel_0.winui_2.timelabel_4",
                            "TIME"
                        )
                    );
                }

                if (loseSurvivedLabel != null)
                {
                    SetText(
                        loseSurvivedLabel,
                        FatefulRushLocalization.Text(
                            "scene.gamescene.game_0.ui_3.canvas_0.resultsuicontroller_7.resultspanel_0.loseui_3.survivedlabel_6",
                            "SURVIVED"
                        )
                    );
                }
            }
            else
            {
                if (winUI != null && winUI.activeInHierarchy && winTimeValue != null)
                {
                    SetText(
                        winTimeValue,
                        FatefulRushLocalization.Text(
                            "result.you_survived",
                            "YOU SURVIVED"
                        )
                    );
                }

                if (loseUI != null && loseUI.activeInHierarchy && loseSurvivedLabel != null)
                {
                    SetText(
                        loseSurvivedLabel,
                        FatefulRushLocalization.Text(
                            "result.you_survived_for",
                            "YOU SURVIVED FOR"
                        )
                    );
                }
            }

            TMP_Text unlockedSkinName = GetPrivate<TMP_Text>(ui, "unlockedSkinNameText");
            if (unlockedSkinName != null && unlockedSkinName.gameObject.activeInHierarchy)
                SetText(unlockedSkinName, LocalizeSkinNameFromText(unlockedSkinName.text));
        }
    }

    private void ApplyDeathMessages()
    {
        for (int i = 0; i < deathMessageUIs.Length; i++)
        {
            LoseDeathMessageUI ui = deathMessageUIs[i];
            if (ui == null)
                continue;

            TMP_Text text = GetPrivate<TMP_Text>(ui, "messageText");
            if (text == null || string.IsNullOrWhiteSpace(text.text))
                continue;

            EntityId id = text.GetEntityId();
            string current = text.text.Trim();

            if (DeathEnglishToKey.TryGetValue(current, out string detectedKey))
                deathMessageKeys[id] = detectedKey;

            if (!deathMessageKeys.TryGetValue(id, out string key))
                continue;

            string englishFallback = current;
            foreach (KeyValuePair<string, string> pair in DeathEnglishToKey)
            {
                if (pair.Value == key)
                {
                    englishFallback = pair.Key;
                    break;
                }
            }

            SetText(text, FatefulRushLocalization.Text(key, englishFallback));
        }
    }

    private void ApplyFloatingTexts()
    {
        for (int i = 0; i < floatingTexts.Length; i++)
        {
            MenuFloatingText floating = floatingTexts[i];
            if (floating == null || !floating.IsInitialized)
                continue;

            EntityId id = floating.GetEntityId();

            if (!floatingSourceMessages.TryGetValue(id, out string[] sourceMessages))
            {
                string[] serializedMessages = GetPrivate<string[]>(floating, "messages");
                if (serializedMessages != null && serializedMessages.Length > 0)
                {
                    sourceMessages = (string[])serializedMessages.Clone();
                    floatingSourceMessages[id] = sourceMessages;
                }
            }

            // Keep MenuFloatingText's own cycle localized too. Otherwise the
            // component would briefly write its original English array every
            // time it advances to a new message.
            List<string> usableMessages = GetPrivate<List<string>>(floating, "usableMessages");
            if (usableMessages != null && sourceMessages != null)
            {
                usableMessages.Clear();

                for (int messageIndex = 0; messageIndex < sourceMessages.Length; messageIndex++)
                {
                    string english = sourceMessages[messageIndex];
                    if (string.IsNullOrWhiteSpace(english))
                        continue;

                    if (AmbientEnglishToKey.TryGetValue(english.Trim(), out string messageKey))
                        usableMessages.Add(FatefulRushLocalization.Text(messageKey, english.Trim()));
                    else
                        usableMessages.Add(english.Trim());
                }
            }

            string stable = floating.StableText;
            string detectedKey = null;

            bool randomOrder = GetPrivate<bool>(floating, "randomOrder");
            int activeIndex = randomOrder
                ? GetPrivate<int>(floating, "previousRandomIndex")
                : GetPrivate<int>(floating, "currentMessageIndex");

            if (sourceMessages != null &&
                activeIndex >= 0 &&
                activeIndex < sourceMessages.Length &&
                AmbientEnglishToKey.TryGetValue(sourceMessages[activeIndex].Trim(), out string indexedKey))
            {
                detectedKey = indexedKey;
            }
            else if (AmbientEnglishToKey.TryGetValue(stable, out string stableKey))
            {
                detectedKey = stableKey;
            }

            if (!string.IsNullOrEmpty(detectedKey))
                floatingKeys[id] = detectedKey;

            if (!floatingKeys.TryGetValue(id, out string key))
                continue;

            string fallback = stable;
            foreach (KeyValuePair<string, string> pair in AmbientEnglishToKey)
            {
                if (pair.Value == key)
                {
                    fallback = pair.Key;
                    break;
                }
            }

            floating.SetStableText(FatefulRushLocalization.Text(key, fallback));
        }
    }

    private void ApplyGameplayHUD()
    {
        PlayerCoinCollector collector = PlayerCoinCollector.Instance;
        if (collector != null && collector.scoreText != null)
        {
            SetText(
                collector.scoreText,
                FatefulRushLocalization.Text("hud.score", "SCORE: {0}", collector.Score)
            );
        }
    }

    private void ApplyNearMissTexts()
    {
        for (int i = 0; i < nearMissUIs.Length; i++)
        {
            NearMissStreakUI ui = nearMissUIs[i];
            if (ui == null)
                continue;

            TMP_Text text = GetPrivate<TMP_Text>(ui, "text");
            if (text == null || string.IsNullOrWhiteSpace(text.text))
                continue;

            Match match = Regex.Match(text.text, @"x\s*(\d+)", RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;

            if (int.TryParse(match.Groups[1].Value, out int streak))
                SetText(text, FatefulRushLocalization.Text("hud.near_miss", "NEAR MISS  x{0}", streak));
        }
    }

    private void ApplyThemeColorGuards()
    {
        PlayerSkinCatalog catalog =
            PlayerSkinCatalog.LoadedInstance;

        if (catalog == null)
        {
            PlayerSkinCatalog[] catalogs =
                Resources.FindObjectsOfTypeAll<PlayerSkinCatalog>();

            if (catalogs != null && catalogs.Length > 0)
                catalog = catalogs[0];
        }

        if (catalog == null)
            return;

        Color themeColor =
            catalog.GetSelectedUIThemeColor();

        themeColor.r = Mathf.Clamp01(themeColor.r);
        themeColor.g = Mathf.Clamp01(themeColor.g);
        themeColor.b = Mathf.Clamp01(themeColor.b);
        themeColor.a = 1f;

        // MenuFloatingText writes TMP color every Update, so update the
        // component's cached theme color instead of only changing TMP.color.
        for (int i = 0; i < floatingTexts.Length; i++)
        {
            MenuFloatingText floating = floatingTexts[i];

            if (floating != null)
                floating.SetThemeColor(themeColor);
        }

        // Footer status must always use the selected skin theme, regardless
        // of locale and regardless of the stable/unstable text value.
        for (int i = 0; i < mainMenus.Length; i++)
        {
            MainMenu menu = mainMenus[i];
            if (menu == null)
                continue;

            TMP_Text signal =
                GetPrivate<TMP_Text>(
                    menu,
                    "signalStatusText"
                );

            if (signal == null)
                continue;

            Color current = signal.color;

            signal.color = new Color(
                themeColor.r,
                themeColor.g,
                themeColor.b,
                current.a
            );
        }

        // Version text and Main Menu ambient/static accent texts are all
        // authored as skin-theme elements. Match them by hierarchy/object
        // identity instead of their localized string content.
        for (int i = 0; i < sceneTexts.Length; i++)
        {
            TMP_Text text = sceneTexts[i];
            if (text == null)
                continue;

            string path = BuildPath(text.transform);
            string objectName = text.gameObject.name;

            bool isMainMenuThemeText =
                path.IndexOf(
                    "MainMenuPanel/AmbientTexts",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0 ||
                (
                    path.IndexOf(
                        "/Footer/",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0 &&
                    (
                        objectName.IndexOf(
                            "StatusText",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0 ||
                        objectName.IndexOf(
                            "Version",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0
                    )
                );

            if (!isMainMenuThemeText)
                continue;

            // Animated floating labels already received SetThemeColor above.
            if (text.GetComponent<MenuFloatingText>() != null)
                continue;

            Color current = text.color;

            text.color = new Color(
                themeColor.r,
                themeColor.g,
                themeColor.b,
                current.a
            );
        }

        // Absolute final guard: Continue level label is NOT a skin-theme
        // element. Reapply its target LevelConfig NearStars color after all
        // theme work, including locale changes and late UI refreshes.
        for (int i = 0; i < mainMenus.Length; i++)
        {
            MainMenu menu = mainMenus[i];
            if (menu != null)
                menu.RefreshContinueLevelColor();
        }
    }

    private static string LocalizeDeathCause(string cause)
    {
        if (string.IsNullOrWhiteSpace(cause))
            return "UNKNOWN";

        string normalized =
            cause.Trim().ToUpperInvariant();

        // Sadece TIME EXPIRED dil ile değişsin.
        if (normalized == "TIME EXPIRED")
        {
            return FatefulRushLocalization.IsTurkish
                ? "SÜRE DOLDU"
                : "TIME EXPIRED";
        }

        // Diğer bütün death cause isimleri canonical İngilizce kalsın.
        return normalized;
    }

    private static string LocalizeSkinNameFromText(string current)
    {
        if (string.IsNullOrWhiteSpace(current))
            return current;

        string normalized = current.Trim().ToLowerInvariant();
        string key = null;

        if (normalized.Contains("white") || normalized.Contains("beyaz")) key = "skin.name.white";
        else if (normalized.Contains("blue") || normalized.Contains("mavi")) key = "skin.name.blue";
        else if (normalized.Contains("orange") || normalized.Contains("turuncu")) key = "skin.name.orange";
        else if (normalized.Contains("red") || normalized.Contains("kırmızı")) key = "skin.name.red";
        else if (normalized.Contains("green") || normalized.Contains("yeşil")) key = "skin.name.green";
        else if (normalized.Contains("pink") || normalized.Contains("pembe")) key = "skin.name.pink";
        else if (normalized.Contains("yellow") || normalized.Contains("sarı")) key = "skin.name.yellow";
        else if (normalized.Contains("cyan") || normalized.Contains("camgöbeği")) key = "skin.name.cyan";
        else if (normalized.Contains("purple") || normalized.Contains("mor")) key = "skin.name.purple";
        else if (normalized.Contains("dark") || normalized.Contains("karanlık")) key = "skin.name.dark";
        else if (normalized.Contains("gold") || normalized.Contains("altın")) key = "skin.name.golden";

        return key == null ? current : FatefulRushLocalization.Text(key, current.ToUpperInvariant());
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
