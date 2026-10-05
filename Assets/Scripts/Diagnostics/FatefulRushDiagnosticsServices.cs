#if FATEFULRUSH_DIAGNOSTICS
using UnityEngine;

public static class FatefulRushDiagnosticsServices
{
    public static bool AdsEnabled { get; internal set; }
    public static bool PlayGamesEnabled { get; internal set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { AdsEnabled = false; PlayGamesEnabled = false; }
}
#endif
