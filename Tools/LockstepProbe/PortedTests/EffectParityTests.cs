using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class EffectParityTests {
        [PortedTest("game/domain/object/component/interval_attacker.rs")] public static void a_paced_component_is_found_through_the_interface() {
            var owner = new SimObject(new SimulationContext(),Master.LeftPlayer,"DragonFlame",Vector3.Zero);
            var paced = new TestBehavior(owner); owner.AddComponentImmediately(paced);
            var found = owner.GetComponents<IIntervalAttacker>(); Assert.Equal(1,found.Length);
            found[0].AttackInterval.ModifierPercent = (Fix64)(-0.5m);
            Assert.Equal((Fix64)0.5m,found[0].AttackInterval.Total);
            Assert.Equal((Fix64)0.5m,paced.AttackInterval.Total);
        }
        private const string Burn = "game/domain/object/component/effect/statuseffect/burn_status_effect.rs";
        private const string Dot = "game/domain/object/component/effect/statuseffect/dot_status_effect.rs";
        private const string Stun = "game/domain/object/component/effect/statuseffect/stun_status_effect.rs";
        private const string Frenzy = "game/domain/object/component/effect/statuseffect/frenzy_status_effect.rs";
        private const string Receiver = "game/domain/object/component/effect/receiver/common_effect_receiver.rs";
        private const string Knockback = "game/domain/object/component/effect/statuseffect/knockback_status_effect.rs";
        private sealed class TestMob : MobComponent {
            public TestMob(SimObject owner, int hp, Fix64? speed = null) : base(owner, hp, speed ?? Fix64.One) { }
            public override void OnDeath() { Owner.Destroy(); }
        }
        private sealed class TestBehavior : SimComponent, IStunnableBehavior {
            public readonly List<Fix64> Stuns = new List<Fix64>();
            public int Idles;
            public Stat AttackInterval { get; } = new Stat(Fix64.One);
            public TestBehavior(SimObject owner) : base(owner) { }
            public void SetStun(Fix64 duration) { Stuns.Add(duration); }
            public void SetIdle() { Idles++; }
        }
        private static SimObject Owner() => new SimObject(new SimulationContext(0), Master.LeftPlayer, "DragonFlame", Vector3.Zero);
        [PortedTest(Burn)]
        public static void wet_and_burn_cancel_each_other() {
            var owner = Owner(); var wet = new WetStatusEffect(owner, 5, StatusEffectKey.WetReceive);
            owner.AddComponentImmediately(wet); wet.Start(); Assert.True(owner.Elements.Has(Element.Water));
            var burn = new BurnStatusEffect(owner, 5, StatusEffectKey.BurnReceive); owner.AddComponentImmediately(burn); burn.Start();
            Assert.Equal(false, owner.Elements.Has(Element.Water)); Assert.Equal(false, owner.HasEffect(VisualEffect.Wet)); Assert.Equal(false, owner.HasEffect(VisualEffect.Burn));
            owner.FlushComponents(); Assert.Equal(0, owner.Components.Length);
        }
        [PortedTest(Burn)]
        public static void a_status_effect_expires_when_its_duration_runs_out() {
            var owner = Owner(); owner.Context.DeltaTime = (Fix64)0.5m;
            var burn = new BurnStatusEffect(owner, 1, StatusEffectKey.BurnReceive); owner.AddComponentImmediately(burn);
            burn.Update(); Assert.Equal((Fix64)0.5m, burn.Remaining); Assert.True(owner.HasEffect(VisualEffect.Burn));
            burn.Update(); Assert.Equal(false, owner.HasEffect(VisualEffect.Burn)); owner.FlushComponents(); Assert.Equal(false, owner.HasComponent<BurnStatusEffect>());
        }
        [PortedTest(Dot)]
        public static void deals_the_total_amount_over_the_duration() {
            var owner = Owner(); owner.Context.DeltaTime = (Fix64)0.5m; owner.AddComponentImmediately(new TestMob(owner, 100));
            var dot = new DotStatusEffect(owner, 2, 4, Element.Fire, StatusEffectKey.DotDealBurn);
            for (int i = 0; i < 4; i++) dot.Update();
            owner.FlushComponents(); Assert.Equal(false, owner.HasComponent<DotStatusEffect>());
            var finisher = new DotStatusEffect(owner, (Fix64)0.5m, 100, Element.None, StatusEffectKey.DotSandStorm); finisher.Update(); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(Stun)]
        public static void shock_stuns_locks_hover_and_releases_on_expiry() {
            var owner = Owner(); var behavior = new TestBehavior(owner); owner.AddComponentImmediately(behavior);
            var physics = new ZPhysics(owner, 3); owner.AddComponentImmediately(physics);
            var shock = new ShockStatusEffect(owner, 2, StatusEffectKey.ShockReceive); owner.AddComponentImmediately(shock); Assert.True(owner.HasEffect(VisualEffect.Shock));
            shock.Start(); Assert.Sequence(new Fix64[] { 2 }, behavior.Stuns); Assert.Equal(false, physics.CanHover);
            shock.Expire(); Assert.Equal(1, behavior.Idles); Assert.True(physics.CanHover); Assert.Equal(false, owner.HasEffect(VisualEffect.Shock));
            owner.FlushComponents(); Assert.Equal(false, owner.HasComponent<StunStatusEffect>());
        }
        [PortedTest(Stun)]
        public static void gravity_multiplier_is_restored_on_expiry() {
            var owner = Owner(); var physics = new ZPhysics(owner, 3); owner.AddComponentImmediately(physics); var original = physics.Gravity;
            var stun = new StunStatusEffect(owner, 2, StatusEffectKey.TrapStunReceive, VisualEffect.Shock, 2);
            stun.Start(); stun.Start(); Assert.Equal(original * 2, physics.Gravity); stun.Expire(); Assert.Equal(original, physics.Gravity);
        }
        [PortedTest(Frenzy)]
        public static void takes_the_master_away_and_restores_it() {
            var owner = Owner(); owner.AddComponentImmediately(new TestMob(owner, 10));
            var frenzy = new FrenzyStatusEffect(owner, 10, StatusEffectKey.FrenzyReceive); owner.AddComponentImmediately(frenzy);
            frenzy.Start(); Assert.Equal(Master.None, owner.Master); Assert.True(owner.GetComponent<FrenzyStatusEffect>().IsFrenzied);
            frenzy.Expire(); Assert.Equal(Master.LeftPlayer, owner.Master); Assert.Equal(false, frenzy.IsFrenzied);
        }
        [PortedTest(Frenzy)]
        public static void an_object_that_is_not_a_controllable_summon_drops_the_effect() {
            var owner = Owner(); var frenzy = new FrenzyStatusEffect(owner, 10, StatusEffectKey.FrenzyReceive); owner.AddComponentImmediately(frenzy);
            frenzy.Start(); owner.FlushComponents(); Assert.Equal(Master.LeftPlayer, owner.Master); Assert.Equal(false, owner.HasComponent<FrenzyStatusEffect>());
        }
        private static SimObject Tuned(Element element = Element.None) {
            var owner = Owner(); owner.Elements.AddNative(element);
            string[] keys = { "burn_duration", "burn_total_damage", "wet_duration", "wet_nature_heal", "shock_stun_duration", "shock_refresh_duration", "snare_duration", "snare_fire_damage", "snare_slow_percent", "leaf_field_heal_duration", "leaf_field_heal_amount", "sandstorm_effect_duration", "sandstorm_effect_damage", "building_snare_heal" };
            decimal[] values = { 7, 9, 6, 4, 2, 5, 8, 11, 0.25m, 12, 6, 1.5m, 4, 3 };
            for (int i = 0; i < keys.Length; i++) owner.Context.GameParameters.Add("game." + keys[i], (Fix64)values[i]);
            return owner;
        }
        [PortedTest(Receiver)]
        public static void ordinary_effects_read_tuned_values() {
            var receiver = new CommonEffectReceiver(Tuned(Element.Nature)); receiver.Receive(VisualEffect.Burn);
            Assert.Equal((Fix64)7, receiver.GetEffectByKey(StatusEffectKey.BurnReceive).Remaining); Assert.Equal((Fix64)7, receiver.GetEffectByKey(StatusEffectKey.DotDealBurn).Remaining);
            receiver.Receive(VisualEffect.Wet); Assert.Equal((Fix64)6, receiver.GetEffectByKey(StatusEffectKey.WetReceive).Remaining); Assert.Equal((Fix64)6, receiver.GetEffectByKey(StatusEffectKey.DotHealNatureWithWaterField).Remaining);
            receiver.Receive(VisualEffect.LeafFieldHeal); Assert.Equal((Fix64)12, receiver.GetEffectByKey(StatusEffectKey.DotHealNatureField).Remaining);
            receiver.Receive(VisualEffect.Sandstorm); Assert.Equal((Fix64)1.5m, receiver.GetEffectByKey(StatusEffectKey.DotSandStorm).Remaining);
        }
        [PortedTest(Receiver)]
        public static void control_effects_read_tuned_values() {
            var receiver = new CommonEffectReceiver(Tuned()); receiver.Receive(VisualEffect.Shock); Assert.Equal((Fix64)2, receiver.GetEffectByKey(StatusEffectKey.ShockReceive).Remaining);
            receiver.Receive(VisualEffect.Snared); Assert.Equal((Fix64)8, receiver.GetEffectByKey(StatusEffectKey.SnaredReceive).Remaining);
        }
        [PortedTest(Receiver)]
        public static void building_snare_heal_reads_tuned_value() {
            var receiver = new BuildingEffectReceiver(Tuned()); receiver.Receive(VisualEffect.Snared); Assert.Equal((Fix64)8, receiver.GetEffectByKey(StatusEffectKey.SnaredReceive).Remaining);
        }
        [PortedTest(Receiver)]
        public static void reapplying_refreshes_the_duration_and_knockback_is_ignored_while_active() {
            var receiver = new CommonEffectReceiver(Tuned()); receiver.Receive(VisualEffect.Burn); receiver.GetEffectByKey(StatusEffectKey.BurnReceive).Extend(3);
            receiver.Receive(VisualEffect.Burn); Assert.Equal((Fix64)7, receiver.GetEffectByKey(StatusEffectKey.BurnReceive).Remaining);
            receiver.ReceiveDirectional(VisualEffect.Knockback, Vector3.Right, 1); var first = receiver.GetEffectByKey(StatusEffectKey.KnockbackReceive);
            receiver.ReceiveDirectional(VisualEffect.Knockback, Vector3.Left, 1); Assert.True(ReferenceEquals(first, receiver.GetEffectByKey(StatusEffectKey.KnockbackReceive)));
        }
        [PortedTest(Receiver)]
        public static void immune_elements_block_the_matching_effect() {
            var receiver = new CommonEffectReceiver(Tuned(Element.Fire)); receiver.Receive(VisualEffect.Burn); Assert.True(receiver.GetEffectByKey(StatusEffectKey.BurnReceive) == null);
            receiver.Receive(VisualEffect.Wet); Assert.True(receiver.GetEffectByKey(StatusEffectKey.WetReceive) != null);
        }
        [PortedTest(Receiver)]
        public static void a_frenzy_with_a_duration_uses_that_duration() {
            var receiver = new CommonEffectReceiver(Tuned()); receiver.Receive(VisualEffect.Frenzy, 4); Assert.Equal((Fix64)4, receiver.GetEffectByKey(StatusEffectKey.FrenzyReceive).Remaining);
        }
        [PortedTest("game/domain/object/component/effect/statuseffect/bubble_status_effect.rs")]
        public static void absorbs_one_damaging_hit_and_pops() {
            var owner = Owner(); var bubble = new BubbleStatusEffect(owner, 8, StatusEffectKey.BubbleReceive); owner.AddComponentImmediately(bubble);
            Assert.Equal(false, bubble.BeforeDamage(new Hit(0, 0))); Assert.True(owner.HasEffect(VisualEffect.Bubble));
            Assert.True(bubble.BeforeDamage(new Hit(3, 0))); Assert.Equal(false, owner.HasEffect(VisualEffect.Bubble)); Assert.Equal(1, owner.GetComponents<IDamageInterceptor>().Length);
        }
        [PortedTest(Knockback)]
        public static void moves_and_lifts_by_inverse_square_root_of_mass() {
            var owner = Owner(); owner.Context.DeltaTime = (Fix64)0.1m; var body = new RigidBody(owner, 10); owner.AddComponentImmediately(body); owner.AddComponentImmediately(new ZPhysics(owner));
            var effect = new KnockbackStatusEffect(owner, Vector3.Right, 1, StatusEffectKey.KnockbackReceive); owner.AddComponentImmediately(effect); effect.Start();
            for (int i = 0; i < 6; i++) effect.Update();
            Assert.Near((Fix64)2 / Fix64.Sqrt(10), body.Velocity.X * (Fix64)0.1m, (Fix64)0.0001m);
            owner.FlushComponents(); Assert.Equal(false, owner.HasComponent<KnockbackStatusEffect>()); Assert.Equal(false, owner.HasEffect(VisualEffect.Knockback));
        }
        [PortedTest(Knockback)]
        public static void a_light_target_moves_further_than_a_heavy_one() {
            Fix64 Travel(int mass) {
                var owner = Owner(); owner.Context.DeltaTime = (Fix64)0.1m; var body = new RigidBody(owner, mass); owner.AddComponentImmediately(body);
                new KnockbackStatusEffect(owner, Vector3.Right, 1, StatusEffectKey.KnockbackReceive).Update(); return body.Velocity.X;
            }
            Assert.True(Travel(1) > Travel(4));
        }
        [PortedTest(Knockback)]
        public static void expiration_keeps_speed_modifier_owned_by_another_status_effect() {
            var owner = Owner(); owner.Context.DeltaTime = Fix64.One; var mob = new TestMob(owner, 10, 10); owner.AddComponentImmediately(mob);
            var snare = new SnaredStatusEffect(owner, 3, 0, (Fix64)0.5m, StatusEffectKey.SnaredReceive); owner.AddComponentImmediately(snare); snare.Start(); Assert.Equal((Fix64)5, mob.Speed.Total);
            var knockback = new KnockbackStatusEffect(owner, Vector3.Right, 1, StatusEffectKey.KnockbackReceive); owner.AddComponentImmediately(knockback); knockback.Start(); knockback.Update(); knockback.Update(); owner.FlushComponents();
            Assert.Equal(false, owner.HasComponent<KnockbackStatusEffect>()); Assert.True(owner.HasComponent<SnaredStatusEffect>()); Assert.Equal((Fix64)5, mob.Speed.Total);
        }
    }
}
