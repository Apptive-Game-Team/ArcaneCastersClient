using System.Collections.Generic;
using Global.Coach;
using UnityEngine;

namespace GameScene.Coach
{
    /// <summary>
    /// 옆에 붙은 director에 게임 씬 규칙을 넣어 준다. 씬은 이 컴포넌트를 갖는 것으로
    /// 훈수에 참여하므로 규칙 목록을 어디에도 하드코딩하지 않는다.
    /// </summary>
    [RequireComponent(typeof(CoachDirector))]
    public class GameCoachRuleProvider : MonoBehaviour, ICoachRuleProvider
    {
        public IEnumerable<ICoachRule> CreateRules()
        {
            return new ICoachRule[]
            {
                new FieldSelectIdleRule(),
                new MagicFailingRule(),
                new MagicUnusedRule(),
                new ManaBarUnopenedRule()
            };
        }
    }
}
