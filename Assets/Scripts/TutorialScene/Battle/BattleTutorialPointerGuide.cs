using System;
using Data.Magic;
using UnityEngine;

namespace TutorialScene
{
    /// <summary>
    /// 전투 튜토리얼에서 마법을 써야 하는 단계의 손가락. 매 프레임 지금 눌러야 할 것을 다시 고른다.
    /// 마나 바가 내려가 카드가 화면 밖이면 마나 바를, 카드가 보이면 카드를 누르고 놓을 자리로
    /// 옮겨 가 누르는 동작을, 카드를 고른 뒤에는 놓을 자리를 가리킨다.
    /// </summary>
    internal sealed class BattleTutorialPointerGuide
    {
        private enum Mode
        {
            None,
            OpenBar,
            DragCard,
            PickField
        }

        // InteractiveTutorialScene.unity 의 UI/Bars/ManaBar. TutorialBarController 는 이 버튼을
        // 밖에 내놓지 않아 이름으로 찾는다.
        private const string ManaBarChildName = "ManaBar";

        // 하수인을 세울 자리. LeftPlayer 가 x = 1 에 서고 필드는 x 0~18, z 0~10 이다.
        private static readonly Vector3 SummonPoint = new Vector3(5f, 0f, 5f);

        private TutorialPointer pointer;
        private Mode mode;
        private Transform pointedAt;

        /// <param name="magicName">써야 할 마법의 serverName. null 이면 아무 카드나 쓴다.</param>
        public void Update(string magicName, TutorialCardSender sender)
        {
            if (sender == null)
            {
                Clear();
                return;
            }

            TutorialCardUI card = FindCard(magicName);
            CombinedMagicData magic = sender.IsFieldSelectMode() ? sender.GetCurrentMagic() : card != null ? card.Magic : null;
            Func<Vector2?> fieldPoint = () => FieldScreenPoint(magic);

            if (sender.IsFieldSelectMode())
            {
                Show(Mode.PickField, null, () => TutorialPointer.PointAtScreen(fieldPoint));
                return;
            }

            if (card != null && TutorialPointer.IsFullyOnScreen(card.transform as RectTransform))
            {
                Show(Mode.DragCard, card.transform, () => TutorialPointer.Drag(card.transform, fieldPoint));
                return;
            }

            Transform manaBar = FindManaBar();
            Show(Mode.OpenBar, manaBar, () => manaBar != null ? TutorialPointer.PointAt(manaBar) : null);
        }

        public void Clear()
        {
            if (pointer != null)
            {
                pointer.Dismiss();
            }

            pointer = null;
            mode = Mode.None;
            pointedAt = null;
        }

        private void Show(Mode next, Transform target, Func<TutorialPointer> create)
        {
            if (next == mode && target == pointedAt && pointer != null)
            {
                return;
            }

            Clear();
            mode = next;
            pointedAt = target;
            pointer = create();
        }

        private static TutorialCardUI FindCard(string magicName)
        {
            TutorialCardUI first = null;
            foreach (TutorialCardUI card in UnityEngine.Object.FindObjectsOfType<TutorialCardUI>())
            {
                if (string.IsNullOrEmpty(magicName))
                {
                    if (first == null || card.transform.GetSiblingIndex() < first.transform.GetSiblingIndex())
                    {
                        first = card;
                    }

                    continue;
                }

                if (string.Equals(card.CardName, magicName, StringComparison.OrdinalIgnoreCase))
                {
                    return card;
                }
            }

            return first;
        }

        private static Transform FindManaBar()
        {
            TutorialBarController bar = UnityEngine.Object.FindObjectOfType<TutorialBarController>();
            return bar != null ? bar.transform.Find(ManaBarChildName) : null;
        }

        /// <summary>투사체는 상대를 겨누고, 그 밖의 마법은 내 진영 가운데에 세운다.</summary>
        private static Vector2? FieldScreenPoint(CombinedMagicData magic)
        {
            if (magic != null && GameScene.MagicIndicatorResolver.IsLaneAim(magic))
            {
                GameObject enemy = GameObject.Find("RightPlayer");
                if (enemy != null)
                {
                    return TutorialPointer.ScreenPointOfWorld(enemy.transform.position);
                }
            }

            return TutorialPointer.ScreenPointOfWorld(SummonPoint);
        }
    }
}
