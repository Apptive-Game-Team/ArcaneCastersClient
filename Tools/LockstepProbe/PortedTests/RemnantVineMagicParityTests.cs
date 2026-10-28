using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RemnantVineMagicParityTests {
        private const string BlastSource = "game/domain/magic/implement/explode/rock_blast_magic.rs";
        private const string GrowthSource = "game/domain/object/component/magic/vine_world_growth.rs";
        private static SimulationContext Context() { var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; return c; }
        private static BoundMagic Magic(string name) { var registry = new MagicRegistry(); RemnantAndVineMagics.Register(registry); return registry.GetBean(name).Bind(1, name, Element.None); }
        [PortedTest(BlastSource)] public static void consumes_every_remnant_and_leaves_small_remnant_after_medium_remnant_explodes() {
            var c = Context(); RockRemnantPrefabs.Register(c); ExplosionPrefabs.Register(c); c.RegisterPrefab("RockGolem", _ => { });
            c.GameParameters["rock_blast.radius"] = 3; c.GameParameters["rock_explode.radius"] = 3; c.GameParameters["rock_explode.damage"] = 20;
            c.GameParameters["mini_rock.radius"] = (Fix64)0.5m; c.GameParameters["medium_rock_remnant.radius"] = (Fix64)0.5m;
            var small = c.Spawn(Master.LeftPlayer, "RockRemnant", new Vector3(2, 0, 3)); var medium = c.Spawn(Master.LeftPlayer, "MediumRockRemnant", new Vector3(4, 0, 5)); var unrelated = c.Spawn(Master.LeftPlayer, "RockGolem", new Vector3(3, 0, 4)); c.FlushObjects();
            Magic("rock_blast").Run(c, Master.RightPlayer, new Vector3(3, 0, 4)); var created = c.PendingObjects;
            Assert.Equal(3, created.Length); var explosions = created.Where(o => o.Prefab == "RockExplode").ToArray(); Assert.True(explosions.All(o => o.Master == Master.RightPlayer)); Assert.Sequence(new[] { small.Position, medium.Position }, explosions.Select(o => o.Position));
            var remainder = created.Single(o => o.Prefab == "RockRemnant"); Assert.Equal(Master.LeftPlayer, remainder.Master); Assert.Equal(medium.Position, remainder.Position); Assert.True(small.IsDestroyed && medium.IsDestroyed && !unrelated.IsDestroyed);
        }
        [PortedTest(GrowthSource)] public static void grows_two_rings_of_vines_after_the_delay_and_summons_a_seed_spirit_per_hit_weight() {
            var c = Context(); c.DeltaTime = (Fix64)0.5m; c.RegisterPrefab("Vine", _ => { }); c.RegisterPrefab("SeedSpirit", _ => { }); var owner = new SimObject(c, Master.LeftPlayer, "GiantVine", new Vector3(9, 0, 5)); var tracker = new VineHitTracker(); tracker.MarkIfFirstHit(1); var growth = new VineWorldGrowth(owner, tracker);
            growth.Update(); Assert.Equal(0, c.PendingObjects.Length); growth.Update(); Assert.Equal(26, c.PendingObjects.Count(o => o.Prefab == "Vine")); Assert.Equal(2, c.PendingObjects.Count(o => o.Prefab == "SeedSpirit")); growth.Update(); Assert.Equal(28, c.PendingObjects.Length);
        }
        public static void CheckActualGrowth() {
            var c = Context(); c.DeltaTime = (Fix64)0.25m; RemnantAndVineMagics.RegisterGiantVine(c); VinePrefabs.Register(c); NatureUnitPrefabs.Register(c); c.RegisterPrefab("Target", _ => { });
            foreach (var key in new[] { "giant_vine", "vine" }) { c.GameParameters[key + ".radius"] = 1; c.GameParameters[key + ".damage"] = 10; c.GameParameters[key + ".duration"] = 5; }
            foreach (var key in new[] { "mass", "hp", "speed", "damage", "attack_interval", "attack_range" }) c.GameParameters["seed_spirit." + key] = 1;
            c.GameParameters["seed_spirit.radius"] = (Fix64)0.25m;
            DummyMob Target(Vector3 position) { var o = c.Spawn(Master.RightPlayer, "Target", position); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); return mob; }
            var center = new Vector3(9, 0, 5); var middle = Target(center); var ringTarget = Target(center + new Vector3((Fix64)2.25m, 0, 0)); c.FlushObjects();
            Magic("vine_world").Run(c, Master.LeftPlayer, new Vector3(1, 0, 1), center); var giant = c.PendingObjects.Single(); Assert.Equal(90, middle.Hp); Assert.Equal(100, ringTarget.Hp); Assert.True(VineSpawnContext.CurrentTracker(c) == null);
            c.FlushObjects(); c.FlushObjects(); var growth = giant.GetComponent<VineWorldGrowth>(); Assert.True(growth != null); growth.Update(); growth.Update(); Assert.Equal(0, c.PendingObjects.Length); growth.Update();
            var vines = c.PendingObjects.Where(o => o.Prefab == "Vine").ToArray(); var seeds = c.PendingObjects.Where(o => o.Prefab == "SeedSpirit").ToArray(); Assert.Equal(26, vines.Length); Assert.Equal(3, seeds.Length); Assert.Equal(90, ringTarget.Hp); Assert.Equal(90, middle.Hp);
            for (int i = 0; i < vines.Length; i++) { Assert.Near(i < 10 ? (Fix64)2.25m : (Fix64)4, Vector3.Distance(center, vines[i].Position), (Fix64)0.00001m); Assert.Equal(center.Y, vines[i].Position.Y); }
            Assert.True(seeds.All(o => o.Master == Master.LeftPlayer && o.HasComponent<MobComponent>() && o.Position.Z == center.Z)); Assert.Equal(center.X + (Fix64)0.65m, seeds[0].Position.X); Assert.True(seeds[1].Position.Y > 0); Assert.True(VineSpawnContext.CurrentTracker(c) == null);
            growth.Update(); Assert.Equal(29, c.PendingObjects.Length); giant.FlushComponents(); Assert.True(!giant.HasComponent<VineWorldGrowth>());
            var timing = Context(); timing.DeltaTime = (Fix64)0.05m; timing.RegisterPrefab("Vine", _ => { }); var timedOwner = new SimObject(timing, Master.LeftPlayer, "GiantVine", center); var timedGrowth = new VineWorldGrowth(timedOwner, new VineHitTracker());
            for (int i = 0; i < 14; i++) timedGrowth.Update(); Assert.Equal(0, timing.PendingObjects.Length); timedGrowth.Update(); Assert.Equal(26, timing.PendingObjects.Length);
        }
    }
}
