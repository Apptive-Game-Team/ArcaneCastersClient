namespace GameScene.Dto
{
    /// <summary>
    /// 땅에 놓는 소환이 다른 몸과 겹쳐 막히는지 가르는 순수 규칙. 서버 CastPlacement 와 같은 값이다.
    /// 장면에 의존하지 않아서 EditMode test 가 직접 부른다.
    /// </summary>
    public static class PlacementOverlapRule
    {
        /// <summary>두 몸의 반지름 합에 곱하는 비율. 서버 CastPlacement.PLACEMENT_OVERLAP_RATIO 와 같다.</summary>
        public const float OverlapRatio = 0.6f;

        /// <summary>서버가 보내는 고정 바위 장애물의 type 문자열. 서버 enum 값 RockObstacle 그대로다.</summary>
        public const string RockObstacleType = "RockObstacle";

        /// <summary>고정 바위 장애물의 반지름. 서버 값과 같다.</summary>
        public const float RockObstacleRadius = 0.6f;

        /// <summary>조준점에서 몸 중심까지 거리가 (두 반지름 합) * 비율 보다 작으면 막힌다. 같으면 막히지 않는다.</summary>
        public static bool Overlaps(float distance, float otherRadius, float summonRadius)
        {
            return distance < (otherRadius + summonRadius) * OverlapRatio;
        }
    }
}
