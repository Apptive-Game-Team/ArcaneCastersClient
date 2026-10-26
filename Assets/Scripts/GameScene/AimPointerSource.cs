using Global;
using UnityEngine;

namespace GameScene
{
    /// <summary>
    /// 필드 선택 모드의 조준 위치를 화면 좌표로 돌려준다. 마우스가 움직이면 마우스를,
    /// gamepad 스틱을 쓰거나 gamepad 로 카드를 고르면 스틱으로 움직이는 가상 cursor 를 따른다.
    /// </summary>
    public static class AimPointerSource
    {
        /// <summary>스틱을 끝까지 밀었을 때 cursor 가 1초에 움직이는 거리. 화면 높이에 대한 비율이라 해상도와 무관하다.</summary>
        private const float CursorSpeedPerScreenHeight = 0.9f;

        /// <summary>마우스가 이만큼(픽셀) 넘게 움직여야 입력 방식을 마우스로 되돌린다.</summary>
        private const float MouseMoveThreshold = 1f;

        private static bool usesGamepad;
        private static bool seedPending;
        private static Vector2 cursor;
        private static Vector2 lastMouse;
        private static int lastFrame = -10;
        private static Vector3 cachedResult;

        /// <summary>지금 조준이 gamepad cursor 를 따르는가. 확정 때 UI hover 검사를 건너뛸지 가르는 데 쓴다.</summary>
        public static bool IsGamepadAiming => usesGamepad;

        /// <summary>
        /// gamepad 로 카드를 골랐을 때 부른다. 다음 <see cref="GetScreenPosition"/> 이 cursor 를 시전자 위치로 옮긴다.
        /// </summary>
        public static void BeginGamepadAim()
        {
            usesGamepad = true;
            seedPending = true;
        }

        /// <summary>
        /// 이번 프레임의 조준 화면 좌표. 한 프레임에 여러 번 불러도 cursor 는 한 번만 움직인다.
        /// <paramref name="casterWorldPosition"/> 은 cursor 를 처음 놓을 시전자의 월드 위치다.
        /// </summary>
        public static Vector3 GetScreenPosition(Vector3? casterWorldPosition = null)
        {
            if (lastFrame == Time.frameCount)
            {
                return cachedResult;
            }

            // 필드 선택 모드 밖에서는 불리지 않으므로, 오래 쉬었다 돌아오면 마우스 기준점만 다시 잡는다.
            bool resumed = lastFrame < Time.frameCount - 1;
            Vector2 mouse = Input.mousePosition;
            if (resumed)
            {
                lastMouse = mouse;
            }

            lastFrame = Time.frameCount;

            if (seedPending)
            {
                seedPending = false;
                cursor = SeedPosition(casterWorldPosition);
            }
            else if (!resumed && (mouse - lastMouse).sqrMagnitude > MouseMoveThreshold * MouseMoveThreshold)
            {
                usesGamepad = false;
            }

            lastMouse = mouse;

            Vector2 stick = GamepadInput.RightStick;
            if (stick == Vector2.zero)
            {
                stick = GamepadInput.LeftStick;
            }

            if (stick != Vector2.zero)
            {
                if (!usesGamepad)
                {
                    // 마우스 자리에서 이어서 움직이게 한다.
                    cursor = mouse;
                    usesGamepad = true;
                }

                cursor += stick * (CursorSpeedPerScreenHeight * Screen.height * Time.unscaledDeltaTime);
                cursor.x = Mathf.Clamp(cursor.x, 0f, Screen.width);
                cursor.y = Mathf.Clamp(cursor.y, 0f, Screen.height);
            }

            cachedResult = usesGamepad ? (Vector3)cursor : (Vector3)mouse;
            return cachedResult;
        }

        private static Vector2 SeedPosition(Vector3? casterWorldPosition)
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Camera camera = Camera.main;
            if (!casterWorldPosition.HasValue || camera == null)
            {
                return center;
            }

            Vector3 screen = camera.WorldToScreenPoint(casterWorldPosition.Value);
            if (screen.z <= 0f)
            {
                return center;
            }

            return new Vector2(Mathf.Clamp(screen.x, 0f, Screen.width), Mathf.Clamp(screen.y, 0f, Screen.height));
        }
    }
}
