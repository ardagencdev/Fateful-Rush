using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public sealed class WinConditionIntroUI : MonoBehaviour
{
    private const int OverlaySortingOrder = 10000;

    private Canvas overlayCanvas;
    private CanvasGroup overlayGroup;
    private RectTransform contentTransform;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI mechanicText;
    private TextMeshProUGUI inputHintText;

    public IEnumerator PlayAndWait(
        LevelConfig level,
        float totalDuration,
        float fadeDuration,
        bool allowSkip)
    {
        if (level == null)
            yield break;

        // IMPORTANT:
        // Do not create any visible TMP object before localization is ready.
        // The old implementation created the overlay in English first and a
        // separate runtime script replaced the strings afterwards, which is
        // exactly what caused the visible EN -> TR flash.
        yield return EnsureLocalizationReady();

        string localizedLevel =
            BuildLevelText(level);

        string localizedObjective =
            BuildObjectiveText(level);

        string introducedMechanics =
            BuildIntroducedMechanicsText(level);

        string localizedTitle =
            FatefulRushLocalization.Text(
                "intro.win_condition",
                FatefulRushLocalization.IsTurkish
                    ? "KAZANMA KOŞULU"
                    : "WIN CONDITION"
            );

        // Android project only. The PC port lives in a separate project, so
        // this project must never advertise SPACE/keyboard input.
        string localizedInputHint =
            FatefulRushLocalization.Text(
                "intro.tap_to_start",
                FatefulRushLocalization.IsTurkish
                    ? "BAŞLAMAK İÇİN DOKUN"
                    : "TAP TO START"
            );

        DestroyOverlay();
        CreateOverlay(localizedTitle);

        // All strings are already final before the first rendered frame.
        levelText.text = localizedLevel;
        objectiveText.text = localizedObjective;

        bool hasIntroducedMechanic =
            !string.IsNullOrWhiteSpace(introducedMechanics);

        mechanicText.text = introducedMechanics;
        mechanicText.gameObject.SetActive(hasIntroducedMechanic);

        inputHintText.rectTransform.anchoredPosition =
            new Vector2(
                0f,
                hasIntroducedMechanic ? -180f : -142f
            );

        inputHintText.text = localizedInputHint;

        float safeTotalDuration =
            Mathf.Max(0.5f, totalDuration);

        float safeFadeDuration =
            Mathf.Clamp(
                fadeDuration,
                0f,
                safeTotalDuration * 0.45f
            );

        overlayGroup.alpha = 0f;

        contentTransform.localScale =
            Vector3.one * 0.92f;

        float elapsedTotal = 0f;
        bool skipRequested = false;

        if (safeFadeDuration > 0f)
        {
            float fadeElapsed = 0f;

            while (fadeElapsed < safeFadeDuration)
            {
                float deltaTime =
                    Time.unscaledDeltaTime;

                fadeElapsed += deltaTime;
                elapsedTotal += deltaTime;

                float progress =
                    Mathf.Clamp01(
                        fadeElapsed /
                        safeFadeDuration
                    );

                float easedProgress =
                    EaseOutCubic(progress);

                overlayGroup.alpha =
                    easedProgress;

                contentTransform.localScale =
                    Vector3.one * Mathf.Lerp(
                        0.92f,
                        1f,
                        easedProgress
                    );

                if (allowSkip &&
                    WasStartInputPressed())
                {
                    skipRequested = true;
                    break;
                }

                yield return null;
            }
        }
        else
        {
            overlayGroup.alpha = 1f;

            contentTransform.localScale =
                Vector3.one;
        }

        if (!skipRequested)
        {
            overlayGroup.alpha = 1f;

            contentTransform.localScale =
                Vector3.one;

            float automaticFadeStart =
                Mathf.Max(
                    elapsedTotal,
                    safeTotalDuration -
                    safeFadeDuration
                );

            while (elapsedTotal < automaticFadeStart)
            {
                float deltaTime =
                    Time.unscaledDeltaTime;

                elapsedTotal += deltaTime;

                if (allowSkip &&
                    WasStartInputPressed())
                {
                    skipRequested = true;
                    break;
                }

                yield return null;
            }
        }

        float currentAlpha =
            overlayGroup != null
                ? overlayGroup.alpha
                : 0f;

        if (overlayGroup != null &&
            currentAlpha > 0f)
        {
            float actualFadeOutDuration =
                safeFadeDuration > 0f
                    ? Mathf.Max(
                        0.12f,
                        safeFadeDuration *
                        currentAlpha
                    )
                    : 0.12f;

            float fadeOutElapsed = 0f;

            Vector3 startScale =
                contentTransform.localScale;

            while (fadeOutElapsed <
                   actualFadeOutDuration)
            {
                fadeOutElapsed +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        fadeOutElapsed /
                        actualFadeOutDuration
                    );

                float easedProgress =
                    EaseInCubic(progress);

                overlayGroup.alpha =
                    Mathf.Lerp(
                        currentAlpha,
                        0f,
                        easedProgress
                    );

                contentTransform.localScale =
                    Vector3.Lerp(
                        startScale,
                        Vector3.one * 1.04f,
                        easedProgress
                    );

                yield return null;
            }
        }

        DestroyOverlay();
    }

    private static IEnumerator EnsureLocalizationReady()
    {
        var initialization =
            LocalizationSettings.InitializationOperation;

        while (!initialization.IsDone)
            yield return null;

        // The saved menu choice is authoritative. Re-apply it after Unity
        // Localization initialization, then discard any early fallback cache.
        FatefulRushLocalization.ClearCache();
        FatefulRushLocalization.ApplySavedLocaleIfPossible();
        FatefulRushLocalization.ClearCache();

        // Locale assignment can notify Unity systems on this frame.
        // Nothing has been rendered yet, so waiting one frame is safe and
        // guarantees that the intro never exposes a fallback language.
        yield return null;
    }

    private void CreateOverlay(string localizedTitle)
    {
        GameObject canvasObject =
            new GameObject(
                "Win Condition Intro Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        overlayCanvas =
            canvasObject.GetComponent<Canvas>();

        overlayCanvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        overlayCanvas.sortingOrder =
            OverlaySortingOrder;

        CanvasScaler canvasScaler =
            canvasObject.GetComponent<CanvasScaler>();

        canvasScaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        canvasScaler.referenceResolution =
            Screen.width >= Screen.height
                ? new Vector2(1920f, 1080f)
                : new Vector2(1080f, 1920f);

        canvasScaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject overlayObject =
            CreateUIObject(
                "Overlay",
                canvasObject.transform
            );

        RectTransform overlayRect =
            overlayObject.GetComponent<RectTransform>();

        StretchToParent(overlayRect);

        Image dimBackground =
            overlayObject.AddComponent<Image>();

        dimBackground.color =
            new Color(0f, 0f, 0f, 0.22f);

        dimBackground.raycastTarget = false;

        overlayGroup =
            overlayObject.AddComponent<CanvasGroup>();

        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        GameObject contentObject =
            CreateUIObject(
                "Content",
                overlayObject.transform
            );

        contentTransform =
            contentObject.GetComponent<RectTransform>();

        contentTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        contentTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        contentTransform.pivot =
            new Vector2(0.5f, 0.5f);

        contentTransform.anchoredPosition =
            Vector2.zero;

        contentTransform.sizeDelta =
            new Vector2(1500f, 600f);

        TMP_FontAsset sceneFont =
            FindSceneFont();

        Color skinAccentColor =
            GetSelectedSkinAccentColor();

        levelText =
            CreateText(
                "Level",
                contentTransform,
                sceneFont,
                string.Empty,
                25f,
                new Vector2(0f, 180f),
                new Vector2(1450f, 55f)
            );

        levelText.color =
            SetAlpha(skinAccentColor, 0.82f);

        levelText.characterSpacing = 4.5f;
        levelText.fontStyle = FontStyles.Bold;

        TextMeshProUGUI titleText =
            CreateText(
                "Title",
                contentTransform,
                sceneFont,
                localizedTitle,
                34f,
                new Vector2(0f, 122f),
                new Vector2(1450f, 70f)
            );

        titleText.color =
            new Color(1f, 1f, 1f, 0.72f);

        titleText.characterSpacing = 5f;

        objectiveText =
            CreateText(
                "Objective",
                contentTransform,
                sceneFont,
                string.Empty,
                68f,
                new Vector2(0f, 5f),
                new Vector2(1500f, 170f)
            );

        objectiveText.enableAutoSizing = true;
        objectiveText.fontSizeMin = 34f;
        objectiveText.fontSizeMax = 68f;
        objectiveText.fontStyle = FontStyles.Bold;

        mechanicText =
            CreateText(
                "New Mechanic",
                contentTransform,
                sceneFont,
                string.Empty,
                26f,
                new Vector2(0f, -108f),
                new Vector2(1450f, 50f)
            );

        mechanicText.enableAutoSizing = true;
        mechanicText.fontSizeMin = 20f;
        mechanicText.fontSizeMax = 26f;
        mechanicText.fontStyle = FontStyles.Bold;
        mechanicText.characterSpacing = 2.2f;
        mechanicText.color =
            SetAlpha(skinAccentColor, 0.92f);

        inputHintText =
            CreateText(
                "Input Hint",
                contentTransform,
                sceneFont,
                string.Empty,
                23f,
                new Vector2(0f, -180f),
                new Vector2(1450f, 60f)
            );

        inputHintText.color =
            new Color(1f, 1f, 1f, 0.52f);

        inputHintText.characterSpacing = 2.5f;
    }

    private static Color GetSelectedSkinAccentColor()
    {
        PlayerSkinCatalog catalog =
            ResolveSkinCatalog();

        if (catalog != null)
            return catalog.GetSelectedUIThemeColor();

        return new Color(0.78f, 0.72f, 1f, 1f);
    }

    private static PlayerSkinCatalog ResolveSkinCatalog()
    {
        if (PlayerSkinCatalog.LoadedInstance != null)
            return PlayerSkinCatalog.LoadedInstance;

        PlayerSkinCatalog[] catalogs =
            Resources.FindObjectsOfTypeAll<PlayerSkinCatalog>();

        if (catalogs == null || catalogs.Length == 0)
            return null;

        for (int i = 0; i < catalogs.Length; i++)
        {
            PlayerSkinCatalog catalog = catalogs[i];

            if (catalog != null &&
                string.Equals(
                    catalog.name,
                    "PlayerSkinCatalog",
                    System.StringComparison.Ordinal
                ))
            {
                return catalog;
            }
        }

        return catalogs[0];
    }

    private static Color SetAlpha(
        Color color,
        float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        TMP_FontAsset font,
        string content,
        float fontSize,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject textObject =
            CreateUIObject(
                objectName,
                parent
            );

        RectTransform rectTransform =
            textObject.GetComponent<RectTransform>();

        rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition =
            anchoredPosition;

        rectTransform.sizeDelta = size;

        TextMeshProUGUI text =
            textObject.AddComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.text = content;
        text.fontSize = fontSize;

        text.alignment =
            TextAlignmentOptions.Center;

        text.textWrappingMode =
            TextWrappingModes.Normal;

        text.overflowMode =
            TextOverflowModes.Overflow;

        text.color = Color.white;
        text.raycastTarget = false;
        text.outlineWidth = 0.16f;

        text.outlineColor =
            new Color(0f, 0f, 0f, 0.9f);

        return text;
    }

    private static GameObject CreateUIObject(
        string objectName,
        Transform parent)
    {
        GameObject result =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        result.transform.SetParent(
            parent,
            false
        );

        return result;
    }

    private static void StretchToParent(
        RectTransform target)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;

        target.pivot =
            new Vector2(0.5f, 0.5f);

        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }

    private static TMP_FontAsset FindSceneFont()
    {
        TextMeshProUGUI[] texts =
            UnityFindCompat.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include
            );

        for (int i = 0;
             i < texts.Length;
             i++)
        {
            if (texts[i] != null &&
                texts[i].font != null)
            {
                return texts[i].font;
            }
        }

        return null;
    }

    private static string BuildLevelText(
        LevelConfig level)
    {
        if (level == null)
            return string.Empty;

        string originalName =
            string.IsNullOrWhiteSpace(level.levelName)
                ? string.Empty
                : level.levelName.Trim();

        string localizedName =
            FatefulRushLocalization.LevelName(
                level.levelNumber,
                originalName
            );

        localizedName =
            ToDisplayUpper(localizedName);

        if (level.levelNumber > 0)
        {
            if (string.IsNullOrWhiteSpace(localizedName))
            {
                return FatefulRushLocalization.Text(
                    "hud.level",
                    FatefulRushLocalization.IsTurkish
                        ? "BÖLÜM {0}"
                        : "LEVEL {0}",
                    level.levelNumber
                );
            }

            return FatefulRushLocalization.Text(
                "briefing.level_title",
                FatefulRushLocalization.IsTurkish
                    ? "BÖLÜM {0} — {1}"
                    : "LEVEL {0} — {1}",
                level.levelNumber,
                localizedName
            );
        }

        if (!string.IsNullOrWhiteSpace(localizedName))
            return localizedName;

        return FatefulRushLocalization.IsTurkish
            ? "GÖREV"
            : "MISSION";
    }

    private static string BuildObjectiveText(
        LevelConfig level)
    {
        switch (level.winCondition)
        {
            case WinConditionType.ReachScore:
                return FatefulRushLocalization.Text(
                    "objective.reach_score",
                    FatefulRushLocalization.IsTurkish
                        ? "{0} SKORA ULAŞ"
                        : "REACH {0} SCORE",
                    level.SafeWinScore
                );

            case WinConditionType.SurviveTime:
                return FatefulRushLocalization.Text(
                    "objective.survive_time",
                    FatefulRushLocalization.IsTurkish
                        ? "{0} SANİYE HAYATTA KAL"
                        : "SURVIVE FOR {0} SECONDS",
                    FormatNumber(level.SafeTimeLimit)
                );

            case WinConditionType.ReachScoreWithinTime:
                return FatefulRushLocalization.Text(
                    "objective.reach_score_in_time",
                    FatefulRushLocalization.IsTurkish
                        ? "{1} SANİYE İÇİNDE {0} SKORA ULAŞ"
                        : "REACH {0} SCORE IN {1} SECONDS",
                    level.SafeWinScore,
                    FormatNumber(level.SafeTimeLimit)
                );

            default:
                return FatefulRushLocalization.Text(
                    "objective.complete_mission",
                    FatefulRushLocalization.IsTurkish
                        ? "GÖREVİ TAMAMLA"
                        : "COMPLETE THE MISSION"
                );
        }
    }

    private static string BuildIntroducedMechanicsText(
        LevelConfig level)
    {
        if (level == null ||
            level.mechanicProgression == null)
        {
            return string.Empty;
        }

        LevelMechanicProgression progression =
            level.mechanicProgression;

        List<string> introduced =
            new List<string>();

        AddIfIntroduced(
            introduced,
            progression.reachScoreMode,
            "mechanic.score_mode",
            "SCORE MODE",
            "SKOR MODU"
        );

        AddIfIntroduced(
            introduced,
            progression.surviveTimeMode,
            "mechanic.survival_mode",
            "SURVIVAL MODE",
            "HAYATTA KALMA MODU"
        );

        AddIfIntroduced(
            introduced,
            progression.timedScoreMode,
            "mechanic.timed_score",
            "TIMED SCORE",
            "SÜRELİ SKOR"
        );

        AddIfIntroduced(introduced, progression.dash, "mechanic.dash", "DASH", "DASH");
        AddIfIntroduced(introduced, progression.clone, "mechanic.clone", "CLONE", "KLON");
        AddIfIntroduced(introduced, progression.combo, "mechanic.combo", "COMBO", "KOMBO");
        AddIfIntroduced(introduced, progression.normalCoin, "mechanic.coins", "COINS", "COINLER");
        AddIfIntroduced(introduced, progression.goldCoin, "mechanic.gold_coins", "GOLD COINS", "ALTIN COINLER");
        AddIfIntroduced(introduced, progression.rareCoin, "mechanic.rare_coins", "RARE COINS", "NADİR COINLER");
        AddIfIntroduced(introduced, progression.staticObstacles, "mechanic.obstacles", "OBSTACLES", "ENGELLER");
        AddIfIntroduced(introduced, progression.normalEnemy, "mechanic.stalker", "STALKER", "STALKER");
        AddIfIntroduced(introduced, progression.projectileEnemy, "mechanic.blaster", "BLASTER", "BLASTER");
        AddIfIntroduced(introduced, progression.hunterEnemy, "mechanic.hunter", "HUNTER", "HUNTER");
        AddIfIntroduced(introduced, progression.boss, "mechanic.boss", "BOSS", "BOSS");
        AddIfIntroduced(introduced, progression.beaconEnemy, "mechanic.beacon", "BEACON", "BEACON");
        AddIfIntroduced(introduced, progression.armor, "mechanic.armor", "ARMOR", "ZIRH");
        AddIfIntroduced(introduced, progression.slow, "mechanic.slow", "SLOW", "YAVAŞLATMA");
        AddIfIntroduced(introduced, progression.verticalLaser, "mechanic.vertical_laser", "VERTICAL LASER", "DİKEY LAZER");
        AddIfIntroduced(introduced, progression.horizontalLaser, "mechanic.horizontal_laser", "HORIZONTAL LASER", "YATAY LAZER");
        AddIfIntroduced(introduced, progression.spaceBomb, "mechanic.space_bomb", "SPACE BOMB", "UZAY BOMBASI");

        if (introduced.Count == 0)
            return string.Empty;

        string newLabel =
            FatefulRushLocalization.Text(
                "intro.new",
                FatefulRushLocalization.IsTurkish
                    ? "YENİ"
                    : "NEW"
            );

        return newLabel +
               "  •  " +
               string.Join("  •  ", introduced);
    }

    private static void AddIfIntroduced(
        List<string> target,
        MechanicProgressionStatus status,
        string key,
        string englishFallback,
        string turkishFallback)
    {
        if (status !=
            MechanicProgressionStatus.IntroducedHere)
        {
            return;
        }

        target.Add(
            FatefulRushLocalization.Text(
                key,
                FatefulRushLocalization.IsTurkish
                    ? turkishFallback
                    : englishFallback
            )
        );
    }

    private static string ToDisplayUpper(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (FatefulRushLocalization.IsTurkish)
        {
            try
            {
                return value.ToUpper(
                    CultureInfo.GetCultureInfo("tr-TR")
                );
            }
            catch
            {
                // Safe fallback below.
            }
        }

        return value.ToUpperInvariant();
    }

    private static string FormatNumber(float value)
    {
        float safe = Mathf.Max(0f, value);

        return Mathf.Approximately(
                safe,
                Mathf.Round(safe))
            ? Mathf.RoundToInt(safe).ToString()
            : safe.ToString("0.#");
    }

    private static bool WasStartInputPressed()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current
                .primaryTouch
                .press
                .wasPressedThisFrame)
        {
            return true;
        }

        // Keeping mouse/keyboard/gamepad as silent Editor testing inputs is
        // useful, but the Android UI never advertises them to the player.
        if (Mouse.current != null &&
            Mouse.current
                .leftButton
                .wasPressedThisFrame)
        {
            return true;
        }

        if (Keyboard.current != null &&
            (Keyboard.current
                 .spaceKey
                 .wasPressedThisFrame ||
             Keyboard.current
                 .enterKey
                 .wasPressedThisFrame ||
             Keyboard.current
                 .numpadEnterKey
                 .wasPressedThisFrame))
        {
            return true;
        }

        if (Gamepad.current != null &&
            Gamepad.current
                .buttonSouth
                .wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - value;

        return 1f -
               inverse *
               inverse *
               inverse;
    }

    private static float EaseInCubic(float value)
    {
        return value * value * value;
    }

    private void DestroyOverlay()
    {
        if (overlayCanvas != null)
            Destroy(overlayCanvas.gameObject);

        overlayCanvas = null;
        overlayGroup = null;
        contentTransform = null;
        levelText = null;
        objectiveText = null;
        mechanicText = null;
        inputHintText = null;
    }

    private void OnDisable()
    {
        DestroyOverlay();
    }
}
