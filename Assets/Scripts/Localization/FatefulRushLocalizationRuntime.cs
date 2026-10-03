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
public sealed partial class FatefulRushLocalizationRuntime : MonoBehaviour
{
    private static FatefulRushLocalizationRuntime instance;

    private bool localizationReady;
    private bool forceRefresh = true;
    private bool lastGameplayEndedState;

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
        forceRefresh = false;
        lastGameplayEndedState = GameStateManager.IsGameplayEnded;

        RefreshSceneCache();
        ApplyThemeColorGuards();

        // Scene-authored UI can finish enabling during the next frame.
        // Do two bounded passes, then stop completely.
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
        forceRefresh = false;
        StartCoroutine(RefreshNextFrames());
    }

    private void HandleSkinChanged()
    {
        // Skin/theme changes are rare and already have a dedicated event.
        // Refresh once instead of keeping a permanent polling loop alive.
        forceRefresh = false;
        RefreshSceneCache();
        ApplyThemeColorGuards();
        ApplySkinPanels();
        ApplyMainMenus();
    }

    private void LateUpdate()
    {
        if (!localizationReady)
            return;

        // Result/death UI is created from objects already cached on scene load.
        // Refresh exactly once when the run changes from active -> ended.
        bool gameplayEnded = GameStateManager.IsGameplayEnded;

        if (gameplayEnded && !lastGameplayEndedState)
        {
            ApplyGameResults();
            ApplyDeathMessages();
            LocalizedUILayoutPolish.RequestRefresh();
        }

        lastGameplayEndedState = gameplayEnded;

        // Kept only as a one-shot compatibility escape hatch for explicit
        // RequestFullRefresh calls. There is no timer and no scene scan here.
        if (forceRefresh)
        {
            forceRefresh = false;
            ApplyEverything();
        }
    }

    public static void RequestFullRefresh(bool refreshSceneCache = false)
    {
        if (instance == null || !instance.localizationReady)
            return;

        if (refreshSceneCache)
            instance.RefreshSceneCache();

        instance.forceRefresh = false;
        instance.ApplyEverything();
    }

    public static void RequestStatsRefresh()
    {
        if (instance == null || !instance.localizationReady)
            return;

        instance.ApplyStatsPanels();
    }

    public static void RequestSkinPanelRefresh()
    {
        if (instance == null || !instance.localizationReady)
            return;

        instance.ApplySkinPanels();
        instance.ApplyThemeColorGuards();
    }

    public static void RequestMissionBriefingRefresh()
    {
        if (instance == null || !instance.localizationReady)
            return;

        instance.ApplyMissionBriefings();
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

}
