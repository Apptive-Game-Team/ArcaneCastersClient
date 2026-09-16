using Coach;
using GameScene.Card;
using Global.Coach;
using UnityEngine;

namespace GameScene.Coach
{
    /// <summary>
    /// 시전이 3번 연속 실패했다. 손패 카드에 테두리를 둘러 다시 고르게 한다.
    /// </summary>
    public class MagicFailingRule : GameCoachRule, ICoachRuleLifecycle
    {
        private const int FailStreakThreshold = 3;

        private int failStreak;

        public override CoachRuleId Id => CoachRuleId.MagicFailing;

        public override string MessageKey => "coach.magicFailing";

        public override int Priority => 3;

        /// <summary>연속 실패를 세는 것 자체가 대기라서 dwell을 따로 두지 않는다.</summary>
        public override float DwellSeconds => 0f;

        public override int MaxShowsPerSession => 2;

        public void Initialize()
        {
            CardInputSender.OnMagicFailed += OnMagicFailed;
            CardInputSender.OnMagicSucceeded += OnMagicSucceeded;
        }

        public void Dispose()
        {
            CardInputSender.OnMagicFailed -= OnMagicFailed;
            CardInputSender.OnMagicSucceeded -= OnMagicSucceeded;
        }

        public override bool IsActive()
        {
            return failStreak >= FailStreakThreshold;
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

        private void OnMagicFailed()
        {
            failStreak++;
        }

        private void OnMagicSucceeded()
        {
            failStreak = 0;
        }
    }
}
