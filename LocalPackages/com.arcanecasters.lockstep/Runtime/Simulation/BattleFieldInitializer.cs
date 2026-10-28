using System;
using BEPUutilities;

namespace ArcaneCasters.Simulation {
    public static class BattleFieldInitializer {
        private sealed class Initialized { }
        public static void Spawn(SimulationContext context, SessionDefinition session, bool createRightPlayer = true) {
            if (context.Service<Initialized>() != null) throw new InvalidOperationException("Battle field has already been initialized.");
            if (context.InputHandler == null || !context.Players.ContainsKey(Master.LeftPlayer) || (createRightPlayer && !context.Players.ContainsKey(Master.RightPlayer)) || !context.GameParameters.ContainsKey("player.hp"))
                throw new InvalidOperationException("Player data, health parameters and the input handler must be initialized before the field.");
            foreach (var prefab in session.Map == GameMap.River ? new[] { "Player", "Wall", "RiverWater", "RiverBridge" } : new[] { "Player", "Wall" })
                if (!context.HasPrefab(prefab)) throw new InvalidOperationException("Missing field prefab: " + prefab);
            context.Spawn(Master.LeftPlayer, "Player", new Vector3(1, 0, 5));
            if (createRightPlayer) context.Spawn(Master.RightPlayer, "Player", new Vector3(17, 0, 5));
            context.Spawn(Master.None, "Wall", Vector3.Zero);
            context.Terrain = GameMapSelector.TerrainFor(session.Map);
            foreach (var cell in context.Terrain.WaterCells) context.Spawn(Master.None, "RiverWater", cell.Center);
            foreach (var cell in context.Terrain.BridgeCells) context.Spawn(Master.None, "RiverBridge", cell.Center);
            context.InsertService(new Initialized());
        }
    }
}
