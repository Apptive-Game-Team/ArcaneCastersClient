using System.Collections.Generic;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class MagicCatalogParityTests {
        private const string Source = "game/domain/magic/parser/database_magic_parser.rs";
        private sealed class Catalog : IMagicCatalog {
            public int Loads;
            public IEnumerable<MagicInfo> GetAllMagic() { Loads++; return new[] { new MagicInfo(34, "leafair", Element.Nature), new MagicInfo(35, "no_bean") }; }
            public bool ExistUserMagic(long userId, long magicId) => userId == 1;
        }
        private static DatabaseMagicParser Parser(out Catalog catalog) {
            var registry = new MagicRegistry();
            registry.Register("leafair", new MagicImplementation((c, m, o, p) => { }));
            registry.Register("rowless", new MagicImplementation((c, m, o, p) => { }));
            catalog = new Catalog(); return new DatabaseMagicParser(catalog, registry);
        }
        [PortedTest(Source)] public static void binds_rows_that_have_a_bean_and_skips_the_rest() {
            var parser = Parser(out _); var magic = parser.GetMagic(34);
            Assert.Equal(34L, magic.Id); Assert.Equal("leafair", magic.Name); Assert.Equal(Element.Nature, magic.Element);
            Assert.True(parser.GetMagic(35) == null); Assert.True(parser.GetMagic(0) == null); Assert.Equal(1, parser.GetAllMagics().Length);
        }
        [PortedTest(Source)] public static void refuses_a_magic_the_user_does_not_own() {
            var parser = Parser(out _); Assert.True(parser.ParseMagic(1, 34) != null); Assert.True(parser.ParseMagic(2, 34) == null);
        }
        [PortedTest(Source)] public static void a_bot_bean_without_a_row_is_named_after_the_bean() {
            var parser = Parser(out _); Assert.Equal(34L, parser.ParseMagicForBotByName("leafair").Id);
            var rowless = parser.ParseMagicForBotByName("rowless"); Assert.Equal(0L, rowless.Id); Assert.Equal("rowless", rowless.Name);
            Assert.True(parser.ParseMagicForBotByName(" ") == null); Assert.True(parser.ParseMagicForBotByName("missing") == null);
        }
        [PortedTest(Source)] public static void invalidate_cache_reloads_on_the_next_lookup() {
            var parser = Parser(out var catalog); parser.InvalidateCache(); Assert.True(parser.GetMagic(34) != null); Assert.Equal(2, catalog.Loads);
        }
    }
}
