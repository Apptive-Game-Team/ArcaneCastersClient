using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class OverchargeParityTests {
        private const string Effect = "game/domain/object/component/effect/statuseffect/overcharge_status_effect.rs";
        private const string Receiver = "game/domain/object/component/effect/receiver/lightning_summon_effect_receiver.rs";
        private sealed class TestMob : MobComponent { public TestMob(SimObject owner) : base(owner, 10, 10) { } public override void OnDeath() { Owner.Destroy(); } }
        private sealed class Recorder : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>(); public readonly List<Fix64> Delays = new List<Fix64>();
            public Recorder(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); Delays.Add(Fix64.Zero); }
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); Delays.Add(delay); }
        }
        private static SimObject Owner(SimulationContext context = null) {
            var owner = new SimObject(context ?? new SimulationContext(0), Master.LeftPlayer, "DragonFlame", Vector3.Zero);
            owner.AddComponentImmediately(new TestMob(owner)); return owner;
        }
        private static LightningSummonEffectReceiver Summon(out SimObject owner) {
            owner = Owner(); owner.Elements.AddNative(Element.Lightning); return new LightningSummonEffectReceiver(owner);
        }
        [PortedTest(Effect)] public static void increases_movement_speed_by_fifty_percent_until_expiration() {
            var owner = Owner(); owner.Context.DeltaTime = (Fix64)1.1m; var mob = owner.GetComponent<TestMob>(); var effect = new OverchargeStatusEffect(owner, 1);
            owner.AddComponentImmediately(effect); Assert.True(owner.HasEffect(VisualEffect.Overcharge)); effect.Start(); Assert.Equal((Fix64)15, mob.Speed.Total);
            effect.Update(); Assert.Equal((Fix64)10, mob.Speed.Total); Assert.Equal(false, owner.HasEffect(VisualEffect.Overcharge)); owner.FlushComponents(); Assert.Equal(false, owner.HasComponent<OverchargeStatusEffect>());
        }
        [PortedTest(Effect)] public static void multiplies_already_modified_movement_speed_by_exactly_one_point_five() {
            var owner = Owner(); owner.Context.DeltaTime = (Fix64)1.1m; var mob = owner.GetComponent<TestMob>(); mob.Speed.ModifierPercent = -(Fix64)0.2m;
            // 0.2의 Q32.32 표현 오차가 원래 속도 10과 배율 1.5를 거친 상한만 허용한다.
            var effect = new OverchargeStatusEffect(owner, 1); effect.Start(); Assert.Near((Fix64)12, mob.Speed.Total, Fix64.FromRaw(16));
            effect.Update(); Assert.Near((Fix64)8, mob.Speed.Total, Fix64.FromRaw(10));
        }
        private static SimObject Shooter(SimulationContext context, Fix64 targetX, out Recorder recorder) {
            context.RegisterPrefab("DragonFlame", _ => { }); context.GameParameters["electric_shot.damage"] = 4;
            var owner = context.Spawn(Master.LeftPlayer, "DragonFlame", Vector3.Zero); owner.AddComponentImmediately(new TestMob(owner)); owner.SetStatus(ObjectStatus.Idle);
            var target = context.Spawn(Master.RightPlayer, "DragonFlame", new Vector3(targetX, 0, 0)); target.SetStatus(ObjectStatus.Idle);
            recorder = new Recorder(target); target.AddComponentImmediately(recorder); context.FlushObjects(); return owner;
        }
        [PortedTest(Effect)] public static void emits_electric_shot_projectile_every_second_and_applies_configured_damage() {
            var context = new SimulationContext(0) { DeltaTime = (Fix64)0.5m }; var owner = Shooter(context, 2, out var recorder); var effect = new OverchargeStatusEffect(owner, 2); effect.Start();
            effect.Update(); Assert.Equal(0, context.DrainEvents().Length); Assert.Equal(0, recorder.Hits.Count);
            effect.Update(); var events = context.DrainEvents(); Assert.Equal(1, events.Length); Assert.Equal("ElectricShot", events[0].Type); Assert.Equal((Fix64)0.2m, events[0].Duration); Assert.Equal(owner.Id, events[0].SourceId);
            Assert.Equal(1, recorder.Hits.Count); Assert.Equal(4, recorder.Hits[0].Damage); Assert.Equal(0, recorder.Hits[0].AttackerId); Assert.Equal(Element.Lightning, recorder.Hits[0].Elements[0]); Assert.Equal((Fix64)0.2m, recorder.Delays[0]);
        }
        [PortedTest(Effect)] public static void does_not_emit_electric_shot_beyond_range_two() {
            var context = new SimulationContext(0) { DeltaTime = 1 }; var owner = Shooter(context, (Fix64)2.01m, out var recorder); var effect = new OverchargeStatusEffect(owner, 2); effect.Start(); effect.Update();
            Assert.Equal(0, context.DrainEvents().Length); Assert.Equal(0, recorder.Hits.Count);
        }
        [PortedTest(Effect)] public static void reapplication_extends_pending_effect_without_adding_another_component() {
            var owner = Owner(); var mob = owner.GetComponent<TestMob>(); var effect = new OverchargeStatusEffect(owner, 1); owner.AddComponent(effect); effect.Start();
            OverchargeStatusEffect.Apply(owner, 1); owner.Context.DeltaTime = (Fix64)1.5m; effect.Update(); Assert.Equal((Fix64)15, mob.Speed.Total); Assert.Equal(1, owner.PendingComponents.Length);
            owner.FlushComponents(); Assert.True(owner.HasComponent<OverchargeStatusEffect>()); owner.Context.DeltaTime = (Fix64)0.6m; effect.Update(); Assert.Equal((Fix64)10, mob.Speed.Total);
            owner.FlushComponents(); Assert.Equal(false, owner.HasComponent<OverchargeStatusEffect>());
        }
        [PortedTest(Effect)] public static void reapplication_cancels_pending_removal_and_reactivates_the_same_component() {
            var owner = Owner(); var effect = new OverchargeStatusEffect(owner, 1); owner.AddComponentImmediately(effect); owner.RemoveComponent(effect); effect.Start(); effect.OnDestroy();
            OverchargeStatusEffect.Apply(owner, 1); Assert.Equal((Fix64)15, owner.GetComponent<TestMob>().Speed.Total); Assert.Equal(0, owner.PendingComponents.Length); owner.FlushComponents(); Assert.True(owner.HasComponent<OverchargeStatusEffect>());
        }
        [PortedTest(Receiver)] public static void shock_applies_overcharge_instead_of_stun() {
            var receiver = Summon(out _); receiver.Receive(VisualEffect.Shock); Assert.True(receiver.GetEffectByKey(StatusEffectKey.ShockReceive) == null); Assert.True(receiver.GetEffectByKey(StatusEffectKey.OverchargeReceive) != null);
        }
        [PortedTest(Receiver)] public static void restacking_shock_extends_overcharge_duration() {
            var receiver = Summon(out _); receiver.Receive(VisualEffect.Shock); receiver.Receive(VisualEffect.Shock); Assert.Equal((Fix64)6, receiver.GetEffectByKey(StatusEffectKey.OverchargeReceive).Remaining);
        }
        private static LightningSummonEffectReceiver ApplyShock(Master master) {
            var receiver = Summon(out var owner); var source = new SimObject(owner.Context, master, "ElectricShot", Vector3.Zero); receiver.Receive(new EffectApplication(VisualEffect.Shock, source)); return receiver;
        }
        [PortedTest(Receiver)] public static void enemy_shock_does_not_overcharge_the_summon() {
            var receiver = ApplyShock(Master.RightPlayer); Assert.True(receiver.GetEffectByKey(StatusEffectKey.OverchargeReceive) == null); Assert.True(receiver.GetEffectByKey(StatusEffectKey.ShockReceive) == null);
        }
        [PortedTest(Receiver)] public static void friendly_shock_overcharges_the_summon() { Assert.True(ApplyShock(Master.LeftPlayer).GetEffectByKey(StatusEffectKey.OverchargeReceive) != null); }
        [PortedTest(Receiver)] public static void neutral_field_shock_overcharges_either_side() { Assert.True(ApplyShock(Master.None).GetEffectByKey(StatusEffectKey.OverchargeReceive) != null); }
        [PortedTest(Receiver)] public static void common_receiver_keeps_shock_immunity_for_lightning_units() {
            Summon(out var owner); var receiver = new CommonEffectReceiver(owner); receiver.Receive(VisualEffect.Shock); Assert.True(receiver.GetEffectByKey(StatusEffectKey.ShockReceive) == null); Assert.True(receiver.GetEffectByKey(StatusEffectKey.OverchargeReceive) == null);
        }
        [PortedTest(Receiver)] public static void a_duration_call_reaches_the_override_like_a_virtual_call() { var receiver = Summon(out _); receiver.Receive(VisualEffect.Shock, 9); Assert.True(receiver.GetEffectByKey(StatusEffectKey.OverchargeReceive) != null); }
    }
}
