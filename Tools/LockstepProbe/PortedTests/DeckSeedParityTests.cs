using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class DeckSeedParityTests {
        private const string Source = "game/util/deck_seed_deriver.rs";
        [PortedTest(Source)] public static void derives_the_java_seeds() {
            Assert.Equal(-6626536486652210521L, DeckSeedDeriver.ForLeftDeck(0x0123456789abcdefL)); Assert.Equal(227691149224014156L, DeckSeedDeriver.ForRightDeck(0x0123456789abcdefL));
        }
    }
}
