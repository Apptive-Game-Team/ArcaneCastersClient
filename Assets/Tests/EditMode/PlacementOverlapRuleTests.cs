using GameScene.Dto;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class PlacementOverlapRuleTests
    {
        // 소환 반지름 0.3, 바위 반지름 0.6 이면 경계는 (0.6 + 0.3) * 0.6 = 0.54 다.
        private const float SummonRadius = 0.3f;

        [Test]
        public void OverlapRatioMatchesServer()
        {
            Assert.AreEqual(0.6f, PlacementOverlapRule.OverlapRatio);
            Assert.AreEqual("RockObstacle", PlacementOverlapRule.RockObstacleType);
            Assert.AreEqual(0.6f, PlacementOverlapRule.RockObstacleRadius);
        }

        [Test]
        public void AimInsideRockBoundaryIsBlocked()
        {
            Assert.IsTrue(PlacementOverlapRule.Overlaps(0f, PlacementOverlapRule.RockObstacleRadius, SummonRadius));
            Assert.IsTrue(PlacementOverlapRule.Overlaps(0.53f, PlacementOverlapRule.RockObstacleRadius, SummonRadius));
        }

        [Test]
        public void AimOnOrBeyondRockBoundaryIsFree()
        {
            Assert.IsFalse(PlacementOverlapRule.Overlaps(0.55f, PlacementOverlapRule.RockObstacleRadius, SummonRadius));
            Assert.IsFalse(PlacementOverlapRule.Overlaps(2f, PlacementOverlapRule.RockObstacleRadius, SummonRadius));
        }

        [Test]
        public void BoundaryGrowsWithSummonRadius()
        {
            // 소환 반지름 0.6 이면 경계는 (0.6 + 0.6) * 0.6 = 0.72 다.
            Assert.IsTrue(PlacementOverlapRule.Overlaps(0.7f, PlacementOverlapRule.RockObstacleRadius, 0.6f));
            Assert.IsFalse(PlacementOverlapRule.Overlaps(0.7f, PlacementOverlapRule.RockObstacleRadius, SummonRadius));
        }
    }
}
