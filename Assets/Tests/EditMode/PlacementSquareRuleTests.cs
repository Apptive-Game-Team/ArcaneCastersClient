using GameScene.Dto;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class PlacementSquareRuleTests
    {
        // 물 칸 하나: 중심 (8.5, 0.5), 한 변 1 이다. 소환 반지름 0.3 이면 경계 거리는 0.3 * 0.6 = 0.18 이다.
        private const float CenterX = 8.5f;
        private const float CenterZ = 0.5f;
        private const float Half = PlacementSquareRule.CellHalfSize;
        private const float SummonRadius = 0.3f;

        private static bool Blocks(float x, float z, float radius = SummonRadius)
        {
            return PlacementSquareRule.Blocks(x, z, CenterX, CenterZ, Half, radius);
        }

        [Test]
        public void ConstantsMatchServer()
        {
            Assert.AreEqual(0.6f, PlacementSquareRule.OverlapRatio);
            Assert.AreEqual(0.5f, PlacementSquareRule.CellHalfSize);
            Assert.AreEqual("RiverWater", PlacementSquareRule.RiverWaterType);
            Assert.AreEqual("RiverBridge", PlacementSquareRule.RiverBridgeType);
        }

        [Test]
        public void AimInsideCellIsBlockedAtDistanceZero()
        {
            Assert.AreEqual(0f, PlacementSquareRule.DistanceToSquare(CenterX, CenterZ, CenterX, CenterZ, Half));
            Assert.IsTrue(Blocks(CenterX, CenterZ));
            Assert.IsTrue(Blocks(CenterX + 0.4f, CenterZ - 0.4f));
        }

        [Test]
        public void AimOnEdgeIsBlocked()
        {
            Assert.AreEqual(0f, PlacementSquareRule.DistanceToSquare(9.0f, CenterZ, CenterX, CenterZ, Half));
            Assert.IsTrue(Blocks(9.0f, CenterZ));
            Assert.IsTrue(Blocks(CenterX, 1.0f));
        }

        [Test]
        public void AimJustOutsideEdgeIsBlockedBelowThresholdAndFreeAbove()
        {
            // 오른쪽 변은 x = 9.0 이다. 0.17 떨어지면 0.18 보다 가까워 막히고, 0.19 떨어지면 풀린다.
            Assert.IsTrue(Blocks(9.17f, CenterZ));
            Assert.IsFalse(Blocks(9.19f, CenterZ));
            // 위쪽 변은 z = 1.0 이다.
            Assert.IsTrue(Blocks(CenterX, 1.17f));
            Assert.IsFalse(Blocks(CenterX, 1.19f));
        }

        [Test]
        public void ThresholdGrowsWithSummonRadius()
        {
            // 소환 반지름 0.6 이면 경계 거리는 0.36 이다. 0.3 떨어진 점은 반지름 0.3 이면 풀리고 0.6 이면 막힌다.
            Assert.IsFalse(Blocks(9.3f, CenterZ, 0.3f));
            Assert.IsTrue(Blocks(9.3f, CenterZ, 0.6f));
        }

        [Test]
        public void CornerUsesDiagonalDistance()
        {
            // 모서리 (9.0, 1.0) 에서 대각선으로 d 만큼 떨어진 점은 거리가 d * sqrt(2) 다.
            // 0.12 → 0.1697 로 막히고, 0.13 → 0.1838 로 풀린다. 변에 수직인 거리 기준이었다면 둘 다 막혔을 것이다.
            Assert.IsTrue(Blocks(9.12f, 1.12f));
            Assert.IsFalse(Blocks(9.13f, 1.13f));
            Assert.AreEqual(0.1697056f, PlacementSquareRule.DistanceToSquare(9.12f, 1.12f, CenterX, CenterZ, Half), 0.0001f);
        }
    }
}
