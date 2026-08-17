using TMPro;
using UnityEngine;

public class CurrentLevelHUD : MonoBehaviour
{
    private const float FontSize = 18f;
    private const float TopOffset = 3f;

    private TextMeshProUGUI levelText;
    private PlayerSkinApplier skinApplier;
    private PlayerSkinCatalog.SkinEntry cachedSkin;

    public static CurrentLevelHUD Create(
        LevelConfig level,
        PlayerSkinApplier playerSkinApplier,
        Canvas targetCanvas,
        int siblingIndex)
    {
        if (level == null ||
            level.levelNumber <= 0 ||
            targetCanvas == null)
        {
            return null;
        }

        TMP_FontAsset sceneFont =
            FindMichromaFont(targetCanvas);

        GameObject levelHudObject =
            new GameObject(
                "Current Level HUD",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(CurrentLevelHUD)
            );

        levelHudObject.transform.SetParent(
            targetCanvas.transform,
            false
        );

        int safeSiblingIndex =
            Mathf.Clamp(
                siblingIndex,
                0,
                targetCanvas.transform.childCount - 1
            );

        levelHudObject.transform.SetSiblingIndex(
            safeSiblingIndex
        );

        RectTransform rectTransform =
            levelHudObject.GetComponent<RectTransform>();

        rectTransform.anchorMin =
            new Vector2(0.5f, 1f);

        rectTransform.anchorMax =
            new Vector2(0.5f, 1f);

        rectTransform.pivot =
            new Vector2(0.5f, 1f);

        rectTransform.anchoredPosition =
            new Vector2(0f, -TopOffset);

        rectTransform.sizeDelta =
            new Vector2(420f, 48f);

        TextMeshProUGUI text =
            levelHudObject.GetComponent<TextMeshProUGUI>();

        if (sceneFont != null)
            text.font = sceneFont;

        text.text =
            $"LEVEL {level.levelNumber}";

        text.fontSize = FontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.characterSpacing = 2f;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Overflow;

        CurrentLevelHUD levelHud =
            levelHudObject.GetComponent<CurrentLevelHUD>();

        levelHud.Configure(
            text,
            playerSkinApplier
        );

        levelHud.SetVisible(false);

        return levelHud;
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
    }

    private void Configure(
        TextMeshProUGUI text,
        PlayerSkinApplier playerSkinApplier)
    {
        levelText = text;
        skinApplier = playerSkinApplier;

        RefreshSkinColor();
    }

    private void OnEnable()
    {
        if (levelText == null)
            levelText = GetComponent<TextMeshProUGUI>();

        RefreshSkinColor();
        RegisterWithOcclusionController();

        if (levelText != null)
            levelText.SetVerticesDirty();
    }

    private void LateUpdate()
    {
        if (skinApplier != null &&
            cachedSkin != skinApplier.CurrentSkin)
        {
            RefreshSkinColor();
        }
    }

    private void RegisterWithOcclusionController()
    {
        HUDPlayerOcclusionController controller =
            FindAnyObjectByType<HUDPlayerOcclusionController>();

        if (controller != null)
            controller.RegisterHUDRoot(gameObject);
    }

    private void RefreshSkinColor()
    {
        if (levelText == null)
            levelText = GetComponent<TextMeshProUGUI>();

        if (levelText == null)
            return;

        cachedSkin =
            skinApplier != null
                ? skinApplier.CurrentSkin
                : null;

        Color skinColor =
            skinApplier != null
                ? skinApplier.CurrentDashTrailColor
                : Color.white;

        levelText.color =
            NormalizeSkinColor(skinColor);
    }

    private static Color NormalizeSkinColor(
        Color color)
    {
        float highestChannel =
            Mathf.Max(
                color.r,
                color.g,
                color.b
            );

        if (highestChannel > 1f)
        {
            color.r /= highestChannel;
            color.g /= highestChannel;
            color.b /= highestChannel;
        }

        color.r = Mathf.Clamp01(color.r);
        color.g = Mathf.Clamp01(color.g);
        color.b = Mathf.Clamp01(color.b);
        color.a = 1f;

        return color;
    }

    private static TMP_FontAsset FindMichromaFont(
        Canvas targetCanvas)
    {
        if (targetCanvas != null)
        {
            TextMeshProUGUI[] canvasTexts =
                targetCanvas.GetComponentsInChildren
                    <TextMeshProUGUI>(true);

            for (int i = 0;
                 i < canvasTexts.Length;
                 i++)
            {
                TMP_FontAsset font =
                    canvasTexts[i] != null
                        ? canvasTexts[i].font
                        : null;

                if (IsMichroma(font))
                    return font;
            }
        }

        TMP_FontAsset[] loadedFonts =
            Resources.FindObjectsOfTypeAll
                <TMP_FontAsset>();

        for (int i = 0;
             i < loadedFonts.Length;
             i++)
        {
            if (IsMichroma(loadedFonts[i]))
                return loadedFonts[i];
        }

        return TMP_Settings.defaultFontAsset;
    }

    private static bool IsMichroma(
        TMP_FontAsset font)
    {
        return
            font != null &&
            !string.IsNullOrEmpty(font.name) &&
            font.name.IndexOf(
                "Michroma",
                System.StringComparison.OrdinalIgnoreCase
            ) >= 0;
    }
}
