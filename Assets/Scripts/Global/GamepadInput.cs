using UnityEngine;
using UnityEngine.InputSystem;

namespace Global
{
    /// <summary>
    /// gamepad 입력을 읽는 단일 창구. Active Input Handling 이 Both 라 마우스·키보드는 기존
    /// <c>UnityEngine.Input</c> 을 그대로 쓰고, gamepad 만 Input System 으로 읽는다.
    /// Xbox 계열과 PlayStation·Switch Pro 는 모두 <see cref="Gamepad"/> 로 올라오므로
    /// 버튼 이름은 Xbox 배치(South = A, East = B, North = Y)를 기준으로 한다.
    /// </summary>
    public static class GamepadInput
    {
        /// <summary>스틱이 이 크기 아래면 0 으로 본다. drift 가 cursor 를 밀지 않게 하는 값.</summary>
        public const float StickDeadZone = 0.2f;

        public static bool IsConnected => Gamepad.current != null;

        /// <summary>왼쪽 스틱. 연결되지 않았으면 zero.</summary>
        public static Vector2 LeftStick => ReadStick(Gamepad.current?.leftStick.ReadValue() ?? Vector2.zero);

        /// <summary>오른쪽 스틱. 연결되지 않았으면 zero.</summary>
        public static Vector2 RightStick => ReadStick(Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero);

        /// <summary>A 버튼(South). 확정·시전.</summary>
        public static bool SubmitDown => Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        /// <summary>B 버튼(East). 취소·뒤로.</summary>
        public static bool CancelDown => Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

        /// <summary>Y 버튼(North).</summary>
        public static bool NorthDown => Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;

        /// <summary>X 버튼(West).</summary>
        public static bool WestDown => Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;

        public static bool LeftShoulderDown => Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame;

        public static bool RightShoulderDown => Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;

        public static bool StartDown => Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

        public static bool DpadLeftDown => Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame;

        public static bool DpadRightDown => Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame;

        public static bool DpadUpDown => Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame;

        public static bool DpadDownDown => Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame;

        /// <summary>
        /// 이번 프레임에 gamepad 의 어느 버튼이든 눌렸는가. 마우스에서 gamepad 로 입력 방식이 바뀐 시점을 잡는 데 쓴다.
        /// </summary>
        public static bool AnyButtonDown
        {
            get
            {
                Gamepad pad = Gamepad.current;
                if (pad == null) return false;

                foreach (var control in pad.allControls)
                {
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl button && !button.synthetic && button.wasPressedThisFrame)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static Vector2 ReadStick(Vector2 raw)
        {
            return raw.magnitude < StickDeadZone ? Vector2.zero : raw;
        }
    }
}
