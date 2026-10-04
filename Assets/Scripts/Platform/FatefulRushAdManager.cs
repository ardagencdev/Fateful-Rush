using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fateful Rush reklam akisini gameplay'den izole tutar.
/// - Her gameplay attempt basladiginda sayar.
/// - Remote Config ile belirlenen attempt araliginda reklam hakki olusturur
///   (varsayilan 5-10).
/// - Attempt reklami sadece gameplay -> MainMenu gecisinde denenir.
/// - MainMenu'de aktif sure dolunca hak hazir olur (varsayilan 5 dakika).
///   Reklam ancak kullanici ana menuden bir panel actiginda denenir.
/// - Tum sayaçlar PlayerPrefs ile kalicidir.
/// - Reklam/consent SDK hatalari gameplay veya scene gecisini asla bloklamaz.
/// </summary>
public sealed partial class FatefulRushAdManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    private const string AndroidTestInterstitialId =
        "ca-app-pub-3940256099942544/1033173712";

    private const string AndroidProductionInterstitialId =
        "ca-app-pub-4850318886881398/8213391263";

#if UNITY_EDITOR
    // Editor testinde reklami hizlica dogrulamak icin her attempt sonrasi hak olustur.
    // Bu blok build'e GIRMEZ; Android/Google Play Games on PC build'leri 5-10 RNG kullanir.
    private const int MinimumAttemptsPerAd = 1;
    private const int MaximumAttemptsPerAd = 1;
#else
    private const int MinimumAttemptsPerAd = 5;
    private const int MaximumAttemptsPerAd = 10;
#endif

    // Active MainMenu time makes the opportunity eligible; explicit panel
    // navigation is required to show it. Idle time never opens an ad itself.
    private const float MainMenuAdIntervalSeconds = 5f * 60f;

    private const float ProgressSaveIntervalSeconds = 30f;
    private const float AdsStartupDelaySeconds = 2.0f;
    private const float FailedLoadRetrySeconds = 30f;
    private const float LoadedAdRefreshSeconds = 55f * 60f;

    private const string AttemptsKey = "FR_Ads_Attempts";
    private const string AttemptTargetKey = "FR_Ads_AttemptTarget";
    private const string MainMenuSecondsKey = "FR_Ads_MainMenuSeconds";

    private static FatefulRushAdManager instance;

    private InterstitialAd loadedInterstitial;
    private InterstitialAd activeInterstitial;

    private int attemptCount;
    private int attemptTarget;
    private float mainMenuActiveSeconds;

    private bool previousGameplayStarted;
    private bool consentFlowActive;
    private bool sdkInitializationStarted;
    private bool sdkInitialized;
    private bool adLoadInProgress;
    private bool adShowInProgress;
    private bool adPauseActive;

    private float adPausePreviousTimeScale = 1f;
    private bool adPausePreviousAudioListenerPause;
    private Action activeAdFinishedCallback;

    private float nextAllowedLoadRealtime;
    private float loadedAdRealtime;
    private float nextProgressSaveRealtime;

#if !UNITY_EDITOR
    private float adsStartupNotBeforeRealtime;
    private bool adsStartupPending;
#endif

#if UNITY_EDITOR
    // Editor mock reklam bazen transition canvas'inin arkasinda kalabiliyor
    // veya tek frame gorunup kapanabiliyor. Bu preview yalnizca Editor'da
    // deterministic bir tam-ekran reklam testi gosterir; build'e GIRMEZ.
    private const float EditorAdPreviewMinimumSeconds = 1.25f;
    private GameObject editorAdPreviewRoot;
    private float editorAdPreviewShownRealtime;
    private bool editorFinishDelayActive;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    /// <summary>
    /// Gameplay'den MainMenu'ye cikarken cagrilir.
    /// Reklam hazir/uygun degilse false doner. Callback gerektirmeyen
    /// eski/harici cagrilar icin bu overload korunur.
    /// </summary>
    public static bool TryShowAttemptAdBeforeReturningToMenu()
    {
        return TryShowAttemptAdBeforeReturningToMenu(null);
    }

    /// <summary>
    /// Gameplay -> MainMenu gecisinde reklam hazirsa once reklami acar.
    /// Reklam kapandiginda/fail oldugunda onFinished cagrilir; caller ancak
    /// o noktada scene gecisini devam ettirebilir. Reklam acilamazsa false
    /// doner ve callback cagrilmaz.
    /// </summary>
    public static bool TryShowAttemptAdBeforeReturningToMenu(
        Action onFinished)
    {
        try
        {
            FatefulRushAdManager manager = EnsureInstance();

            return manager != null &&
                   manager.TryShowAttemptAdInternal(onFinished);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Ads] Main Menu gecis reklami guvenli sekilde atlandi. " +
                exception.Message
            );

            return false;
        }
    }

    /// <summary>
    /// Privacy options formunu acmayi dener. Eski/harici OnClick baglantilari
    /// bozulmasin diye bu wrapper korunur.
    /// </summary>
    public static void ShowPrivacyOptions()
    {
        TryShowPrivacyOptions();
    }

    /// <summary>
    /// UMP privacy options gerekiyorsa formu acmayi dener.
    /// - Form baslatilamazsa false doner; caller kendi fallback'ini calistirir.
    /// - Form asenkron hata ile donerse onFailure guvenli sekilde cagrilir.
    /// - Form basariyla kapanirsa onClosed cagrilir.
    /// Reklam/UMP hatalari UI akisini kilitleyemez.
    /// </summary>
    public static bool TryShowPrivacyOptions(
        Action onFailure = null,
        Action onClosed = null)
    {
        if (!IsPrivacyOptionsRequired)
            return false;

        try
        {
            ConsentForm.ShowPrivacyOptionsForm(
                error =>
                {
                    RunOnUnityThread(
                        () =>
                        {
                            if (error != null)
                            {
                                Debug.LogWarning(
                                    "[Ads] Privacy options form acilamadi: " +
                                    error.Message
                                );

                                InvokeSafely(onFailure);
                            }

                            InvokeSafely(onClosed);
                        }
                    );
                }
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Ads] Privacy options form guvenli sekilde atlandi: " +
                exception.Message
            );

            return false;
        }
    }

    public static bool IsFullScreenBusy
    {
        get
        {
            return instance != null &&
                   (instance.adShowInProgress ||
                    instance.consentFlowActive ||
                    instance.adPauseActive);
        }
    }

    public static bool IsPrivacyOptionsRequired
    {
        get
        {
            try
            {
                return ConsentInformation.PrivacyOptionsRequirementStatus ==
                       PrivacyOptionsRequirementStatus.Required;
            }
            catch
            {
                return false;
            }
        }
    }

    private static FatefulRushAdManager EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<FatefulRushAdManager>();

        if (instance != null)
            return instance;

        GameObject managerObject =
            new GameObject("FatefulRushAdManager");

        instance =
            managerObject.AddComponent<FatefulRushAdManager>();

        return instance;
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

        // UMP callbacks arrive before MobileAds.Initialize(). Our callback flow
        // marshals work through MobileAdsEventExecutor, so the executor must
        // already exist on the Unity main thread. MobileAds.Initialize() also
        // initializes it later, but that is too late for the consent callbacks.
        try
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor.Initialize();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Ads] MobileAdsEventExecutor baslatilamadi: " +
                exception.Message
            );
        }

        LoadProgress();

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        if (!IsAdsRuntimeSupported())
            return;

#if UNITY_EDITOR
        // Editor mock ads stay immediate for deterministic testing.
        TryInitializeAdsSafely(true);
#else
        // On phones, do not compete with first-scene shader warm-up / gameplay
        // startup. Ads are initialized only while the player is safely in the
        // Main Menu, after the one-time shader warm-up has completed.
        adsStartupPending = true;
        adsStartupNotBeforeRealtime =
            Time.realtimeSinceStartup + AdsStartupDelaySeconds;
#endif
    }

    private void Update()
    {
        TryStartAdsWhenSafe();
        TrackGameplayAttempt();
        TrackMainMenuTime();

        // Network/SDK maintenance and disk flushes are intentionally kept out
        // of active gameplay. They can safely happen in MainMenu instead.
        if (IsSafeForAdBackgroundWork())
        {
            RefreshExpiredAdIfNeeded();
            RetryAdLoadIfNeeded();
            SaveProgressPeriodically();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveProgress();
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        EndAdPause();
        activeAdFinishedCallback = null;

#if UNITY_EDITOR
        EndEditorAdPreview();
#endif

        DestroyAdSafely(ref loadedInterstitial);
        DestroyAdSafely(ref activeInterstitial);

        if (instance == this)
            instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        previousGameplayStarted = false;

        if (scene.name == MainMenuSceneName)
        {
            // MainMenu is a safe checkpoint for the ad pacing counters.
            SaveProgress();
            TryStartAdsWhenSafe();

            // Preserve elapsed time across scenes/sessions. Loading MainMenu
            // must never show a timed ad without a user panel-open request.
        }
    }

#if UNITY_EDITOR
    private IEnumerator FinishInterstitialAfterEditorPreviewDelay(
        InterstitialAd ad,
        float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        editorFinishDelayActive = false;
        FinishInterstitial(ad);
    }

    private void BeginEditorAdPreview()
    {
        EnsureEditorAdPreviewOverlay();

        editorAdPreviewShownRealtime =
            Time.realtimeSinceStartup;

        editorFinishDelayActive = false;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.SetEditorAdPreviewMode(true);

        if (editorAdPreviewRoot != null)
        {
            editorAdPreviewRoot.SetActive(true);
            editorAdPreviewRoot.transform.SetAsLastSibling();
        }
    }

    private void EndEditorAdPreview()
    {
        editorFinishDelayActive = false;

        if (editorAdPreviewRoot != null)
            editorAdPreviewRoot.SetActive(false);

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.SetEditorAdPreviewMode(false);
    }

    private void EnsureEditorAdPreviewOverlay()
    {
        if (editorAdPreviewRoot != null)
            return;

        editorAdPreviewRoot =
            new GameObject(
                "EditorAdPreviewCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        editorAdPreviewRoot.transform.SetParent(
            transform,
            false
        );

        Canvas canvas =
            editorAdPreviewRoot.GetComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;
        canvas.targetDisplay = 0;

        CanvasScaler scaler =
            editorAdPreviewRoot.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster =
            editorAdPreviewRoot.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        GameObject panelObject =
            new GameObject(
                "EditorTestInterstitial",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        panelObject.transform.SetParent(
            editorAdPreviewRoot.transform,
            false
        );

        RectTransform panelRect =
            panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(760f, 500f);

        Image panelImage =
            panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        panelImage.raycastTarget = false;

        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        CreateEditorPreviewText(
            panelObject.transform,
            "TEST AD",
            48,
            new Vector2(0f, 145f),
            new Vector2(680f, 80f),
            FontStyle.Bold
        );

        CreateEditorPreviewText(
            panelObject.transform,
            "GOOGLE MOBILE ADS - EDITOR PREVIEW",
            24,
            new Vector2(0f, 75f),
            new Vector2(680f, 50f),
            FontStyle.Normal
        );

        CreateEditorPreviewText(
            panelObject.transform,
            "Interstitial is being shown here.\nAndroid build uses the native full-screen ad.",
            28,
            new Vector2(0f, -20f),
            new Vector2(660f, 120f),
            FontStyle.Normal
        );

        CreateEditorPreviewText(
            panelObject.transform,
            "EDITOR ONLY",
            22,
            new Vector2(0f, -165f),
            new Vector2(680f, 50f),
            FontStyle.Bold
        );

        editorAdPreviewRoot.SetActive(false);
    }

    private static void CreateEditorPreviewText(
        Transform parent,
        string value,
        int fontSize,
        Vector2 anchoredPosition,
        Vector2 size,
        FontStyle fontStyle)
    {
        GameObject textObject =
            new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

        textObject.transform.SetParent(parent, false);

        RectTransform rect =
            textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        try
        {
            text.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
        }
        catch
        {
            // Unity default font fallback'i yeterli; Editor testini bloklama.
        }
    }
#endif

    private static void InvokeSafely(Action action)
    {
        if (action == null)
            return;

        try
        {
            action.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Ads] Privacy callback hatasi oyun akisindan izole edildi: " +
                exception.Message
            );
        }
    }

}
