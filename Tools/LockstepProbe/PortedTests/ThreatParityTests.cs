using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ThreatParityTests {
        private const string Source = "game/domain/bot/threat_assessment.rs";
        private static readonly Vector3 Defended = new Vector3(1, 0, 5);
        private static SimulationContext Context() { var c = new SimulationContext(); c.RegisterPrefab("RockSlime", _ => { }); c.RegisterPrefab("Player", _ => { }); return c; }
        private static SimObject Object(SimulationContext c, decimal x, decimal z, Master side = Master.RightPlayer, ObjectStatus status = ObjectStatus.Idle, string prefab = "RockSlime") {
            var owner = c.Spawn(side, prefab, new Vector3((Fix64)x, 0, (Fix64)z)); owner.SetStatus(status); return owner;
        }
        private static ThreatAssessment Seen(params SimObject[] objects) => ThreatAssessment.Observe(objects.Select(o => BotVisibleObject.Of(o, owner => owner.GetComponent<MobComponent>()?.Hp)), Master.RightPlayer, Defended, 100);
        private static BotVisibleObject Unit(int id, decimal x, decimal z, Master side = Master.RightPlayer) => new BotVisibleObject(id, side, "FireSpirit", new Vector3((Fix64)x, 0, (Fix64)z), 10, true, true);
        private static ThreatAssessment Observe(BotVisibleObject[] objects, bool cover = false) => ThreatAssessment.Observe(objects, Master.RightPlayer, Defended, 100, allyCover: cover);
        [PortedTest(Source)] public static void ignores_own_objects_and_untargetable_enemies() {
            var c = Context(); var ally = Object(c, 4, 5, Master.LeftPlayer); var dying = Object(c, 4, 5, status: ObjectStatus.Dying); var destroyed = Object(c, 4, 5, status: ObjectStatus.Destroyed); var live = Object(c, 6, 5);
            var threats = Seen(ally, dying, destroyed, live); Assert.Equal(1, threats.Threats.Count); Assert.Equal(live.Id, threats.Threats[0].ObjectId);
        }
        [PortedTest(Source)] public static void reads_remaining_hit_points_from_the_mob_component() {
            var c = Context(); var wounded = Object(c, 6, 5); var mob = new DummyMob(wounded, 10); wounded.AddComponentImmediately(mob); mob.ApplyDamage(new Hit(3, 99));
            Assert.Equal(7, Seen(wounded).Threats[0].Hp);
        }
        [PortedTest(Source)] public static void orders_threats_by_how_close_they_are_to_the_defended_position() { var c = Context(); var far = Object(c, 12, 5); var near = Object(c, 3, 5); Assert.Equal(near.Id, Seen(far, near).Threats[0].ObjectId); }
        [PortedTest(Source)] public static void pressure_rises_as_enemies_close_in_and_stays_zero_when_they_are_far_away() {
            var c = Context(); Assert.Equal(Fix64.Zero, Seen(Object(c, 15, 5)).Pressure);
            var pressure = Seen(Object(c, 2, 5), Object(c, 2, 6), Object(c, 2, 4)).Pressure; Assert.True(pressure >= (Fix64)0.8m && pressure <= Fix64.One);
        }
        [PortedTest(Source)] public static void pressure_saturates_at_one_so_an_overwhelming_push_cannot_outweigh_everything_else() {
            var c = Context(); Assert.Equal(Fix64.One, Seen(Enumerable.Range(0, 8).Select(i => Object(c, 2, 2 + i * 0.5m)).ToArray()).Pressure);
        }
        [PortedTest(Source)] public static void reports_the_lane_the_nearby_pressure_sits_in() => Assert.Equal((Fix64)9, Seen(Object(Context(), 4, 9)).ContestedLaneZ);
        [PortedTest(Source)] public static void falls_back_to_the_defended_lane_when_nothing_is_close() { var threats = Seen(); Assert.True(!threats.HasThreats); Assert.Equal(Defended.Z, threats.ContestedLaneZ); }
        [PortedTest(Source)] public static void marks_the_enemy_player_as_the_core_and_takes_its_hp_from_the_session() {
            var owner = Object(Context(), 17, 5, prefab: "Player");
            var threats = ThreatAssessment.Observe(new[] { BotVisibleObject.Of(owner, _ => null) }, Master.RightPlayer, Defended, 63);
            Assert.True(threats.Threats[0].PlayerCore); Assert.Equal(63, threats.Threats[0].Hp); Assert.Equal(Fix64.Zero, threats.Pressure);
        }
        [PortedTest(Source)] public static void strongest_lane_does_not_average_two_lanes_into_the_empty_middle() {
            var threats = Observe(new[] { Unit(1, 4, 1), Unit(2, 4, 1.5m), Unit(3, 4, 9) });
            Assert.True(threats.ContestedLaneZ >= (Fix64)3 && threats.ContestedLaneZ <= (Fix64)5); Assert.Near((Fix64)1.25m, threats.StrongestLaneZ, (Fix64)0.05m);
        }
        [PortedTest(Source)] public static void strongest_lane_falls_back_to_the_defended_lane_when_nothing_presses() => Assert.Equal(Defended.Z, Observe(new[] { Unit(1, 16, 1) }).StrongestLaneZ);
        [PortedTest(Source)] public static void strength_decides_which_lane_is_strongest() {
            var objects = new[] { Unit(1, 4, 9), Unit(2, 4, 1), Unit(3, 4, 1.5m) };
            var threats = ThreatAssessment.Observe(objects, Master.RightPlayer, Defended, 100, o => o.Id == 1 ? (Fix64)3 : (Fix64)0.5m);
            Assert.Equal((Fix64)9, threats.StrongestLaneZ);
        }
        [PortedTest(Source)] public static void strength_raises_pressure() {
            var objects = new[] { Unit(1, 4, 5) }; Assert.True(ThreatAssessment.Observe(objects, Master.RightPlayer, Defended, 100, _ => (Fix64)2).Pressure > Observe(objects).Pressure);
        }
        [PortedTest(Source)] public static void allies_in_the_same_lane_take_pressure_off() { var objects = new[] { Unit(1, 4, 5), Unit(2, 3, 5, Master.LeftPlayer) }; Assert.True(Observe(objects, true).Pressure < Observe(objects).Pressure); }
        [PortedTest(Source)] public static void an_ally_in_the_other_lane_is_no_cover() { var objects = new[] { Unit(1, 4, 1), Unit(2, 3, 9, Master.LeftPlayer) }; Assert.Equal(Observe(objects).Pressure, Observe(objects, true).Pressure); }
        [PortedTest(Source)] public static void led_by_moves_threats_along_their_velocity() {
            var threats = Observe(new[] { Unit(1, 8, 5) }); var led = threats.LedBy(new Dictionary<int, Vector3> { [1] = new Vector3(-2, 0, 0) }, (Fix64)0.5m);
            Assert.Equal(new Vector3(7, 0, 5), led.Threats[0].Position); Assert.Near(6, led.Threats[0].DistanceToDefendedPosition, (Fix64)0.000001m); Assert.Equal(new Vector3(8, 0, 5), threats.Threats[0].Position);
        }
    }
}
