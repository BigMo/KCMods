using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Zat.Shared.UI.Utilities
{
    public class DraggableRect : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public bool Enabled { get; set; }
        public bool IsDragging { get; private set; }

        private Vector2 mousePos;
        public RectTransform movable;

        public UnityEvent onMoved = new UnityEvent();


        public void OnBeginDrag(PointerEventData eventData)
        {
            mousePos = eventData.position;
            IsDragging = movable;
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            var diff = eventData.position - mousePos;
            mousePos = eventData.position;

            var oldPos = movable.position;
            var newPos = movable.position + new Vector3(diff.x, diff.y, 0);
            movable.position = newPos;
            if (!IsInScreen()) movable.position = oldPos;
            else onMoved?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;
        }

        private bool IsInScreen()
        {
            var corners = new Vector3[4];
            movable.GetWorldCorners(corners);
            var screen = new Rect(0, 0, Screen.width, Screen.height);
            foreach (var corner in corners)
                if (!screen.Contains(corner))
                    return false;
            return true;
        }
    }
}
