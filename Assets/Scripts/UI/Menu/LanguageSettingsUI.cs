using System;
using System.Collections;
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

    [Header("Language Transition")]
    [Tooltip("Toplam dil yazisi gecis suresi.")]
    [SerializeField, Min(0.08f)]
    private float transitionDuration = 0.30f;

    [Tooltip("Yeni dil yazisinin sagdan/soldan gelecegi mesafe.")]
    [SerializeField, Min(0f)]
    private float transitionDistance = 72f;

    private readonly List<Locale> availableLocales = new List<Locale>();

    private int currentIndex = -1;
    private bool initialized;
    private bool isTransitioning;

    private Coroutine transitionRoutine;
    private RectTransform languageValueRect;
    private Vector2 languageValueRestPosition;
    private Color languageValueBaseColor;
    private bool languageVisualCached;

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

        CacheLanguageVisual();
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

        CancelTransitionAndRestoreVisual();
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
        BeginLanguageTransition(-1);
    }

    public void SelectNextLanguage()
    {
        BeginLanguageTransition(1);
    }

    private void BeginLanguageTransition(int direction)
    {
        if (!initialized ||
            availableLocales.Count == 0 ||
            isTransitioning)
        {
            return;
        }

        int targetIndex =
            (currentIndex + direction + availableLocales.Count) %
            availableLocales.Count;

        if (targetIndex == currentIndex)
            return;

        if (languageValueText == null)
        {
            ApplyLocale(targetIndex, true);
            return;
        }

        CacheLanguageVisual();

        if (!languageVisualCached)
        {
            ApplyLocale(targetIndex, true);
            return;
        }

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine =
            StartCoroutine(
                LanguageTransitionRoutine(
                    targetIndex,
                    direction
                )
            );
    }

    private IEnumerator LanguageTransitionRoutine(
        int targetIndex,
        int direction)
    {
        isTransitioning = true;
        RefreshButtonState();

        float totalDuration =
            Mathf.Max(0.08f, transitionDuration);

        float halfDuration =
            totalDuration * 0.5f;

        float signedDirection =
            direction >= 0 ? 1f : -1f;

        Vector2 exitPosition =
            languageValueRestPosition +
            Vector2.left *
            (transitionDistance * signedDirection);

        Vector2 enterPosition =
            languageValueRestPosition +
            Vector2.right *
            (transitionDistance * signedDirection);

        // NEXT/right: current text exits left.
        // PREVIOUS/left: current text exits right.
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(elapsed / halfDuration);

            float eased =
                EaseInCubic(t);

            languageValueRect.anchoredPosition =
                Vector2.LerpUnclamped(
                    languageValueRestPosition,
                    exitPosition,
                    eased
                );

            SetLanguageTextAlpha(
                Mathf.Lerp(1f, 0f, Smooth01(t))
            );

            yield return null;
        }

        languageValueRect.anchoredPosition =
            exitPosition;

        SetLanguageTextAlpha(0f);

        // Locale only changes once the old text is fully out.
        // This prevents the new language from popping at center.
        ApplyLocale(targetIndex, false);
        RefreshLanguageValueOnly();

        languageValueRect.anchoredPosition =
            enterPosition;

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(elapsed / halfDuration);

            float eased =
                EaseOutCubic(t);

            languageValueRect.anchoredPosition =
                Vector2.LerpUnclamped(
                    enterPosition,
                    languageValueRestPosition,
                    eased
                );

            SetLanguageTextAlpha(
                Mathf.Lerp(0f, 1f, Smooth01(t))
            );

            yield return null;
        }

        languageValueRect.anchoredPosition =
            languageValueRestPosition;

        SetLanguageTextAlpha(1f);

        isTransitioning = false;
        transitionRoutine = null;

        RefreshUI();
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

        CacheLanguageVisual();

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

    private void ApplyLocale(
        int index,
        bool refreshUI)
    {
        if (index < 0 || index >= availableLocales.Count)
            return;

        currentIndex = index;

        Locale locale =
            availableLocales[currentIndex];

        LocalizationSettings.SelectedLocale = locale;

        SaveLocale(locale);

        if (refreshUI)
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

        // During our slide animation the coroutine controls exactly
        // when the new language name appears.
        if (!isTransitioning)
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
        RefreshButtonState();
        RefreshLanguageValueOnly();
    }

    private void RefreshButtonState()
    {
        bool hasLocales =
            initialized &&
            availableLocales.Count > 0 &&
            currentIndex >= 0 &&
            currentIndex < availableLocales.Count;

        bool canChange =
            hasLocales &&
            availableLocales.Count > 1 &&
            !isTransitioning;

        if (previousButton != null)
            previousButton.interactable = canChange;

        if (nextButton != null)
            nextButton.interactable = canChange;
    }

    private void RefreshLanguageValueOnly()
    {
        if (languageValueText == null)
            return;

        bool hasLocales =
            initialized &&
            availableLocales.Count > 0 &&
            currentIndex >= 0 &&
            currentIndex < availableLocales.Count;

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

    private void CacheLanguageVisual()
    {
        if (languageValueText == null)
            return;

        languageValueRect =
            languageValueText.rectTransform;

        if (languageValueRect == null)
            return;

        languageValueRestPosition =
            languageValueRect.anchoredPosition;

        languageValueBaseColor =
            languageValueText.color;

        languageVisualCached = true;
    }

    private void CancelTransitionAndRestoreVisual()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        isTransitioning = false;

        if (!languageVisualCached)
            return;

        if (languageValueRect != null)
        {
            languageValueRect.anchoredPosition =
                languageValueRestPosition;
        }

        if (languageValueText != null)
        {
            Color color = languageValueBaseColor;
            color.a = languageValueBaseColor.a;
            languageValueText.color = color;
        }
    }

    private void SetLanguageTextAlpha(float normalizedAlpha)
    {
        if (languageValueText == null)
            return;

        Color color = languageValueBaseColor;

        color.a =
            languageValueBaseColor.a *
            Mathf.Clamp01(normalizedAlpha);

        languageValueText.color = color;
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

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static float EaseInCubic(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * value;
    }

    private static float EaseOutCubic(float value)
    {
        value = Mathf.Clamp01(value);
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private void OnValidate()
    {
        transitionDuration =
            Mathf.Max(0.08f, transitionDuration);

        transitionDistance =
            Mathf.Max(0f, transitionDistance);
    }
}
