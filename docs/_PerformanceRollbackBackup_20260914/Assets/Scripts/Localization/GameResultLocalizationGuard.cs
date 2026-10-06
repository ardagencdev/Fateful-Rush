using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(11000)]
public sealed class GameResultLocalizationGuard : MonoBehaviour
{
    private const float RefreshInterval = 0.05f;
    private const float TargetRefreshInterval = 1f;

    private static GameResultLocalizationGuard instance;

    private readonly List<ResultTextSet> resultTextSets =
        new List<ResultTextSet>();

    private float refreshTimer;
    private float targetRefreshTimer;
    private bool forceRefresh = true;

    private sealed class ResultTextSet
    {
        public TMP_Text newBestTime;
        public TMP_Text skinUnlockedTitle;
        public TMP_Text unlockedSkinName;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root =
            new GameObject("Game Result Localization Guard");

        DontDestroyOnLoad(root);

        instance =
            root.AddComponent<GameResultLocalizationGuard>();
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
        LocalizationSettings.SelectedLocaleChanged +=
            HandleLocaleChanged;

        RefreshTargets();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged -=
            HandleLocaleChanged;

        instance = null;
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        forceRefresh = true;
        targetRefreshTimer = TargetRefreshInterval;
        RefreshTargets();
    }

    private void HandleLocaleChanged(Locale locale)
    {
        FatefulRushLocalization.ClearCache();
        RequestRefresh();
    }

    private void LateUpdate()
    {
        // No 20 Hz localization pass and no 1-second FindObjects scan.
        // Result UI and locale-change events call RequestRefresh explicitly.
        if (!forceRefresh)
            return;

        forceRefresh = false;
        ApplyResultLocalization();
    }

    private void RefreshTargets()
    {
        resultTextSets.Clear();

        GameResultUI[] resultUIs =
            FindObjectsByType<GameResultUI>(
                FindObjectsInactive.Include
            );

        for (int i = 0; i < resultUIs.Length; i++)
        {
            GameResultUI ui = resultUIs[i];

            if (ui == null)
                continue;

            TMP_Text[] texts =
                ui.GetComponentsInChildren<TMP_Text>(true);

            ResultTextSet set =
                new ResultTextSet();

            for (int j = 0; j < texts.Length; j++)
            {
                TMP_Text text = texts[j];

                if (text == null)
                    continue;

                switch (text.gameObject.name)
                {
                    case "NewBestTimeText":
                        set.newBestTime = text;
                        break;

                    case "SkinUnlockedTitleText":
                        set.skinUnlockedTitle = text;
                        break;

                    case "UnlockedSkinNameText":
                        set.unlockedSkinName = text;
                        break;
                }
            }

            if (set.newBestTime != null ||
                set.skinUnlockedTitle != null ||
                set.unlockedSkinName != null)
            {
                resultTextSets.Add(set);
            }
        }

        forceRefresh = true;
    }

    private void ApplyResultLocalization()
    {
        for (int i = 0; i < resultTextSets.Count; i++)
        {
            ResultTextSet set = resultTextSets[i];

            if (set == null)
                continue;

            if (set.newBestTime != null)
            {
                string localized =
                    FatefulRushLocalization.Text(
                        "result.new_best_time",
                        "NEW BEST TIME"
                    );

                SetTextIfDifferent(
                    set.newBestTime,
                    RemoveTrailingBang(localized)
                );
            }

            if (set.skinUnlockedTitle != null)
            {
                string localized =
                    FatefulRushLocalization.Text(
                        "result.new_skin",
                        "NEW SKIN UNLOCKED"
                    );

                SetTextIfDifferent(
                    set.skinUnlockedTitle,
                    RemoveTrailingBang(localized)
                );
            }

            if (set.unlockedSkinName != null &&
                !string.IsNullOrWhiteSpace(
                    set.unlockedSkinName.text
                ))
            {
                string skinKey =
                    ResolveSkinKeyFromText(
                        set.unlockedSkinName.text
                    );

                if (!string.IsNullOrWhiteSpace(skinKey))
                {
                    string fallback =
                        GetEnglishSkinFallback(skinKey);

                    string localized =
                        FatefulRushLocalization.Text(
                            skinKey,
                            fallback
                        );

                    SetTextIfDifferent(
                        set.unlockedSkinName,
                        localized
                    );
                }
            }
        }
    }

    private static void SetTextIfDifferent(
        TMP_Text text,
        string value)
    {
        if (text == null ||
            string.IsNullOrWhiteSpace(value) ||
            string.Equals(
                text.text,
                value,
                StringComparison.Ordinal))
        {
            return;
        }

        text.text = value;
    }

    private static string ResolveSkinKeyFromText(
        string current)
    {
        string normalized =
            Normalize(current);

        switch (normalized)
        {
            case "WHITE":
            case "BEYAZ":
                return "skin.name.white";

            case "BLUE":
            case "MAVI":
                return "skin.name.blue";

            case "ORANGE":
            case "TURUNCU":
                return "skin.name.orange";

            case "RED":
            case "KIRMIZI":
                return "skin.name.red";

            case "GREEN":
            case "YESIL":
                return "skin.name.green";

            case "PINK":
            case "PEMBE":
                return "skin.name.pink";

            case "YELLOW":
            case "SARI":
                return "skin.name.yellow";

            case "LIGHT BLUE":
            case "CYAN":
            case "ACIK MAVI":
            case "CAMGOBEGI":
                return "skin.name.cyan";

            case "PURPLE":
            case "MOR":
                return "skin.name.purple";

            case "DARK":
            case "KARANLIK":
                return "skin.name.dark";

            case "GOLDEN":
            case "ALTIN":
                return "skin.name.golden";

            default:
                return string.Empty;
        }
    }

    private static string GetEnglishSkinFallback(
        string key)
    {
        switch (key)
        {
            case "skin.name.white":
                return "WHITE";

            case "skin.name.blue":
                return "BLUE";

            case "skin.name.orange":
                return "ORANGE";

            case "skin.name.red":
                return "RED";

            case "skin.name.green":
                return "GREEN";

            case "skin.name.pink":
                return "PINK";

            case "skin.name.yellow":
                return "YELLOW";

            case "skin.name.cyan":
                return "LIGHT BLUE";

            case "skin.name.purple":
                return "PURPLE";

            case "skin.name.dark":
                return "DARK";

            case "skin.name.golden":
                return "GOLDEN";

            default:
                return string.Empty;
        }
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized =
            value.Trim().ToUpperInvariant();

        normalized =
            normalized
                .Replace('İ', 'I')
                .Replace('I', 'I')
                .Replace('Ş', 'S')
                .Replace('Ğ', 'G')
                .Replace('Ü', 'U')
                .Replace('Ö', 'O')
                .Replace('Ç', 'C');

        while (normalized.Contains("  "))
            normalized = normalized.Replace("  ", " ");

        return normalized;
    }

    private static string RemoveTrailingBang(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        string trimmed = value.TrimEnd();

        if (trimmed.EndsWith(
                "!",
                StringComparison.Ordinal))
        {
            trimmed =
                trimmed.Substring(
                    0,
                    trimmed.Length - 1
                ).TrimEnd();
        }

        return trimmed;
    }


    public static void RequestRefresh()
    {
        if (instance == null)
            return;

        instance.RefreshTargets();
        instance.forceRefresh = false;
        instance.ApplyResultLocalization();
    }
}
