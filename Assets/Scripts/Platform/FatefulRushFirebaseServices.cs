using System;
using System.Collections.Generic;
using UnityEngine;

#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
using Firebase;
using Firebase.Crashlytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
#endif

/// <summary>
/// One small integration point for Firebase Crashlytics and Remote Config.
///
/// The FATEFULRUSH_FIREBASE symbol is intentional: the Firebase Unity SDK is
/// distributed separately and cannot be checked into this repository. Until
/// the SDK, google-services.json and the symbol are added, all callers use the
/// safe in-code defaults and the game keeps working normally.
/// </summary>
public sealed class FatefulRushFirebaseServices : MonoBehaviour
{
    public const string GameStatsEnabledKey = "fr_game_stats_enabled";

    public const string ReviewEnabledKey = "fr_review_enabled";
    public const string ReviewMinimumRunsKey = "fr_review_minimum_runs";
    public const string ReviewMinimumSessionsKey = "fr_review_minimum_sessions";
    public const string ReviewMinimumPlaySecondsKey =
        "fr_review_minimum_play_seconds";
    public const string ReviewCooldownDaysKey = "fr_review_cooldown_days";
    public const string ReviewMaximumAttemptsKey = "fr_review_max_attempts";
    public const string ReviewMenuQuietSecondsKey =
        "fr_review_menu_quiet_seconds";
    public const string ReviewAdCooldownSecondsKey =
        "fr_review_ad_cooldown_seconds";

    public const string AdsMinimumAttemptsKey =
        "fr_ads_min_attempts_per_ad";
    public const string AdsMaximumAttemptsKey =
        "fr_ads_max_attempts_per_ad";
    public const string AdsMainMenuIntervalSecondsKey =
        "fr_ads_main_menu_interval_seconds";

    public const string UpdateEnabledKey = "fr_update_enabled";
    public const string UpdateForceImmediateKey =
        "fr_update_force_immediate";
    public const string UpdateMinimumStalenessDaysKey =
        "fr_update_min_staleness_days";
    public const string UpdateCheckDelaySecondsKey =
        "fr_update_check_delay_seconds";

    private const long DefaultReviewMinimumRuns = 10L;
    private const long DefaultReviewMinimumSessions = 2L;
    private const double DefaultReviewMinimumPlaySeconds = 600.0;
    private const long DefaultReviewCooldownDays = 30L;
    private const long DefaultReviewMaximumAttempts = 3L;
    private const double DefaultReviewMenuQuietSeconds = 5.0;
    private const double DefaultReviewAdCooldownSeconds = 60.0;

    private const long DefaultAdsMinimumAttempts = 5L;
    private const long DefaultAdsMaximumAttempts = 10L;
    private const double DefaultAdsMainMenuIntervalSeconds = 300.0;

    private const long DefaultUpdateMinimumStalenessDays = 7L;
    private const double DefaultUpdateCheckDelaySeconds = 3.0;

    private static FatefulRushFirebaseServices instance;
    private static bool initialized;
    private static bool remoteConfigReady;

    private bool initializationStarted;

#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
    private FirebaseRemoteConfig remoteConfig;
#endif

    public static bool IsReady => initialized;
    public static bool IsRemoteConfigReady => remoteConfigReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        initialized = false;
        remoteConfigReady = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("FatefulRushFirebaseServices");
        instance = root.AddComponent<FatefulRushFirebaseServices>();
        DontDestroyOnLoad(root);
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
        Initialize();
    }

    private void Initialize()
    {
        if (initializationStarted)
            return;

        initializationStarted = true;

#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(dependencyTask =>
            {
                if (dependencyTask.IsCanceled || dependencyTask.IsFaulted)
                {
                    Debug.LogWarning(
                        "[Firebase] Dependency check failed; using local defaults."
                    );
                    remoteConfigReady = true;
                    return;
                }

                if (dependencyTask.Result != DependencyStatus.Available)
                {
                    Debug.LogWarning(
                        "[Firebase] Dependencies unavailable: " +
                        dependencyTask.Result +
                        ". Crashlytics/Remote Config disabled."
                    );
                    remoteConfigReady = true;
                    return;
                }

                InitializeFirebaseProducts();
            });
#else
        // Editor, non-Android players, and builds made before the Firebase SDK
        // import intentionally use the same safe defaults as a failed fetch.
        initialized = false;
        remoteConfigReady = false;
#endif
    }

#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
    private void InitializeFirebaseProducts()
    {
        try
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            initialized = app != null;

            if (!initialized)
            {
                remoteConfigReady = true;
                return;
            }

            Crashlytics.ReportUncaughtExceptionsAsFatal = true;
            Crashlytics.SetCustomKey("app_version", Application.version);
            Crashlytics.SetCustomKey("unity_version", Application.unityVersion);
            Crashlytics.Log("Firebase initialized for Fateful Rush.");

            remoteConfig = FirebaseRemoteConfig.DefaultInstance;

            // Development builds can refresh immediately while tuning values;
            // release builds use Firebase's recommended 12-hour minimum.
            ulong minimumFetchInterval = Debug.isDebugBuild
                ? 0UL
                : 12UL * 60UL * 60UL * 1000UL;

            ConfigSettings settings = new ConfigSettings
            {
                MinimumFetchIntervalInMilliseconds = minimumFetchInterval,
                FetchTimeoutInMilliseconds = 15UL * 1000UL
            };

            remoteConfig.SetConfigSettingsAsync(settings)
                .ContinueWithOnMainThread(settingsTask =>
                {
                    if (settingsTask.IsCanceled || settingsTask.IsFaulted)
                    {
                        RecordNonFatal(
                            settingsTask.Exception,
                            "Remote Config settings failed"
                        );
                    }

                    SetDefaultsAndFetch();
                });
        }
        catch (Exception exception)
        {
            RecordNonFatal(exception, "Firebase product initialization failed");
            remoteConfigReady = true;
        }
    }

    private void SetDefaultsAndFetch()
    {
        if (remoteConfig == null)
        {
            remoteConfigReady = true;
            return;
        }

        remoteConfig.SetDefaultsAsync(BuildDefaults())
            .ContinueWithOnMainThread(defaultsTask =>
            {
                if (defaultsTask.IsCanceled || defaultsTask.IsFaulted)
                {
                    RecordNonFatal(
                        defaultsTask.Exception,
                        "Remote Config defaults failed"
                    );
                    remoteConfigReady = true;
                    return;
                }

                try
                {
                    remoteConfig.FetchAndActivateAsync()
                        .ContinueWithOnMainThread(fetchTask =>
                        {
                            if (fetchTask.IsCanceled || fetchTask.IsFaulted)
                            {
                                RecordNonFatal(
                                    fetchTask.Exception,
                                    "Remote Config fetch failed; local defaults kept"
                                );
                            }

                            remoteConfigReady = true;

#if DEVELOPMENT_BUILD
                            Debug.Log(
                                "[Firebase] Remote Config ready. Activated=" +
                                (!fetchTask.IsCanceled && !fetchTask.IsFaulted &&
                                 fetchTask.Result)
                            );
#endif
                        });
                }
                catch (Exception exception)
                {
                    RecordNonFatal(exception, "Remote Config fetch failed");
                    remoteConfigReady = true;
                }
            });
    }

    private static Dictionary<string, object> BuildDefaults()
    {
        return new Dictionary<string, object>
        {
            { GameStatsEnabledKey, true },

            { ReviewEnabledKey, true },
            { ReviewMinimumRunsKey, DefaultReviewMinimumRuns },
            { ReviewMinimumSessionsKey, DefaultReviewMinimumSessions },
            { ReviewMinimumPlaySecondsKey, DefaultReviewMinimumPlaySeconds },
            { ReviewCooldownDaysKey, DefaultReviewCooldownDays },
            { ReviewMaximumAttemptsKey, DefaultReviewMaximumAttempts },
            { ReviewMenuQuietSecondsKey, DefaultReviewMenuQuietSeconds },
            { ReviewAdCooldownSecondsKey, DefaultReviewAdCooldownSeconds },

            { AdsMinimumAttemptsKey, DefaultAdsMinimumAttempts },
            { AdsMaximumAttemptsKey, DefaultAdsMaximumAttempts },
            {
                AdsMainMenuIntervalSecondsKey,
                DefaultAdsMainMenuIntervalSeconds
            },

            { UpdateEnabledKey, true },
            { UpdateForceImmediateKey, false },
            {
                UpdateMinimumStalenessDaysKey,
                DefaultUpdateMinimumStalenessDays
            },
            { UpdateCheckDelaySecondsKey, DefaultUpdateCheckDelaySeconds }
        };
    }
#endif

    public static int GetInt(string key, int fallback)
    {
#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
        if (remoteConfigReady && instance != null && instance.remoteConfig != null)
        {
            try
            {
                long value = instance.remoteConfig.GetValue(key).LongValue;

                if (value < int.MinValue || value > int.MaxValue)
                    return fallback;

                return (int)value;
            }
            catch
            {
                return fallback;
            }
        }
#endif
        return fallback;
    }

    public static float GetFloat(string key, float fallback)
    {
#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
        if (remoteConfigReady && instance != null && instance.remoteConfig != null)
        {
            try
            {
                double value = instance.remoteConfig.GetValue(key).DoubleValue;

                if (double.IsNaN(value) || double.IsInfinity(value))
                    return fallback;

                return (float)value;
            }
            catch
            {
                return fallback;
            }
        }
#endif
        return fallback;
    }

    public static bool GetBool(string key, bool fallback)
    {
#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
        if (remoteConfigReady && instance != null && instance.remoteConfig != null)
        {
            try
            {
                return instance.remoteConfig.GetValue(key).BooleanValue;
            }
            catch
            {
                return fallback;
            }
        }
#endif
        return fallback;
    }

    public static string GetString(string key, string fallback)
    {
#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
        if (remoteConfigReady && instance != null && instance.remoteConfig != null)
        {
            try
            {
                string value = instance.remoteConfig.GetValue(key).StringValue;
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch
            {
                return fallback;
            }
        }
#endif
        return fallback;
    }

    public static void SetCustomKey(string key, string value)
    {
#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
        if (!initialized || string.IsNullOrEmpty(key))
            return;

        try
        {
            Crashlytics.SetCustomKey(key, value ?? string.Empty);
        }
        catch
        {
            // Diagnostics must never affect gameplay.
        }
#endif
    }

    public static void RecordNonFatal(Exception exception, string context)
    {
        if (exception == null)
            return;

#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
        if (initialized)
        {
            try
            {
                if (!string.IsNullOrEmpty(context))
                    Crashlytics.Log(context);

                Crashlytics.LogException(exception);
                return;
            }
            catch
            {
                // Fall through to the development log below.
            }
        }
#endif

#if DEVELOPMENT_BUILD
        Debug.LogWarning(
            "[Firebase] Non-fatal exception" +
            (string.IsNullOrEmpty(context) ? string.Empty : " (" + context + ")") +
            ": " + exception.Message
        );
#endif
    }

    public static void Log(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

#if FATEFULRUSH_FIREBASE && UNITY_ANDROID && !UNITY_EDITOR
        if (initialized)
        {
            try
            {
                Crashlytics.Log(message);
                return;
            }
            catch
            {
                // Fall through to the development log below.
            }
        }
#endif

#if DEVELOPMENT_BUILD
        Debug.Log("[Firebase] " + message);
#endif
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            initialized = false;
            remoteConfigReady = false;
        }
    }
}
