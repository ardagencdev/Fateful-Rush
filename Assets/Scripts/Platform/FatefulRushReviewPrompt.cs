using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif

// Google owns the review UI and decides whether it is displayed.
// No positive-rating gate, incentive, or forced store redirect.
public sealed class FatefulRushReviewPrompt : MonoBehaviour
{
    private const int DefaultMinimumRuns = 10;
    private const int DefaultMinimumSessions = 2;
    private const float DefaultMinimumPlaySeconds = 600f;
    private const int DefaultCooldownDays = 30;
    private const int DefaultMaximumAttempts = 3;
    private const float DefaultMenuQuietSeconds = 5f;
    private const float DefaultAdCooldownSeconds = 60f;
    private const string Prefix = "FR_Review_";
    private static FatefulRushReviewPrompt instance;
    public static bool IsBusy { get; private set; }
    private bool finishedRunThisSession;
    private bool attemptedThisSession;
    private float quietMenuSeconds;
    private float lastAdRealtime = -1000f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; IsBusy = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var root = new GameObject("FatefulRushReviewPrompt");
        instance = root.AddComponent<FatefulRushReviewPrompt>();
        DontDestroyOnLoad(root);
#if UNITY_ANDROID && !UNITY_EDITOR
        PlayerPrefs.SetInt(Prefix + "Sessions",
            Math.Min(1000000, PlayerPrefs.GetInt(Prefix + "Sessions", 0)) + 1);
        PlayerPrefs.Save();
#endif
    }

    public static void NotifyRunFinished()
    {
        if (instance != null) instance.finishedRunThisSession = true;
    }

    private static bool SafeMenu()
    {
        return Application.isFocused && SceneManager.GetActiveScene().name == "MainMenu"
            && !GameStateManager.IsGameplayStarted
            && !FatefulRushAdManager.IsFullScreenBusy
            && !FatefulRushInAppUpdateManager.IsBusy
            && (SceneTransition.Instance == null || !SceneTransition.Instance.IsTransitioning);
    }

    private bool Eligible()
    {
        if (!FatefulRushFirebaseServices.GetBool(
                FatefulRushFirebaseServices.ReviewEnabledKey,
                true
            ))
        {
            return false;
        }

        int minimumRuns = Mathf.Clamp(
            FatefulRushFirebaseServices.GetInt(
                FatefulRushFirebaseServices.ReviewMinimumRunsKey,
                DefaultMinimumRuns
            ),
            1,
            1000000
        );
        int minimumSessions = Mathf.Clamp(
            FatefulRushFirebaseServices.GetInt(
                FatefulRushFirebaseServices.ReviewMinimumSessionsKey,
                DefaultMinimumSessions
            ),
            1,
            100000
        );
        float minimumPlaySeconds = Mathf.Clamp(
            FatefulRushFirebaseServices.GetFloat(
                FatefulRushFirebaseServices.ReviewMinimumPlaySecondsKey,
                DefaultMinimumPlaySeconds
            ),
            1f,
            30f * 24f * 60f * 60f
        );
        int maximumAttempts = Mathf.Clamp(
            FatefulRushFirebaseServices.GetInt(
                FatefulRushFirebaseServices.ReviewMaximumAttemptsKey,
                DefaultMaximumAttempts
            ),
            1,
            10
        );

        if (attemptedThisSession || !finishedRunThisSession
            || StatsManager.GetTotalRuns() < minimumRuns
            || StatsManager.GetTotalPlayTime() < minimumPlaySeconds
            || PlayerPrefs.GetInt(Prefix + "Sessions", 0) < minimumSessions
            || PlayerPrefs.GetInt(Prefix + "Attempts", 0) >= maximumAttempts) return false;

        int cooldownDays = Mathf.Clamp(
            FatefulRushFirebaseServices.GetInt(
                FatefulRushFirebaseServices.ReviewCooldownDaysKey,
                DefaultCooldownDays
            ),
            0,
            3650
        );

        long ticks;
        if (!long.TryParse(PlayerPrefs.GetString(Prefix + "LastAttemptUtc", "0"), out ticks)) ticks = 0;
        return ticks <= 0 || (DateTime.UtcNow.Ticks - ticks) / (double)TimeSpan.TicksPerDay >= cooldownDays;
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (FatefulRushAdManager.IsFullScreenBusy ||
            FatefulRushInAppUpdateManager.IsBusy)
        {
            lastAdRealtime = Time.realtimeSinceStartup;
        }

        float adCooldownSeconds = Mathf.Clamp(
            FatefulRushFirebaseServices.GetFloat(
                FatefulRushFirebaseServices.ReviewAdCooldownSecondsKey,
                DefaultAdCooldownSeconds
            ),
            0f,
            3600f
        );

        if (IsBusy || !SafeMenu() ||
            Time.realtimeSinceStartup - lastAdRealtime < adCooldownSeconds)
        {
            quietMenuSeconds = 0f;
            return;
        }
        quietMenuSeconds += Time.unscaledDeltaTime;

        float quietMenuSecondsRequired = Mathf.Clamp(
            FatefulRushFirebaseServices.GetFloat(
                FatefulRushFirebaseServices.ReviewMenuQuietSecondsKey,
                DefaultMenuQuietSeconds
            ),
            1f,
            60f
        );

        if (quietMenuSeconds >= quietMenuSecondsRequired && Eligible())
            StartCoroutine(RequestReview());
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator RequestReview()
    {
        IsBusy = true; // Ad manager respects this while requesting and displaying.
        attemptedThisSession = true;
        try
        {
            ReviewManager manager;
            Google.Play.Common.PlayAsyncOperation<PlayReviewInfo, ReviewErrorCode> request;
            try
            {
                manager = new ReviewManager();
                request = manager.RequestReviewFlow();
                if (request == null)
                    yield break;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Review] Request failed: " + e.Message);
                yield break;
            }
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!request.IsDone && Time.realtimeSinceStartup < deadline) yield return null;
            if (!request.IsDone || request.Error != ReviewErrorCode.NoError || !SafeMenu()) yield break;
            var info = request.GetResult();
            if (info == null) yield break;

            // A completed flow does not tell us whether a review was submitted.
            PlayerPrefs.SetString(Prefix + "LastAttemptUtc", DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.SetInt(Prefix + "Attempts", PlayerPrefs.GetInt(Prefix + "Attempts", 0) + 1);
            PlayerPrefs.Save();
            Google.Play.Common.PlayAsyncOperation<Google.Play.Common.VoidResult, ReviewErrorCode> launch;
            try { launch = manager.LaunchReviewFlow(info); }
            catch (Exception e)
            {
                Debug.LogWarning("[Review] Launch failed: " + e.Message);
                yield break;
            }
            if (launch == null)
                yield break;
            yield return launch;
#if DEVELOPMENT_BUILD
            Debug.Log("[Review] Flow finished: " + launch.Error + ". Display/submission is not disclosed by Google.");
#endif
        }
        finally { IsBusy = false; quietMenuSeconds = 0f; }
    }
#endif

    private void OnDestroy()
    {
        if (instance == this) { instance = null; IsBusy = false; }
    }
}
