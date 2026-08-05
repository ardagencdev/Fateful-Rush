using UnityEngine;
using UnityEngine.InputSystem;

public class MissionDebugReset : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    [SerializeField, Min(1)]
    private int missionCount = 40;

    [Header("Debug Keys")]
    [SerializeField]
    private Key skinAccessToggleKey = Key.K;

    private void Start()
    {
        Debug.Log(
            "<color=cyan><b>[DEBUG CONTROLS]</b></color>\n" +
            "<color=yellow>R</color> - Bütün ilerlemeyi sıfırla\n" +
            "<color=lime>U</color> - Bütün levelları aç\n" +
            "<color=magenta>K</color> - Bütün skinleri aç / kapat"
        );
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // R: Bütün ilerlemeyi sıfırla
        if (keyboard.rKey.wasPressedThisFrame)
        {
            ResetMissionProgress();
            return;
        }

        // U: Bütün levelları aç
        if (keyboard.uKey.wasPressedThisFrame)
        {
            UnlockAllMissions();
            return;
        }

        // K (Inspector'dan değiştirilebilir): Bütün skinleri aç / tekrar kilitle
        if (skinAccessToggleKey != Key.None &&
            keyboard[skinAccessToggleKey].wasPressedThisFrame)
        {
            ToggleAllSkinsAccess();
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

        PlayerSkinCatalog.SetDebugAllSkinsUnlocked(false);
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

    private void ToggleAllSkinsAccess()
    {
        bool unlocked =
            PlayerSkinCatalog.ToggleDebugAllSkinsUnlocked();

        Debug.Log(
            unlocked
                ? "<color=cyan>[DEBUG]</color> All player skins were unlocked."
                : "<color=orange>[DEBUG]</color> Skin unlocks returned to normal progression."
        );
    }

#endif
}