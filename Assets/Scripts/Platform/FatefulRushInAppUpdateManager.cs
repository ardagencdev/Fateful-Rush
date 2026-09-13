using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

/// <summary>
/// Checks Google Play for an available update at a safe Main Menu checkpoint.
/// Normal updates download flexibly in the background; high-priority or
/// Remote-Config-enabled updates may use Google's immediate update UI.
///
/// Google Play only reports an update for an app installed/owned through Play;
/// editor and sideloaded builds therefore fail closed without affecting play.
/// </summary>
public sealed class FatefulRushInAppUpdateManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const float CheckCooldownSeconds = 6f * 60f * 60f;
    private const float DefaultCheckDelaySeconds = 3f;
    private const int DefaultMinimumStalenessDays = 7;

    private static FatefulRushInAppUpdateManager instance;

    private bool checkInProgress;
    private bool updateFlowInProgress;
    private bool flexibleUpdateDownloaded;
    private float lastCheckRealtime = -99999f;
    private Coroutine scheduledCheck;
    private Coroutine completionRoutine;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AppUpdateManager appUpdateManager;
#endif

    /// <summary>
    /// Used by ads/review code to avoid stacking multiple Google-owned full
    /// screen or update flows on top of one another.
    /// </summary>
    public static bool IsBusy =>
        instance != null &&
        (instance.checkInProgress ||
         instance.updateFlowInProgress ||
         instance.flexibleUpdateDownloaded);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("FatefulRushInAppUpdateManager");
        instance = root.AddComponent<FatefulRushInAppUpdateManager>();
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
    }

    private void Start()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            appUpdateManager = new AppUpdateManager();

            if (SceneManager.GetActiveScene().name == MainMenuSceneName)
                ScheduleCheck();
        }
        catch (Exception exception)
        {
            FatefulRushFirebaseServices.RecordNonFatal(
                exception,
                "In-app update manager initialization failed"
            );
        }
#endif
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (SceneManager.GetActiveScene().name == MainMenuSceneName &&
            scheduledCheck == null &&
            !checkInProgress &&
            !updateFlowInProgress &&
            !flexibleUpdateDownloaded)
        {
            // Also retries after a gameplay -> Main Menu transition that was
            // still marked as transitioning when sceneLoaded fired.
            ScheduleCheck();
        }

        if (flexibleUpdateDownloaded &&
            completionRoutine == null &&
            IsSafeMainMenu())
        {
            completionRoutine = StartCoroutine(
                CompleteFlexibleUpdateWhenSafe()
            );
        }
#endif
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (flexibleUpdateDownloaded && completionRoutine == null)
        {
            completionRoutine = StartCoroutine(
                CompleteFlexibleUpdateWhenSafe()
            );
        }

        ScheduleCheck();
#endif
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (scene.name != MainMenuSceneName)
            return;

        if (flexibleUpdateDownloaded && completionRoutine == null)
        {
            completionRoutine = StartCoroutine(
                CompleteFlexibleUpdateWhenSafe()
            );
        }

        ScheduleCheck();
#endif
    }

    private void ScheduleCheck()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (appUpdateManager == null ||
            !FatefulRushFirebaseServices.GetBool(
                FatefulRushFirebaseServices.UpdateEnabledKey,
                true
            ) ||
            scheduledCheck != null ||
            checkInProgress ||
            updateFlowInProgress ||
            flexibleUpdateDownloaded ||
            Time.realtimeSinceStartup - lastCheckRealtime <
                CheckCooldownSeconds ||
            !IsSafeMainMenu())
        {
            return;
        }

        scheduledCheck = StartCoroutine(CheckAfterDelay());
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator CheckAfterDelay()
    {
        float delay = Mathf.Clamp(
            FatefulRushFirebaseServices.GetFloat(
                FatefulRushFirebaseServices.UpdateCheckDelaySecondsKey,
                DefaultCheckDelaySeconds
            ),
            0f,
            60f
        );

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        scheduledCheck = null;

        if (FatefulRushFirebaseServices.GetBool(
                FatefulRushFirebaseServices.UpdateEnabledKey,
                true
            ) &&
            IsSafeMainMenu())
            yield return CheckForUpdate();
    }

    private IEnumerator CheckForUpdate()
    {
        if (appUpdateManager == null || checkInProgress)
            yield break;

        checkInProgress = true;
        lastCheckRealtime = Time.realtimeSinceStartup;

        try
        {
            PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> operation;

            try
            {
                operation = appUpdateManager.GetAppUpdateInfo();
            }
            catch (Exception exception)
            {
                FatefulRushFirebaseServices.RecordNonFatal(
                    exception,
                    "GetAppUpdateInfo threw"
                );
                yield break;
            }

            if (operation == null)
                yield break;

            yield return operation;

            if (!operation.IsSuccessful)
            {
                LogUpdate(
                    "GetAppUpdateInfo failed: " + operation.Error
                );
                yield break;
            }

            AppUpdateInfo info = operation.GetResult();

            if (info == null)
                yield break;

            if (info.AppUpdateStatus == AppUpdateStatus.Downloaded)
            {
                flexibleUpdateDownloaded = true;
                FatefulRushFirebaseServices.SetCustomKey(
                    "last_update_status",
                    "Downloaded"
                );
                yield break;
            }

            if (info.UpdateAvailability ==
                UpdateAvailability.DeveloperTriggeredUpdateInProgress)
            {
                // An existing flow owns this update. If it has not finished
                // downloading yet, let Play continue it and check again later.
                LogUpdate(
                    "An in-app update is already in progress: " +
                    info.AppUpdateStatus
                );
                yield break;
            }

            if (info.UpdateAvailability != UpdateAvailability.UpdateAvailable)
                yield break;

            AppUpdateOptions immediateOptions =
                AppUpdateOptions.ImmediateAppUpdateOptions(false);
            AppUpdateOptions flexibleOptions =
                AppUpdateOptions.FlexibleAppUpdateOptions(false);

            bool immediateAllowed = info.IsUpdateTypeAllowed(immediateOptions);
            bool flexibleAllowed = info.IsUpdateTypeAllowed(flexibleOptions);

            bool immediateRequested =
                FatefulRushFirebaseServices.GetBool(
                    FatefulRushFirebaseServices.UpdateForceImmediateKey,
                    false
                );

            int minimumStalenessDays = Mathf.Clamp(
                FatefulRushFirebaseServices.GetInt(
                    FatefulRushFirebaseServices.UpdateMinimumStalenessDaysKey,
                    DefaultMinimumStalenessDays
                ),
                0,
                365
            );

            // Play priorities 4-5 are reserved for important releases. The
            // staleness gate prevents an immediate dialog on the first sighting.
            if (!immediateRequested &&
                info.UpdatePriority >= 4 &&
                info.ClientVersionStalenessDays >= minimumStalenessDays)
            {
                immediateRequested = true;
            }

            if (immediateRequested && immediateAllowed)
            {
                yield return StartUpdateFlow(info, immediateOptions, false);
            }
            else if (flexibleAllowed)
            {
                yield return StartUpdateFlow(info, flexibleOptions, true);
            }
            else if (immediateAllowed)
            {
                // Some Play/device states allow only immediate updates.
                yield return StartUpdateFlow(info, immediateOptions, false);
            }
            else
            {
                LogUpdate("No allowed in-app update type for this device.");
            }
        }
        finally
        {
            checkInProgress = false;
        }
    }

    private IEnumerator StartUpdateFlow(
        AppUpdateInfo info,
        AppUpdateOptions options,
        bool flexible)
    {
        updateFlowInProgress = true;
        FatefulRushFirebaseServices.SetCustomKey(
            "last_update_flow",
            flexible ? "Flexible" : "Immediate"
        );

        try
        {
            AppUpdateRequest request = null;

            try
            {
                request = appUpdateManager.StartUpdate(info, options);
            }
            catch (Exception exception)
            {
                FatefulRushFirebaseServices.RecordNonFatal(
                    exception,
                    "StartUpdate threw"
                );
            }

            if (request == null)
                yield break;

            yield return request;

            if (request.Error != AppUpdateErrorCode.NoError)
            {
                LogUpdate(
                    "Update flow failed: " + request.Error
                );
                yield break;
            }

            if (flexible && request.Status == AppUpdateStatus.Downloaded)
            {
                flexibleUpdateDownloaded = true;
                FatefulRushFirebaseServices.SetCustomKey(
                    "last_update_status",
                    "Downloaded"
                );
            }
            else
            {
                FatefulRushFirebaseServices.SetCustomKey(
                    "last_update_status",
                    request.Status.ToString()
                );
            }
        }
        finally
        {
            updateFlowInProgress = false;
        }
    }

    private IEnumerator CompleteFlexibleUpdateWhenSafe()
    {
        while (flexibleUpdateDownloaded && !IsSafeMainMenu())
            yield return null;

        if (!flexibleUpdateDownloaded)
        {
            completionRoutine = null;
            yield break;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        if (!IsSafeMainMenu())
        {
            completionRoutine = null;
            yield break;
        }

        PlayAsyncOperation<VoidResult, AppUpdateErrorCode> operation = null;

        try
        {
            operation = appUpdateManager.CompleteUpdate();
        }
        catch (Exception exception)
        {
            FatefulRushFirebaseServices.RecordNonFatal(
                exception,
                "CompleteUpdate threw"
            );
        }

        if (operation != null)
        {
            yield return operation;

            if (!operation.IsSuccessful)
            {
                LogUpdate(
                    "CompleteUpdate failed: " + operation.Error
                );
            }
            else
            {
                flexibleUpdateDownloaded = false;
            }
        }

        completionRoutine = null;
    }

    private static void LogUpdate(string message)
    {
        FatefulRushFirebaseServices.Log("In-app update: " + message);

#if DEVELOPMENT_BUILD
        Debug.Log("[InAppUpdate] " + message);
#endif
    }

    private static bool IsSafeMainMenu()
    {
        return Application.isFocused &&
               SceneManager.GetActiveScene().name == MainMenuSceneName &&
               !GameStateManager.IsGameplayStarted &&
               !FatefulRushAdManager.IsFullScreenBusy &&
               !FatefulRushReviewPrompt.IsBusy &&
               (SceneTransition.Instance == null ||
                !SceneTransition.Instance.IsTransitioning);
    }
#endif

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (instance == this)
            instance = null;
    }
}
