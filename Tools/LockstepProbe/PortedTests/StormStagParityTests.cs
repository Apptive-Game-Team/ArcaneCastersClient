using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class StormStagParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/storm_stag_mob.rs";
        private sealed class Health : MobComponent {
            public Health(SimObject owner, Fix64 speed) : base(owner, 100, speed) { }
            public override void OnDeath() { Owner.Destroy(); }
        }
        private sealed class PlayerMarker : SimComponent, IPlayerHealth { public PlayerMarker(SimObject owner) : base(owner) { } }
        private sealed class Recorder : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>(); public Recorder(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); }
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); }
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0) { DeltaTime = (Fix64)0.05m };
            public readonly SimObject Stag;
            public readonly StormStagMob Mob;
            public Fixture() {
                Context.RegisterPrefab("RockSlime", _ => { });
                Stag = new SimObject(Context, Master.LeftPlayer, "StormStag", Vector3.Zero); Stag.Elements.AddNative(Element.Lightning);
                Stag.AddComponentImmediately(new RigidBody(Stag, 3)); Stag.AddComponentImmediately(new MovementSpeedTracker(Stag, 4));
                Mob = new StormStagMob(Stag, 100, 4, 1, 10, 2, 2, 1); Stag.AddComponentImmediately(Mob); Mob.Start();
            }
            public SimObject Target(TargetCategory category, Fix64 x) {
                var target = Context.Spawn(Master.RightPlayer, "RockSlime", new Vector3(x, 0, 0));
                target.AddComponentImmediately(new Health(target, category == TargetCategory.Unit ? Fix64.One : Fix64.Zero));
                if (category == TargetCategory.Player) target.AddComponentImmediately(new PlayerMarker(target));
                Context.FlushObjects(); return target;
            }
            public SimObject RecordedTarget() {
                var target = new SimObject(Context, Master.RightPlayer, "RockSlime", new Vector3((Fix64)0.25m, 0, 0)); target.AddComponentImmediately(new Recorder(target)); return target;
            }
        }
        [PortedTest(Source)] public static void prioritizes_units_before_buildings_and_players_for_charge() {
            var f = new Fixture(); f.Target(TargetCategory.Player, (Fix64)0.5m); f.Target(TargetCategory.Building, 1); var unit = f.Target(TargetCategory.Unit, 5);
            Assert.True(ReferenceEquals(unit, f.Mob.ObjectiveDetector.Detect(f.Stag)));
        }
        [PortedTest(Source)] public static void treats_enemy_player_as_panic_threat_but_not_building() {
            var f = new Fixture(); f.Target(TargetCategory.Building, (Fix64)0.25m); var player = f.Target(TargetCategory.Player, 1);
            Assert.True(ReferenceEquals(player, f.Mob.ThreatDetector.Detect(f.Stag)));
        }
        [PortedTest(Source)] public static void enters_panic_immediately_after_charge_impact() {
            var f = new Fixture(); var target = f.Target(TargetCategory.Unit, 0); f.Mob.Target = target; f.Mob.PanicCooldownRemaining = 10; f.Mob.SetState(new StormStagMob.ChargeState(f.Mob));
            f.Mob.Update(); Assert.True(f.Mob.CurrentState is StormStagMob.PanicState); Assert.Equal(10, 100 - target.GetComponent<Health>().Hp);
        }
        [PortedTest(Source)] public static void damages_an_enemy_when_colliding_during_charge_and_does_not_hit_again() {
            var f = new Fixture(); var target = f.Target(TargetCategory.Unit, 5); var collided = f.RecordedTarget(); f.Mob.Target = target; f.Mob.SetState(new StormStagMob.ChargeState(f.Mob));
            f.Mob.OnCollisionWithEnemy(collided); f.Mob.Update(); f.Mob.OnCollisionWithEnemy(collided);
            Assert.Equal(1, collided.GetComponent<Recorder>().Hits.Count); Assert.True(f.Mob.CurrentState is StormStagMob.PanicState);
        }
        [PortedTest(Source)] public static void ignores_enemy_collision_outside_charge() {
            var f = new Fixture(); var target = f.RecordedTarget(); f.Mob.OnCollisionWithEnemy(target); Assert.Equal(0, target.GetComponent<Recorder>().Hits.Count);
        }
        [PortedTest(Source)] public static void resumes_charge_when_panic_flee_hits_wall() {
            var f = new Fixture(); var target = f.Target(TargetCategory.Unit, 5); var wall = new SimObject(f.Context, Master.None, "Wall", Vector3.Zero);
            f.Mob.Target = target; f.Mob.SetState(new StormStagMob.PanicState(f.Mob, target)); Assert.Equal(ObjectStatus.Hindered, f.Stag.Status);
            f.Mob.OnCollision(wall); Assert.True(f.Mob.CurrentState is StormStagMob.ChargeState); Assert.Equal((Fix64)10, f.Mob.PanicCooldownRemaining); Assert.Equal(ObjectStatus.Move, f.Stag.Status);
        }
        [PortedTest(Source)] public static void rounds_the_impact_damage_like_java() {
            Assert.Equal(13, StormStagMob.JavaRound((Fix64)12.5m)); Assert.Equal(-12, StormStagMob.JavaRound((Fix64)(-12.5m))); Assert.Equal(12, StormStagMob.JavaRound((Fix64)12.4m));
        }
        [PortedTest("tests/physic_system_wall_collision.rs")] public static void storm_stag_resumes_charge_after_panic_hits_left_wall() {
            var context = new SimulationContext(0) { DeltaTime = (Fix64)0.05m }; context.RegisterPrefab("StormStag", _ => { }); context.RegisterPrefab("Wall", _ => { });
            var stag = context.Spawn(Master.LeftPlayer, "StormStag", new Vector3((Fix64)0.4m, 0, 5));
            stag.AddComponent(new RigidBody(stag, 3)); stag.AddComponent(new MovementSpeedTracker(stag, 3));
            var mob = new StormStagMob(stag, 150, 3, 1, 20, 5, 2, 2); stag.AddComponent(mob); stag.AddCollider(new CircleCollider(stag, (Fix64)0.8m)); stag.FlushComponents(); stag.SetStatus(ObjectStatus.Idle); context.FlushObjects();
            var target = new SimObject(context, Master.RightPlayer, "RockSlime", new Vector3(9, 0, 5)); target.SetStatus(ObjectStatus.Idle); mob.Target = target;
            var threat = new SimObject(context, Master.RightPlayer, "RockSlime", new Vector3(2, 0, 5)); mob.SetState(new StormStagMob.PanicState(mob, threat));
            var wall = context.Spawn(Master.None, "Wall", Vector3.Zero); wall.AddComponent(new WallCollision(wall)); wall.AddCollider(new EdgeCollider(wall, Vector3.Zero, new Vector3(0, 0, 10))); wall.FlushComponents(); wall.SetStatus(ObjectStatus.Idle); context.FlushObjects();
            var physics = new BattlePhysicsSystem(); stag.Update(); physics.Update(context); Assert.True(mob.CurrentState is StormStagMob.ChargeState);
            for (int i = 1; i < 20; i++) { stag.Update(); physics.Update(context); }
            Assert.True(mob.CurrentState is StormStagMob.ChargeState); Assert.True(stag.Position.X > (Fix64)0.8m);
        }
    }
}
