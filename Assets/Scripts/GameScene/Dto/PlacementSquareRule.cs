namespace GameScene.Dto
{
    /// <summary>
    /// 땅에 놓는 소환이 정사각형 칸(강 맵의 물)에 막히는지 가르는 순수 규칙. 서버 CastPlacement 와 같은 값이다.
    /// 장면에 의존하지 않아서 EditMode test 가 직접 부른다.
    /// </summary>
    public static class PlacementSquareRule
    {
        /// <summary>조준점 반지름에 곱하는 비율. 서버 CastPlacement.PLACEMENT_OVERLAP_RATIO 와 같다.</summary>
        public const float OverlapRatio = 0.6f;

        /// <summary>서버가 보내는 물 칸의 type 문자열. 서버 enum 값 RiverWater 그대로다.</summary>
        public const string RiverWaterType = "RiverWater";

        /// <summary>서버가 보내는 다리 칸의 type 문자열. 다리는 소환을 막지 않는다.</summary>
        public const string RiverBridgeType = "RiverBridge";

        /// <summary>칸 한 변의 절반. 칸은 1 x 1 world unit 이고 중심이 object 위치다.</summary>
        public const float CellHalfSize = 0.5f;

        /// <summary>조준점에서 정사각형의 가장 가까운 점까지의 땅 위 거리. 정사각형 안이면 0 이다.</summary>
        public static float DistanceToSquare(float aimX, float aimZ, float centerX, float centerZ, float halfSize)
        {
            float outsideX = System.Math.Abs(aimX - centerX) - halfSize;
            float outsideZ = System.Math.Abs(aimZ - centerZ) - halfSize;
            if (outsideX < 0f)
            {
                outsideX = 0f;
            }

            if (outsideZ < 0f)
            {
                outsideZ = 0f;
            }

            return (float)System.Math.Sqrt(outsideX * outsideX + outsideZ * outsideZ);
        }

        /// <summary>그 거리가 summonRadius * 비율 보다 작으면 막힌다. 같으면 막히지 않는다.</summary>
        public static bool Blocks(float aimX, float aimZ, float centerX, float centerZ, float halfSize, float summonRadius)
        {
            return DistanceToSquare(aimX, aimZ, centerX, centerZ, halfSize) < summonRadius * OverlapRatio;
        }
    }
}
