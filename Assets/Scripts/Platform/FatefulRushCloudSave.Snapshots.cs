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
    private static CloudSnapshot BuildLocalSnapshot()
    {
        CloudSnapshot snapshot = new CloudSnapshot
        {
            schemaVersion = 1,
            saveGeneration = ReleaseSaveMigration.CurrentSaveGeneration,
            revisionUtcTicks = GetLocalRevision()
        };

        AddInt(snapshot, "UnlockedLevel", PlayerPrefs.GetInt("UnlockedLevel", 1));

        for (int level = StatsManager.FirstLevelNumber;
             level <= StatsManager.LastLevelNumber;
             level++)
        {
            string completedKey = "CompletedLevel_" + level;
            AddInt(
                snapshot,
                completedKey,
                PlayerPrefs.GetInt(completedKey, 0)
            );

            string bestTimeKey = "BestTime_Level_" + level;
            AddFloat(
                snapshot,
                bestTimeKey,
                PlayerPrefs.GetFloat(bestTimeKey, -1f)
            );
        }


        for (int i = 0; i < MonotonicIntKeys.Length; i++)
        {
            string key = MonotonicIntKeys[i];

            if (key == "UnlockedLevel")
                continue;

            AddInt(
                snapshot,
                key,
                PlayerPrefs.GetInt(key, 0)
            );
        }

        for (int i = 0; i < LatestIntKeys.Length; i++)
        {
            string key = LatestIntKeys[i];
            AddInt(
                snapshot,
                key,
                PlayerPrefs.GetInt(key, 0)
            );
        }

        for (int i = 0; i < MaxFloatKeys.Length; i++)
        {
            string key = MaxFloatKeys[i];
            AddFloat(
                snapshot,
                key,
                PlayerPrefs.GetFloat(key, 0f)
            );
        }

        for (int i = 0; i < LatestStringKeys.Length; i++)
        {
            string key = LatestStringKeys[i];
            AddString(
                snapshot,
                key,
                PlayerPrefs.GetString(key, string.Empty)
            );
        }

        return snapshot;
    }

    private static CloudSnapshot MergeSnapshots(
        CloudSnapshot local,
        CloudSnapshot cloud)
    {
        if (local == null)
            local = new CloudSnapshot();

        if (!IsCloudSnapshotCompatible(cloud))
        {
            CloudSnapshot localOnly = CloneSnapshot(local);
            localOnly.saveGeneration = ReleaseSaveMigration.CurrentSaveGeneration;

            if (localOnly.revisionUtcTicks <= 0)
                localOnly.revisionUtcTicks = DateTime.UtcNow.Ticks;

            return localOnly;
        }

        CloudSnapshot merged = new CloudSnapshot
        {
            schemaVersion = Math.Max(local.schemaVersion, cloud.schemaVersion),
            saveGeneration = ReleaseSaveMigration.CurrentSaveGeneration,
            revisionUtcTicks = Math.Max(
                local.revisionUtcTicks,
                cloud.revisionUtcTicks
            )
        };

        // Mission progression can only move forward.
        AddInt(
            merged,
            "UnlockedLevel",
            Math.Max(
                GetInt(local, "UnlockedLevel", 1),
                GetInt(cloud, "UnlockedLevel", 1)
            )
        );

        for (int level = StatsManager.FirstLevelNumber;
             level <= StatsManager.LastLevelNumber;
             level++)
        {
            string completedKey = "CompletedLevel_" + level;
            AddInt(
                merged,
                completedKey,
                Math.Max(
                    GetInt(local, completedKey, 0),
                    GetInt(cloud, completedKey, 0)
                )
            );

            string bestTimeKey = "BestTime_Level_" + level;
            AddFloat(
                merged,
                bestTimeKey,
                PickBestPositiveTime(
                    GetFloat(local, bestTimeKey, -1f),
                    GetFloat(cloud, bestTimeKey, -1f)
                )
            );
        }

        AddFloat(
            merged,
            "BestTime_DevRoom",
            PickBestPositiveTime(
                GetFloat(local, "BestTime_DevRoom", -1f),
                GetFloat(cloud, "BestTime_DevRoom", -1f)
            )
        );

        for (int i = 0; i < MonotonicIntKeys.Length; i++)
        {
            string key = MonotonicIntKeys[i];

            if (key == "UnlockedLevel")
                continue;

            AddInt(
                merged,
                key,
                Math.Max(
                    GetInt(local, key, 0),
                    GetInt(cloud, key, 0)
                )
            );
        }

        bool cloudIsNewer =
            cloud.revisionUtcTicks > local.revisionUtcTicks;

        CloudSnapshot latest = cloudIsNewer ? cloud : local;

        for (int i = 0; i < LatestIntKeys.Length; i++)
        {
            string key = LatestIntKeys[i];
            AddInt(
                merged,
                key,
                GetInt(latest, key, 0)
            );
        }

        for (int i = 0; i < MaxFloatKeys.Length; i++)
        {
            string key = MaxFloatKeys[i];
            AddFloat(
                merged,
                key,
                Math.Max(
                    GetFloat(local, key, 0f),
                    GetFloat(cloud, key, 0f)
                )
            );
        }

        for (int i = 0; i < LatestStringKeys.Length; i++)
        {
            string key = LatestStringKeys[i];
            string value = GetString(latest, key, string.Empty);

            // If the newer side has no selected skin at all (typical clean
            // reinstall before restore), keep the older non-empty selection.
            if (string.IsNullOrWhiteSpace(value))
            {
                CloudSnapshot older = cloudIsNewer ? local : cloud;
                value = GetString(older, key, string.Empty);
            }

            AddString(merged, key, value);
        }

        return merged;
    }

    private static void ApplySnapshotToLocal(CloudSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        PlayerPrefs.SetInt(
            "FatefulRush_SaveGeneration",
            ReleaseSaveMigration.CurrentSaveGeneration
        );

        for (int i = 0; i < snapshot.ints.Count; i++)
        {
            IntEntry entry = snapshot.ints[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                continue;

            PlayerPrefs.SetInt(entry.key, entry.value);
        }

        for (int i = 0; i < snapshot.floats.Count; i++)
        {
            FloatEntry entry = snapshot.floats[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                continue;

            if (entry.key.StartsWith("BestTime_", StringComparison.Ordinal) &&
                entry.value <= 0f)
            {
                PlayerPrefs.DeleteKey(entry.key);
                continue;
            }

            PlayerPrefs.SetFloat(entry.key, entry.value);
        }

        for (int i = 0; i < snapshot.strings.Count; i++)
        {
            StringEntry entry = snapshot.strings[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                continue;

            if (string.IsNullOrWhiteSpace(entry.value))
                PlayerPrefs.DeleteKey(entry.key);
            else
                PlayerPrefs.SetString(entry.key, entry.value);
        }

        SetLocalRevision(snapshot.revisionUtcTicks);

        // Avoid forcing disk IO if an asynchronous SDK callback happens after
        // the player has already entered a run. The in-memory values are valid
        // immediately and the next normal checkpoint will persist them.
        if (!GameStateManager.IsGameplayStarted)
            PlayerPrefs.Save();
    }

    private static bool IsCloudSnapshotCompatible(CloudSnapshot snapshot)
    {
        return snapshot != null &&
               snapshot.schemaVersion > 0 &&
               snapshot.saveGeneration ==
               ReleaseSaveMigration.CurrentSaveGeneration;
    }

    private static float PickBestPositiveTime(float a, float b)
    {
        bool aValid = a > 0f && !float.IsNaN(a) && !float.IsInfinity(a);
        bool bValid = b > 0f && !float.IsNaN(b) && !float.IsInfinity(b);

        if (aValid && bValid)
            return Math.Min(a, b);

        if (aValid)
            return a;

        if (bValid)
            return b;

        return -1f;
    }

    private static byte[] Serialize(CloudSnapshot snapshot)
    {
        string json = JsonUtility.ToJson(snapshot);
        return Encoding.UTF8.GetBytes(json);
    }

    private static CloudSnapshot Deserialize(byte[] bytes)
    {
        try
        {
            string json = Encoding.UTF8.GetString(bytes);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            CloudSnapshot snapshot =
                JsonUtility.FromJson<CloudSnapshot>(json);

            snapshot?.EnsureLists();
            return snapshot;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[CloudSave] Cloud JSON okunamadi; local save korunuyor: " +
                exception.Message
            );

            return null;
        }
    }

    private static CloudSnapshot CloneSnapshot(CloudSnapshot source)
    {
        if (source == null)
            return new CloudSnapshot();

        return Deserialize(Serialize(source)) ?? new CloudSnapshot();
    }

    private static void AddInt(CloudSnapshot snapshot, string key, int value)
    {
        snapshot.EnsureLists();
        snapshot.ints.Add(new IntEntry { key = key, value = value });
    }

    private static void AddFloat(CloudSnapshot snapshot, string key, float value)
    {
        snapshot.EnsureLists();
        snapshot.floats.Add(new FloatEntry { key = key, value = value });
    }

    private static void AddString(CloudSnapshot snapshot, string key, string value)
    {
        snapshot.EnsureLists();
        snapshot.strings.Add(
            new StringEntry
            {
                key = key,
                value = value ?? string.Empty
            }
        );
    }

    private static int GetInt(CloudSnapshot snapshot, string key, int fallback)
    {
        if (snapshot == null || snapshot.ints == null)
            return fallback;

        for (int i = 0; i < snapshot.ints.Count; i++)
        {
            IntEntry entry = snapshot.ints[i];

            if (entry != null && entry.key == key)
                return entry.value;
        }

        return fallback;
    }

    private static float GetFloat(
        CloudSnapshot snapshot,
        string key,
        float fallback)
    {
        if (snapshot == null || snapshot.floats == null)
            return fallback;

        for (int i = 0; i < snapshot.floats.Count; i++)
        {
            FloatEntry entry = snapshot.floats[i];

            if (entry != null && entry.key == key)
                return entry.value;
        }

        return fallback;
    }

    private static string GetString(
        CloudSnapshot snapshot,
        string key,
        string fallback)
    {
        if (snapshot == null || snapshot.strings == null)
            return fallback;

        for (int i = 0; i < snapshot.strings.Count; i++)
        {
            StringEntry entry = snapshot.strings[i];

            if (entry != null && entry.key == key)
                return entry.value ?? fallback;
        }

        return fallback;
    }
}
