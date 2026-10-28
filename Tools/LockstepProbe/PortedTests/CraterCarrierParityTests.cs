using System;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class CraterCarrierParityTests {
        private const string CarrierSource = "game/domain/object/component/mob/statemachine/attacker/crater_carrier_mob.rs";
        private const string ShellSource = "game/domain/object/component/magic/crater_shell.rs";
        private sealed class NoDetector : IDetector { public SimObject Detect(SimObject source, Func<SimObject, bool> filter = null) => null; }
        private sealed class Fixture {
            public readonly SimulationContext C = new SimulationContext { DeltaTime = (Fix64)0.1m };
            public readonly SimObject Owner, Enemy; public readonly CraterCarrierMob Mob;
            public Fixture() {
                C.InitializationFailed += (o, e) => throw e; C.RegisterPrefab("Object", _ => { }); CraterShellPrefabs.Register(C); C.GameParameters["crater_shell.speed"] = 6; C.GameParameters["crater_shell.radius"] = (Fix64)1.4m;
                Owner = C.Spawn(Master.LeftPlayer, "Object", new Vector3(1, 0, 5)); Enemy = C.Spawn(Master.RightPlayer, "Object", new Vector3(5, 0, 5));
                Owner.AddCollider(new CircleCollider(Owner, (Fix64)0.35m)); Enemy.AddCollider(new CircleCollider(Enemy, (Fix64)0.35m)); Owner.AddComponentImmediately(new RigidBody(Owner, 4));
                Mob = new CraterCarrierMob(Owner, 440, (Fix64)0.65m, 150, (Fix64)2.6m, 7, (Fix64)1.2m, (Fix64)0.9m); Owner.AddComponentImmediately(Mob); Mob.Detector = new NoDetector(); Mob.Target = Enemy; Mob.TargetRadius = (Fix64)0.35m; C.FlushObjects(); Mob.SetAttack();
            }
            public void Tick(int count) { for (int i = 0; i < count; i++) Mob.CurrentState.OnUpdate(); }
            public int Shells => C.PendingObjects.Count(o => o.Prefab == "CraterShell");
        }
        [PortedTest(CarrierSource)] public static void deploys_before_first_shot_then_keeps_the_normal_attack_cadence_and_restores_phase() {
            var f = new Fixture(); f.Tick(11); Assert.Equal(0, f.Shells); Assert.Equal(ObjectStatus.Deploying, f.Owner.Status); f.Tick(2); Assert.Equal(1, f.Shells); Assert.Equal(ObjectStatus.Deployed, f.Owner.Status); f.Owner.SetStatus(ObjectStatus.Damaged); f.Tick(1); Assert.Equal(ObjectStatus.Deployed, f.Owner.Status); f.Tick(27); Assert.Equal(2, f.Shells);
        }
        [PortedTest(CarrierSource)] public static void target_loss_during_setup_cancels_shot_and_repeated_move_requests_cannot_skip_or_reset_packing() {
            var f = new Fixture(); f.Tick(3); f.Enemy.Destroy(); f.Tick(1); Assert.Equal(ObjectStatus.Recovering, f.Owner.Status);
            for (int i = 0; i < 8; i++) { f.Mob.SetMove(); f.Tick(1); Assert.Equal(ObjectStatus.Recovering, f.Owner.Status); } f.Tick(2); Assert.Equal(ObjectStatus.Idle, f.Owner.Status); Assert.Equal(0, f.Shells);
        }
        [PortedTest(CarrierSource)] public static void leaving_attack_range_requires_packing_before_walking() {
            var f = new Fixture(); f.Tick(13); f.Enemy.SetPosition(new Vector3(17, 0, 0)); f.Tick(1); Assert.Equal(ObjectStatus.Recovering, f.Owner.Status); Assert.Equal(Vector3.Zero, f.Owner.GetComponent<RigidBody>().Velocity);
        }
        [PortedTest(CarrierSource)] public static void stun_blocks_directive_requests_and_still_requires_packing_on_expiry() {
            var f = new Fixture(); f.Tick(3); f.Mob.SetStun((Fix64)0.5m); var state = f.Mob.CurrentState;
            for (int i = 0; i < 4; i++) { f.Mob.SetMove(); f.Tick(1); Assert.Equal(state, f.Mob.CurrentState); } f.Tick(3); Assert.Equal(ObjectStatus.Recovering, f.Owner.Status); f.Tick(10); Assert.Equal(ObjectStatus.Idle, f.Owner.Status); Assert.Equal(0, f.Shells);
        }
        [PortedTest(CarrierSource)] public static void ownership_reset_also_packs_instead_of_resuming_an_old_target() { var f = new Fixture(); f.Tick(3); f.Mob.Start(); Assert.Equal(ObjectStatus.Recovering, f.Owner.Status); f.Tick(10); Assert.Equal(0, f.Shells); }
        [PortedTest(CarrierSource)] public static void lethal_delayed_damage_cannot_launch_a_shot_on_the_same_update() { var f = new Fixture(); f.Tick(11); f.Owner.Destroy(); f.Tick(2); Assert.Equal(0, f.Shells); }
        [PortedTest(ShellSource)] public static void waits_for_landing_then_hits_only_ground_enemies_once_even_after_shooter_dies() {
            var f = new Fixture(); var c = f.C; c.DeltaTime = (Fix64)0.25m; f.Owner.Elements.AddNative(Element.Fire); f.Owner.Elements.AddNative(Element.Rock);
            DummyMob Target(Master side, Fix64 height) { var o = c.Spawn(side, "Object", new Vector3(6, height, 0)); o.AddCollider(new CircleCollider(o, (Fix64)0.3m)); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); return mob; }
            var enemy = Target(Master.RightPlayer, 0); var ally = Target(Master.LeftPlayer, 0); var air = Target(Master.RightPlayer, 2); var dead = Target(Master.RightPlayer, 0);
            var shell = c.Spawn(Master.LeftPlayer, "CraterShell", Vector3.Zero); var flight = shell.GetComponent<CraterShell>(); c.FlushObjects(); dead.Owner.Destroy(); flight.Launch(new Vector3(6, 0, 0), 40, f.Owner); f.Owner.Destroy();
            for (int i = 0; i < 3; i++) flight.Update(); Assert.Equal(100, enemy.Hp); Assert.True(shell.Position.Y > 0); flight.Update(); flight.Update(); Assert.Equal(new Vector3(6, 0, 0), shell.Position);
            Assert.Equal(60, enemy.Hp); Assert.Equal(100, ally.Hp); Assert.Equal(100, air.Hp); Assert.Equal(100, dead.Hp); Assert.True(shell.IsDestroyed); Assert.Equal(1, c.PendingObjects.Count(o => o.Prefab == "CraterExplosion"));
        }
        [PortedTest(ShellSource)] public static void zero_distance_shot_still_has_finite_flight_and_single_impact() {
            var f = new Fixture(); var c = f.C; c.DeltaTime = (Fix64)0.05m; var shell = c.Spawn(Master.LeftPlayer, "CraterShell", Vector3.Zero); var flight = shell.GetComponent<CraterShell>(); c.FlushObjects(); flight.Launch(Vector3.Zero, 40, f.Owner);
            flight.Update(); Assert.True(!shell.IsDestroyed); flight.Update(); flight.Update(); Assert.True(shell.IsDestroyed); Assert.Equal(1, c.PendingObjects.Count(o => o.Prefab == "CraterExplosion"));
        }
        public static void CheckActualCarrier() {
            // 20Hz/10Hz 모두 설치 경계 직전에는 발사하지 않고 경계 tick에 첫 발을 생성한다.
            foreach (int ticks in new[] { 12, 24 }) {
                var boundary = new Fixture(); boundary.C.DeltaTime = (Fix64)1.2m / ticks; boundary.Tick(ticks - 1); Assert.Equal(0, boundary.Shells); boundary.Tick(1); Assert.Equal(1, boundary.Shells); Assert.Equal(ObjectStatus.Attack, boundary.Owner.Status);
                boundary.Enemy.Destroy(); boundary.Tick(1); boundary.C.DeltaTime = (Fix64)0.1m; boundary.Tick(8); Assert.Equal(ObjectStatus.Recovering, boundary.Owner.Status); boundary.Tick(1); Assert.Equal(ObjectStatus.Idle, boundary.Owner.Status);
            }
            var beforeBoundary = new Fixture(); beforeBoundary.C.DeltaTime = (Fix64)1.2m / 12 - Fix64.FromRaw(2); beforeBoundary.Tick(12); Assert.Equal(0, beforeBoundary.Shells);
            beforeBoundary.C.DeltaTime = Fix64.Zero; beforeBoundary.Tick(100); Assert.Equal(0, beforeBoundary.Shells);
            var f = new Fixture(); var c = f.C; CraterCarrierMob.RegisterPrefab(c);
            c.GameParameters["crater_carrier.mass"] = 4; c.GameParameters["crater_carrier.radius"] = (Fix64)0.35m; c.GameParameters["crater_carrier.hp"] = 440; c.GameParameters["crater_carrier.speed"] = (Fix64)0.65m;
            c.GameParameters["crater_carrier.damage"] = 40; c.GameParameters["crater_carrier.attack_interval"] = (Fix64)2.6m; c.GameParameters["crater_carrier.attack_range"] = 7; c.GameParameters["crater_carrier.setup_duration"] = (Fix64)1.2m; c.GameParameters["crater_carrier.recovery_duration"] = (Fix64)0.9m;
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); registry.GetBean("crater_carrier").Bind(1, "crater_carrier", Element.Fire).Run(c, Master.LeftPlayer, null, new Vector3(1, 0, 5));
            var carrier = c.PendingObjects[0]; Assert.True(carrier.HasComponent<CommonEffectReceiver>() && carrier.HasComponent<RockDeathRemnant>()); var mob = carrier.GetComponent<CraterCarrierMob>();
            var target = new DummyMob(f.Enemy, 100); f.Enemy.AddComponentImmediately(target); c.FlushObjects(); mob.Target = f.Enemy; mob.TargetRadius = (Fix64)0.35m; mob.SetAttack();
            for (int i = 0; i < 14; i++) mob.Update(); Assert.Equal(1, c.PendingObjects.Count(o => o.Prefab == "CraterShell")); var shell = c.PendingObjects.Single(o => o.Prefab == "CraterShell"); Assert.Equal((Fix64)0.8m, shell.Position.Y);
            carrier.Destroy(); c.FlushObjects(); c.DeltaTime = 1; shell.Update(); Assert.Equal(60, target.Hp); Assert.True(shell.IsDestroyed);
            var explosion = c.PendingObjects.Single(o => o.Prefab == "CraterExplosion"); Assert.Equal(1, explosion.Components.Length); Assert.Equal(0, explosion.Colliders.Length); explosion.Update(); Assert.True(explosion.IsDestroyed); Assert.Equal(60, target.Hp);
        }
    }
}
