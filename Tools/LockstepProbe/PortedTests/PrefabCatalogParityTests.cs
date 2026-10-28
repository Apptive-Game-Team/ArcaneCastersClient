using ArcaneCasters.Simulation;
namespace ArcaneCasters.Parity {
    public static class PrefabCatalogParityTests {
        [PortedTest("game/domain/object/prefab/prefab_type.rs")] public static void has_every_java_entry() {
            Assert.Equal(136, PrefabCatalog.Names.Length); Assert.Equal("fire_slime_prefab", PrefabCatalog.BeanName("EmberSpirit")); Assert.Equal("Player", PrefabCatalog.ValueOf("Player"));
            Assert.Equal("FireSlime", PrefabCatalog.CanonicalName("EmberSpirit")); Assert.True(PrefabCatalog.ValueOf("player") == null && PrefabCatalog.ValueOf("unknown") == null);
        }
        [PortedTest("game/domain/object/prefab/implement/pve/tests.rs")] public static void registers_under_its_own_names() {
            Assert.Equal("pve_evil_ent_prefab", PrefabCatalog.BeanName("PveEvilEnt")); Assert.Equal("pve_evil_ent", PrefabCatalog.ParameterKey("PveEvilEnt"));
        }
    }
}
