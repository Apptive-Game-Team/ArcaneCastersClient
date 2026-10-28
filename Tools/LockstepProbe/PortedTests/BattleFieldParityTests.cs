using System;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BattleFieldParityTests {
        private const string Source = "game/service/game_loop.rs";
        private sealed class EmptyLookup : IMagicLookup {
            public BoundMagic GetMagic(long id) => null;
            public BoundMagic ParseMagic(long user, long id) => null;
        }
        private static SimulationContext Start(GameMap map, SessionType type = SessionType.PVP, bool right = true) {
            var c = new SimulationContext(1); var session = new SessionDefinition("s", 11, 22, 1, new long[0], new long[0], type, map); session.InitializeIdentityAndDecks(c);
            c.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100)); c.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100)); c.GameParameters["player.hp"] = 250;
            c.InputHandler = new MagicInputHandler(new EmptyLookup()); PlayerHealthComponent.Register(c); TerrainPrefabs.Register(c);
            c.InitializationFailed += (owner, error) => throw error;
            BattleFieldInitializer.Spawn(c, session, right); return c;
        }
        private static Vector3[] Positions(SimulationContext c, string prefab) => c.PendingObjects.Where(o => o.Prefab == prefab).Select(o => { Assert.Equal(Master.None, o.Master); return o.Position; }).ToArray();
        [PortedTest(Source)] public static void a_river_match_spawns_eight_water_and_twelve_bridge_objects_at_the_cell_centers() {
            var c = Start(GameMap.River); var water = Positions(c, "RiverWater"); var bridges = Positions(c, "RiverBridge"); Assert.Equal(8, water.Length); Assert.Equal(12, bridges.Length);
            Assert.Sequence(Terrain.River.WaterCells.Select(cell => cell.Center), water); Assert.Sequence(Terrain.River.BridgeCells.Select(cell => cell.Center), bridges);
            Assert.True(water.Contains(new Vector3((Fix64)8.5m, 0, (Fix64)0.5m))); Assert.True(water.Contains(new Vector3((Fix64)9.5m, 0, (Fix64)9.5m))); Assert.True(!water.Contains(new Vector3((Fix64)8.5m, 0, (Fix64)2.5m))); Assert.True(bridges.Contains(new Vector3((Fix64)8.5m, 0, (Fix64)2.5m))); Assert.Equal(Terrain.River, c.Terrain);
        }
        [PortedTest(Source)] public static void a_grassland_match_spawns_no_terrain_objects() { var c = Start(GameMap.Grassland); Assert.Equal(0, Positions(c, "RiverWater").Length); Assert.Equal(0, Positions(c, "RiverBridge").Length); Assert.Equal(1, Positions(c, "Wall").Length); Assert.Equal(Terrain.None, c.Terrain); }
        [PortedTest(Source)] public static void every_pve_map_kind_spawns_no_terrain_objects() {
            foreach (var map in new[] { GameMap.Grassland, GameMap.Fortress, GameMap.Gate, GameMap.Forest }) { var c = Start(map, SessionType.PVE); Assert.Equal(0, Positions(c, "RiverWater").Length); Assert.Equal(0, Positions(c, "RiverBridge").Length); Assert.Equal(Terrain.None, c.Terrain); }
        }
        [PortedTest(Source)] public static void the_loop_uses_the_map_stored_on_the_session_instead_of_choosing_one() { var c = Start(GameMap.River, SessionType.PVE); Assert.Equal(8, Positions(c, "RiverWater").Length); Assert.Equal(Terrain.River, c.Terrain); }
        [PortedTest(Source)] public static void init_spawns_both_players_and_the_wall() { var c = Start(GameMap.Grassland); Assert.Sequence(new[] { Master.LeftPlayer, Master.RightPlayer }, c.PendingObjects.Where(o => o.Prefab == "Player").Select(o => o.Master)); Assert.Equal(1, Positions(c, "Wall").Length); }
        public static void CheckPlayerAndTerrainInitialization() {
            var c = Start(GameMap.River, SessionType.PVE, false); Assert.Equal(22, c.PendingObjects.Length); Assert.Equal(250, c.Players[Master.LeftPlayer].Hp);
            Assert.True(c.PendingObjects.All(o => !o.IsDestroyed)); Assert.True(c.PendingObjects.Where(o => o.Prefab == "RiverBridge").All(o => o.Colliders.Length == 0 && !o.HasComponent<MobComponent>()));
            Assert.True(c.PendingObjects.Where(o => o.Prefab == "RiverWater").All(o => o.HasComponent<WaterBarrier>()));
            bool rejected = false; try { BattleFieldInitializer.Spawn(c, c.Service<SessionDefinition>()); } catch (InvalidOperationException) { rejected = true; } Assert.True(rejected); Assert.Equal(22, c.PendingObjects.Length);
        }
    }
}
