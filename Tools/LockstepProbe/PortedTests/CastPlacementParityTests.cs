using System;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class CastPlacementParityTests {
        private const string Source = "game/service/cast_placement.rs";
        private static Vector3 Point(decimal x, decimal z, decimal y = 0) => new Vector3((Fix64)x, (Fix64)y, (Fix64)z);
        private static PlacementBody[] Bodies(decimal x = 5, decimal z = 5, decimal y = 0, decimal radius = 0.5m) => new[] { new PlacementBody(Point(x, z, y), (Fix64)radius) };
        private static readonly PlacementBody[] Empty = Array.Empty<PlacementBody>();
        private static bool Blocked(Vector3 point, PlacementBody[] bodies = null, bool river = false, bool airborne = false) => CastPlacement.Blocked(point, 1, airborne, bodies ?? Empty, river ? Terrain.River.CopyWaterGrid() : null);
        private static Vector3? Resolve(Vector3 aim, Vector3 origin, decimal range = 20, PlacementBody[] bodies = null, bool river = false, bool airborne = false) => CastPlacement.Resolve(aim, origin, (Fix64)range, 1, airborne, bodies ?? Empty, river ? Terrain.River.CopyWaterGrid() : null, out var spot) ? spot : (Vector3?)null;
        private static void Close(Fix64 actual, decimal expected) => Assert.True(Fix64.Abs(actual - (Fix64)expected) < (Fix64)0.0001m);
        private sealed class Lookup : IMagicLookup {
            public BoundMagic Magic;
            public BoundMagic GetMagic(long id) => Magic;
            public BoundMagic ParseMagic(long user, long id) => Magic;
        }
        private static void CastUnblocked(Vector3 aim, bool river, bool airborne) {
            var c = new SimulationContext(1); c.Terrain = river ? Terrain.River : Terrain.None;
            var lookup = new Lookup(); c.InputHandler = new MagicInputHandler(lookup);
            c.InitializationFailed += (_, error) => throw error;
            PlayerHealthComponent.Register(c); c.RegisterPrefab("Obstacle", _ => { });
            c.GameParameters["player.hp"] = 100; c.GameParameters["shot.range"] = 20; c.GameParameters["shot.mana_cost"] = 0; c.GameParameters["ground_tower.radius"] = 1;
            c.UserIds[Master.LeftPlayer] = 1;
            var player = new PlayerData(new ManaCharger(100)); player.AddCard(2); c.Players[Master.LeftPlayer] = player;
            c.CardDecks[Master.LeftPlayer] = new CardDeck(Array.Empty<long>(), 0);
            c.Spawn(Master.LeftPlayer, "Player", Point(1, 5));
            var obstacle = c.Spawn(Master.None, "Obstacle", aim); obstacle.AddCollider(new CircleCollider(obstacle, (Fix64)0.5m)); obstacle.AddComponentImmediately(new DummyMob(obstacle, 100)); c.FlushObjects();
            Vector3? cast = null;
            lookup.Magic = new BoundMagic(2, "shot", (_, side, origin, target) => cast = target, airborne ? "ground_tower" : null, airborne);
            var result = c.InputHandler.HandleInput(c, 1, 2, aim, 7);
            Assert.True(result.Valid); Assert.Equal((Vector3?)aim, cast);
        }
        [PortedTest(Source)] public static void magics_that_leave_no_body_are_never_blocked() { CastUnblocked(Point(5, 5), false, false); }
        [PortedTest(Source)] public static void an_airborne_summon_and_a_magic_that_leaves_no_body_are_never_blocked_by_water() {
            CastUnblocked(Point(9, 5), true, true); CastUnblocked(Point(9, 5), true, false);
        }

        [PortedTest(Source)] public static void overlap_threshold_is_the_radius_sum_times_the_overlap_ratio() {
            Assert.Equal((Fix64)0.6m, CastPlacement.OverlapRatio);
            Assert.True(!Blocked(Point(6.05m, 5), Bodies())); Assert.True(Blocked(Point(5.75m, 5), Bodies()));
        }
        [PortedTest(Source)] public static void airborne_bodies_neither_block_nor_are_blocked() {
            Assert.True(!Blocked(Point(5, 5), Bodies(y: 3)));
            Assert.True(!Blocked(Point(5, 5), Bodies(), airborne: true));
        }
        [PortedTest(Source)] public static void free_aim_point_is_returned_unchanged() {
            var aim = Point(9, 5); Assert.Equal((Vector3?)aim, Resolve(aim, Point(1, 5), bodies: Bodies()));
        }
        [PortedTest(Source)] public static void blocked_aim_snaps_to_the_first_free_direction_of_the_innermost_free_ring() {
            var spot = Resolve(Point(5, 5), Point(1, 5), 9, Bodies()).Value; Close(spot.X, 6); Close(spot.Z, 5);
        }
        [PortedTest(Source)] public static void directions_are_tried_in_order_skipping_candidates_outside_the_map() {
            var spot = Resolve(Point(18, 5), Point(15, 5), 9, Bodies(x: 18)).Value; Close(spot.X, 18); Close(spot.Z, 6);
        }
        [PortedTest(Source)] public static void candidates_beyond_the_range_are_skipped() {
            var aim = Point(5, 5); Assert.True(!Resolve(aim, aim, 0.9m, Bodies()).HasValue); Assert.True(Resolve(aim, aim, 1.05m, Bodies()).HasValue);
        }
        [PortedTest(Source)] public static void empty_when_everything_within_range_is_blocked() {
            var aim = Point(5, 5); Assert.True(!Resolve(aim, aim, 3, Bodies(radius: 5)).HasValue);
        }
        [PortedTest(Source)] public static void aim_inside_a_water_cell_blocks_a_ground_summon() {
            Assert.True(Blocked(Point(9, 5), river: true)); Assert.True(Blocked(Point(8.5m, 0.5m), river: true));
        }
        [PortedTest(Source)] public static void the_block_edge_is_the_nearest_point_of_the_square_at_the_overlap_threshold() {
            Assert.True(Blocked(Point(7.41m, 5), river: true)); Assert.True(!Blocked(Point(7.39m, 5), river: true));
            Assert.True(Blocked(Point(7.6m, 3.6m), river: true)); Assert.True(!Blocked(Point(7.3m, 3.3m), river: true));
        }
        [PortedTest(Source)] public static void a_bridge_cell_is_not_blocked() {
            Assert.True(!Blocked(Point(8.5m, 2.5m), river: true)); Assert.True(!Blocked(Point(9.5m, 7.5m), river: true));
        }
        [PortedTest(Source)] public static void the_open_arena_never_blocks_on_water() { Assert.True(!Blocked(Point(9, 5))); }
        [PortedTest(Source)] public static void resolve_spot_moves_a_ground_summon_out_of_the_water_to_a_free_spot() {
            var aim = Point(9, 5); var spot = Resolve(aim, Point(1, 5), river: true).Value;
            Assert.True(spot != aim); Assert.True(!Blocked(spot, river: true)); Assert.True(!Terrain.River.IsWaterAt(spot));
        }
        [PortedTest(Source)] public static void resolve_spot_keeps_a_free_aim_point_on_a_bridge() {
            var aim = Point(8.5m, 2.5m); Assert.Equal((Vector3?)aim, Resolve(aim, Point(1, 5), river: true));
        }
        [PortedTest(Source)] public static void an_airborne_summon_aimed_at_the_water_is_kept_where_it_was_aimed() {
            var aim = Point(9, 5); Assert.Equal((Vector3?)aim, Resolve(aim, Point(1, 5), river: true, airborne: true));
        }
    }
}
