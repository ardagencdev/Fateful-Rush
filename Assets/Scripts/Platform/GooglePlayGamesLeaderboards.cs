using System;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

/// <summary>
/// Fateful Rush Google Play Games leaderboards.
///
/// - Submits a level time only when GameStateManager records a new local PB.
/// - Converts seconds to milliseconds because Play Games "Time" leaderboards
///   require millisecond integer scores.
/// - Re-syncs every locally recorded PB when Play Games authentication becomes
///   available, so a PB made offline is uploaded on a later sign-in.
/// - Supports the standard Play Games UI for all leaderboards or one level.
/// </summary>
public sealed class GooglePlayGamesLeaderboards : MonoBehaviour
{
    private static readonly int[] LeaderboardLevels =
    {
        1, 2, 3, 4, 5, 6, 7, 8, 9,
        11, 12,
        14, 15, 16,
        18, 19, 20,
        22, 23,
        25,
        27, 28,
        30,
        32, 33,
        35, 36,
        38, 39
    };

    private const float AuthenticationPollInterval = 1f;
    private const int NoPendingUi = -1;
    private const int AllLeaderboardsUi = 0;

    private static GooglePlayGamesLeaderboards instance;

    private float nextAuthenticationPollTime;
    private bool wasAuthenticated;
    private int pendingLeaderboardUi = NoPendingUi;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static readonly float[] RankReadDelays = { 1f, 2f, 4f };
    private readonly Dictionary<string, long> pendingRankChecks = new Dictionary<string, long>();
    private const int MaxConcurrentRankChecks = 2;
    private int activeRankChecks;

    private readonly struct RankSnapshot
    {
        internal readonly int Rank;
        internal readonly long Milliseconds;

        internal RankSnapshot(int rank, long milliseconds)
        {
            Rank = rank;
            Milliseconds = milliseconds;
        }

        internal bool IsRanked => Rank > 0 && Milliseconds > 0;
    }
#endif

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

        EnsureInstance();
    }

    private static GooglePlayGamesLeaderboards EnsureInstance()
    {
        if (instance != null)
            return instance;

        GooglePlayGamesLeaderboards existing =
            FindAnyObjectByType<GooglePlayGamesLeaderboards>();

        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject root =
            new GameObject("GooglePlayGamesLeaderboards");

        instance =
            root.AddComponent<GooglePlayGamesLeaderboards>();

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

    private void Start()
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

        PollAuthentication(force: true);
    }

    private void Update()
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

        if (Time.unscaledTime < nextAuthenticationPollTime)
            return;

        PollAuthentication(force: false);
    }

    /// <summary>
    /// Called when a new local PB is saved.
    /// If the player is offline/not authenticated, the local PB remains saved
    /// and will be uploaded automatically on a later authenticated launch.
    /// </summary>
    public static void SubmitBestTime(
        int levelNumber,
        float seconds)
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

        EnsureInstance()
            .SubmitBestTimeInternal(
                levelNumber,
                seconds
            );
    }

    /// <summary>
    /// Opens the standard Google Play Games UI containing all 29 leaderboards.
    /// If sign-in is not ready yet, manual sign-in is requested first and the
    /// UI opens automatically when authentication succeeds.
    /// </summary>
    public static void ShowAllLeaderboardsUI()
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

        EnsureInstance()
            .RequestLeaderboardUi(
                AllLeaderboardsUi
            );
    }

    /// <summary>
    /// Opens one specific level leaderboard.
    /// </summary>
    public static void ShowLevelLeaderboardUI(
        int levelNumber)
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

        if (!FatefulRushLeaderboardIds.TryGetId(
                levelNumber,
                out _))
        {
            Debug.LogWarning(
                "[GooglePlayGamesLeaderboards] " +
                $"Level {levelNumber} has no Best Time leaderboard."
            );

            return;
        }

        EnsureInstance()
            .RequestLeaderboardUi(
                levelNumber
            );
    }

    /// <summary>
    /// Safe to call manually, but normally authentication detection invokes it.
    /// </summary>
    public static void SyncLocalBestTimes()
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

        EnsureInstance()
            .SyncLocalBestTimesInternal();
    }

    private void PollAuthentication(
        bool force)
    {
        nextAuthenticationPollTime =
            Time.unscaledTime +
            AuthenticationPollInterval;

        bool authenticated =
            IsPlayGamesReady();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!authenticated)
            MenuNotifications.SetLeaderboardAccount(null);
        else if (!wasAuthenticated || force)
            MenuNotifications.SetLeaderboardAccount(PlayGamesPlatform.Instance.GetUserId());
#endif

        if (authenticated &&
            (!wasAuthenticated || force))
        {
            SyncLocalBestTimesInternal();
        }

        if (authenticated &&
            pendingLeaderboardUi != NoPendingUi)
        {
            int requestedUi =
                pendingLeaderboardUi;

            pendingLeaderboardUi =
                NoPendingUi;

            OpenLeaderboardUiInternal(
                requestedUi
            );
        }

        wasAuthenticated =
            authenticated;
    }

    private void SubmitBestTimeInternal(
        int levelNumber,
        float seconds)
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

        if (!TryBuildSubmission(
                levelNumber,
                seconds,
                out string leaderboardId,
                out long milliseconds))
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsPlayGamesReady())
            return;

        string playerId = PlayGamesPlatform.Instance.GetUserId();
        string scope = MenuNotifications.SetLeaderboardAccount(playerId);
        if (scope == null)
        {
            ReportTime(levelNumber, leaderboardId, milliseconds);
            return;
        }

        string rankKey = scope + "_level_" + levelNumber;
        if (pendingRankChecks.TryGetValue(rankKey, out long pendingTime))
        {
            pendingRankChecks[rankKey] = Math.Min(pendingTime, milliseconds);
            return;
        }

        RankSnapshot previous = ReadSavedRank(rankKey);
        if (previous.IsRanked && previous.Milliseconds <= milliseconds)
        {
            ReportTime(levelNumber, leaderboardId, milliseconds);
            return;
        }

        pendingRankChecks.Add(rankKey, milliseconds);
        StartCoroutine(TrackRankAfterSubmission(playerId, scope, rankKey, levelNumber, leaderboardId));
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator TrackRankAfterSubmission(
        string playerId, string scope, string rankKey, int levelNumber, string leaderboardId)
    {
        bool hasSlot = false;
        try
        {
            while (activeRankChecks >= MaxConcurrentRankChecks)
            {
                if (!IsCurrentPlayer(playerId))
                    yield break;

                yield return null;
            }

            activeRankChecks++;
            hasSlot = true;
            while (IsCurrentPlayer(playerId))
            {
                long milliseconds = pendingRankChecks[rankKey];
                RankSnapshot previous = ReadSavedRank(rankKey);
                RankSnapshot? before = null;
                yield return LoadRank(leaderboardId, rank => before = rank);
                if (!IsCurrentPlayer(playerId))
                    yield break;

                if (before.HasValue && before.Value.IsRanked && before.Value.Milliseconds > milliseconds)
                {
                    previous = before.Value;
                    if (SetRank(rankKey, previous))
                        PlayerPrefs.Save();
                }

                ReportTime(levelNumber, leaderboardId, milliseconds);

                if (!TryConfirmRank(scope, rankKey, levelNumber, previous, before, milliseconds))
                {
                    foreach (float delay in RankReadDelays)
                    {
                        yield return new WaitForSecondsRealtime(delay);
                        if (!IsCurrentPlayer(playerId))
                            yield break;

                        RankSnapshot? after = null;
                        yield return LoadRank(leaderboardId, rank => after = rank);
                        if (!IsCurrentPlayer(playerId))
                            yield break;

                        if (TryConfirmRank(scope, rankKey, levelNumber, previous, after, milliseconds))
                            break;
                    }
                }

                if (pendingRankChecks[rankKey] == milliseconds)
                    yield break;
            }
        }
        finally
        {
            if (hasSlot)
                activeRankChecks--;

            pendingRankChecks.Remove(rankKey);
        }
    }

    private static bool TryConfirmRank(string scope, string key, int level, RankSnapshot previous,
        RankSnapshot? reading, long submittedTime)
    {
        if (!reading.HasValue || !reading.Value.IsRanked || reading.Value.Milliseconds > submittedTime)
            return false;

        RankSnapshot current = reading.Value;
        bool changed = SetRank(key, current);
        if (previous.IsRanked && current.Milliseconds < previous.Milliseconds && current.Rank < previous.Rank)
            MenuNotifications.MarkLeaderboardPending(scope, level);
        else if (changed)
            PlayerPrefs.Save();

        return true;
    }

    private static IEnumerator LoadRank(string leaderboardId, Action<RankSnapshot?> receive)
    {
        bool complete = false;
        RankSnapshot? result = null;
        try
        {
            PlayGamesPlatform.Instance.LoadScores(
                leaderboardId, LeaderboardStart.PlayerCentered, 1,
                LeaderboardCollection.Public, LeaderboardTimeSpan.AllTime, data =>
                {
                    if (complete)
                        return;

                    if (data?.Status == ResponseStatus.Success)
                    {
                        var score = data.PlayerScore;
                        result = score != null ? new RankSnapshot(score.rank, score.value) : default(RankSnapshot);
                    }

                    complete = true;
                });
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            complete = true;
        }

        float deadline = Time.realtimeSinceStartup + 5f;
        while (!complete && Time.realtimeSinceStartup < deadline)
            yield return null;

        complete = true;
        receive(result);
    }

    private static bool IsCurrentPlayer(string playerId)
    {
        return IsPlayGamesReady() && PlayGamesPlatform.Instance.GetUserId() == playerId;
    }

    private static RankSnapshot ReadSavedRank(string key)
    {
        string saved = PlayerPrefs.GetString(key);
        int separator = saved.IndexOf(':');
        if (separator > 0 && int.TryParse(saved.Substring(0, separator), out int rank) &&
            long.TryParse(saved.Substring(separator + 1), out long time))
        {
            return new RankSnapshot(rank, time);
        }

        return default;
    }

    private static bool SetRank(string key, RankSnapshot score)
    {
        string value = score.Rank + ":" + score.Milliseconds;
        if (PlayerPrefs.GetString(key) == value)
            return false;

        PlayerPrefs.SetString(key, value);
        return true;
    }

    private static void ReportTime(int levelNumber, string leaderboardId, long milliseconds)
    {
        try
        {
            PlayGamesPlatform.Instance.ReportScore(milliseconds, leaderboardId, success =>
            {
                if (!success)
                    Debug.LogWarning($"[GooglePlayGamesLeaderboards] Score submission failed for Level {levelNumber}. Time: {milliseconds} ms");
            });
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
#endif

    private void SyncLocalBestTimesInternal()
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsPlayGamesReady())
            return;

        for (int i = 0; i < LeaderboardLevels.Length; i++)
        {
            int levelNumber =
                LeaderboardLevels[i];

            string key =
                "BestTime_Level_" +
                levelNumber;

            float bestTime =
                PlayerPrefs.GetFloat(
                    key,
                    -1f
                );

            if (bestTime <= 0f)
                continue;

            SubmitBestTimeInternal(
                levelNumber,
                bestTime
            );
        }
#endif
    }

    private void RequestLeaderboardUi(
        int requestedLevel)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (IsPlayGamesReady())
        {
            OpenLeaderboardUiInternal(
                requestedLevel
            );

            return;
        }

        pendingLeaderboardUi =
            requestedLevel;

        // Reuse the game's existing PGS sign-in flow.
        GooglePlayGamesManager.ManualSignIn();
#else
        Debug.Log(
            "[GooglePlayGamesLeaderboards] " +
            "Leaderboard UI is only available in an Android player build."
        );
#endif
    }

    private void OpenLeaderboardUiInternal(
        int requestedLevel)
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return; }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsPlayGamesReady())
        {
            pendingLeaderboardUi =
                requestedLevel;

            return;
        }

        string leaderboardId = null;
        if (requestedLevel != AllLeaderboardsUi &&
            !FatefulRushLeaderboardIds.TryGetId(requestedLevel, out leaderboardId))
        {
            Debug.LogWarning(
                "[GooglePlayGamesLeaderboards] " +
                $"No leaderboard ID for Level {requestedLevel}."
            );

            return;
        }

        string scope = MenuNotifications.SetLeaderboardAccount(PlayGamesPlatform.Instance.GetUserId());
        int visibleRevision = scope != null ? MenuNotifications.LeaderboardRevision(scope) : 0;
        PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId, status =>
        {
            if (scope != null && (status == UIStatus.Valid || status == UIStatus.UserClosedUI))
                MenuNotifications.MarkLeaderboardsSeen(scope, requestedLevel, visibleRevision);
        });
#else
        Debug.Log(
            "[GooglePlayGamesLeaderboards] " +
            "Leaderboard UI is only available in an Android player build."
        );
#endif
    }

    private static bool TryBuildSubmission(
        int levelNumber,
        float seconds,
        out string leaderboardId,
        out long milliseconds)
    {
        milliseconds = 0L;

        if (!FatefulRushLeaderboardIds.TryGetId(
                levelNumber,
                out leaderboardId))
        {
            return false;
        }

        if (seconds <= 0f ||
            float.IsNaN(seconds) ||
            float.IsInfinity(seconds))
        {
            Debug.LogWarning(
                "[GooglePlayGamesLeaderboards] " +
                $"Invalid best time for Level {levelNumber}: {seconds}"
            );

            return false;
        }

        milliseconds =
            Math.Max(
                1L,
                (long)Math.Round(
                    seconds * 1000d,
                    MidpointRounding.AwayFromZero
                )
            );

        return true;
    }

    private static bool IsPlayGamesReady()
    {
#if FATEFULRUSH_DIAGNOSTICS
        if (!FatefulRushDiagnosticsServices.PlayGamesEnabled) { return false; }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        return PlayGamesPlatform.Instance != null &&
               PlayGamesPlatform.Instance.IsAuthenticated();
#else
        return false;
#endif
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
