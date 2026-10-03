using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

// Same Unity component. Inspector data and lifecycle entry points remain in FatefulRushCloudSave.cs.
public sealed partial class FatefulRushCloudSave
{
    private static void TouchLocalRevision()
    {
        SetLocalRevision(DateTime.UtcNow.Ticks);
    }

    private static long GetLocalRevision()
    {
        string raw = PlayerPrefs.GetString(LocalRevisionKey, "0");

        long revision;
        return long.TryParse(raw, out revision)
            ? Math.Max(0L, revision)
            : 0L;
    }

    private static void SetLocalRevision(long revision)
    {
        PlayerPrefs.SetString(
            LocalRevisionKey,
            Math.Max(0L, revision).ToString()
        );
    }
}
