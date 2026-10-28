using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SeedSpiritParityTests {
        private const string Evolve = "game/domain/object/component/magic/seed_spirit_evolver.rs";
        private const string Summon = "game/domain/object/component/magic/seed_spirit_vine_summoner.rs";
        private static SimulationContext Context() {
            var context = new SimulationContext(0);
            foreach (string name in new[] { "SeedSpirit", "FireSlime", "VineSpirit", "TreeGolem", "Vine" }) context.RegisterPrefab(name, _ => { }); return context;
        }
        private static SimObject Target(SimulationContext context, string prefab, Master master, int x, int z) {
            var target = context.Spawn(master, prefab, new Vector3(x, 0, z)); target.AddCollider(new CircleCollider(target, (Fix64)0.5m)); context.FlushObjects(); return target;
        }
        [PortedTest(Evolve)] public static void evolves_only_allied_seed_spirits_in_range_into_the_new_prefab_at_the_same_spot() {
            var context = Context(); var source = new SimObject(context, Master.LeftPlayer, "Overgrowth", Vector3.Zero);
            var ally = Target(context, "SeedSpirit", Master.LeftPlayer, 1, 2); var enemy = Target(context, "SeedSpirit", Master.RightPlayer, 2, 2); var other = Target(context, "FireSlime", Master.LeftPlayer, 3, 2);
            Assert.Equal(1, SeedSpiritEvolver.EvolveAlliedSeedSpirits(source, 3, "TreeGolem")); Assert.True(ally.IsDestroyed); Assert.True(!enemy.IsDestroyed && !other.IsDestroyed && !source.IsDestroyed);
            var created = context.PendingObjects; Assert.Equal(1, created.Length); Assert.Equal("TreeGolem", created[0].Prefab); Assert.Equal(Master.LeftPlayer, created[0].Master); Assert.Equal(new Vector3(1, 0, 2), created[0].Position);
        }
        [PortedTest(Evolve)] public static void a_neutral_source_evolves_seed_spirits_of_either_side() {
            var context = Context(); var source = new SimObject(context, Master.None, "Overgrowth", Vector3.Zero); Target(context, "SeedSpirit", Master.RightPlayer, 1, 0);
            Assert.Equal(1, SeedSpiritEvolver.EvolveAlliedSeedSpirits(source, 3, "TreeGolem"));
        }
        [PortedTest(Summon)] public static void summons_a_vine_owned_by_the_building_at_an_allied_seed_spirit() {
            var context = Context(); var owner = new SimObject(context, Master.LeftPlayer, "GrassGenerator", Vector3.Zero); Target(context, "SeedSpirit", Master.LeftPlayer, 3, 2);
            new SeedSpiritVineSummoner(owner, 7).Update(); var created = context.PendingObjects; Assert.Equal(1, created.Length); Assert.Equal("Vine", created[0].Prefab); Assert.Equal(Master.LeftPlayer, created[0].Master); Assert.Equal((Fix64)3, created[0].Position.X); Assert.Equal((Fix64)2, created[0].Position.Z);
        }
        [PortedTest(Summon)] public static void ignores_enemy_seed_spirits_and_other_units() {
            var context = Context(); var owner = new SimObject(context, Master.LeftPlayer, "GrassGenerator", Vector3.Zero); Target(context, "SeedSpirit", Master.RightPlayer, 1, 0); Target(context, "VineSpirit", Master.LeftPlayer, 2, 0);
            new SeedSpiritVineSummoner(owner, 7).Update(); Assert.Equal(0, context.PendingObjects.Length);
        }
        [PortedTest(Summon)] public static void skips_a_seed_spirit_already_transformed_earlier_in_the_same_tick() {
            var context = Context(); var owner = new SimObject(context, Master.LeftPlayer, "GrassGenerator", Vector3.Zero); Target(context, "SeedSpirit", Master.LeftPlayer, 1, 0).Destroy();
            new SeedSpiritVineSummoner(owner, 7).Update(); Assert.Equal(0, context.PendingObjects.Length);
        }
        public static void CheckVinePrefab() {
            var context = new SimulationContext(0); context.RegisterPrefab("SeedSpirit", _ => { }); context.RegisterPrefab("FireSlime", _ => { }); context.RegisterPrefab("VineSpirit", _ => { });
            context.GameParameters["vine.radius"] = 1; context.GameParameters["vine.duration"] = (Fix64)0.5m; context.GameParameters["vine.damage"] = 4;
            context.GameParameters["game.snare_duration"] = 1; context.GameParameters["game.snare_fire_damage"] = 2; context.GameParameters["game.snare_slow_percent"] = (Fix64)0.5m; VinePrefabs.Register(context);
            var seed = Target(context, "SeedSpirit", Master.LeftPlayer, 3, 2); var enemy = Target(context, "FireSlime", Master.RightPlayer, 3, 2);
            enemy.AddComponentImmediately(new DummyMob(enemy, 20)); enemy.AddComponentImmediately(new CommonEffectReceiver(enemy));
            var vine = context.Spawn(Master.LeftPlayer, "Vine", seed.Position); vine.FlushComponents(); context.FlushObjects();
            Assert.True(seed.IsDestroyed); Assert.Equal(1, context.Objects.Count(owner => owner.Prefab == "VineSpirit")); Assert.Equal(16, enemy.GetComponent<DummyMob>().Hp);
            Assert.Equal(Element.Nature, vine.Elements.Total.Single()); Assert.True(vine.Colliders[0].IsTrigger);
            new BattlePhysicsSystem().Update(context); Assert.True(enemy.GetComponent<CommonEffectReceiver>().GetEffectByKey(StatusEffectKey.SnaredReceive) != null);
        }
    }
}
