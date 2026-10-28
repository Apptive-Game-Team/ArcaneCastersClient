using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SimpleBossPrefabParityTests {
        private const string Source = "game/domain/object/prefab/implement/pve/tests.rs";
        private sealed class Rows : IMagicCatalog {
            public IEnumerable<MagicInfo> GetAllMagic() => new[] { new MagicInfo(1, "vine_toss", Element.Nature), new MagicInfo(2, "vine_colony", Element.Nature), new MagicInfo(3, "vine_fan", Element.Nature) };
            public bool ExistUserMagic(long user, long magic) => true;
        }
        private sealed class Stub : IBossMagicParser {
            public IBossMagic ParseBossMagic(string name) => name == "vine_toss" ? null : new BoundMagic(7, name, (c, m, o, p) => { });
            public IVineFan ParseVineFan(string name) => new VineFanMagic();
        }
        private static DatabaseBossMagicParser Parser() => new DatabaseBossMagicParser(new DatabaseMagicParser(new Rows(), MagicRegistry.Standard()));
        private static SimulationContext Context(IBossMagicParser parser = null, int seed = 1) {
            var c = new SimulationContext { BossMagicParser = parser, Random = new JavaRandom(seed) }; c.InitializationFailed += (o, e) => throw e; SimplePveBossPrefabs.Register(c);
            foreach (var key in new[] { "pve_water_slime_nest", "pve_nature_slime_nest", "vine_colony", "pve_vine_witch" }) { c.GameParameters[key + ".mass"] = 1; c.GameParameters[key + ".radius"] = 1; c.GameParameters[key + ".hp"] = 500; }
            return c;
        }
        [PortedTest(Source)] public static void damage_reaches_the_boss_mob_not_the_spawner() {
            var c = Context(); c.GameParameters["pve_nature_slime_nest.mass"] = 1000000; c.GameParameters["pve_nature_slime_nest.hp"] = 1000; var owner = c.Spawn(Master.RightPlayer, "PveNatureSlimeNest", Vector3.Zero);
            Assert.True(owner.HasComponent<Spawner>() && owner.HasComponent<ICollidable>()); Assert.True(owner.GetComponent<MobComponent>() is PveBossMob); owner.GetComponent<MobComponent>().ApplyDamage(new Hit(100, 0, Element.None)); Assert.Equal(900, owner.GetComponent<PveBossMob>().Hp);
            var gauges = GaugeExtractor.Extract(owner).Where(g => g.Category == GaugeCategory.Hp).ToArray(); Assert.Equal(1, gauges.Length); Assert.Equal((Fix64)900, gauges[0].Value); Assert.Equal((Fix64)1000, gauges[0].MaxValue);
        }
        [PortedTest(Source)] public static void nest_spawner_uses_the_configured_prefab_and_element() {
            var c = Context(); c.GameParameters["pve_water_slime_nest.hp"] = 10; var owner = c.Spawn(Master.RightPlayer, "PveWaterSlimeNest", Vector3.Zero); Assert.True(owner.Elements.NativeHas(Element.Water) && owner.HasComponent<Spawner>()); Assert.Equal((Fix64)1, owner.Colliders.Single().CircleRadius.Value);
        }
        [PortedTest(Source)] public static void vine_witch_builds_a_vine_witch_mob_with_its_magics_and_the_vinefan_lookup() {
            var c = Context(new Stub()); var owner = c.Spawn(Master.RightPlayer, "PveVineWitch", Vector3.Zero); var mob = owner.GetComponent<VineWitchMob>(); Assert.Equal(1, mob.Magics.Count); Assert.Equal(500, mob.MaxHp); Assert.True(owner.HasComponent<Spawner>() && owner.Elements.NativeHas(Element.Nature));
        }
        [PortedTest(Source)] public static void vine_colony_asks_for_the_vine_magic_and_builds_a_plain_boss_mob() {
            var c = Context(new Stub()); var owner = c.Spawn(Master.RightPlayer, "PveVineColony", Vector3.Zero); Assert.True(owner.HasComponent<PveBossMob>() && !owner.HasComponent<VineWitchMob>());
        }
        [PortedTest(Source)] public static void a_boss_without_a_magic_parser_gets_an_empty_magic_list() {
            var c = Context(); var owner = c.Spawn(Master.RightPlayer, "PveVineColony", Vector3.Zero); Assert.Equal(0, owner.GetComponent<PveBossMob>().Magics.Count); Assert.Equal(1, owner.GetComponents<Spawner>().Length);
        }
        [PortedTest(Source)] public static void vine_witch_gets_vine_toss_and_vine_colony() {
            var c = Context(Parser()); var mob = c.Spawn(Master.RightPlayer, "PveVineWitch", Vector3.Zero).GetComponent<VineWitchMob>(); Assert.Sequence(new long[] { 1, 2 }, mob.Magics.Select(m => m.Id)); Assert.True(mob.HasVineFan);
        }
        [PortedTest(Source)] public static void the_vine_fan_resolves_only_under_its_bean_name() { var parser = Parser(); Assert.True(parser.ParseVineFan("vine_fan") != null); Assert.True(parser.ParseVineFan("vinefan") == null); Assert.True(parser.ParseVineFan("vine_toss") == null); }
        [PortedTest(Source)] public static void vine_colony_gets_vine_toss() {
            var c = Context(Parser()); var mob = c.Spawn(Master.RightPlayer, "PveVineColony", Vector3.Zero).GetComponent<PveBossMob>(); Assert.Sequence(new long[] { 1 }, mob.Magics.Select(m => m.Id));
        }
        public static void CheckSpawnSelection() {
            foreach (int seed in new int[] { 1, 2, 3, 42, 99 }) {
                var c = Context(seed: seed); foreach (var prefab in new[] { "LeafSlime", "WaterSlime", "VineSpirit" }) c.RegisterPrefab(prefab, _ => { }); var expectedRandom = new JavaRandom(seed); int selected = expectedRandom.NextBound(3);
                var owner = c.Spawn(Master.RightPlayer, "PveVineWitch", new Vector3(9, 0, 5)); c.FlushObjects(); var boss = owner.GetComponent<VineWitchMob>(); Assert.Equal((Fix64)8, boss.AttackInterval.Total); Assert.Equal((Fix64)7, boss.AttackRange); Assert.Equal(1, boss.TargetMask); Assert.Equal(Fix64.Zero, boss.Speed.Total); Assert.True(owner.Colliders.Single().IsTrigger);
                var spawner = owner.GetComponent<Spawner>(); c.DeltaTime = 8; spawner.Update(); var spawned = c.PendingObjects; Assert.Equal(2, spawned.Length); Assert.True(spawned.All(o => o.Prefab == new[] { "LeafSlime", "WaterSlime", "VineSpirit" }[selected] && o.Master == owner.Master)); Assert.Sequence(new Fix64[] { (Fix64)8.75m, (Fix64)9.25m }, spawned.Select(o => o.Position.X));
                Assert.Equal(expectedRandom.NextBound(1000), c.Random.NextBound(1000)); spawner.OnDamaged(new Hit(100, 0, Element.None)); Assert.True(!owner.IsDestroyed); spawner.Update(); Assert.Equal(4, c.PendingObjects.Length);
            }
        }
    }
}


