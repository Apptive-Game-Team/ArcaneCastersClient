using System;
using System.IO;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class InitialStateChecks {
        public static BattleInitialState Example() => new BattleInitialState {
            schemaVersion = 1, randomSeed = 123, sessionType = "PVP", map = "Grassland", leftDeck = new long[] { 1 }, rightDeck = new long[] { 1 },
            content = new BattleContentSnapshot {
                schemaVersion = 1, parameters = new[] {
                    new BattleParameterRow { key = "player.hp", rawValue = 100L << 32 }, new BattleParameterRow { key = "player.max_mana", rawValue = 10L << 32 },
                    new BattleParameterRow { key = "game.duration", rawValue = 1000L << 32 }, new BattleParameterRow { key = "game.fever_duration", rawValue = 0 }
                }, magics = new[] { new BattleMagicRow { id = 1, name = "lightning_explosion", element = "Lightning" } }, leftOwnedMagicIds = new long[] { 1 }, rightOwnedMagicIds = new long[] { 1 }
            },
            tags = new BattleTagSnapshot { magicTags = new BattleMagicTags[0], objectTags = new BattleObjectTags[0], counterRules = new BattleCounterRule[0] },
            profiles = new[] {
                new BattleParticipantSnapshot { userId = 7, noviceProgressRaw = 1L << 31, persona = new BattlePersonaSnapshot { name = "test", tier = "Elite", thinkingTimeMs = 125, reactionIntervalFrames = 8, counterAggressionRaw = (1L << 30) + 1, enabled = true, hospitality = true, temperament = "Stoic", brainType = "Rule" } },
                new BattleParticipantSnapshot { userId = 9, noviceProgressRaw = 1L << 32 }
            }
        };
        public static void Run() {
            var start = new SessionStart { sessionId = "initial", participants = new long[] { 7, 9 }, tickRate = 20 };
            var snapshot = Example(); var runtime = snapshot.CreateRuntime(start);
            snapshot.profiles[0].persona.tier = "Beginner"; snapshot.leftDeck[0] = 99; snapshot.content.parameters[0].rawValue = 1;
            runtime.BotLifecycle.ActivateForUser(7); Assert.Equal(BotTier.Elite, runtime.Bots.Left.Persona.Tier);
            Assert.Equal((1L << 30) + 1, runtime.Bots.Left.Persona.CounterAggression.RawValue); Assert.True(runtime.Bots.Left.Persona.Hospitality);
            runtime.BotLifecycle.DeactivateForUser(7);
            for (int frame = 1; frame <= 40; frame++) runtime.AdvanceConfirmedFrame(frame);
            Assert.True(runtime.Loop.StopRequested); Assert.True(runtime.Context.Result.Loser == null);
            Assert.Equal(100, runtime.Context.Players[Master.LeftPlayer].Hp); Assert.Sequence(new long[] { 1 }, runtime.Context.Players[Master.LeftPlayer].Cards);
            foreach (Action<BattleInitialState> invalidate in new Action<BattleInitialState>[] {
                s => s.schemaVersion = 0, s => s.sessionType = "0", s => s.map = "unknown", s => s.profiles[1].userId = 7,
                s => s.profiles[0].persona.brainType = "RULE", s => s.profiles[0].persona.temperament = null
            }) {
                var bad = Example(); invalidate(bad); bool rejected = false;
                try { bad.CreateRuntime(start); } catch (InvalidDataException) { rejected = true; } Assert.True(rejected);
            }
        }
    }
}
