using UnityEngine;

/// <summary>Legacy API adapter; GameResultUI now owns localized result text.</summary>
[AddComponentMenu("")]
public sealed class GameResultLocalizationGuard : MonoBehaviour
{
    public static void RequestRefresh()
    {
        GameResultUI[] panels = UnityFindCompat.FindObjectsByType<GameResultUI>(FindObjectsInactive.Include);
        foreach (GameResultUI panel in panels)
            if (panel != null) panel.RefreshLocalizedText();
    }
}
