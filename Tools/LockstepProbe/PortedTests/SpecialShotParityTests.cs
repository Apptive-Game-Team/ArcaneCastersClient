using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SpecialShotParityTests {
        private const string Wind = "game/domain/object/component/magic/wind_blade_shot.rs";
        private const string Chain = "game/domain/object/component/magic/chain_shot.rs";
        private sealed class Recorder : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>(); public Recorder(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); }
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); }
        }
        private static SimObject Target(SimulationContext context, int x) {
            if (context.Objects.Length == 0) context.RegisterPrefab("FireSlime", _ => { });
            var target = context.Spawn(Master.RightPlayer, "FireSlime", new Vector3(x, 0, 0));
            target.AddComponentImmediately(new Recorder(target)); target.AddCollider(new CircleCollider(target, (Fix64)0.5m)); context.FlushObjects(); return target;
        }
        [PortedTest(Wind)] public static void damages_target_without_applying_knockback() {
            var context = new SimulationContext(0); var owner = new SimObject(context, Master.LeftPlayer, "WindBlade", Vector3.Zero); var target = Target(context, 1);
            new WindBladeShot(owner, 9, 5).OnCollisionWithEnemy(target); Assert.Equal(1, target.GetComponent<Recorder>().Hits.Count); Assert.Equal(0, target.PendingComponents.Length);
        }
        [PortedTest(Wind)] public static void each_pierced_target_takes_two_thirds_of_the_last_hit() {
            var context = new SimulationContext(0); var owner = new SimObject(context, Master.LeftPlayer, "WindBlade", Vector3.Zero); var blade = new WindBladeShot(owner, 9, 5);
            foreach (int damage in new[] { 9, 6, 4 }) { var target = Target(context, 1); blade.OnCollisionWithEnemy(target); blade.OnCollisionWithEnemy(target); var hits = target.GetComponent<Recorder>().Hits; Assert.Equal(1, hits.Count); Assert.Equal(damage, hits[0].Damage); }
        }
        [PortedTest(Chain)] public static void chains_to_the_next_target_with_half_the_damage_after_a_delay() {
            var context = new SimulationContext(0); var owner = new SimObject(context, Master.LeftPlayer, "ChainLightning", Vector3.Zero); var chain = new ChainShot(owner, 10, 5, 3, 2);
            var first = Target(context, 1); var second = Target(context, 2);
            chain.OnCollisionWithEnemy(first); Assert.Equal(10, first.GetComponent<Recorder>().Hits[0].Damage); Assert.Equal(first.Position, owner.Position);
            chain.OnCollisionWithEnemy(second); Assert.Equal(0, second.GetComponent<Recorder>().Hits.Count);
            context.DeltaTime = (Fix64)0.1m; for (int i = 0; i < 3; i++) chain.Update(); chain.Update(); chain.OnCollisionWithEnemy(second);
            Assert.Equal(5, second.GetComponent<Recorder>().Hits[0].Damage);
        }
        [PortedTest(Chain)] public static void without_a_next_target_the_shot_is_destroyed() {
            var context = new SimulationContext(0); var owner = new SimObject(context, Master.LeftPlayer, "ChainLightning", Vector3.Zero); var chain = new ChainShot(owner, 10, 5, 3, 2); var first = Target(context, 1);
            chain.OnCollisionWithEnemy(first); Assert.True(owner.IsDestroyed);
        }
        public static void CheckBoulderCollision() {
            var context = new SimulationContext(0) { DeltaTime = (Fix64)0.05m }; context.RegisterPrefab("Body", _ => { }); TerrainPrefabs.Register(context);
            var source = new SimObject(context, Master.LeftPlayer, "BoulderStrike", new Vector3(2, 0, 5));
            var target = context.Spawn(Master.RightPlayer, "Body", new Vector3((Fix64)0.4m, 0, 5)); var recorder = new Recorder(target);
            target.AddComponentImmediately(recorder); target.AddComponentImmediately(new RigidBody(target, 1)); target.AddCollider(new CircleCollider(target, (Fix64)0.5m));
            context.Spawn(Master.None, "Wall", Vector3.Zero); context.FlushObjects();
            var shot = new BoulderStrikeShot(source, 10, 5, 3, 8, (Fix64)0.65m); shot.SetTarget(target.Position); shot.OnCollisionWithEnemy(target); shot.OnCollisionWithEnemy(target);
            Assert.Equal(1, recorder.Hits.Count); Assert.True(source.IsDestroyed); Assert.Equal(1, target.GetPendingComponents<BoulderStrikeKnockback>().Length);
            target.FlushComponents(); target.Update(); var physics = new BattlePhysicsSystem(); physics.Update(context);
            Assert.Equal(2, recorder.Hits.Count); Assert.Equal(3, recorder.Hits[1].Damage); Assert.Equal(source.Id, recorder.Hits[1].AttackerId);
            var events = context.DrainEvents(); Assert.Equal(1, events.Length); Assert.Equal("BoulderStrikeImpact", events[0].Type); Assert.Equal((Fix64)0.35m, events[0].Duration); Assert.True(events[0].StartPosition.HasValue); Assert.Equal(events[0].StartPosition, events[0].EndPosition);
            target.FlushComponents(); physics.Update(context); Assert.Equal(2, recorder.Hits.Count); Assert.Equal(false, target.HasComponent<BoulderStrikeKnockback>());
        }
    }
}
