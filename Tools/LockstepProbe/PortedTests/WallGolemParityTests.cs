using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class WallGolemParityTests {
        private const string Source = "game/domain/object/prefab/implement/misc/wall_golem_prefab_initializer.rs";
        private static SimObject Build() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; RockUnitPrefabs.Register(c); RockRemnantPrefabs.Register(c);
            string[] keys = { "hp", "speed", "damage", "attack_interval", "mass", "radius" }; decimal[] values = { 5000, 0.1m, 5, 1.5m, 200, 1.5m };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["wall_golem." + keys[i]] = (Fix64)values[i]; c.GameParameters["medium_rock_remnant.radius"] = 1;
            return c.Spawn(Master.LeftPlayer, "WallGolem", new Vector3(5, 0, 5));
        }
        [PortedTest(Source)] public static void advertises_wall_golem_prefab_type() { Assert.Equal("WallGolem", Build().Prefab); }
        [PortedTest(Source)] public static void leaves_a_remnant_when_killed_in_combat() { Assert.True(Build().HasComponent<RockDeathRemnant>()); }
        [PortedTest(Source)] public static void is_a_slow_heavily_armored_melee_golem_with_solid_circle_collider() {
            var owner = Build(); var mob = owner.GetComponent<RockGolemMob>(); Assert.Equal(5000, mob.Hp); Assert.Equal(5000, mob.MaxHp); Assert.Equal((Fix64)1.5m, owner.Colliders.Single().CircleRadius.Value); Assert.True(!owner.Colliders.Single().IsTrigger);
            Assert.True(owner.HasComponent<RigidBody>() && owner.HasComponent<ZPhysics>() && owner.HasComponent<CommonEffectReceiver>());
        }
        public static void CheckCombatAndRemnant() {
            var owner = Build(); var c = owner.Context; var mob = owner.GetComponent<RockGolemMob>(); Assert.Equal((Fix64)0.1m, mob.Speed.Total); Assert.Equal((Fix64)1.5m, mob.AttackInterval.Total); Assert.Equal(200, owner.GetComponent<RigidBody>().Mass); Assert.Equal(1, mob.TargetMask); Assert.True(owner.Elements.NativeHas(Element.Rock));
            c.RegisterPrefab("Target", _ => { }); var target = c.Spawn(Master.RightPlayer, "Target", new Vector3(6, 0, 5)); var health = new DummyMob(target, 100); target.AddComponentImmediately(health); c.FlushObjects();
            Assert.True(mob.Behavior(target)); Assert.Equal(95, health.Hp); mob.ApplyDamage(new Hit(6000, 0, Element.None)); Assert.True(owner.IsDestroyed); var remnant = c.PendingObjects.Single(); Assert.Equal("MediumRockRemnant", remnant.Prefab); Assert.Equal(Master.LeftPlayer, remnant.Master); Assert.Equal(owner.Position, remnant.Position); Assert.True(remnant.HasComponent<StaticObstacle>() && remnant.HasComponent<TimedSelfDestroyer>());
            mob.ApplyDamage(new Hit(6000, 0, Element.None)); Assert.Equal(1, c.PendingObjects.Length);
        }
    }
}
