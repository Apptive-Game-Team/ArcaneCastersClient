using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class TidalWarheadParityTests {
        private const string ProjectileSource = "game/domain/object/component/magic/tidal_warhead_projectile.rs";
        private const string MagicSource = "game/domain/magic/implement/shoot/tidal_warhead_magic.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.25m }; c.InitializationFailed += (o, e) => throw e;
            c.RegisterPrefab("Object", _ => { }); c.RegisterPrefab("Player", _ => { }); TidalWarheadMagic.RegisterPrefabs(c);
            c.GameParameters["tidal_warhead.damage"] = 100; c.GameParameters["tidal_warhead.speed"] = 8; c.GameParameters["tidal_warhead.radius"] = 3; return c;
        }
        private static DummyMob Target(SimulationContext c, Master side, Vector3 position) {
            var o = c.Spawn(side, "Object", position); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); return mob;
        }
        [PortedTest(ProjectileSource)] public static void ground_lock_halves_area_damage_and_does_not_hit_allies() {
            var c = Context(); var missile = c.Spawn(Master.LeftPlayer, "GroundTidalWarhead", Vector3.Zero); var enemy = Target(c, Master.RightPlayer, new Vector3(1, 0, 0)); var ally = Target(c, Master.LeftPlayer, new Vector3(1, 0, 1)); c.FlushObjects();
            var flight = missile.GetComponent<TidalWarheadProjectile>(); flight.SetTarget(enemy.Owner, false); flight.OnCollisionWithEnemy(enemy.Owner);
            Assert.Equal(50, enemy.Hp); Assert.Equal(100, ally.Hp); Assert.True(missile.IsDestroyed); Assert.Equal("TidalWarheadExplosion", c.PendingObjects.Single().Prefab);
        }
        [PortedTest(ProjectileSource)] public static void aerial_lock_deals_full_area_damage() {
            var c = Context(); var missile = c.Spawn(Master.LeftPlayer, "TidalWarhead", new Vector3(0, 3, 0)); var enemy = Target(c, Master.RightPlayer, new Vector3(1, 3, 0)); c.FlushObjects();
            var flight = missile.GetComponent<TidalWarheadProjectile>(); flight.SetTarget(enemy.Owner, true); flight.OnCollisionWithEnemy(enemy.Owner); Assert.Equal(0, enemy.Hp);
        }
        [PortedTest(ProjectileSource)] public static void flies_toward_the_target_and_explodes_when_it_arrives() {
            var c = Context(); var missile = c.Spawn(Master.LeftPlayer, "TidalWarhead", Vector3.Zero); var enemy = Target(c, Master.RightPlayer, new Vector3(3, 0, 0)); c.FlushObjects(); var flight = missile.GetComponent<TidalWarheadProjectile>(); flight.SetTarget(enemy.Owner, true);
            flight.Update(); Assert.Equal(new Vector3(2, 0, 0), missile.Position); Assert.True(!missile.IsDestroyed); flight.Update(); Assert.Equal(new Vector3(3, 0, 0), missile.Position); Assert.True(missile.IsDestroyed);
        }
        [PortedTest(MagicSource)] public static void prefers_aerial_target_even_when_ground_target_is_closer_to_aim() {
            var c = Context(); var player = c.Spawn(Master.LeftPlayer, "Player", Vector3.Zero); var ground = Target(c, Master.RightPlayer, new Vector3(1, 0, 0)); var air = Target(c, Master.RightPlayer, new Vector3(8, 3, 0)); c.FlushObjects();
            Assert.Equal(air.Owner, TidalWarheadMagic.SelectTarget(c, player, ground.Owner.Position));
        }
        [PortedTest(MagicSource)] public static void falls_back_to_ground_target_when_no_aerial_enemy_exists() {
            var c = Context(); var player = c.Spawn(Master.LeftPlayer, "Player", Vector3.Zero); var near = Target(c, Master.RightPlayer, new Vector3(2, 0, 0)); Target(c, Master.RightPlayer, new Vector3(6, 0, 0)); c.FlushObjects(); Assert.Equal(near.Owner, TidalWarheadMagic.SelectTarget(c, player, Vector3.Zero));
        }
        public static void CheckActualCast() {
            var c = Context(); c.GameParameters["tidal_warhead.damage"] = 5; var registry = new MagicRegistry(); TidalWarheadMagic.Register(registry); var magic = registry.GetBean("tidal_warhead").Bind(1, "tidal_warhead", Element.Water);
            var player = c.Spawn(Master.LeftPlayer, "Player", new Vector3(5, 0, 5)); var target = Target(c, Master.RightPlayer, new Vector3(8, 0, 5)); var secondary = Target(c, Master.RightPlayer, new Vector3(9, 0, 5)); var ally = Target(c, Master.LeftPlayer, new Vector3(8, 0, 5)); c.FlushObjects();
            magic.Run(c, Master.LeftPlayer, new Vector3(1, 3, 1), target.Owner.Position); var missile = c.PendingObjects.Single(); Assert.Equal("GroundTidalWarhead", missile.Prefab); Assert.Equal(player.Position, missile.Position); Assert.True(missile.Elements.Has(Element.Water)); var flight = missile.GetComponent<TidalWarheadProjectile>();
            flight.Update(); Assert.Equal(new Vector3(7, 0, 5), missile.Position); target.Owner.SetPosition(new Vector3(9, 0, 5)); flight.Update(); Assert.True(missile.IsDestroyed); Assert.Equal(97, target.Hp); Assert.Equal(97, secondary.Hp); Assert.Equal(100, ally.Hp); flight.OnCollisionWithEnemy(target.Owner); Assert.Equal(97, target.Hp);
            c.FlushObjects(); var air = Target(c, Master.RightPlayer, new Vector3(5, 3, 5)); c.FlushObjects(); magic.Run(c, Master.LeftPlayer, null, target.Owner.Position); var aerial = c.PendingObjects.Single(); Assert.Equal("TidalWarhead", aerial.Prefab); var airFlight = aerial.GetComponent<TidalWarheadProjectile>(); airFlight.Update(); Assert.Equal(new Vector3(5, 2, 5), aerial.Position); air.Owner.Destroy(); air.Owner.SetPosition(new Vector3(10, 3, 5)); airFlight.Update(); Assert.True(aerial.IsDestroyed); Assert.Equal(new Vector3(5, 3, 5), aerial.Position);
        }
    }
}
