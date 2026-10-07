using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
namespace Ashlight {
    // Defense timing is measured at touch-down, rather than when the finger is released.
    public sealed class PressAction : MonoBehaviour, IPointerDownHandler {
        public UnityAction Action;
        public void OnPointerDown(PointerEventData data) {
            if (data.button != PointerEventData.InputButton.Left) return;
            var button = GetComponent<Button>();
            if (button != null && button.IsActive() && button.IsInteractable()) Action?.Invoke();
        }
    }
}
