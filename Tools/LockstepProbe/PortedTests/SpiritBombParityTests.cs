using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SpiritBombParityTests {
        private const string ChannelSource = "game/domain/object/component/magic/spirit_bomb_channel.rs";
        private const string MagicSource = "game/domain/magic/implement/shoot/spirit_bomb_magic.rs";
        private sealed class Fixture {
            public readonly SimulationContext C = new SimulationContext { DeltaTime = (Fix64)0.4m };
            public readonly SimObject Player;
            public Fixture() { C.RegisterPrefab("Player", _ => { }); C.RegisterPrefab("Object", _ => { }); Player = C.Spawn(Master.LeftPlayer, "Player", Vector3.Zero); }
            public DummyMob Enemy(Vector3 position, Master side = Master.RightPlayer, int hp = 100) { var o = C.Spawn(side, "Object", position); var mob = new DummyMob(o, hp); o.AddComponentImmediately(mob); o.AddCollider(new CircleCollider(o, (Fix64)0.5m)); return mob; }
            public SpiritBombChannel Channel(int damage = 40, Vector3? aim = null) { C.FlushObjects(); var channel = new SpiritBombChannel(Player, aim ?? new Vector3(6, 0, 0), damage, 18); Player.AddComponentImmediately(channel); return channel; }
            public Vector3[] Ends(int damage) {
                var events = C.DrainEvents().Where(e => e.Type == "SpiritBombBeam").ToArray();
                foreach (var ev in events) { Assert.Equal(Player.Id, ev.SourceId); Assert.Equal((Fix64)1.05m, ev.Duration); Assert.Equal(SpiritBombChannel.VisualWidth(damage), ev.Width); Assert.True(!ev.StartPosition.HasValue); }
                return events.Select(e => e.EndPosition.Value).ToArray();
            }
        }
        [PortedTest(ChannelSource)] public static void damages_closest_enemy_on_beam_and_preserves_exact_four_tick_total() {
            var f = new Fixture(); var far = f.Enemy(new Vector3(4, 0, 0)); var near = f.Enemy(new Vector3(2, 0, 0)); var channel = f.Channel(7); channel.Update(); f.C.DeltaTime = 1; channel.Update(); channel.Update(); channel.Update();
            Assert.Equal(93, near.Hp); Assert.Equal(100, far.Hp); f.Player.FlushComponents(); Assert.True(!f.Player.HasComponent<SpiritBombChannel>()); var ends = f.Ends(7); Assert.Equal(4, ends.Length); foreach (var end in ends) Assert.Equal(new Vector3((Fix64)1.5m, 0, 0), end);
        }
        [PortedTest(ChannelSource)] public static void a_small_hit_draws_the_thinnest_beam_and_a_huge_one_the_thickest() { Assert.Equal((Fix64)0.25m, SpiritBombChannel.VisualWidth(0)); Assert.Equal((Fix64)0.25m, SpiritBombChannel.VisualWidth(100)); Assert.Equal(Fix64.One, SpiritBombChannel.VisualWidth(1000)); Assert.Equal(Fix64.One, SpiritBombChannel.VisualWidth(5000)); }
        [PortedTest(ChannelSource)] public static void between_the_ends_the_beam_thickens_with_the_damage() { var half = SpiritBombChannel.VisualWidth(550); Assert.Equal((Fix64)0.625m, half); Assert.True(SpiritBombChannel.VisualWidth(300) < half && SpiritBombChannel.VisualWidth(800) > half); }
        [PortedTest(ChannelSource)] public static void beam_reaches_cast_range_even_when_clicked_close_to_the_player() { var f = new Fixture(); var enemy = f.Enemy(new Vector3(12, 0, 0)); f.Channel(40, Vector3.Right).Update(); Assert.Equal(90, enemy.Hp); }
        [PortedTest(ChannelSource)] public static void enemy_outside_the_lane_is_not_hit() { var f = new Fixture(); var off = f.Enemy(new Vector3(5, 0, 3)); var far = f.Enemy(new Vector3(25, 0, 0)); f.Channel().Update(); Assert.Equal(100, off.Hp); Assert.Equal(100, far.Hp); }
        [PortedTest(ChannelSource)] public static void overkill_carries_over_to_the_next_enemy_in_the_same_tick() {
            var f = new Fixture(); var second = f.Enemy(new Vector3(6, 0, 0)); var first = f.Enemy(new Vector3(3, 0, 0)); first.ApplyDamage(new Hit(70, 0, Element.None)); f.Channel(160).Update(); Assert.True(first.Hp <= 0); Assert.Equal(90, second.Hp); Assert.Sequence(new[] { new Vector3((Fix64)2.5m, 0, 0), new Vector3((Fix64)5.5m, 0, 0) }, f.Ends(160));
        }
        [PortedTest(ChannelSource)] public static void beam_stays_on_the_aim_line_and_stops_beside_an_off_center_enemy() { var f = new Fixture(); var enemy = f.Enemy(new Vector3(5, 0, (Fix64)0.3m)); f.Channel().Update(); Assert.Equal(90, enemy.Hp); var end = f.Ends(40).Single(); Assert.Near((Fix64)4.6m, end.X, (Fix64)0.0001m); Assert.Equal(Fix64.Zero, end.Y); Assert.Equal(Fix64.Zero, end.Z); }
        [PortedTest(ChannelSource)] public static void beam_reaches_cast_range_when_nothing_is_on_the_line() { var f = new Fixture(); f.Channel().Update(); Assert.Equal(new Vector3(18, 0, 0), f.Ends(40).Single()); }
        [PortedTest(ChannelSource)] public static void enemy_just_behind_the_caster_is_not_hit() { var f = new Fixture(); var behind = f.Enemy(new Vector3((Fix64)(-0.5m), 0, 0)); var ahead = f.Enemy(new Vector3(8, 0, 0)); f.Channel().Update(); Assert.Equal(100, behind.Hp); Assert.Equal(90, ahead.Hp); }
        [PortedTest(ChannelSource)] public static void clicking_a_flying_enemy_aims_the_beam_up_at_it() {
            var f = new Fixture(); var ground = f.Enemy(new Vector3(3, 0, 0)); var air = f.Enemy(new Vector3(6, 3, 0)); f.Channel(40, air.Owner.Position).Update(); Assert.Equal(90, air.Hp); Assert.Equal(100, ground.Hp); var end = f.Ends(40).Single(); var length = Fix64.Sqrt(45); Assert.Near(6 / length * (length - (Fix64)0.5m), end.X, (Fix64)0.0001m); Assert.Near(3 / length * (length - (Fix64)0.5m), end.Y, (Fix64)0.0001m); Assert.Equal(Fix64.Zero, end.Z);
        }
        [PortedTest(MagicSource)] public static void drains_only_health_above_half_and_starts_channel_with_absorbed_damage() {
            var f = new Fixture(); var healthy = f.Enemy(new Vector3(1, 0, 0), Master.LeftPlayer); var low = f.Enemy(new Vector3(1, 0, 1), Master.LeftPlayer); healthy.ApplyDamage(new Hit(20, 0, Element.None)); low.ApplyDamage(new Hit(60, 0, Element.None)); f.C.FlushObjects(); f.C.GameParameters["spirit_bomb.range"] = 18;
            var r = new MagicRegistry(); SpiritBombMagic.Register(r); r.GetBean("spirit_bomb").Bind(1, "spirit_bomb", Element.None).Run(f.C, Master.LeftPlayer, Vector3.Zero, new Vector3(6, 0, 0));
            Assert.Equal(50, healthy.Hp); Assert.Equal(40, low.Hp); Assert.Equal(1, f.Player.GetPendingComponents<SpiritBombChannel>().Length); var ev = f.C.DrainEvents().Single(); Assert.Equal("ElectricAbsorb", ev.Type); Assert.Equal((Fix64)0.35m, ev.Duration); Assert.Equal(healthy.Owner.Id, ev.SourceId); Assert.Equal(f.Player.Id, ev.TargetId);
        }
        public static void CheckChannelEdges() {
            var timed = new Fixture(); var timedEnemy = timed.Enemy(new Vector3(3, 0, 0)); var timedChannel = timed.Channel(4); timed.C.DeltaTime = (Fix64)0.05m;
            for (int tick = 1; tick <= 68; tick++) { timedChannel.Update(); int expectedHits = tick < 8 ? 0 : 1 + (tick - 8) / 20; Assert.Equal(100 - expectedHits, timedEnemy.Hp); }
            var f = new Fixture(); var ally = f.Enemy(new Vector3(1, 0, 1), Master.LeftPlayer, 10); var enemy = f.Enemy(new Vector3(3, 0, 0)); f.C.FlushObjects(); f.C.GameParameters["spirit_bomb.range"] = 18;
            var r = new MagicRegistry(); SpiritBombMagic.Register(r); var magic = r.GetBean("spirit_bomb").Bind(1, "spirit_bomb", Element.None);
            magic.Run(f.C, Master.LeftPlayer, null, Vector3.Zero); Assert.Equal(10, ally.Hp); Assert.Equal(0, f.Player.PendingComponents.Length);
            magic.Run(f.C, Master.LeftPlayer, new Vector3(10, 3, 3), new Vector3(6, 0, 0)); Assert.Equal(5, ally.Hp); f.Player.FlushComponents(); var channel = f.Player.GetComponent<SpiritBombChannel>(); f.C.DeltaTime = (Fix64)0.2m; channel.Update(); Assert.Equal(100, enemy.Hp); f.C.DeltaTime = (Fix64)0.4m - f.C.DeltaTime; channel.Update(); Assert.Equal(99, enemy.Hp);
            f.C.DeltaTime = 3; channel.Update(); Assert.Equal(96, enemy.Hp); channel.Update(); Assert.Equal(96, enemy.Hp); f.Player.FlushComponents(); Assert.True(!f.Player.HasComponent<SpiritBombChannel>());
        }
    }
}
