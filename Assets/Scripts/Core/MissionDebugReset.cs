using UnityEngine;
using UnityEngine.InputSystem;

public class MissionDebugReset : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    [SerializeField, Min(1)]
    private int missionCount = 40;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // R: Bütün ilerlemeyi sıfırla
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetMissionProgress();
            return;
        }

        // U: Bütün levelları aç
        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            UnlockAllMissions();
        }
    }

    private void ResetMissionProgress()
    {
        for (int i = 1; i <= missionCount; i++)
        {
            PlayerPrefs.DeleteKey($"CompletedLevel_{i}");
            PlayerPrefs.DeleteKey($"BestTime_Level_{i}");
        }

        PlayerPrefs.SetInt("UnlockedLevel", 1);

        PlayerSkinCatalog.ClearSavedSelection();

        PlayerPrefs.Save();

        Debug.Log(
            "<color=yellow>[DEBUG]</color> " +
            $"Progress for {missionCount} missions was reset."
        );
    }

    private void UnlockAllMissions()
    {
        PlayerPrefs.SetInt("UnlockedLevel", missionCount);
        PlayerPrefs.Save();

        Debug.Log(
            "<color=lime>[DEBUG]</color> " +
            $"All {missionCount} missions were unlocked."
        );
    }

#endif
}