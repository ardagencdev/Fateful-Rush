using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public sealed class LanguageSettingsUI : MonoBehaviour
{
    private const string LocalePreferenceKey = "SelectedLocaleCode";

    [Header("Language Selector")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text languageValueText;

    private readonly List<Locale> availableLocales = new List<Locale>();

    private int currentIndex = -1;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedLocaleAtStartup()
    {
        AsyncOperationHandle<LocalizationSettings> initialization =
            LocalizationSettings.InitializationOperation;

        if (initialization.IsDone)
        {
            ApplySavedLocaleIfAvailable();
            return;
        }

        initialization.Completed +=
            _ => ApplySavedLocaleIfAvailable();
    }

    private void Awake()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(SelectPreviousLanguage);
            previousButton.onClick.AddListener(SelectPreviousLanguage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(SelectNextLanguage);
            nextButton.onClick.AddListener(SelectNextLanguage);
        }
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;

        InitializeWhenReady();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(SelectPreviousLanguage);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(SelectNextLanguage);
    }

    public void SelectPreviousLanguage()
    {
        if (!initialized || availableLocales.Count == 0)
            return;

        int targetIndex =
            (currentIndex - 1 + availableLocales.Count) %
            availableLocales.Count;

        SelectLocale(targetIndex);
    }

    public void SelectNextLanguage()
    {
        if (!initialized || availableLocales.Count == 0)
            return;

        int targetIndex =
            (currentIndex + 1) %
            availableLocales.Count;

        SelectLocale(targetIndex);
    }

    private void InitializeWhenReady()
    {
        AsyncOperationHandle<LocalizationSettings> initialization =
            LocalizationSettings.InitializationOperation;

        if (initialization.IsDone)
        {
            Initialize();
            return;
        }

        initialization.Completed +=
            _ =>
            {
                if (this != null && isActiveAndEnabled)
                    Initialize();
            };
    }

    private void Initialize()
    {
        RebuildLocaleList();

        Locale selectedLocale =
            LocalizationSettings.SelectedLocale;

        currentIndex =
            FindLocaleIndex(selectedLocale);

        if (currentIndex < 0 && availableLocales.Count > 0)
        {
            currentIndex = 0;
            LocalizationSettings.SelectedLocale =
                availableLocales[currentIndex];
        }

        initialized = true;

        SaveCurrentLocale();
        RefreshUI();
    }

    private void RebuildLocaleList()
    {
        availableLocales.Clear();

        IReadOnlyList<Locale> locales =
            LocalizationSettings.AvailableLocales.Locales;

        for (int i = 0; i < locales.Count; i++)
        {
            Locale locale = locales[i];

            if (locale == null)
                continue;

            string code = locale.Identifier.Code;

            if (string.IsNullOrWhiteSpace(code))
                continue;

            availableLocales.Add(locale);
        }
    }

    private void SelectLocale(int index)
    {
        if (index < 0 || index >= availableLocales.Count)
            return;

        currentIndex = index;

        Locale locale =
            availableLocales[currentIndex];

        LocalizationSettings.SelectedLocale = locale;

        SaveLocale(locale);
        RefreshUI();
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        if (!initialized)
            return;

        int index = FindLocaleIndex(locale);

        if (index >= 0)
            currentIndex = index;

        SaveLocale(locale);
        RefreshUI();
    }

    private int FindLocaleIndex(Locale locale)
    {
        if (locale == null)
            return -1;

        string selectedCode =
            locale.Identifier.Code;

        for (int i = 0; i < availableLocales.Count; i++)
        {
            if (string.Equals(
                    availableLocales[i].Identifier.Code,
                    selectedCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void RefreshUI()
    {
        bool hasLocales =
            initialized &&
            availableLocales.Count > 0 &&
            currentIndex >= 0 &&
            currentIndex < availableLocales.Count;

        if (previousButton != null)
            previousButton.interactable =
                hasLocales && availableLocales.Count > 1;

        if (nextButton != null)
            nextButton.interactable =
                hasLocales && availableLocales.Count > 1;

        if (languageValueText == null)
            return;

        if (!hasLocales)
        {
            languageValueText.text = "—";
            return;
        }

        languageValueText.text =
            GetNativeDisplayName(
                availableLocales[currentIndex]
            );
    }

    private void SaveCurrentLocale()
    {
        if (currentIndex < 0 ||
            currentIndex >= availableLocales.Count)
        {
            return;
        }

        SaveLocale(
            availableLocales[currentIndex]
        );
    }

    private static void SaveLocale(Locale locale)
    {
        if (locale == null)
            return;

        string code =
            locale.Identifier.Code;

        if (string.IsNullOrWhiteSpace(code))
            return;

        PlayerPrefs.SetString(
            LocalePreferenceKey,
            code
        );

        PlayerPrefs.Save();
    }

    private static void ApplySavedLocaleIfAvailable()
    {
        if (!PlayerPrefs.HasKey(LocalePreferenceKey))
            return;

        string savedCode =
            PlayerPrefs.GetString(
                LocalePreferenceKey,
                string.Empty
            );

        if (string.IsNullOrWhiteSpace(savedCode))
            return;

        IReadOnlyList<Locale> locales =
            LocalizationSettings.AvailableLocales.Locales;

        Locale bestMatch = null;

        for (int i = 0; i < locales.Count; i++)
        {
            Locale locale = locales[i];

            if (locale == null)
                continue;

            string localeCode =
                locale.Identifier.Code;

            if (string.Equals(
                    localeCode,
                    savedCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                bestMatch = locale;
                break;
            }
        }

        if (bestMatch != null)
        {
            LocalizationSettings.SelectedLocale =
                bestMatch;
        }
    }

    private static string GetNativeDisplayName(Locale locale)
    {
        if (locale == null)
            return string.Empty;

        string code =
            locale.Identifier.Code;

        try
        {
            CultureInfo culture =
                CultureInfo.GetCultureInfo(code);

            string nativeName =
                culture.NativeName;

            if (!string.IsNullOrWhiteSpace(nativeName))
            {
                return culture.TextInfo.ToUpper(
                    nativeName
                );
            }
        }
        catch (CultureNotFoundException)
        {
            // Locale custom ise Unity'deki locale adina duser.
        }

        string fallback =
            locale.LocaleName;

        if (string.IsNullOrWhiteSpace(fallback))
            fallback = code;

        return fallback.ToUpperInvariant();
    }
}
