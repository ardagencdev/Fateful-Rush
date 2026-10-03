using UnityEngine;

/// <summary>Compatibility entry point. The panel owns briefing text; no polling or reflection.</summary>
[AddComponentMenu("")]
public sealed class MissionBriefingLiveSync : MonoBehaviour
{
    public static void RequestRefresh()
    {
        MissionBriefingPanelUI[] panels = UnityFindCompat.FindObjectsByType<MissionBriefingPanelUI>(FindObjectsInactive.Include);
        foreach (MissionBriefingPanelUI panel in panels)
            if (panel != null) panel.RefreshLocalizedText();
    }
}
