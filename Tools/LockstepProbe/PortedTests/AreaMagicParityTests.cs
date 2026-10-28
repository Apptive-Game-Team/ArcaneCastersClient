using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class AreaMagicParityTests {
        private const string OvergrowthSource = "game/domain/object/component/magic/overgrowth_explosion.rs";
        private const string GaleSource = "game/domain/object/component/magic/razor_gale.rs";
        private const string RegistrySource = "game/domain/magic/implement/explode/mod.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.5m }; c.InitializationFailed += (o, e) => throw e;
            foreach (var name in new[] { "Object", "SeedSpirit", "TreeGolem" }) c.RegisterPrefab(name, _ => { }); AreaMagics.RegisterPrefabs(c); ExplosionPrefabs.Register(c);
            foreach (var key in new[] { "overgrowth", "razor_gale", "magma_explosion", "water_explosion", "sand_storm", "electric_explode", "wind_explode", "shock_overload" }) {
                c.GameParameters[key + ".radius"] = 3; c.GameParameters[key + ".damage"] = 6; c.GameParameters[key + ".duration"] = 2;
            }
            c.GameParameters["overgrowth.quantity"] = 3; c.GameParameters["razor_gale.attack_interval"] = 1; c.GameParameters["water_explosion.z_force"] = 4;
            c.GameParameters["game.snare_duration"] = 2; c.GameParameters["game.snare_fire_damage"] = 0; c.GameParameters["game.snare_slow_percent"] = (Fix64)0.5m; return c;
        }
        private static DummyMob Target(SimulationContext c, Master side, Vector3 position, string prefab = "Object") {
            var o = c.Spawn(side, prefab, position); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); o.AddComponentImmediately(new CommonEffectReceiver(o)); return mob;
        }
        [PortedTest(OvergrowthSource)] public static void with_no_seed_spirit_to_evolve_it_summons_a_centered_row_and_binds_enemies() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "Overgrowth", new Vector3(5, 0, 5)); var enemy = Target(c, Master.RightPlayer, new Vector3(6, 0, 5)); c.FlushObjects(); var explosion = owner.GetComponent<OvergrowthExplosion>();
            explosion.Update(); Assert.True(!owner.IsDestroyed); explosion.Update(); Assert.Equal(94, enemy.Hp); Assert.True(enemy.Owner.GetComponent<CommonEffectReceiver>().GetEffectByKey(StatusEffectKey.SnaredReceive) != null);
            Assert.Sequence(new Fix64[] { (Fix64)4.5m, 5, (Fix64)5.5m }, c.PendingObjects.Select(o => o.Position.X)); Assert.True(c.PendingObjects.All(o => o.Prefab == "SeedSpirit" && o.Master == Master.LeftPlayer)); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(OvergrowthSource)] public static void with_an_allied_seed_spirit_in_range_it_evolves_instead_of_summoning() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "Overgrowth", new Vector3(5, 0, 5)); var spirit = Target(c, Master.LeftPlayer, new Vector3(6, 0, 5), "SeedSpirit"); c.FlushObjects(); var explosion = owner.GetComponent<OvergrowthExplosion>(); explosion.Update(); explosion.Update();
            Assert.Equal("TreeGolem", c.PendingObjects.Single().Prefab); Assert.True(spirit.Owner.IsDestroyed);
        }
        [PortedTest(GaleSource)] public static void hits_everything_damageable_in_range_once_per_interval_but_not_itself() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); var ownMob = new DummyMob(owner, 100); owner.AddComponentImmediately(ownMob); owner.AddCollider(new CircleCollider(owner, (Fix64)0.25m)); var victim = Target(c, Master.RightPlayer, Vector3.Right); c.FlushObjects(); var gale = new RazorGale(owner, 4, 2, 1);
            gale.Update(); Assert.Equal(100, victim.Hp); gale.Update(); Assert.Equal(96, victim.Hp); Assert.Equal(100, ownMob.Hp);
        }
        [PortedTest(RegistrySource)] public static void registers_every_magic_under_its_java_bean_name() {
            var registry = new MagicRegistry(); AreaMagics.Register(registry);
            foreach (var name in new[] { "lightning_explosion", "magma_explosion", "overgrowth", "razor_gale", "rock_blast", "sand_storm", "shock_overload", "vine_world", "water_explosion", "wind_explosion" }) Assert.True(registry.ContainsBean(name));
        }
        public static void CheckActualAreas() {
            var c = Context(); var registry = new MagicRegistry(); AreaMagics.Register(registry); var aim = new Vector3(5, 0, 5);
            var names = new[] { "lightning_explosion", "magma_explosion", "overgrowth", "razor_gale", "sand_storm", "shock_overload", "water_explosion", "wind_explosion" };
            var prefabs = new[] { "ElectricExplode", "MagmaExplosion", "Overgrowth", "RazorGale", "SandStorm", "ShockOverload", "WaterExplosion", "WindExplode" };
            for (int i = 0; i < names.Length; i++) {
                var magic = registry.GetBean(names[i]).Bind(1, names[i], Element.None); Assert.True(magic.IsExplosionMagic); magic.Run(c, Master.LeftPlayer, new Vector3(1, 2, 3), aim); var owner = c.PendingObjects.Last();
                Assert.Equal(prefabs[i], owner.Prefab); Assert.Equal(aim, owner.Position); Assert.Equal(i == 7 ? Master.None : Master.LeftPlayer, owner.Master); Assert.True(owner.Colliders.Single().IsTrigger && !owner.IsDestroyed);
            }
            Assert.True(!registry.GetBean("rock_blast").Bind(1, "rock_blast", Element.Rock).IsExplosionMagic); Assert.True(!registry.GetBean("vine_world").Bind(2, "vine_world", Element.Nature).IsExplosionMagic);
            var water = c.PendingObjects.Single(o => o.Prefab == "WaterExplosion"); var magma = c.PendingObjects.Single(o => o.Prefab == "MagmaExplosion"); var sand = c.PendingObjects.Single(o => o.Prefab == "SandStorm");
            Assert.True(water.Elements.Has(Element.Water) && water.HasComponent<WaterExplode>()); Assert.Equal(VisualEffect.Burn, water.GetComponent<EffectProvider>().Effect); Assert.True(magma.Elements.Has(Element.Fire) && magma.HasComponent<Explode>()); Assert.Equal(VisualEffect.Burn, magma.GetComponent<EffectProvider>().Effect);
            Assert.Sequence(new[] { Element.Rock, Element.Wind }.OrderBy(e => e), sand.Elements.Total); Assert.Equal(VisualEffect.Sandstorm, sand.GetComponent<EffectProvider>().Effect); c.DeltaTime = 2; sand.Update(); Assert.True(sand.IsDestroyed);
            var waterTarget = Target(c, Master.LeftPlayer, aim); waterTarget.Owner.AddComponentImmediately(new RigidBody(waterTarget.Owner, 1)); var vertical = new ZPhysics(waterTarget.Owner); waterTarget.Owner.AddComponentImmediately(vertical); c.FlushObjects(); c.DeltaTime = (Fix64)0.5m;
            water.GetComponent<WaterExplode>().Update(); Assert.Equal(100, waterTarget.Hp); water.GetComponent<WaterExplode>().Update(); Assert.Equal(94, waterTarget.Hp); c.DeltaTime = (Fix64)0.05m; vertical.ApplyZForce(); Assert.True(waterTarget.Owner.Position.Y > 0);
            var combat = Context(); var enemy = Target(combat, Master.RightPlayer, aim); var ally = Target(combat, Master.LeftPlayer, aim); var neutral = Target(combat, Master.None, aim); var far = Target(combat, Master.RightPlayer, new Vector3(15, 0, 5)); combat.FlushObjects();
            var galeOwner = combat.Spawn(Master.LeftPlayer, "RazorGale", aim); var gale = galeOwner.GetComponent<RazorGale>(); gale.Update(); gale.Update(); Assert.Equal(94, enemy.Hp); Assert.Equal(94, ally.Hp); Assert.Equal(94, neutral.Hp); Assert.Equal(100, far.Hp);
            combat.DeltaTime = (Fix64)1.25m; gale.Update(); Assert.Equal(88, enemy.Hp); combat.DeltaTime = (Fix64)0.75m; gale.Update(); Assert.Equal(82, enemy.Hp);
            var growth = combat.Spawn(Master.LeftPlayer, "Overgrowth", aim).GetComponent<OvergrowthExplosion>(); combat.DeltaTime = (Fix64)0.5m; growth.Update(); growth.Update(); Assert.Equal(76, enemy.Hp); Assert.Equal(82, ally.Hp); Assert.Equal(82, neutral.Hp); growth.Update(); Assert.Equal(76, enemy.Hp);
            var empty = Context(); empty.GameParameters["overgrowth.quantity"] = 0; var neutralGrowth = empty.Spawn(Master.None, "Overgrowth", aim).GetComponent<OvergrowthExplosion>(); var unaffected = Target(empty, Master.RightPlayer, aim); empty.FlushObjects(); neutralGrowth.Update(); neutralGrowth.Update(); Assert.Equal(100, unaffected.Hp); Assert.Equal(aim, empty.PendingObjects.Single().Position);
        }
    }
}
