using System.Collections.Generic;
using GameScene.ServedObjectComponent;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Selectable = GameScene.ServedObjectComponent.Selectable;

namespace GameScene
{
    public static class PointerInputUtility
    {
        private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>();

        // PointerEventData를 호출마다 새로 만들면 그대로 GC 부담이 된다. EventSystem이 바뀔 때만 다시 만든다.
        private static EventSystem cachedEventSystem;
        private static PointerEventData cachedPointerEventData;

        private static int pointerCaptureCount;
        private static int lastCaptureReleaseFrame = -1;

        /// <summary>
        /// UI 하나가 누름 하나를 통째로 가져갔는지. 눌러서 끌고 떼는 UI 가 필드 위에서 손을
        /// 떼도 그 누름이 필드 입력이 되지 않게 막는다.
        /// </summary>
        /// <remarks>
        /// 뗀 프레임까지 참을 유지한다. EventSystem 의 Update 와 필드 입력을 읽는 Update 중
        /// 어느 쪽이 먼저 도는지는 정해져 있지 않다. 떼는 순간 바로 거짓으로 바꾸면 EventSystem
        /// 이 먼저 돈 프레임에서만 필드가 그 누름을 자기 것으로 읽는다.
        /// </remarks>
        public static bool IsPointerCapturedByUi =>
            pointerCaptureCount > 0 || lastCaptureReleaseFrame == Time.frameCount;

        public static void BeginPointerCapture()
        {
            pointerCaptureCount++;
        }

        public static void EndPointerCapture()
        {
            if (pointerCaptureCount > 0)
            {
                pointerCaptureCount--;
            }

            if (pointerCaptureCount == 0)
            {
                lastCaptureReleaseFrame = Time.frameCount;
            }
        }

        /// <summary>
        /// 주어진 화면 좌표 아래에서 <typeparamref name="T"/> 를 찾는다. 뗀 자리가 무엇인지
        /// 알아내는 용도다. hover 로는 알 수 없다. 손가락에는 hover 가 없기 때문이다.
        /// </summary>
        public static T FindUnderPointer<T>(Vector2 screenPosition) where T : Component
        {
            RefreshRaycastResults(screenPosition);
            foreach (RaycastResult result in RaycastResults)
            {
                if (result.gameObject == null)
                {
                    continue;
                }

                T found = result.gameObject.GetComponentInParent<T>();
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static bool IsPointerOverUi()
        {
            RefreshRaycastResults(Input.mousePosition);
            foreach (RaycastResult result in RaycastResults)
            {
                if (result.module is GraphicRaycaster)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPointerOverUiOrSelectable()
        {
            RefreshRaycastResults(Input.mousePosition);
            foreach (RaycastResult result in RaycastResults)
            {
                if (result.module is GraphicRaycaster ||
                    result.gameObject != null && result.gameObject.GetComponentInParent<Selectable>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RefreshRaycastResults(Vector2 screenPosition)
        {
            RaycastResults.Clear();
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            if (cachedPointerEventData == null || cachedEventSystem != eventSystem)
            {
                cachedEventSystem = eventSystem;
                cachedPointerEventData = new PointerEventData(eventSystem);
            }

            cachedPointerEventData.position = screenPosition;
            eventSystem.RaycastAll(cachedPointerEventData, RaycastResults);
        }
    }
}
