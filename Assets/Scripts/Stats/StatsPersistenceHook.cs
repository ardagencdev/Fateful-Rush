using UnityEngine;

/// <summary>
/// Flushes deferred gameplay statistics at safe lifecycle points.
/// No scene setup is required.
/// </summary>
public sealed class StatsPersistenceHook : MonoBehaviour
{
    private static StatsPersistenceHook instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject hookObject = new GameObject("Stats Persistence Hook");
        instance = hookObject.AddComponent<StatsPersistenceHook>();
        DontDestroyOnLoad(hookObject);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            StatsManager.SaveIfDirty();
    }

    /*
     * IMPORTANT:
     * Do not save on OnApplicationFocus(false).
     *
     * Android system overlays such as heads-up notifications can temporarily
     * remove window focus while the game is still visible and running.
     * StatsManager.SaveIfDirty() eventually calls PlayerPrefs.Save(), which is
     * a synchronous disk flush and can create a visible gameplay hitch.
     *
     * Real app backgrounding is still covered by OnApplicationPause(true),
     * while normal run completion already saves stats through GameStateManager.
     */

    private void OnApplicationQuit()
    {
        StatsManager.SaveIfDirty();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}