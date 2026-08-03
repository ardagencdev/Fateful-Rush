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

    private const string CompletedLevelKeyPrefix =
        "CompletedLevel_";

    [Serializable]
    public class SkinEntry
    {
        [Tooltip("Kalıcı kayıt için benzersiz kimlik. Örnek: white, red, golden.")]
        public string id;

        public string displayName;
        public Sprite playerSprite;
        [ColorUsage(true, true)]
        public Color dashTrailColor = Color.white;

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
            return IsSelected(skin)
                ? "EQUIPPED"
                : "UNLOCKED";

        return $"COMPLETE LEVEL {skin.requiredCompletedLevel}";
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skins == null)
            return;

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
                Mathf.Max(0, skin.requiredCompletedLevel);

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
