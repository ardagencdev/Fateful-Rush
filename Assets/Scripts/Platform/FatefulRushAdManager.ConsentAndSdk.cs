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
    private void TryStartAdsWhenSafe()
    {
#if UNITY_EDITOR
        return;
#else
        if (!adsStartupPending ||
            sdkInitialized ||
            sdkInitializationStarted ||
            consentFlowActive ||
            Time.realtimeSinceStartup < adsStartupNotBeforeRealtime ||
            !AndroidShaderWarmup.IsComplete ||
            !IsSafeForAdBackgroundWork())
        {
            return;
        }

        adsStartupPending = false;
        BeginConsentFlowSafely();
#endif
    }

    private bool IsSafeForAdBackgroundWork()
    {
        if (!Application.isFocused || adShowInProgress)
            return false;

        Scene activeScene = SceneManager.GetActiveScene();

        return activeScene.IsValid() &&
               activeScene.name == MainMenuSceneName &&
               !GameStateManager.IsGameplayStarted &&
               !FatefulRushInAppUpdateManager.IsBusy;
    }

    private void BeginConsentFlowSafely()
    {
        if (consentFlowActive || sdkInitializationStarted)
            return;

        consentFlowActive = true;

        try
        {
            ConsentRequestParameters requestParameters =
                new ConsentRequestParameters();

            ConsentInformation.Update(
                requestParameters,
                error =>
                {
                    RunOnUnityThread(
                        () => HandleConsentInfoUpdated(error)
                    );
                }
            );
        }
        catch (Exception exception)
        {
            consentFlowActive = false;

            Debug.LogWarning(
                "[Ads] Consent update baslatilamadi; reklam sistemi " +
                "oyunu etkilemeden devre disi kalabilir: " +
                exception.Message
            );

            TryInitializeAdsSafely();
        }
    }

    private void HandleConsentInfoUpdated(FormError error)
    {
        if (this == null)
            return;

        if (error != null)
        {
            consentFlowActive = false;

            Debug.LogWarning(
                "[Ads] Consent bilgisi guncellenemedi: " +
                error.Message
            );

            // Onceki session'dan gecerli consent varsa SDK yine baslayabilir.
            TryInitializeAdsSafely();
            return;
        }

        try
        {
            ConsentForm.LoadAndShowConsentFormIfRequired(
                formError =>
                {
                    RunOnUnityThread(
                        () => HandleConsentFormFinished(formError)
                    );
                }
            );
        }
        catch (Exception exception)
        {
            consentFlowActive = false;

            Debug.LogWarning(
                "[Ads] Consent form akisi baslatilamadi: " +
                exception.Message
            );

            TryInitializeAdsSafely();
        }
    }

    private void HandleConsentFormFinished(FormError error)
    {
        if (this == null)
            return;

        consentFlowActive = false;

        if (error != null)
        {
            Debug.LogWarning(
                "[Ads] Consent form tamamlanamadi: " +
                error.Message
            );
        }

        TryInitializeAdsSafely();
    }

    private void TryInitializeAdsSafely(bool skipConsentCheck = false)
    {
#if !UNITY_EDITOR
        if (!IsSafeForAdBackgroundWork())
        {
            adsStartupPending = true;
            return;
        }
#endif

        if (!IsAdsRuntimeSupported() ||
            sdkInitialized ||
            sdkInitializationStarted)
        {
            return;
        }

        if (!skipConsentCheck)
        {
            bool canRequestAds;

            try
            {
                canRequestAds =
                    ConsentInformation.CanRequestAds();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Ads] Consent durumu okunamadi; reklamlar atlandi: " +
                    exception.Message
                );

                return;
            }

            if (!canRequestAds)
            {
                Debug.LogWarning(
                    "[Ads] UMP CanRequestAds=false; Mobile Ads SDK baslatilmadi."
                );
                return;
            }
        }

        sdkInitializationStarted = true;

        try
        {
            MobileAds.Initialize(
                initializationStatus =>
                {
                    RunOnUnityThread(
                        () => HandleAdsInitialized(
                            initializationStatus
                        )
                    );
                }
            );
        }
        catch (Exception exception)
        {
            sdkInitializationStarted = false;

            Debug.LogWarning(
                "[Ads] Mobile Ads SDK baslatilamadi; oyun normal devam edecek: " +
                exception.Message
            );
        }
    }

    private void HandleAdsInitialized(
        InitializationStatus initializationStatus)
    {
        if (this == null)
            return;

        sdkInitializationStarted = false;

        if (initializationStatus == null)
        {
            Debug.LogWarning(
                "[Ads] Mobile Ads SDK initialization sonucu bos geldi."
            );

            return;
        }

        sdkInitialized = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Ads TEST] Google Mobile Ads initialized.");
#endif

        LoadInterstitialSafely();
    }

    private void LoadInterstitialSafely()
    {
#if !UNITY_EDITOR
        if (!IsSafeForAdBackgroundWork())
            return;
#endif

        if (!IsAdsRuntimeSupported() ||
            !sdkInitialized ||
            adLoadInProgress ||
            adShowInProgress ||
            loadedInterstitial != null ||
            Time.realtimeSinceStartup < nextAllowedLoadRealtime)
        {
            return;
        }

        adLoadInProgress = true;

        try
        {
            AdRequest request = new AdRequest();

            InterstitialAd.Load(
                GetInterstitialAdUnitId(),
                request,
                (ad, error) =>
                {
                    RunOnUnityThread(
                        () => HandleInterstitialLoaded(
                            ad,
                            error
                        )
                    );
                }
            );
        }
        catch (Exception exception)
        {
            adLoadInProgress = false;
            ScheduleLoadRetry();

            Debug.LogWarning(
                "[Ads] Interstitial load baslatilamadi: " +
                exception.Message
            );
        }
    }

    private void HandleInterstitialLoaded(
        InterstitialAd ad,
        LoadAdError error)
    {
        if (this == null)
        {
            DestroySpecificAdSafely(ad);
            return;
        }

        adLoadInProgress = false;

        if (error != null || ad == null)
        {
            if (error != null)
            {
                Debug.LogWarning(
                    "[Ads] Interstitial yuklenemedi: " + error
                );
            }
            else
            {
                Debug.LogWarning(
                    "[Ads] Interstitial load callback null reklam dondurdu."
                );
            }

            DestroySpecificAdSafely(ad);
            ScheduleLoadRetry();
            return;
        }

        DestroyAdSafely(ref loadedInterstitial);

        loadedInterstitial = ad;
        loadedAdRealtime = Time.realtimeSinceStartup;
        nextAllowedLoadRealtime = 0f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Ads TEST] Interstitial loaded and ready.");
#endif

        RegisterInterstitialCallbacks(ad);

        // 5 dakika daha once dolduysa ve reklam ancak simdi yuklendiyse
        // MainMenu'de guvenli sekilde hemen denenebilir.
        if (SceneManager.GetActiveScene().name == MainMenuSceneName)
            TryShowMainMenuTimedAdIfDue();
    }

    private void RegisterInterstitialCallbacks(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentOpened +=
            () =>
            {
                RunOnUnityThread(
                    () =>
                    {
                        if (this != null)
                        {
                            adShowInProgress = true;
                            BeginAdPause();
                        }
                    }
                );
            };

        ad.OnAdFullScreenContentClosed +=
            () =>
            {
                RunOnUnityThread(
                    () => FinishInterstitial(ad)
                );
            };

        ad.OnAdFullScreenContentFailed +=
            error =>
            {
                RunOnUnityThread(
                    () => FinishInterstitial(ad)
                );
            };
    }

    private void RefreshExpiredAdIfNeeded()
    {
        if (loadedInterstitial == null)
            return;

        if (Time.realtimeSinceStartup - loadedAdRealtime <
            LoadedAdRefreshSeconds)
        {
            return;
        }

        DestroyAdSafely(ref loadedInterstitial);
        nextAllowedLoadRealtime = 0f;
        LoadInterstitialSafely();
    }

    private void RetryAdLoadIfNeeded()
    {
        if (!sdkInitialized ||
            adShowInProgress ||
            adLoadInProgress ||
            loadedInterstitial != null ||
            Time.realtimeSinceStartup < nextAllowedLoadRealtime)
        {
            return;
        }

        LoadInterstitialSafely();
    }

    private void ScheduleLoadRetry()
    {
        nextAllowedLoadRealtime =
            Time.realtimeSinceStartup +
            FailedLoadRetrySeconds;
    }

    private static string GetInterstitialAdUnitId()
    {
#if UNITY_EDITOR
        // Play Mode must never request production inventory.
        return AndroidTestInterstitialId;
#else
        // Android player builds use the real AdMob unit. Google Play Games on
        // PC runs the Android build, so it uses this production ID as well.
        return AndroidProductionInterstitialId;
#endif
    }

    private static bool IsAdsRuntimeSupported()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    private static void RunOnUnityThread(Action action)
    {
        if (action == null)
            return;

        try
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor
                .ExecuteInUpdate(action);
        }
        catch
        {
            // Callback'i background thread'de zorla calistirmiyoruz.
            // Reklam devre disi kalabilir ama Unity state'i riske atilmaz.
        }
    }

    private static void DestroyAdSafely(ref InterstitialAd ad)
    {
        InterstitialAd adToDestroy = ad;
        ad = null;
        DestroySpecificAdSafely(adToDestroy);
    }

    private static void DestroySpecificAdSafely(InterstitialAd ad)
    {
        if (ad == null)
            return;

        try
        {
            ad.Destroy();
        }
        catch
        {
            // Reklam objesi cleanup hatasi gameplay'i etkileyemez.
        }
    }
}
