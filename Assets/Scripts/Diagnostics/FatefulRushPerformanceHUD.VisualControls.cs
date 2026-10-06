#if FATEFULRUSH_DIAGNOSTICS
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Part of the existing HUD, not an extra scene component. No gameplay objects,
// colliders, scores, spawn rules, frame caps or performance policies are changed.
public sealed partial class FatefulRushPerformanceHUD
{
    sealed class VisualGroup
    {
        public readonly string name;
        public bool off;
        public TextMeshProUGUI buttonText;
        public readonly List<Renderer> renderers = new List<Renderer>();
        public readonly List<bool> rendererStates = new List<bool>();
        public readonly List<Behaviour> effects = new List<Behaviour>();
        public readonly List<bool> effectStates = new List<bool>();
        public readonly List<Image> art = new List<Image>();
        public readonly List<bool> artStates = new List<bool>();
        public VisualGroup(string value) { name = value; }
    }
    readonly VisualGroup[] visualGroups = {
        new VisualGroup("LIGHT"), new VisualGroup("PLANETS"),
        new VisualGroup("ASTEROIDS"), new VisualGroup("STARS"),
        new VisualGroup("COMBO"), new VisualGroup("OTHER FX"),
        new VisualGroup("PANEL ART"), new VisualGroup("BORDER"), new VisualGroup("OVERLAY")
    };
    GameObject testPanel;
    TextMeshProUGUI adsCaption, playCaption;
    readonly List<GameObject> watchedPanels = new List<GameObject>();
    Coroutine bindVisualRoutine;
    readonly List<Renderer> worldRenderers = new List<Renderer>();
    readonly List<bool> worldStates = new List<bool>();
    bool worldOff;
    TextMeshProUGUI worldCaption;
    const double UiHideSeconds = 15.0;
    readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    readonly List<bool> canvasDrawingStates = new List<bool>();
    readonly List<GraphicRaycaster> hiddenUiRaycasters = new List<GraphicRaycaster>();
    readonly List<bool> uiRaycasterStates = new List<bool>();
    bool uiDrawHidden;
    double uiHideDeadline;
    int uiHideRun;

    void BeginUiDrawingTest()
    {
        if (uiDrawHidden) return;
        string scene = SceneManager.GetActiveScene().name;
        if (scene != "MainMenu" && !(scene == "GameScene" && Time.timeScale <= 0f))
        {
            logError = "UI test: open MainMenu or Pause/Results first.";
            pendingMark += " UI_HIDE_BLOCKED_ACTIVE_GAMEPLAY";
            return;
        }
        // Avoid a native keyboard being left over the blank test screen.
        if (DevCheatConsole.IsOpen) DevCheatConsole.Instance.CloseConsole();
        testPanel.SetActive(false);
        hiddenCanvases.Clear(); canvasDrawingStates.Clear();
        hiddenUiRaycasters.Clear(); uiRaycasterStates.Clear();
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas == null || !canvas.gameObject.scene.IsValid()) continue;
            hiddenCanvases.Add(canvas);
            canvasDrawingStates.Add(canvas.enabled);
        }
        foreach (GraphicRaycaster raycaster in Object.FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (raycaster == null || !raycaster.gameObject.scene.IsValid()) continue;
            hiddenUiRaycasters.Add(raycaster);
            uiRaycasterStates.Add(raycaster.enabled);
        }
        uiHideRun++;
        uiDrawHidden = true;
        uiHideDeadline = Time.realtimeSinceStartupAsDouble + UiHideSeconds;
        pendingMark += " UI_HIDE_START=" + uiHideRun + " DURATION=15S";
        EnforceHiddenUiDrawing();
        Flush();
    }
    void EnforceHiddenUiDrawing()
    {
        // Only drawing and UI hit tests are suspended; GameObjects remain active.
        foreach (Canvas canvas in hiddenCanvases)
            if (canvas != null && canvas.enabled) canvas.enabled = false;
        foreach (GraphicRaycaster raycaster in hiddenUiRaycasters)
            if (raycaster != null && raycaster.enabled) raycaster.enabled = false;
    }
    void RestoreUiDrawing(string reason)
    {
        if (!uiDrawHidden) return;
        // Clear the flag first, so cleanup callbacks cannot restore twice.
        uiDrawHidden = false;
        for (int i = 0; i < hiddenCanvases.Count; i++)
            if (hiddenCanvases[i] != null) hiddenCanvases[i].enabled = canvasDrawingStates[i];
        for (int i = 0; i < hiddenUiRaycasters.Count; i++)
            if (hiddenUiRaycasters[i] != null) hiddenUiRaycasters[i].enabled = uiRaycasterStates[i];
        hiddenCanvases.Clear(); canvasDrawingStates.Clear();
        hiddenUiRaycasters.Clear(); uiRaycasterStates.Clear();
        pendingMark += " UI_HIDE_END=" + uiHideRun + " REASON=" + reason;
        Flush();
    }
    void OnDisable() => RestoreUiDrawing("HUD_DISABLED");

    void CaptureAndHideWorld()
    {
        worldStates.Clear();
        foreach (Renderer renderer in worldRenderers)
        {
            worldStates.Add(renderer != null && renderer.forceRenderingOff);
            if (renderer != null) renderer.forceRenderingOff = true;
        }
    }
    void RestoreWorldRendering()
    {
        if (!worldOff) return;
        for (int i = 0; i < worldRenderers.Count && i < worldStates.Count; i++)
            if (worldRenderers[i] != null) worldRenderers[i].forceRenderingOff = worldStates[i];
    }
    void ToggleWorldRendering()
    {
        if (worldOff)
        {
            RestoreWorldRendering();
            worldOff = false;
            foreach (VisualGroup group in visualGroups) if (group.off) EnforceGroup(group);
        }
        else
        {
            RefreshVisualTargets(); // Include objects created since the last scan.
            worldOff = true;
            CaptureAndHideWorld();
        }
        worldCaption.SetText("WORLD DRAW: " + (worldOff ? "OFF" : "ON"));
        pendingMark += " WORLD_DRAW=" + (worldOff ? "OFF" : "ON");
    }

    void InitializeVisualControls()
    {
        testPanel = new GameObject("Visual Test Controls", typeof(RectTransform), typeof(Image));
        testPanel.transform.SetParent(safeRoot, false);
        RectTransform rect = testPanel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-12, -64);
        rect.sizeDelta = new Vector2(350, 979);
        testPanel.GetComponent<Image>().color = new Color(0, 0, 0, .9f);
        testPanel.GetComponent<Image>().raycastTarget = false;
        var title = MakeText(testPanel.transform, "VISUAL A/B TEST\nOFF = hidden / test only", 21);
        PlaceTestItem(title.rectTransform, 8, 60);
        for (int i = 0; i < visualGroups.Length; i++)
        {
            int index = i;
            visualGroups[i].buttonText = MakeTestButton(visualGroups[i].name, 74 + i * 49,
                () => SetVisualGroup(index, !visualGroups[index].off));
        }
        MakeTestButton("ALL ON / RESET", 520, ResetVisuals);
        MakeTestButton("ALL OFF", 569, () => {
            for (int i = 0; i < visualGroups.Length; i++) SetVisualGroup(i, true);
        });
        MakeTestButton("RESCAN (new objects)", 618, RefreshVisualTargets);
        adsCaption = MakeTestButton("ADS: OFF", 667, ToggleAds);
        playCaption = MakeTestButton("PLAY GAMES: OFF", 716, TogglePlayGames);
        MakeTestButton("CHEATS / LEVEL UNLOCK", 765, () => DevCheatConsole.Instance?.OpenConsole());
        worldCaption = MakeTestButton("WORLD DRAW: ON", 814, ToggleWorldRendering);
        MakeTestButton("HIDE ALL UI - 15s", 863, BeginUiDrawingTest);
        MakeTestButton("SHADER CHECK (read only)", 912, RequestShaderAudit);
        testPanel.SetActive(false);
        SceneManager.sceneLoaded += VisualSceneLoaded;
        ScheduleVisualBind();
    }
    TextMeshProUGUI MakeTestButton(string value, float y, UnityEngine.Events.UnityAction action)
    {
        var button = new GameObject(value, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(testPanel.transform, false);
        PlaceTestItem(button.GetComponent<RectTransform>(), y, 44);
        button.GetComponent<Image>().color = new Color(.07f, .22f, .29f, 1);
        button.GetComponent<Button>().onClick.AddListener(action);
        var caption = MakeText(button.transform, value, 21);
        caption.alignment = TextAlignmentOptions.Center;
        caption.rectTransform.anchorMin = Vector2.zero;
        caption.rectTransform.anchorMax = Vector2.one;
        caption.rectTransform.offsetMin = caption.rectTransform.offsetMax = Vector2.zero;
        return caption;
    }
    static void PlaceTestItem(RectTransform rect, float y, float height)
    {
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, 1);
        rect.offsetMin = new Vector2(10, -y - height);
        rect.offsetMax = new Vector2(-10, -y);
    }
    void ToggleTestPanel() { if (testPanel != null) testPanel.SetActive(!testPanel.activeSelf); }
    void VisualSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreUiDrawing("SCENE_CHANGED");
        ResetVisuals(); // Every new scene starts with the real game's visuals.
        ScheduleVisualBind();
    }
    void ScheduleVisualBind()
    {
        if (bindVisualRoutine != null) StopCoroutine(bindVisualRoutine);
        bindVisualRoutine = StartCoroutine(BindAfterStart());
    }
    IEnumerator BindAfterStart()
    {
        yield return null;
        yield return null; // Generated planet/light/asteroid renderers are made in Start.
        RefreshVisualTargets();
        shaderAuditPending = true;
        shaderAuditState = "PENDING_SCENE_CHECK";
        bindVisualRoutine = null;
    }
    void RefreshVisualTargets()
    {
        bool restoreWorldOff = worldOff;
        RestoreWorldRendering();
        worldOff = false;
        worldRenderers.Clear();
        worldStates.Clear();
        var previous = new bool[visualGroups.Length];
        for (int i = 0; i < visualGroups.Length; i++)
        {
            previous[i] = visualGroups[i].off;
            RestoreGroup(visualGroups[i]);
            visualGroups[i].renderers.Clear();
            visualGroups[i].effects.Clear();
            visualGroups[i].art.Clear();
        }
        var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || !behaviour.gameObject.scene.IsValid()) continue;
            string type = behaviour.GetType().Name;
            // Only these cosmetic components are suspended. Everything else keeps simulating.
            if (type == "SolarAtmosphere") visualGroups[0].effects.Add(behaviour);
            if (type == "PlayerComboElectricFX") visualGroups[4].effects.Add(behaviour);
        }
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid()) continue;
            if (!renderer.transform.IsChildOf(transform)) worldRenderers.Add(renderer);
            int group = RendererGroup(renderer);
            if (group >= 0) visualGroups[group].renderers.Add(renderer);
        }
        watchedPanels.Clear();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "StatsPanel" || t.name == "MissionBriefingPanel" || t.name == "ResultsPanel"
                || t.name == "PausePanel" || t.name == "OptionsPanel" || t.name == "LevelSelectPanel"
                || t.name == "MainMenuPanel") watchedPanels.Add(t.gameObject);
        }
        foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image == null || image.GetComponent<Selectable>() != null) continue;
            bool panel = HasAncestorName(image.transform, "StatsPanel")
                || HasAncestorName(image.transform, "MissionBriefingPanel")
                || HasAncestorName(image.transform, "ResultsPanel");
            string n = image.name;
            if (HasAncestorName(image.transform, "ResultsPanel") && n == "ResultEdgeGlow")
            { visualGroups[7].art.Add(image); continue; }
            if (HasAncestorName(image.transform, "ResultsPanel") && n == "DarkOverlay")
            { visualGroups[8].art.Add(image); continue; }
            if (panel && (n.Contains("Window") || n.Contains("Background")
                || n.Contains("EdgeGlow") || n.Contains("DarkOverlay")))
                visualGroups[6].art.Add(image);
        }
        for (int i = 0; i < visualGroups.Length; i++)
        {
            if (previous[i]) SetVisualGroup(i, true);
            else UpdateVisualButton(visualGroups[i]);
        }
        worldOff = restoreWorldOff;
        if (worldOff) CaptureAndHideWorld();
        pendingMark += " VISUAL_RESCAN";
    }
    static int RendererGroup(Renderer renderer)
    {
        Transform t = renderer.transform;
        for (Transform parent = t; parent != null; parent = parent.parent)
        {
            if (parent.name == "ComboElectricFX") return 4;
            if (parent.name == "SolarAtmosphereBackdrop") return 0;
            if (parent.name.IndexOf("Stars", System.StringComparison.OrdinalIgnoreCase) >= 0
                && renderer is ParticleSystemRenderer) return 3;
            if (parent.GetComponent<BackgroundAsteroidSpawner>() != null) return 2;
            if (parent.GetComponent<BackgroundPlanetSpawner>() != null
                || parent.GetComponent<HomePlanetVisual>() != null) return 1;
        }
        // Drawing-only suppression; projectiles, damage and physics still work.
        if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer) return 5;
        return -1;
    }
    static bool HasAncestorName(Transform transform, string name)
    {
        for (Transform p = transform; p != null; p = p.parent)
            if (p.name == name) return true;
        return false;
    }
    void SetVisualGroup(int index, bool off)
    {
        VisualGroup group = visualGroups[index];
        if (group.off == off) return;
        RestoreWorldRendering();
        if (off)
        {
            group.rendererStates.Clear(); group.effectStates.Clear(); group.artStates.Clear();
            foreach (Renderer r in group.renderers) group.rendererStates.Add(r != null && r.forceRenderingOff);
            foreach (Behaviour b in group.effects)
            {
                group.effectStates.Add(b != null && b.enabled);
                if (b != null) b.enabled = false;
            }
            foreach (Image image in group.art)
            {
                group.artStates.Add(image != null && image.enabled);
                if (image != null) image.enabled = false;
            }
            group.off = true;
            EnforceGroup(group);
        }
        else RestoreGroup(group);
        if (worldOff) CaptureAndHideWorld();
        UpdateVisualButton(group);
        pendingMark += " VISUAL_" + group.name.Replace(" ", "_") + "=" + (off ? "OFF" : "ON");
    }
    void RestoreGroup(VisualGroup group)
    {
        if (!group.off) return;
        for (int i = 0; i < group.renderers.Count; i++)
            if (group.renderers[i] != null) group.renderers[i].forceRenderingOff = group.rendererStates[i];
        for (int i = 0; i < group.effects.Count; i++)
            if (group.effects[i] != null) group.effects[i].enabled = group.effectStates[i];
        for (int i = 0; i < group.art.Count; i++)
            if (group.art[i] != null) group.art[i].enabled = group.artStates[i];
        group.off = false;
    }
    static void EnforceGroup(VisualGroup group)
    {
        foreach (Renderer renderer in group.renderers)
            if (renderer != null && !renderer.forceRenderingOff) renderer.forceRenderingOff = true;
        foreach (Image image in group.art)
            if (image != null && image.enabled) image.enabled = false;
    }
    void LateUpdate()
    {
        if (uiDrawHidden)
        {
            if (Time.realtimeSinceStartupAsDouble >= uiHideDeadline)
                RestoreUiDrawing("TIMER_COMPLETE");
            else if (SceneManager.GetActiveScene().name == "GameScene" && Time.timeScale > 0f)
                RestoreUiDrawing("GAMEPLAY_RESUMED");
            else
                EnforceHiddenUiDrawing();
        }
        // Cached references only; no scene search, allocations or per-frame rebuilds.
        foreach (VisualGroup group in visualGroups) if (group.off) EnforceGroup(group);
        if (worldOff)
            foreach (Renderer renderer in worldRenderers)
                if (renderer != null && !renderer.forceRenderingOff) renderer.forceRenderingOff = true;
    }
    void UpdateVisualButton(VisualGroup group)
    {
        if (group.buttonText != null) group.buttonText.SetText(group.name + ": " + (group.off ? "OFF" : "ON")
            + " (" + (group.renderers.Count + group.effects.Count + group.art.Count) + ")");
    }
    string VisualState()
    {
        var state = new StringBuilder(100);
        for (int i = 0; i < visualGroups.Length; i++)
        {
            if (i > 0) state.Append(';');
            state.Append(visualGroups[i].name.Replace(" ", "_")).Append('=').Append(visualGroups[i].off ? "OFF" : "ON");
        }
        state.Append(";ADS=").Append(FatefulRushDiagnosticsServices.AdsEnabled ? "ON" : "OFF");
        state.Append(";PLAY_GAMES=").Append(FatefulRushDiagnosticsServices.PlayGamesEnabled ? "ON" : "OFF");
        state.Append(";WORLD_DRAW=").Append(worldOff ? "OFF" : "ON");
        state.Append(";CHEATS=").Append(DevCheatConsole.IsOpen ? "OPEN" : "CLOSED");
        state.Append(";WARMUP=").Append(AndroidShaderWarmup.WarmupState);
        state.Append(";SHADER_AUDIT=").Append(shaderAuditState);
        state.Append(";UI_DRAW=").Append(uiDrawHidden ? "OFF" : "ON");
        state.Append(";UI_TEST_RUN=").Append(uiHideRun);
        return state.ToString();
    }
    void ToggleAds()
    {
        bool requested = !FatefulRushDiagnosticsServices.AdsEnabled;
        bool accepted = FatefulRushAdManager.SetDiagnosticsAdsEnabled(requested);
        adsCaption.SetText("ADS: " + (FatefulRushDiagnosticsServices.AdsEnabled ? "ON" : "OFF"));
        pendingMark += accepted ? " ADS=" + (requested ? "ON" : "OFF") : " ADS_TOGGLE_BLOCKED_BUSY";
    }
    void TogglePlayGames()
    {
        bool requested = !FatefulRushDiagnosticsServices.PlayGamesEnabled;
        GooglePlayGamesManager.SetDiagnosticsPlayGamesEnabled(requested);
        playCaption.SetText("PLAY GAMES: " + (requested ? "ON" : "OFF"));
        pendingMark += " PLAY_GAMES=" + (requested ? "ON" : "OFF");
    }
    string ActivePanels()
    {
        var names = new StringBuilder(100);
        foreach (GameObject panelRoot in watchedPanels)
        {
            if (panelRoot == null || !panelRoot.activeInHierarchy) continue;
            CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
            if (group != null && group.alpha <= .001f) continue;
            if (names.Length > 0) names.Append(';');
            names.Append(panelRoot.name);
        }
        return names.Length == 0 ? "none" : names.ToString();
    }
    void ResetVisuals()
    {
        RestoreWorldRendering();
        worldOff = false;
        if (worldCaption != null) worldCaption.SetText("WORLD DRAW: ON");
        foreach (VisualGroup group in visualGroups) { RestoreGroup(group); UpdateVisualButton(group); }
        pendingMark += " VISUAL_RESET=ALL_ON";
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    bool TryShareFullCsv()
    {
        if (activity == null || api < 29 || string.IsNullOrEmpty(logPath)
            || !System.IO.File.Exists(logPath)) return false;
        try
        {
            using (AndroidJavaObject resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
            using (AndroidJavaClass downloads = new AndroidJavaClass("android.provider.MediaStore$Downloads"))
            using (AndroidJavaObject collection = downloads.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"))
            using (AndroidJavaObject values = new AndroidJavaObject("android.content.ContentValues"))
            {
                string filename = System.IO.Path.GetFileNameWithoutExtension(logPath)
                    + "_" + System.DateTime.UtcNow.ToString("HHmmss") + ".csv";
                values.Call("put", "_display_name", filename);
                values.Call("put", "mime_type", "text/csv");
                values.Call("put", "relative_path", "Download/FatefulRush");
                using (AndroidJavaObject uri = resolver.Call<AndroidJavaObject>("insert", collection, values))
                {
                    if (uri == null) return false;
                    using (AndroidJavaObject stream = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
                    {
                        if (stream == null) return false;
                        try
                        {
                            byte[] data = System.IO.File.ReadAllBytes(logPath);
                            stream.Call("write", (object)data);
                            stream.Call("flush");
                        }
                        finally { stream.Call("close"); }
                    }
                    using (AndroidJavaClass intents = new AndroidJavaClass("android.content.Intent"))
                    using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
                    using (AndroidJavaClass clip = new AndroidJavaClass("android.content.ClipData"))
                    using (AndroidJavaObject clipData = clip.CallStatic<AndroidJavaObject>("newRawUri", "FPS report", uri))
                    {
                        using (var ignored = intent.Call<AndroidJavaObject>("setType", "text/csv")) { }
                        using (var ignored = intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri)) { }
                        using (var ignored = intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.SUBJECT", "Fateful Rush full FPS session")) { }
                        intent.Call("setClipData", clipData);
                        using (var ignored = intent.Call<AndroidJavaObject>("addFlags", 1)) { } // read URI grant
                        using (AndroidJavaObject chooser = intents.CallStatic<AndroidJavaObject>("createChooser", intent, "Share complete FPS report"))
                            activity.Call("startActivity", chooser);
                    }
                    logError = "Full CSV saved: Download/FatefulRush/" + filename;
                    return true;
                }
            }
        }
        catch (System.Exception e)
        {
            logError = "Full CSV export unavailable (" + e.GetType().Name + "); sharing recent samples instead.";
            return false;
        }
    }
#endif
    void ShutdownVisualControls()
    {
        RestoreUiDrawing("HUD_DESTROYED");
        SceneManager.sceneLoaded -= VisualSceneLoaded;
        ResetVisuals();
    }
}
#endif
