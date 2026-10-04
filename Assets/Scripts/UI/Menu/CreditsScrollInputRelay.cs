using UnityEngine;
using UnityEngine.EventSystems;

// Lives beside ScrollRect/Scrollbar. Their native handlers still move the content.
// The controller may live elsewhere in the scene, so it cannot receive their events
// through parent bubbling reliably.
[DisallowMultipleComponent]
public sealed class CreditsScrollInputRelay : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IBeginDragHandler,
    IDragHandler, IEndDragHandler, IScrollHandler
{
    private FatefulRushCreditsController owner;

    public void Initialize(FatefulRushCreditsController controller) => owner = controller;
    public void OnPointerDown(PointerEventData data) { if (CanNotify) owner.OnPointerDown(data); }
    public void OnPointerUp(PointerEventData data) { if (CanNotify) owner.OnPointerUp(data); }
    public void OnBeginDrag(PointerEventData data) { if (CanNotify) owner.OnBeginDrag(data); }
    public void OnDrag(PointerEventData data) { }
    public void OnEndDrag(PointerEventData data) { if (CanNotify) owner.OnEndDrag(data); }
    public void OnScroll(PointerEventData data) { if (CanNotify) owner.OnScroll(data); }
    private bool CanNotify => owner != null && owner.isActiveAndEnabled && owner.gameObject != gameObject;
}
