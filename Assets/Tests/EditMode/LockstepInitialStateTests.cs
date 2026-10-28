using ArcaneCasters.Simulation;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests {
    public class LockstepInitialStateTests {
        internal const string PveSnapshot = @"{
            ""scenarioId"":5,""objectives"":[""wall""],
            ""installers"":[{""installerId"":""wall"",""prefabType"":""Wall"",""master"":""None"",""positionX"":0,""positionY"":0,""positionZ"":0}],
            ""events"":[{""eventId"":""intro"",""triggerType"":""FrameNumGte"",""triggerValue"":0,""speakerInstallerId"":""wall"",""messageKey"":""intro"",""lines"":[""hello""],""actions"":[
                {""actionType"":""SetSpawner"",""installerId"":""wall"",""count"":0,""intervalRaw"":4294967297},
                {""actionType"":""PlayBgm"",""bgmKey"":""battle""}]}],
            ""shields"":[],""rules"":{""winCondition"":""Survive"",""surviveSeconds"":1}
        }";
        private sealed class PveSink : IPveSessionSink {
            public long LeftUserId => 7;
            public long RightUserId => 9;
            public int Scripts, States;
            public void SendObjective(long id, PveObjectiveState state) { }
            public void SendScriptEvent(long id, PveScriptEvent message) { Scripts++; Assert.AreEqual("hello", message.Lines[0]); }
            public void SendState(long id, PveChannelState state) { States++; Assert.AreEqual("battle", state.Value); }
        }
        internal const string Snapshot = @"{
            ""schemaVersion"":1,""randomSeed"":9223372036854775807,""sessionType"":""PVP"",""map"":""Grassland"",""leftDeck"":[],""rightDeck"":[],
            ""content"":{""schemaVersion"":1,""parameters"":[
                {""key"":""player.hp"",""rawValue"":429496729600},
                {""key"":""player.max_mana"",""rawValue"":42949672960},
                {""key"":""game.duration"",""rawValue"":4294967296000},
                {""key"":""game.fever_duration"",""rawValue"":0},
                {""key"":""precision"",""rawValue"":9007199254740993}],""magics"":[],""leftOwnedMagicIds"":[],""rightOwnedMagicIds"":[]},
            ""tags"":{""magicTags"":[],""objectTags"":[],""counterRules"":[]},
            ""profiles"":[{""userId"":7,""noviceProgressRaw"":2147483649},{""userId"":9,""noviceProgressRaw"":4294967296}]
        }";
        [Test] public void JsonCodecPreservesRawIntegersThroughRoundTrip() {
            var state = JsonCodec.Deserialize<BattleInitialState>(Snapshot);
            var copy = JsonCodec.Deserialize<BattleInitialState>(JsonCodec.Serialize(state));
            Assert.AreEqual(long.MaxValue, copy.randomSeed);
            Assert.AreEqual(9007199254740993L, copy.content.parameters[4].rawValue);
            Assert.AreEqual(2147483649L, copy.profiles[0].noviceProgressRaw);
        }
        [Test] public void DecodedSnapshotRunsToMatchCompletion() {
            var state = JsonCodec.Deserialize<BattleInitialState>(Snapshot);
            var runtime = state.CreateRuntime(new SessionStart { sessionId = "json", participants = new long[] { 7, 9 }, tickRate = 20 });
            for (int frame = 1; frame <= 20; frame++) runtime.AdvanceConfirmedFrame(frame);
            Assert.AreEqual(4, runtime.Context.Players[Master.LeftPlayer].Mana);
            for (int frame = 21; frame <= 40; frame++) runtime.AdvanceConfirmedFrame(frame);
            Assert.IsTrue(runtime.Loop.StopRequested); Assert.IsNull(runtime.Context.Result.Loser);
            // 종료 유예 중에도 fever 충전이 적용되어 최대 마나에 도달한다.
            Assert.AreEqual(10, runtime.Context.Players[Master.LeftPlayer].Mana);
            Assert.AreEqual(9007199254740993L, runtime.Context.GameParameters["precision"].RawValue);
        }
        [Test] public void ConfirmedMapPolicyMustResolveToTheAnnouncedMap() {
            var state = JsonCodec.Deserialize<BattleInitialState>(Snapshot);
            var start = new SessionStart { sessionId = "map-policy", participants = new long[] { 7, 9 }, tickRate = 20 };
            state.mapPolicy = new GameMapPolicySnapshot { selection = "Random", coin = false };
            state = JsonCodec.Deserialize<BattleInitialState>(JsonCodec.Serialize(state));
            Assert.AreEqual(GameMap.Grassland, state.CreateRuntime(start).Definition.Map);
            state.mapPolicy.coin = true;
            Assert.Throws<System.IO.InvalidDataException>(() => state.CreateRuntime(start));
            state.mapPolicy.coin = null;
            Assert.Throws<System.IO.InvalidDataException>(() => state.CreateRuntime(start));
            state.mapPolicy.selection = "Grassland";
            Assert.AreEqual(GameMap.Grassland, state.CreateRuntime(start).Definition.Map);
            state.sessionType = "Playground"; state.mapPolicy.selection = "River";
            Assert.AreEqual(GameMap.Grassland, state.CreateRuntime(start).Definition.Map);
        }
        [Test] public void PveJsonPreservesRawIntervalAndFreezesScenario() {
            var source = JsonCodec.Deserialize<BattlePveSnapshot>(PveSnapshot);
            var copy = JsonCodec.Deserialize<BattlePveSnapshot>(JsonCodec.Serialize(source));
            var scenario = copy.Build(); copy.events[0].lines[0] = "changed"; copy.events[0].actions[0].intervalRaw = 0;
            Assert.AreEqual(4294967297L, scenario.Events[0].Actions[0].Interval.Value.RawValue);
            Assert.AreEqual("hello", scenario.Events[0].Lines[0]); Assert.AreEqual("wall", scenario.ObjectiveInstallerIds[0]);
        }
        [Test] public void DecodedPveSnapshotRunsEventsAndEndsWithoutRightPlayer() {
            var state = JsonCodec.Deserialize<BattleInitialState>(Snapshot); state.sessionType = "PVE"; state.scenarioId = 5;
            state.map = "Forest"; state.mapPolicy = new GameMapPolicySnapshot { selection = "River", scenarioMap = "Forest" };
            state.pve = JsonCodec.Deserialize<BattlePveSnapshot>(PveSnapshot); var sink = new PveSink();
            var start = new SessionStart { sessionId = "pve-json", participants = new long[] { 7, 9 }, tickRate = 20 };
            var runtime = state.CreateRuntime(start, pveSink: sink);
            for (int frame = 1; frame <= 40; frame++) runtime.AdvanceConfirmedFrame(frame);
            Assert.IsTrue(runtime.Loop.StopRequested); Assert.AreEqual(Master.RightPlayer, runtime.Context.Result.Loser);
            Assert.IsNull(runtime.Context.FindPlayer(Master.RightPlayer)); Assert.AreEqual(2, sink.Scripts); Assert.AreEqual(2, sink.States);
            state.scenarioId = 6; Assert.Throws<System.IO.InvalidDataException>(() => state.CreateRuntime(start, pveSink: sink));
        }
    }
}
