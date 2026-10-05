using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Global
{
    /// <summary>
    /// 경기 밖 메뉴 scene 을 gamepad 로 조작한다. 왼쪽 스틱·D-pad 는 선택을 옮기고, A 는 제출, B 는 취소한다.
    /// 레거시 joystick 축은 WebGL 에서 읽히지 않아 StandaloneInputModule 에 맡기지 못하므로
    /// <see cref="GamepadInput"/> 으로 직접 폴링하고, 이동은 선택된 오브젝트에 MoveEvent 를 보내
    /// Selectable(내비게이션)·Slider·Scrollbar·Dropdown 이 각자 처리하게 한다.
    /// GameScene 의 경기 조작은 다른 곳에서 처리하므로 건드리지 않는다.
    /// </summary>
    public class GamepadUINavigator : MonoBehaviour
    {
        private const string MatchSceneName = "GameScene";
        private const float RepeatDelay = 0.4f;
        private const float RepeatInterval = 0.12f;

        private MoveDirection heldDirection = MoveDirection.None;
        private float nextRepeatAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            GameObject host = new GameObject(nameof(GamepadUINavigator));
            host.AddComponent<GamepadUINavigator>();
            DontDestroyOnLoad(host);
        }

        private void Update()
        {
            if (!GamepadInput.IsConnected) return;
            if (EventSystem.current == null) return;
            if (SceneManager.GetActiveScene().name == MatchSceneName) return;

            MoveDirection direction = ReadDirection();
            bool moveNow = ResolveRepeat(direction);
            bool submit = GamepadInput.SubmitDown;
            bool cancel = GamepadInput.CancelDown;

            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem.currentSelectedGameObject;

            // 입력 필드를 편집 중이면 글자 입력을 방해하지 않는다. B 로만 편집을 끝낸다.
            if (IsEditingText(selected))
            {
                if (cancel) DeactivateInput(selected);
                return;
            }

            if (!IsUsable(selected))
            {
                // 선택이 비었거나 scene 전환으로 사라졌다. 첫 입력은 선택만 잡고 소비한다.
                if (!moveNow && !submit && !cancel) return;

                Selectable first = FindFirst();
                if (first != null) first.Select();
                return;
            }

            if (moveNow)
            {
                AxisEventData axisData = new AxisEventData(eventSystem)
                {
                    moveDir = direction,
                    moveVector = ToVector(direction)
                };
                ExecuteEvents.Execute(selected, axisData, ExecuteEvents.moveHandler);
                ScrollSelectionIntoView(eventSystem.currentSelectedGameObject);
            }

            if (submit)
            {
                ExecuteEvents.Execute(selected, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
            }
            else if (cancel)
            {
                ExecuteEvents.Execute(selected, new BaseEventData(eventSystem), ExecuteEvents.cancelHandler);
            }
        }

        /// <summary>
        /// 선택이 ScrollRect 안의 칸으로 옮겨 갔으면 그 칸이 viewport 에 들어오도록 content 를 민다.
        /// Selectable 의 이동은 스크롤을 모르므로 이것이 없으면 덱·마법책 목록의 화면 밖 카드로 선택이 사라진다.
        /// </summary>
        private static void ScrollSelectionIntoView(GameObject selected)
        {
            if (selected == null) return;

            ScrollRect scrollRect = selected.GetComponentInParent<ScrollRect>();
            if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null) return;
            if (!selected.transform.IsChildOf(scrollRect.content)) return;

            RectTransform viewport = scrollRect.viewport;
            RectTransform content = scrollRect.content;
            Bounds item = RectTransformUtility.CalculateRelativeRectTransformBounds(content, (RectTransform)selected.transform);
            Bounds view = RectTransformUtility.CalculateRelativeRectTransformBounds(content, viewport);

            Vector2 shift = Vector2.zero;
            if (scrollRect.vertical)
            {
                if (item.max.y > view.max.y) shift.y = view.max.y - item.max.y;
                else if (item.min.y < view.min.y) shift.y = view.min.y - item.min.y;
            }

            if (scrollRect.horizontal)
            {
                if (item.min.x < view.min.x) shift.x = view.min.x - item.min.x;
                else if (item.max.x > view.max.x) shift.x = view.max.x - item.max.x;
            }

            if (shift == Vector2.zero) return;

            content.anchoredPosition += shift;
            scrollRect.velocity = Vector2.zero;
        }

        /// <summary>왼쪽 스틱이 우선이고, 없으면 D-pad. 두 축 중 큰 쪽 하나만 방향으로 삼는다.</summary>
        private static MoveDirection ReadDirection()
        {
            Vector2 value = GamepadInput.LeftStick;
            if (value == Vector2.zero) value = Gamepad.current.dpad.ReadValue();
            if (value == Vector2.zero) return MoveDirection.None;

            if (Mathf.Abs(value.x) > Mathf.Abs(value.y))
            {
                return value.x > 0f ? MoveDirection.Right : MoveDirection.Left;
            }

            return value.y > 0f ? MoveDirection.Up : MoveDirection.Down;
        }

        /// <summary>눌린 첫 프레임에 true, 계속 누르고 있으면 RepeatDelay 뒤 RepeatInterval 마다 true.</summary>
        private bool ResolveRepeat(MoveDirection direction)
        {
            if (direction == MoveDirection.None)
            {
                heldDirection = MoveDirection.None;
                return false;
            }

            if (direction != heldDirection)
            {
                heldDirection = direction;
                nextRepeatAt = Time.unscaledTime + RepeatDelay;
                return true;
            }

            if (Time.unscaledTime < nextRepeatAt) return false;

            nextRepeatAt = Time.unscaledTime + RepeatInterval;
            return true;
        }

        private static Vector2 ToVector(MoveDirection direction)
        {
            switch (direction)
            {
                case MoveDirection.Left: return Vector2.left;
                case MoveDirection.Right: return Vector2.right;
                case MoveDirection.Up: return Vector2.up;
                case MoveDirection.Down: return Vector2.down;
                default: return Vector2.zero;
            }
        }

        private static bool IsUsable(GameObject selected)
        {
            if (selected == null || !selected.activeInHierarchy) return false;

            Selectable selectable = selected.GetComponent<Selectable>();
            return selectable != null && selectable.IsInteractable();
        }

        private static bool IsEditingText(GameObject selected)
        {
            if (selected == null) return false;

            TMP_InputField tmpInputField = selected.GetComponent<TMP_InputField>();
            if (tmpInputField != null && tmpInputField.isFocused) return true;

            InputField inputField = selected.GetComponent<InputField>();
            return inputField != null && inputField.isFocused;
        }

        private static void DeactivateInput(GameObject selected)
        {
            TMP_InputField tmpInputField = selected.GetComponent<TMP_InputField>();
            if (tmpInputField != null) tmpInputField.DeactivateInputField();

            InputField inputField = selected.GetComponent<InputField>();
            if (inputField != null) inputField.DeactivateInputField();
        }

        /// <summary>화면 왼쪽 위에 가장 가까운 선택 가능 요소. 위쪽을 먼저, 같은 높이면 왼쪽을 고른다.</summary>
        private static Selectable FindFirst()
        {
            Selectable best = null;
            Vector3 bestPosition = Vector3.zero;

            foreach (Selectable candidate in Selectable.allSelectablesArray)
            {
                if (!IsFocusable(candidate)) continue;

                Vector3 position = candidate.transform.position;
                bool better = best == null
                    || position.y > bestPosition.y + 1f
                    || (Mathf.Abs(position.y - bestPosition.y) <= 1f && position.x < bestPosition.x);
                if (!better) continue;

                best = candidate;
                bestPosition = position;
            }

            return best;
        }

        private static bool IsFocusable(Selectable selectable)
        {
            if (selectable == null) return false;
            if (!selectable.IsActive() || !selectable.IsInteractable()) return false;

            return selectable.navigation.mode != Navigation.Mode.None;
        }
    }
}
