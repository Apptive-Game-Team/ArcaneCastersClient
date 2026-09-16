using Coach;
using GameScene.Card;
using Global.Coach;
using UnityEngine;

namespace GameScene.Coach
{
    /// <summary>
    /// 한동안 카드를 한 장도 쓰지 않았다. 카드를 쓰면 그 프레임에 이 규칙이 거짓이 되고
    /// 스케줄러가 타이머를 초기화하므로, dwell 타이머가 곧 마지막 카드 이후 경과 시간이 된다.
    /// </summary>
    public class MagicUnusedRule : GameCoachRule, ICoachRuleLifecycle
    {
        private bool cardUsedSinceLastCheck;

        public override CoachRuleId Id => CoachRuleId.MagicUnused;

        public override string MessageKey => "coach.magicUnused";

        public override int Priority => 4;

        public override float DwellSeconds => 25f;

        public override int MaxShowsPerSession => 2;

        public void Initialize()
        {
            CardInputSender.OnCardUsed += OnCardUsed;
        }

        public void Dispose()
        {
            CardInputSender.OnCardUsed -= OnCardUsed;
        }

        public override bool IsActive()
        {
            if (cardUsedSinceLastCheck)
            {
                cardUsedSinceLastCheck = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 카드 한 장이 곧 마법 하나라서 손패 카드가 그대로 시전 대상이다. 손패는 매 턴
        /// 새로 만들어지므로 캐시하지 않고 그때그때 찾는다.
        /// </summary>
        public override Transform[] ResolveTargets()
        {
            CardUI[] cards = UnityEngine.Object.FindObjectsOfType<CardUI>();
            if (cards.Length == 0)
            {
                return null;
            }

            var targets = new Transform[cards.Length];
            for (int index = 0; index < cards.Length; index++)
            {
                targets[index] = cards[index].transform;
            }

            return targets;
        }

        private void OnCardUsed()
        {
            cardUsedSinceLastCheck = true;
        }
    }
}
