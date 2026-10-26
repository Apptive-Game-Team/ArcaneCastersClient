using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameScene
{
    public class BotThoughtLogPointerCapture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsPressed { get; private set; }
        public event Action<bool> PressChanged;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsPressed) return;
            IsPressed = true;
            PointerInputUtility.BeginPointerCapture();
            PressChanged?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        private void OnDisable() => Release();

        private void Release()
        {
            if (!IsPressed) return;
            IsPressed = false;
            PointerInputUtility.EndPointerCapture();
            PressChanged?.Invoke(false);
        }
    }
}
