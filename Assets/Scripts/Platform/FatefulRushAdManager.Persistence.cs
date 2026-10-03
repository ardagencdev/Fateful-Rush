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
    private void LoadProgress()
    {
        attemptCount = Mathf.Max(
            0,
            PlayerPrefs.GetInt(
                AttemptsKey,
                0
            )
        );

        attemptTarget =
            PlayerPrefs.GetInt(
                AttemptTargetKey,
                0
            );

        int minimumAttempts = GetMinimumAttemptsPerAd();
        int maximumAttempts = GetMaximumAttemptsPerAd();

        if (attemptTarget < minimumAttempts ||
            attemptTarget > maximumAttempts)
        {
            attemptTarget = RollNextAttemptTarget();

            PlayerPrefs.SetInt(
                AttemptTargetKey,
                attemptTarget
            );
        }

        mainMenuActiveSeconds = Mathf.Max(
            0f,
            PlayerPrefs.GetFloat(
                MainMenuSecondsKey,
                0f
            )
        );

        nextProgressSaveRealtime =
            Time.realtimeSinceStartup +
            ProgressSaveIntervalSeconds;
    }

    private void SaveProgressPeriodically()
    {
        if (Time.realtimeSinceStartup < nextProgressSaveRealtime)
            return;

        nextProgressSaveRealtime =
            Time.realtimeSinceStartup +
            ProgressSaveIntervalSeconds;

        SaveProgress();
    }

    private void SaveProgress()
    {
        try
        {
            PlayerPrefs.SetInt(
                AttemptsKey,
                Mathf.Max(0, attemptCount)
            );

            PlayerPrefs.SetInt(
                AttemptTargetKey,
                attemptTarget
            );

            PlayerPrefs.SetFloat(
                MainMenuSecondsKey,
                Mathf.Max(0f, mainMenuActiveSeconds)
            );

            PlayerPrefs.Save();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Ads] Reklam sayaçlari kaydedilemedi; oyun etkilenmeyecek: " +
                exception.Message
            );
        }
    }

    private static int RollNextAttemptTarget()
    {
        int minimumAttempts = GetMinimumAttemptsPerAd();
        int maximumAttempts = GetMaximumAttemptsPerAd();

        return UnityEngine.Random.Range(
            minimumAttempts,
            maximumAttempts + 1
        );
    }

    private void NormalizeAttemptTargetToConfig()
    {
        int minimumAttempts = GetMinimumAttemptsPerAd();
        int maximumAttempts = GetMaximumAttemptsPerAd();

        if (attemptTarget >= minimumAttempts &&
            attemptTarget <= maximumAttempts)
        {
            return;
        }

        attemptTarget = RollNextAttemptTarget();
        PlayerPrefs.SetInt(AttemptTargetKey, attemptTarget);
    }

    private static int GetMinimumAttemptsPerAd()
    {
#if UNITY_EDITOR
        return MinimumAttemptsPerAd;
#else
        return Mathf.Clamp(
            FatefulRushFirebaseServices.GetInt(
                FatefulRushFirebaseServices.AdsMinimumAttemptsKey,
                MinimumAttemptsPerAd
            ),
            1,
            50
        );
#endif
    }

    private static int GetMaximumAttemptsPerAd()
    {
#if UNITY_EDITOR
        return MaximumAttemptsPerAd;
#else
        int minimumAttempts = GetMinimumAttemptsPerAd();

        return Mathf.Max(
            minimumAttempts,
            Mathf.Clamp(
                FatefulRushFirebaseServices.GetInt(
                    FatefulRushFirebaseServices.AdsMaximumAttemptsKey,
                    MaximumAttemptsPerAd
                ),
                1,
                50
            )
        );
#endif
    }

    private static float GetMainMenuAdIntervalSeconds()
    {
#if UNITY_EDITOR
        return MainMenuAdIntervalSeconds;
#else
        return Mathf.Clamp(
            FatefulRushFirebaseServices.GetFloat(
                FatefulRushFirebaseServices.AdsMainMenuIntervalSecondsKey,
                MainMenuAdIntervalSeconds
            ),
            1f,
            24f * 60f * 60f
        );
#endif
    }
}
