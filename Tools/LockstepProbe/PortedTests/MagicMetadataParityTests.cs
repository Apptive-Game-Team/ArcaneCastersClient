using System.Linq;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class MagicMetadataParityTests {
        private const string Source = "game/service/bot/magic_metadata_service.rs";
        [PortedTest(Source)] public static void looks_a_magic_up_once() {
            var tags = new FakeBotTags { Tags = new[] { "tag_7" } }; var service = new MagicMetadataService(tags);
            Assert.True(service.GetMagicTags(7).Contains("tag_7")); service.GetMagicTags(7); Assert.Equal(1, tags.Lookups);
        }
        [PortedTest(Source)] public static void invalidating_clears_its_own_cache_and_the_catalogs() {
            var tags = new FakeBotTags(); var service = new MagicMetadataService(tags); service.GetMagicTags(7);
            service.InvalidateCache(); service.GetMagicTags(7); Assert.Equal(2, tags.Lookups); Assert.Equal(1, tags.Invalidations);
        }
    }
}
