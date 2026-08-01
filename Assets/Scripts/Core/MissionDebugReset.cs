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

        if (!Keyboard.current.rKey.wasPressedThisFrame)
            return;

        ResetMissionProgress();
    }

    private void ResetMissionProgress()
    {
        for (int i = 1; i <= missionCount; i++)
        {
            PlayerPrefs.DeleteKey(
                $"CompletedLevel_{i}"
            );

            PlayerPrefs.DeleteKey(
                $"BestTime_Level_{i}"
            );
        }

        PlayerPrefs.SetInt(
            "UnlockedLevel",
            1
        );

        PlayerPrefs.Save();

        Debug.Log(
            "<color=yellow>[DEBUG]</color> " +
            $"Progress for {missionCount} missions was reset."
        );
    }

#endif
}