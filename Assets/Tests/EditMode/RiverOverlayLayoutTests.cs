using GameScene.Dto;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class RiverOverlayLayoutTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void WaterCoversXSevenToElevenAndZeroToTen()
        {
            Assert.AreEqual(256f, RiverOverlayLayout.PixelsPerUnit);
            Assert.AreEqual(4f, RiverOverlayLayout.WaterWidth, Tolerance);
            Assert.AreEqual(10f, RiverOverlayLayout.WaterDepth, Tolerance);

            GroundRectangle water = RiverOverlayLayout.WaterRectangle;
            Assert.AreEqual(7f, water.MinX, Tolerance);
            Assert.AreEqual(0f, water.MinZ, Tolerance);
            Assert.AreEqual(11f, water.MaxX, Tolerance);
            Assert.AreEqual(10f, water.MaxZ, Tolerance);
        }

        [Test]
        public void WaterContainsBothRiverColumnsWithRoomForTheBanks()
        {
            GroundRectangle water = RiverOverlayLayout.WaterRectangle;
            // 서버의 강은 열 8 과 9, 즉 x 8 부터 10 이고, 둑은 그 밖으로 0.25 까지 나온다.
            Assert.LessOrEqual(water.MinX, 8f - 0.25f);
            Assert.GreaterOrEqual(water.MaxX, 10f + 0.25f);
        }

        [Test]
        public void BridgesAreCenteredOnColumnsEightAndNineAtRowsOneToThreeAndSixToEight()
        {
            Assert.AreEqual(2, RiverOverlayLayout.BridgeCount);
            Assert.AreEqual(4f, RiverOverlayLayout.BridgeSize, Tolerance);

            GroundRectangle first = RiverOverlayLayout.BridgeRectangle(0);
            Assert.AreEqual(9f, first.CenterX, Tolerance);
            Assert.AreEqual(2.5f, first.CenterZ, Tolerance);
            Assert.AreEqual(0.5f, first.MinZ, Tolerance);
            Assert.AreEqual(4.5f, first.MaxZ, Tolerance);

            GroundRectangle second = RiverOverlayLayout.BridgeRectangle(1);
            Assert.AreEqual(9f, second.CenterX, Tolerance);
            Assert.AreEqual(7.5f, second.CenterZ, Tolerance);
            Assert.AreEqual(5.5f, second.MinZ, Tolerance);
            Assert.AreEqual(9.5f, second.MaxZ, Tolerance);
        }

        [Test]
        public void BridgeXPositionIsMirroredAboutNine()
        {
            for (int i = 0; i < RiverOverlayLayout.BridgeCount; i++)
            {
                GroundRectangle bridge = RiverOverlayLayout.BridgeRectangle(i);
                Assert.AreEqual(2f * 9f, bridge.MinX + bridge.MaxX, Tolerance);
                Assert.AreEqual(7f, bridge.MinX, Tolerance);
                Assert.AreEqual(11f, bridge.MaxX, Tolerance);
            }
        }

        [Test]
        public void BridgeIndexOutOfRangeThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RiverOverlayLayout.BridgeCenterZ(2));
        }

        [Test]
        public void LiftKeepsPictureAboveTheGroundMesh()
        {
            Assert.AreEqual(0.02f, RiverOverlayLayout.GroundLift, Tolerance);
        }
    }
}
