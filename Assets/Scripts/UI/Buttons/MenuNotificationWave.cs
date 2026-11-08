using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal enum MenuNotice
{
    Stats,
    Skins,
    Achievements,
    Continue,
    Missions,
    Leaderboards,
    Extras
}

internal static class MenuNotifications
{
    private const string Prefix = "MenuNotification_";
#if UNITY_EDITOR || FATEFULRUSH_DIAGNOSTICS
    private const string DebugSkinKey = Prefix + "DebugSkins";
#endif
    private static string achievementScope;
    private static string leaderboardScope;

    internal static event Action Changed;

    internal static bool IsPending(MenuNotice notice)
    {
        if (notice == MenuNotice.Extras)
            return IsPending(MenuNotice.Achievements) || IsPending(MenuNotice.Leaderboards);

        if (notice == MenuNotice.Leaderboards)
            return HasPendingLeaderboard();

        if (notice == MenuNotice.Achievements)
        {
            return achievementScope != null &&
                AchievementRevision(achievementScope) >
                PlayerPrefs.GetInt(achievementScope + "_seen");
        }

#if UNITY_EDITOR || FATEFULRUSH_DIAGNOSTICS
        if (notice == MenuNotice.Skins && PlayerPrefs.HasKey(DebugSkinKey))
            return true;
#endif

        return PlayerPrefs.GetInt(Prefix + notice) == 1;
    }

    internal static void MarkPending(MenuNotice notice)
    {
        if (PlayerPrefs.GetInt(Prefix + notice) == 1)
            return;

        PlayerPrefs.SetInt(Prefix + notice, 1);
        SaveAndRefresh();
    }

    internal static void MarkSeen(MenuNotice notice)
    {
        if (!IsPending(notice))
            return;

        PlayerPrefs.DeleteKey(Prefix + notice);
#if UNITY_EDITOR || FATEFULRUSH_DIAGNOSTICS
        if (notice == MenuNotice.Skins)
            PlayerPrefs.DeleteKey(DebugSkinKey);
#endif
        SaveAndRefresh();
    }

#if UNITY_EDITOR || FATEFULRUSH_DIAGNOSTICS
    internal static void SetDebugSkinPending(bool pending)
    {
        if (pending == PlayerPrefs.HasKey(DebugSkinKey))
            return;

        if (pending)
            PlayerPrefs.SetInt(DebugSkinKey, 1);
        else
            PlayerPrefs.DeleteKey(DebugSkinKey);

        SaveAndRefresh();
    }

    internal static void ResetDebugProgressNotices()
    {
        PlayerPrefs.DeleteKey(Prefix + MenuNotice.Continue);
        PlayerPrefs.DeleteKey(Prefix + MenuNotice.Missions);
        PlayerPrefs.DeleteKey(Prefix + MenuNotice.Skins);
        PlayerPrefs.DeleteKey(Prefix + MenuNotice.Stats);
        PlayerPrefs.DeleteKey(DebugSkinKey);
        SaveAndRefresh();
    }
#endif

    internal static void SetAchievementAccount(string playerId)
    {
        string scope = Prefix + "PlayGames_" + playerId;
        if (achievementScope == scope)
            return;

        achievementScope = scope;
        Changed?.Invoke();
    }

    internal static int AchievementRevision(string scope)
    {
        return PlayerPrefs.GetInt(scope + "_revision");
    }

    internal static string AchievementScope => achievementScope;

    internal static void RecordAchievement(string scope, string achievementId, bool notify)
    {
        string key = scope + "_known_" + achievementId;
        if (PlayerPrefs.GetInt(key) == 1)
            return;

        PlayerPrefs.SetInt(key, 1);
        if (!notify)
            return;

        PlayerPrefs.SetInt(scope + "_revision", AchievementRevision(scope) + 1);
        SaveAndRefresh();
    }

    internal static void MarkAchievementsSeen(string scope, int revision)
    {
        if (scope == null || revision <= PlayerPrefs.GetInt(scope + "_seen"))
            return;

        PlayerPrefs.SetInt(scope + "_seen", revision);
        SaveAndRefresh();
    }

    internal static string SetLeaderboardAccount(string playerId)
    {
        string scope = string.IsNullOrEmpty(playerId) ? null : Prefix + "Rank_" + playerId;
        if (leaderboardScope != scope)
        {
            leaderboardScope = scope;
            Changed?.Invoke();
        }

        return scope;
    }

    internal static int LeaderboardRevision(string scope)
    {
        return PlayerPrefs.GetInt(scope + "_revision");
    }

    internal static void MarkLeaderboardPending(string scope, int level)
    {
        int revision = LeaderboardRevision(scope) + 1;
        PlayerPrefs.SetInt(scope + "_revision", revision);
        PlayerPrefs.SetInt(LeaderboardNoticeKey(scope, level), revision);
        SaveAndRefresh();
    }

    internal static void MarkLeaderboardsSeen(string scope, int level, int visibleRevision)
    {
        int first = level == 0 ? 1 : level;
        int last = level == 0 ? 40 : level;
        bool changed = false;
        for (int i = first; i <= last; i++)
        {
            string key = LeaderboardNoticeKey(scope, i);
            int revision = PlayerPrefs.GetInt(key);
            if (revision > 0 && revision <= visibleRevision)
            {
                PlayerPrefs.DeleteKey(key);
                changed = true;
            }
        }

        if (changed)
            SaveAndRefresh();
    }

    private static bool HasPendingLeaderboard()
    {
        if (leaderboardScope == null)
            return false;

        for (int level = 1; level <= 40; level++)
        {
            if (PlayerPrefs.HasKey(LeaderboardNoticeKey(leaderboardScope, level)))
                return true;
        }

        return false;
    }

    private static string LeaderboardNoticeKey(string scope, int level)
    {
        return scope + "_pending_" + level;
    }

    private static void SaveAndRefresh()
    {
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class MenuNotificationWave : MaskableGraphic
{
    private const float SweepDuration = 0.85f;
    private const float RepeatDelay = 5.5f;

    private MenuNotice notice;
    private float stagger;
    private RectTransform clipRect;
    private Coroutine animationRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= AttachToMenu;
        SceneManager.sceneLoaded += AttachToMenu;
    }

    private static void AttachToMenu(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu")
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                switch (button.name)
                {
                    case "StatsButton": Attach(button, MenuNotice.Stats, 0f); break;
                    case "SkinsButton": Attach(button, MenuNotice.Skins, 0.25f); break;
                    case "ContinueButton": Attach(button, MenuNotice.Continue, 0f); break;
                    case "MissionsButton": Attach(button, MenuNotice.Missions, 0.15f); break;
                    case "ExtrasButton": Attach(button, MenuNotice.Extras, 0.5f); break;
                    case "AchievementsButton": Attach(button, MenuNotice.Achievements, 0f); break;
                    case "LeaderboardsButton": Attach(button, MenuNotice.Leaderboards, 0.25f); break;
                }
            }
        }
    }

    private static void Attach(Button button, MenuNotice notice, float stagger)
    {
        Transform parent = button.targetGraphic != null
            ? button.targetGraphic.rectTransform
            : button.transform;

        var clip = new GameObject("Notification Wave Clip", typeof(RectTransform), typeof(RectMask2D));
        clip.SetActive(false);
        var clipRect = (RectTransform)clip.transform;
        clipRect.SetParent(parent, false);
        clipRect.SetAsFirstSibling();
        clipRect.anchorMin = new Vector2(0.06f, 0.13f);
        clipRect.anchorMax = new Vector2(0.94f, 0.87f);
        clipRect.offsetMin = clipRect.offsetMax = Vector2.zero;

        var waveObject = new GameObject("Notification Wave", typeof(RectTransform));
        var waveRect = (RectTransform)waveObject.transform;
        waveRect.SetParent(clipRect, false);
        waveRect.anchorMin = new Vector2(0.34f, 0f);
        waveRect.anchorMax = new Vector2(0.66f, 1f);
        waveRect.offsetMin = waveRect.offsetMax = Vector2.zero;

        var wave = waveObject.AddComponent<MenuNotificationWave>();
        wave.raycastTarget = false;
        wave.notice = notice;
        wave.stagger = stagger;
        wave.clipRect = clipRect;
        clip.SetActive(true);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        canvasRenderer.cullTransparentMesh = true;
        canvasRenderer.SetAlpha(0f);
        MenuNotifications.Changed += Refresh;
        PlayerSkinCatalog.SelectedSkinChanged += ApplyTheme;
        Refresh();
    }

    protected override void OnDisable()
    {
        MenuNotifications.Changed -= Refresh;
        PlayerSkinCatalog.SelectedSkinChanged -= ApplyTheme;
        HideWave();
        base.OnDisable();
    }

    private void Refresh()
    {
        if (clipRect == null || !MenuNotifications.IsPending(notice))
        {
            HideWave();
            return;
        }

        if (animationRoutine == null)
            animationRoutine = StartCoroutine(Sweep());
    }

    private void ApplyTheme()
    {
        PlayerSkinCatalog catalog = PlayerSkinCatalog.ResolveLoadedCatalog();
        Color theme = catalog != null ? catalog.GetSelectedUIThemeColor() : Color.white;
        theme.a = 1f;
        color = theme;
    }

    private IEnumerator Sweep()
    {
        yield return new WaitForSecondsRealtime(0.25f + stagger);

        while (true)
        {
#if UNITY_EDITOR || FATEFULRUSH_DIAGNOSTICS
            while (DevCheatConsole.IsOpen)
                yield return null;
#endif
            ApplyTheme();
            float elapsed = 0f;
            while (elapsed < SweepDuration)
            {
                float t = Mathf.Clamp01(elapsed / SweepDuration);
                float travel = (clipRect.rect.width + rectTransform.rect.width) * 0.5f;
                rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, t), 0f);
                canvasRenderer.SetAlpha(Mathf.Sin(t * Mathf.PI));
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            canvasRenderer.SetAlpha(0f);
            yield return new WaitForSecondsRealtime(RepeatDelay);
        }
    }

    private void HideWave()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = null;
        canvasRenderer.SetAlpha(0f);
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Color tint = color;
        float skew = rect.width * 0.22f;
        float bandWidth = rect.width - skew;

        AddColumn(mesh, rect, rect.xMin, skew, tint, 0f);
        AddColumn(mesh, rect, rect.xMin + bandWidth * 0.38f, skew, tint, 0.18f);
        AddColumn(mesh, rect, rect.xMin + bandWidth * 0.57f, skew, tint, 0.54f);
        AddColumn(mesh, rect, rect.xMin + bandWidth, skew, tint, 0f);

        for (int column = 0; column < 3; column++)
        {
            for (int row = 0; row < 3; row++)
            {
                int i = column * 4 + row;
                mesh.AddTriangle(i, i + 1, i + 5);
                mesh.AddTriangle(i, i + 5, i + 4);
            }
        }
    }

    private static void AddColumn(VertexHelper mesh, Rect rect, float x, float skew, Color tint, float alpha)
    {
        Color faded = new Color(tint.r, tint.g, tint.b, 0f);
        tint.a *= alpha;
        mesh.AddVert(new Vector3(x, rect.yMin), faded, Vector2.zero);
        mesh.AddVert(new Vector3(x + skew * 0.18f, rect.yMin + rect.height * 0.18f), tint, Vector2.zero);
        mesh.AddVert(new Vector3(x + skew * 0.82f, rect.yMin + rect.height * 0.82f), tint, Vector2.zero);
        mesh.AddVert(new Vector3(x + skew, rect.yMax), faded, Vector2.zero);
    }
}
