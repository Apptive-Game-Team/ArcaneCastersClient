using System;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class DetectorParityTests {
        private const string Source = "tests/closest_enemy_detector.rs";
        private static SimulationContext Context() { var context = new SimulationContext(0); context.RegisterPrefab("WaterSlime", _ => { }); return context; }
        private static SimObject Self(SimulationContext context, Fix64 height = default) => new SimObject(context, Master.LeftPlayer, "WaterSlime", new Vector3(0, height, 0));
        private static SimObject Enemy(SimulationContext context, Master master, Fix64 x, Fix64 y = default) {
            var owner = context.Spawn(master, "WaterSlime", new Vector3(x, y, 0)); owner.AddComponentImmediately(new DummyMob(owner, 10)); context.FlushObjects(); return owner;
        }
        [PortedTest(Source)] public static void filtered_detection_skips_a_closer_enemy_outside_the_allowed_area() {
            var context = Context(); var self = Self(context); Enemy(context, Master.RightPlayer, 1); var allowed = Enemy(context, Master.RightPlayer, 2);
            Assert.True(ReferenceEquals(allowed, new ClosestEnemyDetector(context, 1).Detect(self, target => ReferenceEquals(target, allowed))));
        }
        [PortedTest(Source)] public static void frenzied_mob_can_target_another_frenzied_mob_with_the_same_neutral_master() {
            var context = Context(); var self = Self(context); self.AddComponentImmediately(new DummyMob(self, 10)); var frenzy = new FrenzyStatusEffect(self, 10, StatusEffectKey.FrenzyReceive); self.AddComponentImmediately(frenzy); frenzy.Start();
            Assert.Equal(Master.None, self.Master); var other = Enemy(context, Master.None, 1); Assert.True(ReferenceEquals(other, new ClosestEnemyDetector(context, 1).Detect(self)));
        }
        [PortedTest(Source)] public static void non_frenzied_mob_still_ignores_targets_with_the_same_master() {
            var context = Context(); var self = Self(context); Enemy(context, Master.LeftPlayer, 1); Assert.True(new ClosestEnemyDetector(context, 1).Detect(self) == null);
        }
        [PortedTest(Source)] public static void picks_closest_across_ground_and_air_when_targeting_any() {
            var context = Context(); var self = Self(context, 3); Enemy(context, Master.RightPlayer, 6, 3); var ground = Enemy(context, Master.RightPlayer, 2);
            Assert.True(ReferenceEquals(ground, new ClosestEnemyDetector(context, 3).Detect(self)));
        }
        [PortedTest(Source)] public static void repeats_the_same_choice_for_the_same_world_state() {
            var context = Context(); var self = Self(context, 3); var air = Enemy(context, Master.RightPlayer, 3, 3); Enemy(context, Master.RightPlayer, 3); var detector = new ClosestEnemyDetector(context, 3);
            var first = detector.Detect(self); var second = detector.Detect(self); Assert.True(first != null); Assert.True(ReferenceEquals(first, second)); Assert.True(ReferenceEquals(first, air));
        }
        [PortedTest(Source)] public static void ignores_allies_self_destroyed_and_uninitialized_targets() {
            var context = Context(); var self = Enemy(context, Master.LeftPlayer, 0, 3); Enemy(context, Master.LeftPlayer, 1);
            var destroyed = Enemy(context, Master.RightPlayer, 2); var uninitialized = Enemy(context, Master.RightPlayer, 3); var reachable = Enemy(context, Master.RightPlayer, 9);
            destroyed.SetStatus(ObjectStatus.Destroyed); uninitialized.SetStatus(ObjectStatus.Initializing);
            Assert.True(ReferenceEquals(reachable, new ClosestEnemyDetector(context, 3).Detect(self)));
        }
        [PortedTest(Source)] public static void keeps_ground_only_detectors_unchanged() {
            var context = Context(); var self = Self(context); Enemy(context, Master.RightPlayer, 1, 3); var ground = Enemy(context, Master.RightPlayer, 5);
            Assert.True(ReferenceEquals(ground, new ClosestEnemyDetector(context, 1).Detect(self)));
        }
    }
}
