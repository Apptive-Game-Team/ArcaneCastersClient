using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RepairAuraParityTests {
        private const string Source = "game/domain/object/component/build/repair_aura.rs";
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(1) { DeltaTime = 1 };
            public readonly SimObject Totem;
            public readonly RepairAura Aura;
            public Fixture(Master master = Master.LeftPlayer) { Context.RegisterPrefab("DragonFlame", _ => { }); Totem = Object(master); Aura = new RepairAura(Totem, 5); }
            public SimObject Object(Master master) { var target = Context.Spawn(master, "DragonFlame", Vector3.Zero); target.AddCollider(new CircleCollider(target, 1)); Context.FlushObjects(); return target; }
            public TimedSelfDestroyer Ally(Master master, Fix64? speed = null) {
                var target = Object(master); var lifetime = new TimedSelfDestroyer(target, 100); target.AddComponentImmediately(lifetime);
                if (speed.HasValue) target.AddComponentImmediately(new TestMob(target, speed.Value));
                return lifetime;
            }
        }
        private sealed class TestMob : MobComponent { public TestMob(SimObject owner, Fix64 speed) : base(owner, 10, speed) { } public override void OnDeath() { Owner.Destroy(); } }
        private sealed class Damageable : SimComponent, IDamageable { public Damageable(SimObject owner) : base(owner) { } public void OnDamaged(Hit hit) { } public void OnDamagedWithDelay(Hit hit, Fix64 delay) { } }
        private static void Frozen(TimedSelfDestroyer lifetime, bool frozen) { var before = lifetime.GetGauge().Value; lifetime.Update(); Assert.Equal(before - (frozen ? Fix64.Zero : lifetime.Context.DeltaTime), lifetime.GetGauge().Value); }
        public static void CheckPrefabAndRangeExit() {
            var f = new Fixture(); NatureUnitPrefabs.Register(f.Context);
            foreach (var key in new[] { "mass", "radius" }) f.Context.GameParameters["repair_totem." + key] = 1;
            f.Context.GameParameters["repair_totem.hp"] = 20; f.Context.GameParameters["repair_totem.effect_radius"] = 5; f.Context.GameParameters["repair_totem.duration"] = 10;
            var totem = f.Context.Spawn(Master.LeftPlayer, "RepairTotem", new Vector3(5, 0, 5)); var life = f.Ally(Master.LeftPlayer, 0); life.Owner.SetPosition(new Vector3(6, 0, 5)); f.Context.FlushObjects();
            Assert.True(!totem.IsDestroyed && totem.HasComponent<BuildingEffectReceiver>()); var aura = totem.GetComponent<RepairAura>(); Assert.True(aura != null);
            aura.Update(); Frozen(life, true); totem.GetComponent<TimedSelfDestroyer>().Update(); Assert.Equal((Fix64)9, totem.GetComponent<TimedSelfDestroyer>().GetGauge().Value);
            life.Owner.SetPosition(new Vector3(13, 0, 5)); aura.Update(); Frozen(life, false);
            life.Owner.SetPosition(new Vector3(6, 0, 5)); aura.Update(); Frozen(life, true); Frozen(life, false);
            Assert.Equal(10, life.Owner.GetComponent<TestMob>().Hp);
        }
        [PortedTest(Source)] public static void freezes_the_lifetime_of_allied_targets_in_range() { var f = new Fixture(); var life = f.Ally(Master.LeftPlayer, 0); f.Aura.Update(); Frozen(life, true); }
        [PortedTest(Source)] public static void freezes_every_tick_so_the_hold_lasts_while_the_target_stays_in_range() { var f = new Fixture(); var life = f.Ally(Master.LeftPlayer, 0); for (int i = 0; i < 3; i++) { f.Aura.Update(); life.Update(); } Assert.Equal((Fix64)100, life.GetGauge().Value); }
        [PortedTest(Source)] public static void never_rewinds_elapsed_time() { var f = new Fixture(); var life = f.Ally(Master.LeftPlayer, 0); life.Update(); f.Aura.Update(); life.Update(); Assert.Equal((Fix64)99, life.GetGauge().Value); }
        [PortedTest(Source)] public static void ignores_enemy_targets_even_with_a_lifetime() { var f = new Fixture(); var life = f.Ally(Master.RightPlayer, 0); f.Aura.Update(); Frozen(life, false); }
        [PortedTest(Source)] public static void never_freezes_its_own_lifetime_even_if_it_overlaps_itself() { var f = new Fixture(); var life = new TimedSelfDestroyer(f.Totem, 100); f.Totem.AddComponentImmediately(life); f.Totem.AddComponentImmediately(new TestMob(f.Totem, 0)); f.Aura.Update(); Frozen(life, false); }
        [PortedTest(Source)] public static void does_nothing_for_a_target_with_no_lifetime() { var f = new Fixture(); f.Object(Master.LeftPlayer); f.Aura.Update(); }
        [PortedTest(Source)] public static void does_nothing_while_it_has_no_master() { var f = new Fixture(Master.None); var life = f.Ally(Master.None, 0); f.Aura.Update(); Frozen(life, false); }
        [PortedTest(Source)] public static void ignores_allied_non_building_targets_with_no_mob_or_damageable() { var f = new Fixture(); var life = f.Ally(Master.LeftPlayer); f.Aura.Update(); Frozen(life, false); }
        [PortedTest(Source)] public static void a_damageable_without_a_mob_counts_as_a_building() { var f = new Fixture(); var life = f.Ally(Master.LeftPlayer); life.Owner.AddComponentImmediately(new Damageable(life.Owner)); f.Aura.Update(); Frozen(life, true); }
        [PortedTest(Source)] public static void ignores_allied_units_with_positive_speed() { var f = new Fixture(); var life = f.Ally(Master.LeftPlayer, 1); f.Aura.Update(); Frozen(life, false); }
        [PortedTest(Source)] public static void ignores_another_repair_totem_even_though_it_is_an_allied_building() { var f = new Fixture(); var life = f.Ally(Master.LeftPlayer, 0); life.Owner.AddComponentImmediately(new RepairAura(life.Owner, 5)); f.Aura.Update(); Frozen(life, false); }
    }
}
