using System;
using Global;

namespace GameScene.Card
{
    /// <summary>
    /// LB / RB 로 손패 슬롯을 이전 · 다음으로 옮기는 입력 도우미. 슬롯 번호는 <see cref="CardHotkey"/> 와 같다.
    /// </summary>
    public static class GamepadHandNavigation
    {
        /// <summary>이번 프레임에 LB 면 -1, RB 면 +1. 둘 다 아니면 false.</summary>
        public static bool TryGetPressedStep(out int step)
        {
            step = 0;
            if (GamepadInput.RightShoulderDown)
            {
                step = 1;
            }
            else if (GamepadInput.LeftShoulderDown)
            {
                step = -1;
            }

            return step != 0;
        }

        /// <summary>
        /// step 방향으로 다음에 고를 슬롯. 고른 카드가 없으면 RB 는 첫 카드, LB 는 마지막 카드를 고른다.
        /// 끝에서는 반대쪽 끝으로 넘어간다. 고를 슬롯이 없거나 지금 슬롯뿐이면 -1.
        /// </summary>
        public static int PickSlot(int step, int selectedSlot, Func<int, bool> isFilled)
        {
            int count = CardHotkey.SlotCount;
            int start = selectedSlot >= 0 ? selectedSlot : (step > 0 ? -1 : count);

            for (int offset = 1; offset <= count; offset++)
            {
                int slot = ((start + step * offset) % count + count) % count;
                if (slot == selectedSlot)
                {
                    return -1;
                }

                if (isFilled(slot))
                {
                    return slot;
                }
            }

            return -1;
        }
    }
}
