using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class EvilEntParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/evil_ent_mob.rs";
        [PortedTest("game/domain/object/prefab/implement/misc/third/evil_ent_prefab_initializer.rs")] public static void builds_a_ranged_nature_fire_ent_from_the_evil_ent_parameters() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; EvilEntPrefabs.Register(c);
            string[] keys = { "mass", "radius", "hp", "speed", "damage", "attack_interval", "attack_range", "projectile_speed", "sub_damage", "sub_attack_range", "sub_attack_interval", "pull_mass_limit" };
            decimal[] values = { 10, 1.2m, 180, 0.45m, 9, 1.8m, 5, 14, 28, 6, 12, 5 }; for (int i = 0; i < keys.Length; i++) c.GameParameters["evil_ent." + keys[i]] = (Fix64)values[i];
            var ent = c.Spawn(Master.LeftPlayer, "EvilEnt", new Vector3(5, 0, 5)); var mob = ent.GetComponent<EvilEntMob>(); Assert.Equal(10, ent.GetComponent<RigidBody>().Mass); Assert.True(ent.HasComponent<ZPhysics>() && ent.HasComponent<CommonEffectReceiver>());
            Assert.Equal(180, mob.MaxHp); Assert.Equal((Fix64)0.45m, mob.Speed.Total); Assert.Equal(9, mob.Damage); Assert.Equal((Fix64)14, mob.ProjectileSpeed); Assert.Equal(28, mob.SubDamage); Assert.Equal((Fix64)6, mob.SubAttackRange); Assert.Equal((Fix64)12, mob.SubAttackInterval); Assert.Equal((Fix64)5, mob.PullMassLimit);
            Assert.Equal((Fix64)1.2m, ent.Colliders.Single().CircleRadius.Value); Assert.True(!ent.Colliders.Single().IsTrigger); Assert.True(ent.Elements.NativeHas(Element.Nature) && ent.Elements.NativeHas(Element.Fire));
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext { DeltaTime = (Fix64)0.1m };
            public readonly SimObject Victim; public readonly BehaviorMob VictimMob; public readonly EvilEntMob Ent;
            public readonly List<BattleEvent> Events = new List<BattleEvent>();
            public Fixture(int? mass = 1, int y = 0, Fix64? cooldown = null) {
                Context.RegisterPrefab("Object", _ => { }); Victim = Context.Spawn(Master.RightPlayer, "Object", new Vector3(5, y, 0)); Victim.AddCollider(new CircleCollider(Victim, (Fix64)0.5m));
                VictimMob = new BehaviorMob(Victim, 1000, 0, 3, 0, 0, null); Victim.AddComponentImmediately(VictimMob); if (mass.HasValue) Victim.AddComponentImmediately(new RigidBody(Victim, mass.Value));
                var owner = Context.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); owner.AddCollider(new CircleCollider(owner, (Fix64)1.2m)); owner.AddComponentImmediately(new RigidBody(owner, 10));
                Ent = new EvilEntMob(owner, 180, (Fix64)0.45m, 3, 9, (Fix64)1.8m, 5, 14, 28, 6, cooldown ?? (Fix64)0.1m, 5); owner.AddComponentImmediately(Ent); Ent.Target = Victim; Context.FlushObjects();
            }
            public void Tick() { Ent.Update(); Events.AddRange(Context.DrainEvents()); }
            public bool Until(Func<bool> condition) { for (int i = 0; i < 60; i++) { Tick(); if (condition()) return true; } return false; }
            public bool Has(string type) => Events.Any(e => e.Type == type);
            public bool Stunned => VictimMob.CurrentState is BehaviorMob.StunState;
        }
        [PortedTest(Source)] public static void grab_arm_stuns_and_smoothly_moves_a_light_victim_to_the_ent_front_before_the_fire_fist() {
            var f = new Fixture(); Assert.True(f.Until(() => f.Stunned)); Assert.Equal((Fix64)5, f.Victim.Position.X); f.Tick(); Assert.True(f.Victim.Position.X >= (Fix64)1.7m && f.Victim.Position.X <= 5);
            Assert.True(f.Until(() => f.Ent.CurrentState is EvilEntMob.FistState)); Assert.Near((Fix64)1.7m, f.Victim.Position.X, (Fix64)0.0001m); Assert.Equal(Fix64.Zero, f.Victim.Position.Y); Assert.Equal(Fix64.Zero, f.Victim.Position.Z); Assert.True(f.Stunned); f.Tick(); Assert.True(f.Has("EvilEntGrabArm") && f.Has("EvilEntFireFist"));
        }
        [PortedTest(Source)] public static void grab_arm_moves_an_aerial_victim_to_the_ent_front() { var f = new Fixture(y: 2); Assert.True(f.Until(() => f.Ent.CurrentState is EvilEntMob.FistState)); Assert.Near((Fix64)1.7m, f.Victim.Position.X, (Fix64)0.0001m); Assert.Near(Fix64.Zero, f.Victim.Position.Y, (Fix64)0.0001m); Assert.True(f.Stunned); }
        [PortedTest(Source)] public static void pull_charges_recover_one_per_interval_and_cap_at_three() { var f = new Fixture(); f.Victim.Destroy(); for (int i = 0; i < 40; i++) f.Tick(); Assert.Equal(3, f.Ent.PullCharges); }
        [PortedTest(Source)] public static void saved_pull_charges_can_start_consecutive_grab_sequences() { var f = new Fixture(cooldown: 10); f.Ent.PullCharges = 3; f.Tick(); Assert.Equal(2, f.Ent.PullCharges); f.Ent.SetIdle(); f.Tick(); Assert.True(f.Ent.CurrentState is EvilEntMob.GrabState); Assert.Equal(1, f.Ent.PullCharges); }
        [PortedTest(Source)] public static void fire_fist_deals_the_configured_sub_damage() { var f = new Fixture(); Assert.True(f.Until(() => f.VictimMob.Hp < 1000)); Assert.Equal(972, f.VictimMob.Hp); }
        [PortedTest(Source)] public static void arm_impact_fractions_mirror_the_client_motion_table() { Assert.Equal((Fix64)0.55m, EvilEntMob.PunchImpactFraction); Assert.Equal((Fix64)0.22m, EvilEntMob.GrabImpactFraction); Assert.Equal((Fix64)0.45m, EvilEntMob.FistImpactFraction); }
        [PortedTest(Source)] public static void grab_arm_projectile_stays_out_for_the_whole_drag() {
            var f = new Fixture(); f.Tick(); f.Tick(); var reach = ((EvilEntMob.GrabState)f.Ent.CurrentState).ReachTime; var duration = f.Events.Single(e => e.Type == "EvilEntGrabArm").Duration;
            Assert.Near(reach, duration * EvilEntMob.GrabImpactFraction, (Fix64)0.0001m); Assert.True(duration >= reach + (Fix64)0.8m);
        }
        private static void Punches(int? mass) { var f = new Fixture(mass); f.Ent.Start(); Assert.True(f.Until(() => f.Has("EvilEntPunchArm"))); Assert.True(!f.Has("EvilEntGrabArm") && !f.Has("EvilEntFireFist")); }
        [PortedTest(Source)] public static void golem_mass_victim_never_opens_a_grab_and_is_punched_instead() => Punches(10);
        [PortedTest(Source)] public static void immovable_victim_never_opens_a_grab() => Punches(-1);
        [PortedTest(Source)] public static void victim_without_a_rigid_body_never_opens_a_grab() => Punches(null);
        [PortedTest(Source)] public static void grab_that_loses_its_drag_aborts_without_a_fire_fist() {
            var f = new Fixture(cooldown: 30); f.Ent.PullCharges = 1; f.Tick(); f.Tick(); Assert.True(f.Ent.CurrentState is EvilEntMob.GrabState); f.Victim.RemoveComponent(f.Victim.GetComponent<RigidBody>()); f.Victim.FlushComponents();
            Assert.True(f.Until(() => f.Ent.CurrentState is BehaviorMob.IdleState)); Assert.True(f.Has("EvilEntGrabArm") && !f.Has("EvilEntFireFist")); Assert.Equal(0, f.Ent.PullCharges);
        }
        [PortedTest(Source)] public static void destroyed_victim_aborts_the_grab_sequence_without_a_fire_fist() { var f = new Fixture(); f.Tick(); f.Tick(); f.Victim.Destroy(); f.Tick(); Assert.True(f.Ent.CurrentState is BehaviorMob.IdleState); Assert.True(!f.Has("EvilEntFireFist")); }
        [PortedTest(Source)] public static void state_stolen_mid_sequence_lets_the_special_recover() {
            var f = new Fixture(cooldown: 1); f.Ent.PullCharges = 1; f.Tick(); Assert.True(f.Ent.CurrentState is EvilEntMob.GrabState); f.Tick(); Assert.Near((Fix64)0.2m, f.Ent.PullRechargeTimer, (Fix64)0.0001m); f.Ent.SetIdle(); f.Tick(); Assert.Near((Fix64)0.3m, f.Ent.PullRechargeTimer, (Fix64)0.0001m); Assert.True(f.Until(() => f.Ent.CurrentState is EvilEntMob.GrabState));
        }
        [PortedTest(Source)] public static void aborted_grab_consumes_only_the_charge_it_started_with() { var f = new Fixture(cooldown: 30); f.Ent.PullCharges = 1; f.Tick(); f.Victim.Destroy(); f.Tick(); Assert.Equal(0, f.Ent.PullCharges); }
    }
}
