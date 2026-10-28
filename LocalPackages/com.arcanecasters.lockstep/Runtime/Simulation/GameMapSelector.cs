using System;
using System.IO;

namespace ArcaneCasters.Simulation {
    public enum MapSelection { Random, Grassland, River }
    public sealed class GameMapSelector {
        public MapSelection Selection { get; }
        private readonly Func<bool> coin;
        private readonly Func<long, GameMap?> scenarioMap;
        public GameMapSelector(MapSelection? selection, Func<bool> coin, Func<long, GameMap?> scenarioMap) {
            Selection = selection ?? MapSelection.Random;
            this.coin = coin ?? throw new ArgumentNullException(nameof(coin));
            this.scenarioMap = scenarioMap ?? throw new ArgumentNullException(nameof(scenarioMap));
        }
        public GameMap Choose(SessionType type, long? scenarioId) {
            if (type == SessionType.PVE) return scenarioId.HasValue ? scenarioMap(scenarioId.Value) ?? GameMap.Grassland : GameMap.Grassland;
            if (type == SessionType.Playground) return GameMap.Grassland;
            switch (Selection) {
                case MapSelection.Grassland: return GameMap.Grassland;
                case MapSelection.River: return GameMap.River;
                case MapSelection.Random: return coin() ? GameMap.River : GameMap.Grassland;
                default: throw new ArgumentOutOfRangeException(nameof(Selection));
            }
        }
        public static string WireName(GameMap map) => map.ToString().ToUpperInvariant();
        public static Terrain TerrainFor(GameMap map) => map == GameMap.River ? Terrain.River : Terrain.None;
    }
    [Serializable] public sealed class GameMapPolicySnapshot {
        public string selection;
        public bool? coin;
        public string scenarioMap;
        public GameMap Choose(SessionType type, long? scenarioId) {
            var selected = BattleInitialState.Parse<MapSelection>(selection);
            GameMap? stage = scenarioMap == null ? (GameMap?)null : BattleInitialState.Parse<GameMap>(scenarioMap);
            return new GameMapSelector(selected, () => coin ?? throw new InvalidDataException("Missing confirmed map coin."), _ => stage).Choose(type, scenarioId);
        }
    }
}
