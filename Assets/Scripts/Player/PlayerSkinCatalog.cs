using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerSkinCatalog",
    menuName = "Void Rush/Player Skin Catalog"
)]
public class PlayerSkinCatalog : ScriptableObject
{
    public const string SelectedSkinKey =
        "SelectedPlayerSkinId";

    private const string DebugAllSkinsUnlockedKey =
        "DebugAllPlayerSkinsUnlocked";

    private const string CompletedLevelKeyPrefix =
        "CompletedLevel_";

    private const int CurrentArmorColorVersion = 2;

    [Serializable]
    public class SkinEntry
    {
        [Tooltip("Kalıcı kayıt için benzersiz kimlik. Örnek: white, red, golden.")]
        public string id;

        public string displayName;
        public Sprite playerSprite;

        [ColorUsage(true, true)]
        public Color dashTrailColor = Color.white;

        [ColorUsage(true, true)]
        public Color armorVisualColor = Color.white;

        [HideInInspector]
        public int armorVisualColorVersion;

        [Min(0)]
        [Tooltip(
            "0 ise başlangıçtan açık. 1-40 ise o bölüm tamamlanınca açılır."
        )]
        public int requiredCompletedLevel;
    }

    [SerializeField]
    private List<SkinEntry> skins =
        new List<SkinEntry>();

    [SerializeField, Min(0)]
    [Tooltip("Normalde WhitePlayer girdisinin indexi 0 olmalı.")]
    private int defaultSkinIndex;

    public IReadOnlyList<SkinEntry> Skins => skins;

    public SkinEntry DefaultSkin
    {
        get
        {
            if (skins == null || skins.Count == 0)
                return null;

            int safeIndex = Mathf.Clamp(
                defaultSkinIndex,
                0,
                skins.Count - 1
            );

            return skins[safeIndex];
        }
    }

    public static bool AreAllSkinsDebugUnlocked
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return PlayerPrefs.GetInt(
                DebugAllSkinsUnlockedKey,
                0
            ) == 1;
#else
            return false;
#endif
        }
    }

    private void OnEnable()
    {
        EnsureArmorVisualColors();
    }

    public SkinEntry GetSelectedSkin()
    {
        SkinEntry fallback = GetFallbackSkin();

        if (fallback == null)
            return null;

        string savedId = PlayerPrefs.GetString(
            SelectedSkinKey,
            fallback.id
        );

        SkinEntry savedSkin = FindById(savedId);

        if (savedSkin != null && IsUnlocked(savedSkin))
            return savedSkin;

        SaveSelectedSkin(fallback);
        return fallback;
    }

    public bool TrySelectSkin(SkinEntry skin)
    {
        if (skin == null || !IsUnlocked(skin))
            return false;

        SaveSelectedSkin(skin);
        return true;
    }

    public bool IsSelected(SkinEntry skin)
    {
        if (skin == null)
            return false;

        SkinEntry selected = GetSelectedSkin();

        return selected != null &&
               string.Equals(
                   selected.id,
                   skin.id,
                   StringComparison.Ordinal
               );
    }

    public bool IsUnlocked(SkinEntry skin)
    {
        if (skin == null)
            return false;

        if (AreAllSkinsDebugUnlocked)
            return true;

        if (skin.requiredCompletedLevel <= 0)
            return true;

        return PlayerPrefs.GetInt(
            CompletedLevelKeyPrefix +
            skin.requiredCompletedLevel,
            0
        ) == 1;
    }

    public SkinEntry FindById(string skinId)
    {
        if (skins == null ||
            string.IsNullOrWhiteSpace(skinId))
        {
            return null;
        }

        for (int i = 0; i < skins.Count; i++)
        {
            SkinEntry skin = skins[i];

            if (skin == null)
                continue;

            if (string.Equals(
                    skin.id,
                    skinId,
                    StringComparison.Ordinal))
            {
                return skin;
            }
        }

        return null;
    }

    public string GetRequirementText(SkinEntry skin)
    {
        if (skin == null)
            return string.Empty;

        if (IsUnlocked(skin))
        {
            return IsSelected(skin)
                ? "EQUIPPED"
                : "UNLOCKED";
        }

        return $"COMPLETE LEVEL {skin.requiredCompletedLevel}";
    }

    public static void SetDebugAllSkinsUnlocked(
        bool unlocked
    )
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (unlocked)
        {
            PlayerPrefs.SetInt(
                DebugAllSkinsUnlockedKey,
                1
            );
        }
        else
        {
            PlayerPrefs.DeleteKey(
                DebugAllSkinsUnlockedKey
            );
        }

        PlayerPrefs.Save();
#endif
    }

    public static bool ToggleDebugAllSkinsUnlocked()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool newState =
            !AreAllSkinsDebugUnlocked;

        SetDebugAllSkinsUnlocked(newState);

        return newState;
#else
        return false;
#endif
    }

    public static void ClearSavedSelection()
    {
        PlayerPrefs.DeleteKey(SelectedSkinKey);
    }

    private SkinEntry GetFallbackSkin()
    {
        SkinEntry defaultSkin = DefaultSkin;

        if (defaultSkin != null &&
            IsUnlocked(defaultSkin))
        {
            return defaultSkin;
        }

        if (skins == null)
            return null;

        for (int i = 0; i < skins.Count; i++)
        {
            if (IsUnlocked(skins[i]))
                return skins[i];
        }

        return null;
    }

    private static void SaveSelectedSkin(SkinEntry skin)
    {
        if (skin == null ||
            string.IsNullOrWhiteSpace(skin.id))
        {
            return;
        }

        PlayerPrefs.SetString(
            SelectedSkinKey,
            skin.id
        );

        PlayerPrefs.Save();
    }

    private void EnsureArmorVisualColors()
    {
        if (skins == null)
            return;

        for (int i = 0; i < skins.Count; i++)
        {
            SkinEntry skin = skins[i];

            if (skin == null ||
                skin.armorVisualColorVersion >=
                CurrentArmorColorVersion)
            {
                continue;
            }

            skin.armorVisualColor =
                GetDefaultArmorVisualColor(skin);

            skin.armorVisualColorVersion =
                CurrentArmorColorVersion;
        }
    }

    private static Color GetDefaultArmorVisualColor(
        SkinEntry skin
    )
    {
        if (skin == null)
            return Color.white;

        string normalizedId =
            string.IsNullOrWhiteSpace(skin.id)
                ? string.Empty
                : skin.id
                    .Trim()
                    .ToLowerInvariant()
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace(" ", string.Empty);

        switch (normalizedId)
        {
            case "white":
                return new Color32(220, 232, 240, 255);

            case "blue":
                return new Color32(40, 170, 232, 255);

            case "cyan":
            case "lightblue":
                return new Color32(49, 233, 241, 255);

            case "yellow":
                return new Color32(254, 236, 7, 255);

            case "orange":
                return new Color32(255, 166, 6, 255);

            case "red":
                return new Color32(245, 30, 34, 255);

            case "purple":
                return new Color32(170, 81, 209, 255);

            case "dark":
            case "black":
                return new Color32(78, 96, 108, 255);

            case "silver":
            case "gray":
            case "grey":
                return new Color32(190, 205, 216, 255);

            case "gold":
            case "golden":
                return new Color32(238, 196, 85, 255);

            default:
                return NormalizeHdrColor(
                    skin.dashTrailColor
                );
        }
    }

    private static Color NormalizeHdrColor(Color color)
    {
        float highestChannel = Mathf.Max(
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

        color.a = 1f;
        return color;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skins == null)
            return;

        EnsureArmorVisualColors();

        defaultSkinIndex = Mathf.Clamp(
            defaultSkinIndex,
            0,
            Mathf.Max(0, skins.Count - 1)
        );

        HashSet<string> usedIds =
            new HashSet<string>();

        HashSet<int> usedUnlockLevels =
            new HashSet<int>();

        for (int i = 0; i < skins.Count; i++)
        {
            SkinEntry skin = skins[i];

            if (skin == null)
                continue;

            skin.requiredCompletedLevel =
                Mathf.Max(
                    0,
                    skin.requiredCompletedLevel
                );

            if (string.IsNullOrWhiteSpace(skin.id))
            {
                Debug.LogWarning(
                    $"Player Skin Catalog: Skin {i} için ID boş.",
                    this
                );
            }
            else if (!usedIds.Add(skin.id))
            {
                Debug.LogWarning(
                    $"Player Skin Catalog: Tekrarlanan skin ID: {skin.id}",
                    this
                );
            }

            if (skin.requiredCompletedLevel > 0 &&
                !usedUnlockLevels.Add(
                    skin.requiredCompletedLevel))
            {
                Debug.LogWarning(
                    "Player Skin Catalog: Birden fazla skin aynı " +
                    $"level görevine bağlı: {skin.requiredCompletedLevel}",
                    this
                );
            }
        }

        SkinEntry defaultSkin = DefaultSkin;

        if (defaultSkin != null &&
            defaultSkin.requiredCompletedLevel != 0)
        {
            Debug.LogWarning(
                "Player Skin Catalog: Varsayılan skin başlangıçtan " +
                "açık olmalı. Required Completed Level değerini 0 yap.",
                this
            );
        }
    }
#endif
}
