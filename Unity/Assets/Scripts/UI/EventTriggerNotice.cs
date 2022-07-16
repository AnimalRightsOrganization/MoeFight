using UnityEngine;
using UnityEngine.EventSystems;

public delegate void OnDragDelegate();
public delegate void OnEndDragDelegate();
public delegate void OnPointerClickDelegate();
public class EventTriggerNotice : MonoBehaviour
    , IPointerClickHandler
    , IDragHandler
    , IEndDragHandler
{
    public OnDragDelegate onDrag;
    public OnEndDragDelegate onEndDrag;
    public OnPointerClickDelegate onPointClick;

    public void OnDrag(PointerEventData eventData)
    {
        onDrag?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        onEndDrag?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onPointClick?.Invoke();
    }
}