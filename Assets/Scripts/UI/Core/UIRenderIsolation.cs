using UnityEngine;
using UnityEngine.UI;

/// <summary>Isolates moving UI content without reparenting or changing sorting.</summary>
public static class UIRenderIsolation
{
    public static void Ensure(Transform content)
    {
        if (content == null)
            return;
        Canvas parent = content.GetComponentInParent<Canvas>(true);
        if (parent == null)
            return;
        Canvas canvas = content.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = content.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = false;
        }
        if (content.GetComponent<GraphicRaycaster>() != null)
            return;
        GraphicRaycaster raycaster = content.gameObject.AddComponent<GraphicRaycaster>();
        GraphicRaycaster source = parent.GetComponent<GraphicRaycaster>();
        if (source != null)
        {
            raycaster.ignoreReversedGraphics = source.ignoreReversedGraphics;
            raycaster.blockingObjects = source.blockingObjects;
            raycaster.blockingMask = source.blockingMask;
        }
    }
}
