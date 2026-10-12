using Data;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// 서버가 보내는 <c>mapType</c> 문자열을 map 종류로, map 종류를 battle theme 선택으로 바꾸는
    /// 순수 함수를 고정한다. 모르는 값이나 빈 값은 던지지 않고 "서버가 정하지 않음"으로 돌아와야
    /// 옛 서버와 새 client 가 같이 동작한다.
    /// </summary>
    public class MapKindTests
    {
        [TestCase("GRASSLAND", MapKind.Grassland)]
        [TestCase("RIVER", MapKind.River)]
        [TestCase("FORTRESS", MapKind.Fortress)]
        [TestCase("GATE", MapKind.Gate)]
        [TestCase("FOREST", MapKind.Forest)]
        public void ParsesEveryKnownMapType(string mapType, MapKind expected)
        {
            Assert.AreEqual(expected, MapKinds.Parse(mapType));
        }

        [TestCase("forest", MapKind.Forest)]
        [TestCase("  River ", MapKind.River)]
        public void ParseIgnoresCaseAndSurroundingSpaces(string mapType, MapKind expected)
        {
            Assert.AreEqual(expected, MapKinds.Parse(mapType));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ParseTreatsMissingValueAsUnspecified(string mapType)
        {
            Assert.AreEqual(MapKind.Unspecified, MapKinds.Parse(mapType));
        }

        [TestCase("DESERT")]
        [TestCase("GRASS LAND")]
        [TestCase("0")]
        public void ParseTreatsUnknownNameAsUnknownWithoutThrowing(string mapType)
        {
            Assert.AreEqual(MapKind.Unknown, MapKinds.Parse(mapType));
        }

        [TestCase(MapKind.Grassland, MapThemeChoice.SceneDefault)]
        [TestCase(MapKind.River, MapThemeChoice.SceneDefault)]
        [TestCase(MapKind.Forest, MapThemeChoice.Forest)]
        [TestCase(MapKind.Fortress, MapThemeChoice.Fortress)]
        [TestCase(MapKind.Gate, MapThemeChoice.Gate)]
        public void ThemeForMapsEveryKnownKind(MapKind kind, MapThemeChoice expected)
        {
            Assert.AreEqual(expected, MapKinds.ThemeFor(kind));
        }

        [TestCase(MapKind.Unspecified)]
        [TestCase(MapKind.Unknown)]
        public void ThemeForHandsTheDecisionBackWhenServerSaidNothingUsable(MapKind kind)
        {
            Assert.AreEqual(MapThemeChoice.KeepFallback, MapKinds.ThemeFor(kind));
        }

        [Test]
        public void ThemeForCoversEveryDeclaredKind()
        {
            foreach (MapKind kind in System.Enum.GetValues(typeof(MapKind)))
            {
                Assert.DoesNotThrow(() => MapKinds.ThemeFor(kind), kind.ToString());
            }
        }
    }
}
