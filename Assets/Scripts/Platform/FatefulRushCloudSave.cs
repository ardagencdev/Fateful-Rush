using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

/// <summary>
/// Google Play Games Saved Games integration for Fateful Rush progression.
///
/// Design goals:
/// - A reinstall must be able to restore progression after PGS authentication.
/// - Local and cloud data are merged instead of blindly overwriting one another.
/// - Old save generations are never allowed to resurrect deprecated progress.
/// - Cloud writes are debounced and never happen every frame / during gameplay loops.
/// - Failure of the cloud service must never block normal gameplay.
/// </summary>
public sealed partial class FatefulRushCloudSave : MonoBehaviour
{
    private const string SaveFileName = "FatefulRush_Save_v1";
    private const string LocalRevisionKey = "FatefulRush_CloudRevisionUtcTicks";
    private const float UploadDebounceSeconds = 2.0f;

    private static readonly string[] MonotonicIntKeys =
    {
        "UnlockedLevel",

        "Stats_TotalRuns",
        "Stats_TotalWins",
        "Stats_TotalDeaths",
        "Stats_TotalCoins",
        "Stats_TotalCoinValue",
        "Stats_NormalCoins",
        "Stats_GoldCoins",
        "Stats_RareCoins",
        "Stats_DashUses",
        "Stats_CloneUses",
        "Stats_SlowBuffUses",
        "Stats_ArmorBuffUses",
        "Stats_ArmorKills",
        "Stats_ArmorEnemyKills",
        "Stats_SpaceBombTriggers",
        "Stats_TotalScore",
        "Stats_CompletedScoreTotal",
        "Stats_ScoreRuns",
        "Stats_BestRunScore",
        "Stats_MostCoinsInRun",
        "Stats_BestWinStreak",
        "Stats_HighestCombo",
        "Stats_LongestComboChain",
        "Stats_MaxComboReached",
        "Stats_ComboBonusScore",
        "Stats_NearMisses",
        "Stats_BestNearMissStreak",
        "Stats_MagnetCoins",
        "Stats_BeaconsDestroyed",
        "Stats_HuntersStunned",
        "Stats_BossEncounters",
        "Stats_BossSplits",
        "Stats_BossAoeEvades",
        "Stats_MiniBossAoeEvades",
        "Stats_Mode_Score_Runs",
        "Stats_Mode_Score_Wins",
        "Stats_Mode_Survival_Runs",
        "Stats_Mode_Survival_Wins",
        "Stats_Mode_TimedScore_Runs",
        "Stats_Mode_TimedScore_Wins",
        "Stats_Death_STALKER",
        "Stats_Death_HUNTER",
        "Stats_Death_BLASTER",
        "Stats_Death_LASER_BULLET",
        "Stats_Death_LASER_WALL",
        "Stats_Death_BOSS",
        "Stats_Death_MINI_BOSS",
        "Stats_Death_SPACE_BOMB",
        "Stats_Death_TIME_EXPIRED",
        "Stats_Death_UNKNOWN"
    };

    private static readonly string[] LatestIntKeys =
    {
        "Stats_CurrentWinStreak",
        "FatefulRush_SignalStable"
    };

    private static readonly string[] MaxFloatKeys =
    {
        "Stats_TotalPlayTime",
        "Stats_LongestRunTime"
    };

    private static readonly string[] LatestStringKeys =
    {
        PlayerSkinCatalog.SelectedSkinKey
    };

    private static FatefulRushCloudSave instance;

    private bool initialSyncCompleted;

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool initialSyncRequested;
    private bool operationInFlight;
    private bool uploadRequested;
    private float uploadNotBeforeRealtime;
    private Action pendingInitialSyncCallback;
#endif

    public static bool InitialSyncCompleted =>
        instance != null && instance.initialSyncCompleted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static FatefulRushCloudSave EnsureInstance()
    {
        if (instance != null)
            return instance;

        FatefulRushCloudSave existing =
            FindAnyObjectByType<FatefulRushCloudSave>();

        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject root = new GameObject("FatefulRushCloudSave");
        instance = root.AddComponent<FatefulRushCloudSave>();
        DontDestroyOnLoad(root);
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
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        TryStartInitialSyncIfSafe();

        if (!uploadRequested ||
            operationInFlight ||
            !initialSyncCompleted ||
            Time.realtimeSinceStartup < uploadNotBeforeRealtime ||
            !IsSafeForCloudWork())
        {
            return;
        }

        if (!IsPlatformAuthenticated())
            return;

        uploadRequested = false;
        UploadCurrentSaveInternal();
#endif
    }

    /// <summary>
    /// Called immediately after a successful PGS authentication.
    /// The callback is always invoked, even if cloud save fails, so achievements/UI
    /// can continue to work without being coupled to Saved Games availability.
    /// </summary>
    public static void SyncAfterAuthentication(Action onFinished)
    {
        EnsureInstance().SyncAfterAuthenticationInternal(onFinished);
    }

    /// <summary>
    /// Marks the current local progression as changed and schedules one cloud write.
    /// Multiple calls close together collapse into a single upload.
    /// </summary>
    public static void RequestUpload()
    {
        TouchLocalRevision();

#if UNITY_ANDROID && !UNITY_EDITOR
        FatefulRushCloudSave manager = EnsureInstance();
        manager.uploadRequested = true;
        manager.uploadNotBeforeRealtime =
            Time.realtimeSinceStartup + UploadDebounceSeconds;
#else
        // Cloud upload is Android-only; keep the method harmless in Editor/other builds.
        EnsureInstance();
#endif
    }

    private void SyncAfterAuthenticationInternal(Action onFinished)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (initialSyncCompleted)
        {
            InvokeSafely(onFinished);
            return;
        }

        pendingInitialSyncCallback += onFinished;
        initialSyncRequested = true;
        TryStartInitialSyncIfSafe();
#else
        initialSyncCompleted = true;
        InvokeSafely(onFinished);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void TryStartInitialSyncIfSafe()
    {
        if (!initialSyncRequested ||
            initialSyncCompleted ||
            operationInFlight ||
            !IsPlatformAuthenticated() ||
            !IsSafeForCloudWork())
        {
            return;
        }

        initialSyncRequested = false;
        operationInFlight = true;

        try
        {
            PlayGamesPlatform.Instance.SavedGame
                .OpenWithAutomaticConflictResolution(
                    SaveFileName,
                    DataSource.ReadCacheOrNetwork,
                    ConflictResolutionStrategy.UseLongestPlaytime,
                    HandleInitialSaveOpened
                );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[CloudSave] Saved Game acilamadi; local save kullanilacak: " +
                exception.Message
            );

            CompleteInitialSync(false);
        }
    }

    private static bool IsSafeForCloudWork()
    {
        // Initial restore / upload may allocate JSON and invoke Play Games SDK
        // callbacks. Never deliberately start that work while a run is active.
        return !GameStateManager.IsGameplayStarted;
    }

    private void HandleInitialSaveOpened(
        SavedGameRequestStatus status,
        ISavedGameMetadata metadata)
    {
        if (status != SavedGameRequestStatus.Success || metadata == null)
        {
            Debug.LogWarning(
                "[CloudSave] Saved Game open basarisiz: " + status
            );

            CompleteInitialSync(false);
            return;
        }

        try
        {
            PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(
                metadata,
                (readStatus, data) =>
                    HandleInitialSaveRead(
                        metadata,
                        readStatus,
                        data
                    )
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[CloudSave] Cloud veri okunamadi: " + exception.Message
            );

            CompleteInitialSync(false);
        }
    }

    private void HandleInitialSaveRead(
        ISavedGameMetadata metadata,
        SavedGameRequestStatus status,
        byte[] data)
    {
        if (status != SavedGameRequestStatus.Success)
        {
            Debug.LogWarning(
                "[CloudSave] Cloud read basarisiz; local save korunuyor: " +
                status
            );

            CompleteInitialSync(false);
            return;
        }

        CloudSnapshot local = BuildLocalSnapshot();
        CloudSnapshot cloud =
            data != null && data.Length > 0
                ? Deserialize(data)
                : null;

        CloudSnapshot merged = MergeSnapshots(local, cloud);
        ApplySnapshotToLocal(merged);

        // The local copy is already usable at this point. We still commit the
        // merged result so both devices converge on the same canonical state.
        CommitSnapshot(
            metadata,
            merged,
            commitStatus =>
            {
                if (commitStatus != SavedGameRequestStatus.Success)
                {
                    Debug.LogWarning(
                        "[CloudSave] Ilk merge cloud'a yazilamadi: " +
                        commitStatus
                    );
                }

                CompleteInitialSync(true);
            }
        );
    }

    private void UploadCurrentSaveInternal()
    {
        if (operationInFlight || !IsPlatformAuthenticated())
            return;

        operationInFlight = true;

        try
        {
            PlayGamesPlatform.Instance.SavedGame
                .OpenWithAutomaticConflictResolution(
                    SaveFileName,
                    DataSource.ReadCacheOrNetwork,
                    ConflictResolutionStrategy.UseLongestPlaytime,
                    HandleUploadSaveOpened
                );
        }
        catch (Exception exception)
        {
            operationInFlight = false;
            uploadRequested = true;
            uploadNotBeforeRealtime = Time.realtimeSinceStartup + 10f;

            Debug.LogWarning(
                "[CloudSave] Upload icin Saved Game acilamadi: " +
                exception.Message
            );
        }
    }

    private void HandleUploadSaveOpened(
        SavedGameRequestStatus status,
        ISavedGameMetadata metadata)
    {
        if (status != SavedGameRequestStatus.Success || metadata == null)
        {
            operationInFlight = false;
            uploadRequested = true;
            uploadNotBeforeRealtime = Time.realtimeSinceStartup + 10f;

            Debug.LogWarning(
                "[CloudSave] Upload open basarisiz: " + status
            );

            return;
        }

        try
        {
            PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(
                metadata,
                (readStatus, data) =>
                    HandleUploadSaveRead(
                        metadata,
                        readStatus,
                        data
                    )
            );
        }
        catch (Exception exception)
        {
            operationInFlight = false;
            uploadRequested = true;
            uploadNotBeforeRealtime = Time.realtimeSinceStartup + 10f;

            Debug.LogWarning(
                "[CloudSave] Upload oncesi cloud read basarisiz: " +
                exception.Message
            );
        }
    }

    private void HandleUploadSaveRead(
        ISavedGameMetadata metadata,
        SavedGameRequestStatus status,
        byte[] data)
    {
        if (status != SavedGameRequestStatus.Success)
        {
            operationInFlight = false;
            uploadRequested = true;
            uploadNotBeforeRealtime = Time.realtimeSinceStartup + 10f;

            Debug.LogWarning(
                "[CloudSave] Upload oncesi cloud read basarisiz: " +
                status
            );

            return;
        }

        CloudSnapshot local = BuildLocalSnapshot();
        CloudSnapshot cloud =
            data != null && data.Length > 0
                ? Deserialize(data)
                : null;

        CloudSnapshot merged = MergeSnapshots(local, cloud);
        ApplySnapshotToLocal(merged);

        CommitSnapshot(
            metadata,
            merged,
            commitStatus =>
            {
                operationInFlight = false;

                if (commitStatus != SavedGameRequestStatus.Success)
                {
                    uploadRequested = true;
                    uploadNotBeforeRealtime =
                        Time.realtimeSinceStartup + 10f;

                    Debug.LogWarning(
                        "[CloudSave] Upload commit basarisiz: " +
                        commitStatus
                    );
                }
            }
        );
    }

    private void CommitSnapshot(
        ISavedGameMetadata metadata,
        CloudSnapshot snapshot,
        Action<SavedGameRequestStatus> onFinished)
    {
        if (metadata == null)
        {
            operationInFlight = false;
            InvokeSafely(
                onFinished,
                SavedGameRequestStatus.BadInputError
            );
            return;
        }

        snapshot.saveGeneration = ReleaseSaveMigration.CurrentSaveGeneration;
        snapshot.revisionUtcTicks = DateTime.UtcNow.Ticks;

        SetLocalRevision(snapshot.revisionUtcTicks);

        byte[] bytes = Serialize(snapshot);

        float totalPlayTime = GetFloat(
            snapshot,
            "Stats_TotalPlayTime",
            0f
        );

        SavedGameMetadataUpdate metadataUpdate =
            new SavedGameMetadataUpdate.Builder()
                .WithUpdatedDescription(
                    "Fateful Rush progression - " +
                    DateTime.UtcNow.ToString("u")
                )
                .WithUpdatedPlayedTime(
                    TimeSpan.FromSeconds(
                        Math.Max(0d, totalPlayTime)
                    )
                )
                .Build();

        try
        {
            PlayGamesPlatform.Instance.SavedGame.CommitUpdate(
                metadata,
                metadataUpdate,
                bytes,
                (status, committedMetadata) =>
                    InvokeSafely(onFinished, status)
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[CloudSave] CommitUpdate exception: " + exception.Message
            );

            InvokeSafely(
                onFinished,
                SavedGameRequestStatus.InternalError
            );
        }
    }

    private static bool IsPlatformAuthenticated()
    {
        try
        {
            return PlayGamesPlatform.Instance != null &&
                   PlayGamesPlatform.Instance.IsAuthenticated();
        }
        catch
        {
            return false;
        }
    }
    private void CompleteInitialSync(bool cloudWasReachable)
    {
        operationInFlight = false;
        initialSyncRequested = false;
        initialSyncCompleted = true;

        Action callback = pendingInitialSyncCallback;
        pendingInitialSyncCallback = null;

        InvokeSafely(callback);

#if DEVELOPMENT_BUILD
        Debug.Log(
            "[CloudSave] Initial sync complete. CloudReachable=" +
            cloudWasReachable
        );
#endif
    }
#endif

    private static void InvokeSafely(Action callback)
    {
        if (callback == null)
            return;

        try
        {
            callback();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void InvokeSafely(
        Action<SavedGameRequestStatus> callback,
        SavedGameRequestStatus status)
    {
        if (callback == null)
            return;

        try
        {
            callback(status);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
#endif

    [Serializable]
    private sealed class CloudSnapshot
    {
        public int schemaVersion = 1;
        public int saveGeneration;
        public long revisionUtcTicks;
        public List<IntEntry> ints = new List<IntEntry>();
        public List<FloatEntry> floats = new List<FloatEntry>();
        public List<StringEntry> strings = new List<StringEntry>();

        public void EnsureLists()
        {
            if (ints == null)
                ints = new List<IntEntry>();

            if (floats == null)
                floats = new List<FloatEntry>();

            if (strings == null)
                strings = new List<StringEntry>();
        }
    }

    [Serializable]
    private sealed class IntEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    private sealed class FloatEntry
    {
        public string key;
        public float value;
    }

    [Serializable]
    private sealed class StringEntry
    {
        public string key;
        public string value;
    }
}
