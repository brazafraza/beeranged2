using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public RectTransform background;
    public RectTransform handle;
    public float handleRange = 60f;

    private Vector2 _input;

    public Vector2 Read() { return _input; }

    public void OnPointerDown(PointerEventData e) { OnDrag(e); }
    public void OnPointerUp(PointerEventData e)
    {
        _input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

    public void OnDrag(PointerEventData e)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out pos);
        Vector2 clamped = Vector2.ClampMagnitude(pos, handleRange);
        handle.anchoredPosition = clamped;
        _input = clamped / handleRange;
    }
}
