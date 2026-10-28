using System;
using System.Linq;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class SessionDefinitionParityTests {
        private const string Source = "game/domain/session_object.rs";
        private const string Maps = "game/domain/map/game_map_selector.rs";
        private sealed class MapRepository {
            public readonly System.Collections.Generic.Dictionary<long, GameMap> Maps = new System.Collections.Generic.Dictionary<long, GameMap>();
            public int Reads;
            public GameMap? Find(long id) { Reads++; return Maps.TryGetValue(id, out var map) ? map : (GameMap?)null; }
        }
        private static (GameMap, int) ChooseMap(MapSelection selection, bool value, MapRepository repository, SessionType type, long? scenario = null) {
            int calls = 0;
            var map = new GameMapSelector(selection, () => { calls++; return value; }, repository.Find).Choose(type, scenario);
            return (map, calls);
        }
        [PortedTest(Maps)] public static void a_fixed_random_source_gives_both_maps_for_pvp_and_practice() {
            var repository = new MapRepository();
            foreach (var type in new[] { SessionType.PVP, SessionType.Practice }) {
                Assert.Equal((GameMap.River, 1), ChooseMap(MapSelection.Random, true, repository, type));
                Assert.Equal((GameMap.Grassland, 1), ChooseMap(MapSelection.Random, false, repository, type));
            }
        }
        [PortedTest(Maps)] public static void forcing_a_map_ignores_the_random_source() {
            var repository = new MapRepository();
            Assert.Equal((GameMap.Grassland, 0), ChooseMap(MapSelection.Grassland, true, repository, SessionType.PVP));
            Assert.Equal((GameMap.River, 0), ChooseMap(MapSelection.River, false, repository, SessionType.Practice));
        }
        [PortedTest(Maps)] public static void the_developer_playground_is_always_grassland() {
            foreach (MapSelection selection in Enum.GetValues(typeof(MapSelection)))
                Assert.Equal((GameMap.Grassland, 0), ChooseMap(selection, true, new MapRepository(), SessionType.Playground));
        }
        [PortedTest(Maps)] public static void pve_takes_the_map_of_the_stage_whatever_the_selection_says() {
            var repository = new MapRepository(); repository.Maps[7] = GameMap.Forest;
            Assert.Equal((GameMap.Forest, 0), ChooseMap(MapSelection.Random, true, repository, SessionType.PVE, 7));
            Assert.Equal((GameMap.Forest, 0), ChooseMap(MapSelection.River, true, repository, SessionType.PVE, 7));
            Assert.Equal((GameMap.Grassland, 0), ChooseMap(MapSelection.River, true, repository, SessionType.PVE, 8));
            Assert.Equal((GameMap.Grassland, 0), ChooseMap(MapSelection.River, true, repository, SessionType.PVE));
        }
        [PortedTest(Maps)] public static void non_pve_sessions_do_not_read_the_database() {
            var repository = new MapRepository(); ChooseMap(MapSelection.Random, true, repository, SessionType.PVP, 7);
            Assert.Equal(0, repository.Reads);
        }
        [PortedTest(Maps)] public static void the_selection_defaults_to_random_and_the_names_are_the_contract() {
            Assert.Equal(MapSelection.Random, new GameMapSelector(null, () => false, _ => null).Selection);
            Assert.Sequence(new[] { "GRASSLAND", "RIVER", "FORTRESS", "GATE", "FOREST" }, Enum.GetValues(typeof(GameMap)).Cast<GameMap>().Select(GameMapSelector.WireName));
        }
        [PortedTest(Maps)] public static void only_the_river_has_terrain() {
            Assert.True(ReferenceEquals(Terrain.River, GameMapSelector.TerrainFor(GameMap.River)));
            foreach (var map in new[] { GameMap.Grassland, GameMap.Fortress, GameMap.Gate, GameMap.Forest })
                Assert.True(ReferenceEquals(Terrain.None, GameMapSelector.TerrainFor(map)));
        }
        [PortedTest(Source)] public static void sides_and_ids_resolve_both_ways() {
            var session = new SessionDefinition("s", 5, -7, 0, new long[0], new long[0]); Assert.Equal((Master?)Master.LeftPlayer, session.GetUserSide(5)); Assert.Equal((Master?)Master.RightPlayer, session.GetUserSide(-7)); Assert.Equal((Master?)null, session.GetUserSide(9)); Assert.Equal((long?)(-7), session.GetUserId(Master.RightPlayer)); Assert.True(session.IsRightBot && !session.IsLeftBot);
        }
        [PortedTest(Source)] public static void decks_are_dealt_from_the_session_seed() {
            var cards = Enumerable.Range(1, 10).Select(i => (long)i).ToArray(); var session = new SessionDefinition("s", 1, 2, 0x0123456789abcdefL, cards, new long[0]);
            Assert.Sequence(new CardDeck(cards, DeckSeedDeriver.ForLeftDeck(session.RandomSeed)).RemainingCards, session.CreateLeftDeck().RemainingCards);
        }
        public static void CheckInitialization() {
            var cards = Enumerable.Range(1, 10).Select(i => (long)i).ToArray(); var session = new SessionDefinition("s", 1, 2, 0x0123456789abcdefL, cards, cards, SessionType.PVE, GameMap.River, 12);
            cards[0] = 99; Assert.Equal(1L, session.LeftDeck[0]); Assert.Equal(1L, session.RightDeck[0]);
            var a = new SimulationContext(1); var b = new SimulationContext(1); session.InitializeIdentityAndDecks(a); session.InitializeIdentityAndDecks(b);
            Assert.Sequence(a.CardDecks[Master.LeftPlayer].RemainingCards, b.CardDecks[Master.LeftPlayer].RemainingCards); Assert.Sequence(a.CardDecks[Master.RightPlayer].RemainingCards, b.CardDecks[Master.RightPlayer].RemainingCards);
            Assert.True(!a.CardDecks[Master.LeftPlayer].RemainingCards.SequenceEqual(a.CardDecks[Master.RightPlayer].RemainingCards));
            a.CardDecks[Master.LeftPlayer].ReturnCard(999); Assert.Equal(10, b.CardDecks[Master.LeftPlayer].RemainingCards.Length);
            Assert.Equal(session, a.Service<SessionDefinition>()); Assert.Equal(SessionType.PVE, session.Type); Assert.Equal(GameMap.River, session.Map); Assert.Equal((long?)12, session.ScenarioId);
            bool rejected = false; try { session.InitializeIdentityAndDecks(a); } catch (InvalidOperationException) { rejected = true; } Assert.True(rejected); Assert.Equal(11, a.CardDecks[Master.LeftPlayer].RemainingCards.Length);
        }
    }
}
