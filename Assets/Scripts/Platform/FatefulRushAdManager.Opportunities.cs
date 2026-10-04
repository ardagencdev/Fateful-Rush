using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Same Unity component. Inspector data and lifecycle entry points remain in FatefulRushAdManager.cs.
public sealed partial class FatefulRushAdManager
{
    private void TrackGameplayAttempt()
    {
        NormalizeAttemptTargetToConfig();

        Scene activeScene = SceneManager.GetActiveScene();

        bool gameplayStarted =
            activeScene.name != MainMenuSceneName &&
            GameStateManager.IsGameplayStarted;

        if (gameplayStarted && !previousGameplayStarted)
            RegisterGameplayAttempt();

        previousGameplayStarted = gameplayStarted;
    }

    private void RegisterGameplayAttempt()
    {
        attemptCount = Mathf.Max(0, attemptCount) + 1;

        PlayerPrefs.SetInt(
            AttemptsKey,
            attemptCount
        );

        // Do not force a disk flush at the exact moment gameplay starts.
        // PlayerPrefs.SetInt updates memory immediately; persistence happens at
        // result/menu/pause checkpoints instead, avoiding a possible frame hitch.

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[Ads TEST] Gameplay attempt: {attemptCount}/{attemptTarget}"
        );
#endif
    }

    private void TrackMainMenuTime()
    {
        if (SceneManager.GetActiveScene().name != MainMenuSceneName)
            return;

        // Consent/reklam ekrandayken veya uygulama focus disindayken
        // kullanicinin menu suresini ilerletme.
        if (!Application.isFocused ||
            consentFlowActive ||
            adShowInProgress ||
            FatefulRushReviewPrompt.IsBusy ||
            FatefulRushInAppUpdateManager.IsBusy)
        {
            return;
        }

        mainMenuActiveSeconds += Time.unscaledDeltaTime;

        // Time only makes the opportunity eligible. Never show while idle.
        // A later user-requested MainMenu -> panel transition is the trigger.
    }

    private bool TryShowAttemptAdInternal(Action onFinished)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(
            $"[Ads TEST] MainMenu ad check. Attempts={attemptCount}/{attemptTarget}, " +
            $"SDK={sdkInitialized}, Loaded={(loadedInterstitial != null)}, " +
            $"Showing={adShowInProgress}"
        );
#endif

        if (attemptCount < attemptTarget)
            return false;

        return TryShowLoadedInterstitial(onFinished);
    }

    /// <summary>Only called by an explicit MainMenu -> panel navigation request.</summary>
    public static bool TryShowTimedAdBeforeOpeningMenuPanel(Action onFinished)
    {
        // Do not bootstrap ads from UI transitions outside MainMenu.
        if (instance == null ||
            SceneManager.GetActiveScene().name != MainMenuSceneName ||
            !Application.isFocused)
        {
            return false;
        }

        try
        {
            return instance.TryShowMainMenuTimedAdIfDue(onFinished);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Ads] Menu panel reklami guvenli sekilde atlandi: " + exception.Message);
            return false;
        }
    }

    private bool TryShowMainMenuTimedAdIfDue(Action onFinished)
    {
        if (mainMenuActiveSeconds < GetMainMenuAdIntervalSeconds())
            return false;

        // An unavailable ad must not delay panel navigation. Keep the elapsed
        // time intact and try again on the NEXT explicit panel-open request.
        if (SceneTransition.Instance != null &&
            SceneTransition.Instance.IsTransitioning)
        {
            return false;
        }

        // Gameplay attempt ads retain their separate return-to-menu trigger.
        return TryShowLoadedInterstitial(onFinished);
    }

    private bool TryShowLoadedInterstitial(Action onFinished = null)
    {
        if (!IsAdsRuntimeSupported() ||
            consentFlowActive ||
            !sdkInitialized ||
            adShowInProgress ||
            FatefulRushReviewPrompt.IsBusy ||
            FatefulRushInAppUpdateManager.IsBusy)
        {
            return false;
        }

        InterstitialAd ad = loadedInterstitial;

        if (ad == null)
        {
            LoadInterstitialSafely();
            return false;
        }

        bool canShow;

        try
        {
            canShow = ad.CanShowAd();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Ads] CanShowAd kontrolu basarisiz; reklam atlandi: " +
                exception.Message
            );

            DestroyAdSafely(ref loadedInterstitial);
            ScheduleLoadRetry();
            return false;
        }

        if (!canShow)
        {
            DestroyAdSafely(ref loadedInterstitial);
            LoadInterstitialSafely();
            return false;
        }

        // Reklam ekrandayken oyun/menunun arkada ilerlememesi icin
        // Show() isteginden hemen once global oyun akisini dondur.
        // Gameplay -> MainMenu yolunda caller, onFinished callback'i gelene
        // kadar scene gecisini de baslatmaz.
        loadedInterstitial = null;
        activeInterstitial = ad;
        activeAdFinishedCallback = onFinished;
        adShowInProgress = true;
        BeginAdPause();

#if UNITY_EDITOR
        BeginEditorAdPreview();
#endif

        try
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Ads TEST] Interstitial Show() requested.");
#endif

            ad.Show();

            // Show() exception atmadan kabul edildiyse hakki tuket.
            // Callback kaybolsa dahi ikinci bir reklam ust uste cikmasin.
            ConsumeAdOpportunity();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Ads] Reklam acilamadi; oyun akisina devam ediliyor: " +
                exception.Message
            );

            adShowInProgress = false;
            activeInterstitial = null;
            activeAdFinishedCallback = null;
#if UNITY_EDITOR
            EndEditorAdPreview();
#endif
            EndAdPause();
            DestroySpecificAdSafely(ad);
            ScheduleLoadRetry();
            return false;
        }
    }

    private void ConsumeAdOpportunity()
    {
        attemptCount = 0;
        attemptTarget = RollNextAttemptTarget();
        mainMenuActiveSeconds = 0f;

        PlayerPrefs.SetInt(
            AttemptsKey,
            attemptCount
        );

        PlayerPrefs.SetInt(
            AttemptTargetKey,
            attemptTarget
        );

        PlayerPrefs.SetFloat(
            MainMenuSecondsKey,
            mainMenuActiveSeconds
        );

        PlayerPrefs.Save();
    }

    private void FinishInterstitial(InterstitialAd ad)
    {
        if (this == null)
        {
            DestroySpecificAdSafely(ad);
            return;
        }

#if UNITY_EDITOR
        // Google'in Editor placeholder'i bazi surumlerde tek frame icinde
        // kapanabiliyor. Scene transition'in hemen devam edip preview'i
        // yutmamasi icin Editor'da minimum gorunme suresini garanti et.
        if (editorFinishDelayActive)
            return;

        if (editorAdPreviewRoot != null &&
            editorAdPreviewRoot.activeSelf)
        {
            float elapsed =
                Time.realtimeSinceStartup - editorAdPreviewShownRealtime;

            float remaining =
                EditorAdPreviewMinimumSeconds - elapsed;

            if (remaining > 0f)
            {
                editorFinishDelayActive = true;
                StartCoroutine(
                    FinishInterstitialAfterEditorPreviewDelay(
                        ad,
                        remaining
                    )
                );
                return;
            }
        }

        EndEditorAdPreview();
#endif

        if (activeInterstitial == ad)
            activeInterstitial = null;

        adShowInProgress = false;

        Action finishedCallback = activeAdFinishedCallback;
        activeAdFinishedCallback = null;

        // Once oyunu/sesi geri getir, sonra gameplay -> MainMenu gibi
        // bekleyen akisin devam etmesine izin ver.
        EndAdPause();
        DestroySpecificAdSafely(ad);

        // Full-screen reklam tek kullanimliktir; sonrakini preload et.
        nextAllowedLoadRealtime = Time.realtimeSinceStartup + 0.5f;

        InvokeSafely(finishedCallback);
    }

    private void BeginAdPause()
    {
        if (adPauseActive)
            return;

        adPauseActive = true;
        adPausePreviousTimeScale = Time.timeScale;
        adPausePreviousAudioListenerPause = AudioListener.pause;

        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    private void EndAdPause()
    {
        if (!adPauseActive)
            return;

        Time.timeScale = adPausePreviousTimeScale;
        AudioListener.pause = adPausePreviousAudioListenerPause;
        adPauseActive = false;
    }
}
