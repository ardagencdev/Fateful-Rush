#if FATEFULRUSH_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using TMPro;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Compiled only by the dedicated APK builder. Read-only telemetry: no FPS,
// quality, CPU/GPU level or thermal-policy changes. Optional visual switches are test-only.
[DefaultExecutionOrder(32000)]
public sealed partial class FatefulRushPerformanceHUD : MonoBehaviour
{
    const string Header = "seconds,scene,level,gameplay,time_scale,fps,target_fps,mean_ms,p95_ms,worst_ms,frames_over_33ms,frames_over_50ms,android_thermal,ap_warning,ap_temperature_normalized,battery_c,battery_percent,charging,power_saver,render_scale,screen_hz,cpu_frame_ms,cpu_main_ms,cpu_render_ms,present_wait_ms,gpu_ms,gc_collections,unity_allocated_mb,mark,visual_switches,active_panels,attempt,recorder_main_ms,recorder_render_ms,draw_calls,batches,triangles,coin_spawn_work_ms,enemy_steering_work_ms,coin_spawn_batches,enemy_steering_calls,shader_warmup,shader_progress,shader_audit,shader_audit_scene,shader_audit_age_s,loaded_shaders,loaded_materials,shader_warnings,shader_log_issues,shader_scan_ms,shader_audit_details_event,shader_log_event";
    readonly float[] frames = new float[2048];
    readonly float[] sorted = new float[2048];
    readonly UnityEngine.FrameTiming[] timings = new UnityEngine.FrameTiming[1];
    readonly Queue<string> recent = new Queue<string>(900);
    readonly StringBuilder text = new StringBuilder(1600);
    StreamWriter writer;
    string logPath, logError = "", pendingMark = "", lastThermalEvent = "none";
    TextMeshProUGUI label;
    GameObject panel;
    RectTransform safeRoot;
    Rect lastSafe;
    int lastWidth, lastHeight, count, over33, over50, gcBaseline;
    double sum, windowStart, nextFlush, nextNative, cpu, main, render, present, gpu;
    float worst;
    string apWarning = "N/A", androidThermal = "N/A";
    float apTemperature = float.NaN, batteryC = float.NaN, renderScale = 1;
    int batteryPercent = -1, charging = -1, saver = -1;
    bool focused = true, paused, skipNextFrame;
    bool lastAttemptStarted;
    int attempt;
    ProfilerRecorder mainRecorder, renderRecorder, drawsRecorder, batchesRecorder, trianglesRecorder;
    static string diagnosticsEvents = "";
    public static void RecordDiagnosticsEvent(string value) => diagnosticsEvents += " " + value;
    static long coinWorkTicks, steeringWorkTicks;
    static int coinWorkCalls, steeringWorkCalls;
    public readonly struct WorkMeasurement : IDisposable
    {
        readonly int group;
        readonly long start;
        public WorkMeasurement(int value) { group = value; start = System.Diagnostics.Stopwatch.GetTimestamp(); }
        public void Dispose()
        {
            long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - start;
            if (group == 0) { coinWorkTicks += elapsed; coinWorkCalls++; }
            else { steeringWorkTicks += elapsed; steeringWorkCalls++; }
        }
    }
    public static WorkMeasurement MeasureWork(int group) => new WorkMeasurement(group);
    static ProfilerRecorder StartCounter(ProfilerCategory category, string name)
    {
        try { return ProfilerRecorder.StartNew(category, name, 1); }
        catch { return default; }
    }
    static double CounterValue(ProfilerRecorder recorder, double divisor = 1)
        => recorder.Valid && recorder.Count > 0 ? recorder.LastValue / divisor : double.NaN;
#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject activity, power;
    int api;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        var root = new GameObject("Performance Diagnostics (test APK only)");
        DontDestroyOnLoad(root);
        root.AddComponent<FatefulRushPerformanceHUD>();
    }
    void Awake()
    {
        diagnosticsEvents = "";
        gcBaseline = GC.CollectionCount(0);
        windowStart = Time.realtimeSinceStartupAsDouble;
        mainRecorder = StartCounter(ProfilerCategory.Internal, "Main Thread");
        renderRecorder = StartCounter(ProfilerCategory.Internal, "Render Thread");
        drawsRecorder = StartCounter(ProfilerCategory.Render, "Draw Calls Count");
        batchesRecorder = StartCounter(ProfilerCategory.Render, "Batches Count");
        trianglesRecorder = StartCounter(ProfilerCategory.Render, "Triangles Count");
        OpenLog();
        CreateUI();
        InitializeVisualControls();
        Application.logMessageReceived += ObserveShaderLog;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                api = version.GetStatic<int>("SDK_INT");
            power = activity.Call<AndroidJavaObject>("getSystemService", "power");
        }
        catch (Exception e) { logError = "Android telemetry: " + e.GetType().Name; }
#endif
    }
    void OpenLog()
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "PerformanceLogs");
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "FR_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".csv");
            writer = new StreamWriter(logPath, false, new UTF8Encoding(false));
            writer.WriteLine("# " + DeviceInfo());
            writer.WriteLine(Header);
            writer.Flush();
        }
        catch (Exception e) { logError = "CSV unavailable: " + e.GetType().Name; }
    }
    string DeviceInfo()
    {
#if FATEFULRUSH_FRAME_PACING_OFF_TEST
        const string pacingState = "OFF_DIAGNOSTICS_TEST";
#else
        const string pacingState = "ON_BUILD_GUARD";
#endif
#if FATEFULRUSH_GPU_RECORDERS_OFF
        const string gpuRecorderState = "OFF";
#else
        const string gpuRecorderState = "ON";
#endif
        return "UTC=" + DateTime.UtcNow.ToString("O") + " | " + SystemInfo.deviceModel +
            " | " + SystemInfo.operatingSystem + " | Unity=" + Application.unityVersion +
            " | GPU=" + SystemInfo.graphicsDeviceName + " | API=" + SystemInfo.graphicsDeviceType +
            " | resolution=" + Screen.width + "x" + Screen.height + " | app=" + Application.identifier +
            " | version=" + Application.version + " | development=" + Debug.isDebugBuild +
            " | gpu_recorders=" + gpuRecorderState + " | frame_pacing=" + pacingState;
    }
    void Update()
    {
        UpdateSafeArea();
        if (!focused || paused) return;
        bool started = GameStateManager.IsGameplayStarted;
        if (started && !lastAttemptStarted) { attempt++; pendingMark += " ATTEMPT=" + attempt; }
        lastAttemptStarted = started;
        double now = Time.realtimeSinceStartupAsDouble;
        if (skipNextFrame) { skipNextFrame = false; ResetWindow(now); return; }
        float ms = Time.unscaledDeltaTime * 1000f;
        if (count < frames.Length) frames[count] = ms;
        count++; sum += ms; worst = Mathf.Max(worst, ms);
        if (ms > 33.34f) over33++;
        if (ms > 50f) over50++;
#if FATEFULRUSH_FRAME_TIMINGS
        FrameTimingManager.CaptureFrameTimings();
#endif
        if (now - windowStart >= 1.0)
        {
            Sample(now);
            ResetWindow(now);
        }
        TryPendingShaderAudit();
    }
    void ResetWindow(double now)
    {
        count = over33 = over50 = 0; sum = 0; worst = 0;
        windowStart = now; gcBaseline = GC.CollectionCount(0);
        coinWorkTicks = steeringWorkTicks = 0;
        coinWorkCalls = steeringWorkCalls = 0;
    }
    void Sample(double now)
    {
        if (count == 0) return;
        int n = Mathf.Min(count, frames.Length);
        Array.Copy(frames, sorted, n);
        Array.Sort(sorted, 0, n);
        float p95 = sorted[Mathf.Clamp(Mathf.CeilToInt(n * .95f) - 1, 0, n - 1)];
        double fps = count / (now - windowStart), mean = sum / count;
        cpu = main = render = present = gpu = double.NaN;
#if FATEFULRUSH_FRAME_TIMINGS
        if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
        {
            // A single recent completed frame, NOT a one-second CPU/GPU mean.
            cpu = ValidTiming(timings[0].cpuFrameTime);
            main = ValidTiming(timings[0].cpuMainThreadFrameTime);
            render = ValidTiming(timings[0].cpuRenderThreadFrameTime);
            present = timings[0].cpuMainThreadPresentWaitTime;
            // Some empty GLES frames return an absolute timestamp-sized value
            // instead of a duration. Never show it as real GPU work time.
            double rawGpu = timings[0].gpuFrameTime;
            gpu = ValidGpuTiming(rawGpu);
            if (double.IsNaN(gpu) && rawGpu != 0)
                pendingMark += " GPU_TIMING_REJECTED_RAW_MS=" + F(rawGpu);
        }
#endif
        var ap = Holder.Instance;
        apWarning = "N/A"; apTemperature = float.NaN;
        if (ap != null && ap.Initialized && ap.ThermalStatus != null)
        {
            var metrics = ap.ThermalStatus.ThermalMetrics;
            apWarning = metrics.WarningLevel.ToString();
            apTemperature = metrics.TemperatureLevel; // normalized, never degrees Celsius
        }
        if (now >= nextNative) { ReadAndroid(); nextNative = now + 5; }
        string thermalKey = androidThermal + "/" + apWarning;
        if (thermalKey != lastThermalKey)
        {
            lastThermalKey = thermalKey;
            lastThermalEvent = F(now) + "s " + thermalKey;
            pendingMark += " THERMAL=" + thermalKey;
        }
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        renderScale = urp != null ? urp.renderScale : 1f;
        string scene = SceneManager.GetActiveScene().name;
        var level = SelectedLevelData.SelectedLevel;
        string levelName = level != null ? level.levelNumber.ToString(CultureInfo.InvariantCulture) : "N/A";
        int gc = GC.CollectionCount(0) - gcBaseline;
        double mb = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0);
        double hz = Screen.currentResolution.refreshRateRatio.value;
        pendingMark += diagnosticsEvents;
        diagnosticsEvents = "";
        double recorderMain = CounterValue(mainRecorder, 1e6);
        double recorderRender = CounterValue(renderRecorder, 1e6);
        double coinWork = coinWorkTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        double steeringWork = steeringWorkTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        string row = string.Join(",", F(now), Csv(scene), Csv(levelName),
            GameStateManager.IsGameplayStarted && !GameStateManager.IsGameplayEnded ? "1" : "0",
            F(Time.timeScale), F(fps), Application.targetFrameRate.ToString(CultureInfo.InvariantCulture),
            F(mean), F(p95), F(worst), over33.ToString(), over50.ToString(), Csv(androidThermal),
            Csv(apWarning), F(apTemperature), F(batteryC), batteryPercent.ToString(), charging.ToString(),
            saver.ToString(), F(renderScale), F(hz), F(cpu), F(main), F(render), F(present), F(gpu),
            gc.ToString(), F(mb), Csv(pendingMark), Csv(VisualState()), Csv(ActivePanels()), attempt.ToString(CultureInfo.InvariantCulture),
            F(recorderMain), F(recorderRender), F(CounterValue(drawsRecorder)),
            F(CounterValue(batchesRecorder)), F(CounterValue(trianglesRecorder)),
            F(coinWork), F(steeringWork), coinWorkCalls.ToString(), steeringWorkCalls.ToString(),
            Csv(AndroidShaderWarmup.WarmupState), Csv(ShaderProgress()), Csv(shaderAuditState),
            Csv(shaderAuditScene), F(shaderAuditTime < 0 ? double.NaN : now - shaderAuditTime),
            shaderAuditShaders.ToString(), shaderAuditMaterials.ToString(), shaderAuditWarnings.ToString(),
            shaderLogErrors.ToString(), F(shaderAuditMs), Csv(shaderAuditReportPending ? shaderAuditDetails : ""), Csv(shaderLogLast));
        recent.Enqueue(row); while (recent.Count > 900) recent.Dequeue();
        try
        {
            if (writer != null)
            {
                writer.WriteLine(row);
                if (now >= nextFlush) { writer.Flush(); nextFlush = now + 10; }
            }
        }
        catch (Exception e) { logError = "CSV write failed: " + e.GetType().Name; CloseLog(); }
        pendingMark = "";
        shaderAuditReportPending = false;
        shaderLogLast = "";
        if (uiDrawHidden) return; // CSV/counters above keep recording; skip invisible label rebuilds.
        text.Clear();
        text.Append("FR DEBUG | ").Append(scene).Append(" | L ").Append(levelName);
        text.Append("\nFPS ").Append(F(fps)).Append(" / target ").Append(Application.targetFrameRate)
            .Append(" | ").Append(F(hz)).Append(" Hz");
        text.Append("\nFrame ms avg ").Append(F(mean)).Append(" | p95 ").Append(F(p95)).Append(" | max ").Append(F(worst));
        text.Append("\n>33ms ").Append(over33).Append(" | >50ms ").Append(over50).Append(" | GC ").Append(gc);
        text.Append("\nThermal Android: ").Append(androidThermal).Append(" | AP: ").Append(apWarning);
        text.Append("\nBattery ").Append(F(batteryC)).Append(" C | ").Append(batteryPercent < 0 ? "N/A" : batteryPercent.ToString() + "%")
            .Append(" | charging ").Append(State(charging)).Append(" | saver ").Append(State(saver));
        text.Append("\nAP temp (0..1): ").Append(F(apTemperature)).Append(" | URP scale ").Append(F(renderScale));
        text.Append("\nCPU counter latest (includes waits) ").Append(F(recorderMain)).Append(" | render ").Append(F(recorderRender));
        text.Append("\nLatest GPU ").Append(F(gpu)).Append(" | present wait ").Append(F(present)).Append(" ms");
        text.Append("\nWindow CPU work: coin ").Append(F(coinWork)).Append(" | steering ").Append(F(steeringWork)).Append(" ms");
        text.Append("\nUnity allocated ").Append(F(mb)).Append(" MB | elapsed ").Append(F(now)).Append("s");
        text.Append("\nLast thermal change: ").Append(lastThermalEvent);
        text.Append("\n").Append(string.IsNullOrEmpty(logError) ? "CSV recording | SHARE: full session CSV (Android 10+)" : logError);
        text.Append("\nVisuals: ").Append(VisualState());
        text.Append("\nShader audit: ").Append(shaderAuditState).Append(" | warmed ").Append(ShaderProgress())
            .Append(" | warnings ").Append(shaderAuditWarnings).Append(" | log issues ").Append(shaderLogErrors);
        label.SetText(text.ToString());
    }
    // Debug-only observation. No Shader.Find/Resources.Load/WarmUp calls, no
    // material instantiation, no retained Material/Shader inventory after a scan.
    static readonly string[] RequiredShaderNames = {
        "UI/Default", "Sprites/Default", "TextMeshPro/Mobile/Distance Field",
        "FatefulRush/BackgroundPlanet", "FatefulRush/BackgroundAsteroid",
        "FatefulRush/ComboElectric", "FatefulRush/ObstacleCollisionAccent",
        "FatefulRush/HomePlanetTheme", "FatefulRush/SolarAtmosphere",
        "FatefulRush/SolarAtmosphereBake", "FatefulRush/SolarAtmosphereDisplay",
        "FatefulRush/UI/RoundedResultEdgeGlow", "FatefulRush/UI/SolidResultOverlay",
        "FatefulRush/BossDangerPreview"
    };
    bool shaderAuditPending, shaderAuditReportPending;
    string shaderAuditState = "NOT_CHECKED", shaderAuditScene = "", shaderAuditDetails = "", shaderLogLast = "";
    int shaderAuditShaders, shaderAuditMaterials, shaderAuditWarnings, shaderLogErrors;
    double shaderAuditTime = -1, shaderAuditMs;

    static string ShaderProgress()
    {
        var collection = AndroidShaderWarmup.DiagnosticsCollection;
        return collection == null ? "N/A" : collection.warmedUpVariantCount + "/" + collection.variantCount;
    }
    bool ShaderAuditSafe()
    {
        return focused && !paused && !uiDrawHidden &&
            SceneManager.GetActiveScene().name != "IntroScene" &&
            (!GameStateManager.IsGameplayStarted || GameStateManager.IsGameplayEnded || Time.timeScale <= 0);
    }
    void RequestShaderAudit()
    {
        if (!ShaderAuditSafe())
        {
            pendingMark += " SHADER_CHECK_BLOCKED_PAUSE_FIRST";
            return;
        }
        shaderAuditPending = true;
        pendingMark += " SHADER_CHECK_REQUESTED";
    }
    void TryPendingShaderAudit()
    {
        if (!shaderAuditPending || !AndroidShaderWarmup.IsComplete || !ShaderAuditSafe()) return;
        shaderAuditPending = false;
        double start = Time.realtimeSinceStartupAsDouble;
        shaderAuditScene = SceneManager.GetActiveScene().name;
        shaderAuditWarnings = 0;
        shaderAuditShaders = shaderAuditMaterials = 0;
        var notes = new StringBuilder(4096);
        int failures = 0;
        try
        {
            var collection = AndroidShaderWarmup.DiagnosticsCollection;
            bool androidRuntime = Application.platform == RuntimePlatform.Android && !Application.isEditor;
            if (androidRuntime && (collection == null || collection.shaderCount == 0 || collection.variantCount == 0))
            {
                failures++;
                ShaderNote(notes, "COLLECTION_MISSING_OR_EMPTY");
            }
            if (androidRuntime && (AndroidShaderWarmup.WarmupState != "DONE" ||
                collection == null || !collection.isWarmedUp || collection.warmedUpVariantCount != collection.variantCount))
            {
                failures++;
                ShaderNote(notes, "WARMUP_NOT_VERIFIED=" + AndroidShaderWarmup.WarmupState);
            }
            var loadedShaders = Resources.FindObjectsOfTypeAll<Shader>();
            shaderAuditShaders = loadedShaders.Length;
            var byName = new Dictionary<string, Shader>(loadedShaders.Length);
            foreach (var shader in loadedShaders)
            {
                if (shader == null) continue;
                if (byName.TryGetValue(shader.name, out var other) && other != shader)
                {
                    shaderAuditWarnings++;
                    ShaderNote(notes, "DUPLICATE_LOADED_NAME=" + shader.name);
                }
                else byName[shader.name] = shader;
            }
            // A missing loaded reference is suspicious, not proof of stripping.
            // Editor/desktop intentionally skips this Android startup requirement.
            if (androidRuntime)
                foreach (string name in RequiredShaderNames)
                {
                    if (!byName.TryGetValue(name, out var shader))
                    {
                        shaderAuditWarnings++;
                        ShaderNote(notes, "REQUIRED_NOT_OBSERVED=" + name);
                    }
                    else if (!shader.isSupported)
                    {
                        shaderAuditWarnings++;
                        ShaderNote(notes, "REQUIRED_UNSUPPORTED=" + name);
                    }
                }
            var loadedMaterials = Resources.FindObjectsOfTypeAll<Material>();
            shaderAuditMaterials = loadedMaterials.Length;
            foreach (var material in loadedMaterials)
            {
                if (material == null) continue;
                var shader = material.shader;
                if (shader == null || shader.name == "Hidden/InternalErrorShader" || !shader.isSupported)
                {
                    // Inventory includes unused/inactive assets. This does not
                    // assert that the invalid material is drawing on screen.
                    shaderAuditWarnings++;
                    ShaderNote(notes, "LOADED_MATERIAL_INVALID=" + material.name + " shader=" + (shader == null ? "NULL" : shader.name));
                    continue;
                }
                if (collection == null || shader.name.StartsWith("Hidden/", StringComparison.Ordinal)) continue;
                var enabled = material.enabledKeywords;
                string[] keywords = new string[enabled.Length];
                for (int i = 0; i < enabled.Length; i++) keywords[i] = enabled[i].name;
                if (!ContainsMaterialVariant(collection, shader, PassType.Normal, keywords) &&
                    !ContainsMaterialVariant(collection, shader, PassType.ScriptableRenderPipeline, keywords) &&
                    !ContainsMaterialVariant(collection, shader, PassType.ScriptableRenderPipelineDefaultUnlit, keywords))
                {
                    shaderAuditWarnings++;
                    ShaderNote(notes, "MATERIAL_STATE_NOT_COVERED=" + material.name + " shader=" + shader.name +
                        " keywords=" + string.Join("|", keywords));
                }
            }
            // Observe existing draw assignments without requesting .material or
            // materialForRendering (those can instantiate runtime materials).
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!renderer.enabled || renderer.forceRenderingOff) continue;
                foreach (var material in renderer.sharedMaterials)
                    if (InvalidShaderMaterial(material))
                    {
                        failures++;
                        ShaderNote(notes, "ACTIVE_RENDERER_INVALID=" + renderer.name);
                    }
            }
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<CanvasRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (renderer.cull) continue;
                for (int i = 0; i < renderer.materialCount; i++)
                    if (InvalidShaderMaterial(renderer.GetMaterial(i)))
                    {
                        failures++;
                        ShaderNote(notes, "ACTIVE_UI_MATERIAL_INVALID=" + renderer.name);
                    }
            }
            // Match is only local keywords + one listed runtime pass. Pipeline
            // global keywords, vertex layouts, targets and driver PSOs aren't proven.
            shaderAuditState = failures > 0 ? "FAIL" : shaderAuditWarnings > 0 || shaderLogErrors > 0 ? "WARNING" :
                androidRuntime ? "OBSERVED_OK_LIMITED" : "EDITOR_DESKTOP_LIMITED";
            ShaderNote(notes, "SCOPE=loaded_materials_and_collection;GPU_VARIANTS_NOT_PROVEN;FULL_LOG_VISIBILITY_NOT_GUARANTEED");
        }
        catch (Exception exception)
        {
            shaderAuditState = "CHECK_FAILED";
            ShaderNote(notes, "AUDIT_EXCEPTION=" + exception.GetType().Name + ":" + exception.Message);
        }
        shaderAuditDetails = notes.ToString();
        shaderAuditReportPending = true;
        shaderAuditTime = Time.realtimeSinceStartupAsDouble;
        shaderAuditMs = (shaderAuditTime - start) * 1000.0;
        pendingMark += " SHADER_AUDIT=" + shaderAuditState + " SCAN_MS=" + F(shaderAuditMs) + " AUDIT_MAY_HITCH";
    }
    static bool ContainsMaterialVariant(ShaderVariantCollection collection, Shader shader, PassType pass, string[] keywords)
    {
        try { return collection.Contains(new ShaderVariantCollection.ShaderVariant(shader, pass, keywords)); }
        catch (ArgumentException) { return false; } // Not every shader has all three pass types.
    }
    static bool InvalidShaderMaterial(Material material)
    {
        return material == null || material.shader == null ||
            material.shader.name == "Hidden/InternalErrorShader" || !material.shader.isSupported;
    }
    static void ShaderNote(StringBuilder notes, string value)
    {
        const int limit = 12000;
        if (notes.Length >= limit) return;
        if (notes.Length > 0) notes.Append("; ");
        // Protect CSV row boundaries and bound retained text, not asset references.
        string line = value.Replace('\r', ' ').Replace('\n', ' ');
        int available = Mathf.Max(0, limit - notes.Length);
        notes.Append(line, 0, Mathf.Min(line.Length, available));
    }
    void ObserveShaderLog(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Warning && type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (message == null || (message.IndexOf("shader", StringComparison.OrdinalIgnoreCase) < 0 &&
            message.IndexOf("variant", StringComparison.OrdinalIgnoreCase) < 0)) return;
        // Main-thread callback only. Bounded strings; no stack trace/native log polling.
        if (shaderLogErrors < int.MaxValue) shaderLogErrors++;
        string value = message.Length > 1500 ? message.Substring(0, 1500) : message;
        if (shaderLogLast.Length < 4096)
        {
            string line = type + ": " + value.Replace('\r', ' ').Replace('\n', ' ') + "; ";
            shaderLogLast += line.Substring(0, Mathf.Min(line.Length, 4096 - shaderLogLast.Length));
        }
        if (shaderAuditState == "OBSERVED_OK_LIMITED") shaderAuditState = "WARNING";
    }

    string lastThermalKey = "";
    static double ValidTiming(double value) => value > 0 ? value : double.NaN;
    static double ValidGpuTiming(double value) => value > 0 && value <= 10000 &&
        !double.IsNaN(value) && !double.IsInfinity(value) ? value : double.NaN;
    static string F(double v) => double.IsNaN(v) || double.IsInfinity(v) ? "N/A" : v.ToString("0.00", CultureInfo.InvariantCulture);
    static string State(int value) => value < 0 ? "N/A" : value == 0 ? "off" : "on";
    static string Csv(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
    void ReadAndroid()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (activity == null) return;
        try
        {
            if (power != null)
            {
                saver = power.Call<bool>("isPowerSaveMode") ? 1 : 0;
                if (api >= 29)
                {
                    int status = power.Call<int>("getCurrentThermalStatus");
                    string[] names = { "NONE", "LIGHT", "MODERATE", "SEVERE", "CRITICAL", "EMERGENCY", "SHUTDOWN" };
                    androidThermal = status >= 0 && status < names.Length ? names[status] : "UNKNOWN " + status;
                }
            }
            using (var filter = new AndroidJavaObject("android.content.IntentFilter", "android.intent.action.BATTERY_CHANGED"))
            using (var battery = activity.Call<AndroidJavaObject>("registerReceiver", null, filter))
            {
                if (battery == null) return;
                int raw = battery.Call<int>("getIntExtra", "temperature", int.MinValue);
                batteryC = raw == int.MinValue ? float.NaN : raw / 10f;
                int scale = battery.Call<int>("getIntExtra", "scale", -1);
                int value = battery.Call<int>("getIntExtra", "level", -1);
                batteryPercent = scale > 0 && value >= 0 ? Mathf.RoundToInt(100f * value / scale) : -1;
                int plug = battery.Call<int>("getIntExtra", "plugged", -1);
                charging = plug < 0 ? -1 : plug == 0 ? 0 : 1;
            }
        }
        catch (Exception e) { logError = "Android telemetry unavailable: " + e.GetType().Name; androidThermal = "N/A"; }
#endif
    }
    void Share()
    {
        Flush();
#if UNITY_ANDROID && !UNITY_EDITOR
        if (TryShareFullCsv()) return;
#endif
        var report = new StringBuilder(250000);
        report.AppendLine(DeviceInfo()).AppendLine(Header);
        foreach (var row in recent) report.AppendLine(row);
        string data = report.ToString();
        GUIUtility.systemCopyBuffer = data;
        Flush();
#if UNITY_ANDROID && !UNITY_EDITOR
        if (activity == null) return;
        try
        {
            // Text sharing needs neither FileProvider nor storage permission.
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            {
                using (var ignored = intent.Call<AndroidJavaObject>("setType", "text/plain")) { }
                using (var ignored = intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.SUBJECT", "Fateful Rush FPS report")) { }
                using (var ignored = intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", data)) { }
                using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share FPS report"))
                    activity.Call("startActivity", chooser);
            }
        }
        catch (Exception e) { logError = "Share failed; report copied: " + e.GetType().Name; }
#endif
    }
    void CreateUI()
    {
        var canvasObject = new GameObject("Diagnostics Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        safeRoot = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
        safeRoot.SetParent(canvasObject.transform, false);
        panel = new GameObject("Readout", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(safeRoot, false);
        Position(panel.GetComponent<RectTransform>(), 0, -54, 730, 410);
        var bg = panel.GetComponent<Image>(); bg.color = new Color(0, 0, 0, .82f); bg.raycastTarget = false;
        label = MakeText(panel.transform, "Starting diagnostics...", 20);
        var rt = label.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12, 8); rt.offsetMax = new Vector2(-12, -8);
        MakeButton("HUD", 0, () => panel.SetActive(!panel.activeSelf));
        MakeButton("MARK", 150, () => pendingMark += " USER_HITCH");
        MakeButton("SHARE", 300, Share);
        MakeButton("TEST", 450, ToggleTestPanel);
        UpdateSafeArea();
    }
    TextMeshProUGUI MakeText(Transform parent, string value, int size)
    {
        var t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false); t.font = TMP_Settings.defaultFontAsset;
        t.text = value; t.fontSize = size; t.color = Color.white; t.raycastTarget = false;
        t.alignment = TextAlignmentOptions.TopLeft; t.richText = false; return t;
    }
    void MakeButton(string name, float x, UnityEngine.Events.UnityAction action)
    {
        var b = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); b.transform.SetParent(safeRoot, false);
        Position(b.GetComponent<RectTransform>(), x, 0, 140, 48);
        b.GetComponent<Image>().color = new Color(.05f, .15f, .2f, .95f);
        b.GetComponent<Button>().onClick.AddListener(action);
        var t = MakeText(b.transform, name, 22); t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
    }
    static void Position(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = new Vector2(.5f, 1); r.pivot = new Vector2(.5f, 1);
        r.anchoredPosition = new Vector2(x - 220, y - 8); r.sizeDelta = new Vector2(w, h);
    }
    void UpdateSafeArea()
    {
        Rect safe = Screen.safeArea;
        if (safe == lastSafe && lastWidth == Screen.width && lastHeight == Screen.height) return;
        lastSafe = safe; lastWidth = Screen.width; lastHeight = Screen.height;
        if (Screen.width < 1 || Screen.height < 1) return;
        safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
    }
    void OnApplicationFocus(bool value)
    {
        if (!value) RestoreUiDrawing("APP_LOST_FOCUS");
        focused = value; skipNextFrame = true;
        ResetWindow(Time.realtimeSinceStartupAsDouble); Flush();
    }
    void OnApplicationPause(bool value)
    {
        if (value) RestoreUiDrawing("APP_PAUSED");
        paused = value; skipNextFrame = true;
        ResetWindow(Time.realtimeSinceStartupAsDouble); Flush();
    }
    void Flush() { try { writer?.Flush(); } catch { } }
    void CloseLog() { try { writer?.Dispose(); } catch { } writer = null; }
    void OnDestroy()
    {
        Application.logMessageReceived -= ObserveShaderLog;
        ShutdownVisualControls();
        if (mainRecorder.Valid) mainRecorder.Dispose();
        if (renderRecorder.Valid) renderRecorder.Dispose();
        if (drawsRecorder.Valid) drawsRecorder.Dispose();
        if (batchesRecorder.Valid) batchesRecorder.Dispose();
        if (trianglesRecorder.Valid) trianglesRecorder.Dispose();
        CloseLog();
#if UNITY_ANDROID && !UNITY_EDITOR
        power?.Dispose(); activity?.Dispose();
#endif
    }
}
#endif
