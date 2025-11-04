using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("References")]
    public KeyboardManager keyboard;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform panelTransform;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (keyboard != null)
        {
            keyboard.InteractionStarted();
            Debug.Log("basladi");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        panelTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (keyboard != null)
        {
            keyboard.InteractionEnded();
            Debug.Log("bitti");
        }
    }
}