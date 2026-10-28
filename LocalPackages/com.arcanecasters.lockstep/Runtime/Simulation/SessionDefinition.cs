using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcaneCasters.Simulation {
    public enum SessionType { PVP, Practice, PVE, Playground }
    public enum GameMap { Grassland, River, Fortress, Gate, Forest }
    public static class DeckSeedDeriver {
        public static long ForLeftDeck(long seed) => Derive(seed, 0x4c454654L);
        public static long ForRightDeck(long seed) => Derive(seed, 0x5249474854L);
        private static long Derive(long seed, long salt) {
            unchecked {
                ulong value = (ulong)(seed ^ salt); value ^= value >> 33; value *= 0xff51afd7ed558ccdUL;
                value ^= value >> 33; value *= 0xc4ceb9fe1a85ec53UL; value ^= value >> 33; return (long)value;
            }
        }
    }
    public sealed class SessionDefinition {
        public string SessionId { get; }
        public long LeftUserId { get; }
        public long RightUserId { get; }
        public long RandomSeed { get; }
        public SessionType Type { get; }
        public GameMap Map { get; }
        public long? ScenarioId { get; }
        public IReadOnlyList<long> LeftDeck { get; }
        public IReadOnlyList<long> RightDeck { get; }
        public bool IsLeftBot => LeftUserId < 0;
        public bool IsRightBot => RightUserId < 0;
        // seed는 매치 생성 응답에서 확정한다. 클라이언트에서 임의로 생성하지 않는다.
        public SessionDefinition(string sessionId, long leftUserId, long rightUserId, long randomSeed, IEnumerable<long> leftCards, IEnumerable<long> rightCards, SessionType type = SessionType.PVP, GameMap map = GameMap.Grassland, long? scenarioId = null) {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId)); LeftUserId = leftUserId; RightUserId = rightUserId; RandomSeed = randomSeed; Type = type; Map = map; ScenarioId = scenarioId;
            LeftDeck = Array.AsReadOnly((leftCards ?? throw new ArgumentNullException(nameof(leftCards))).ToArray()); RightDeck = Array.AsReadOnly((rightCards ?? throw new ArgumentNullException(nameof(rightCards))).ToArray());
        }
        public Master? GetUserSide(long id) => id == LeftUserId ? Master.LeftPlayer : id == RightUserId ? Master.RightPlayer : (Master?)null;
        public long? GetUserId(Master side) => side == Master.LeftPlayer ? LeftUserId : side == Master.RightPlayer ? RightUserId : (long?)null;
        public CardDeck CreateLeftDeck() => new CardDeck(LeftDeck.ToArray(), DeckSeedDeriver.ForLeftDeck(RandomSeed));
        public CardDeck CreateRightDeck() => new CardDeck(RightDeck.ToArray(), DeckSeedDeriver.ForRightDeck(RandomSeed));
        public void InitializeIdentityAndDecks(SimulationContext context) {
            if (context.UserIds.Count != 0 || context.CardDecks.Count != 0) throw new InvalidOperationException("Session identity and decks have already been initialized.");
            context.UserIds.Add(Master.LeftPlayer, LeftUserId); context.UserIds.Add(Master.RightPlayer, RightUserId);
            context.CardDecks.Add(Master.LeftPlayer, CreateLeftDeck()); context.CardDecks.Add(Master.RightPlayer, CreateRightDeck());
            context.InsertService(this);
        }
    }
}
