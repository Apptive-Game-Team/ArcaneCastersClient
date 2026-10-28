using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ArcaneCasters.Simulation;
using Data;
using Global.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using Property = NUnit.Framework.PropertyAttribute;

namespace WordOnline.Tests {
    public class LockstepSceneInputTests {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object ReadInput(string typeName, string json) => typeof(JsonCodec).GetMethod("Deserialize")
            .MakeGenericMethod(Type.GetType(typeName + ", Assembly-CSharp", true)).Invoke(null, new object[] { json });
        private static void WithInputConnection(Action<Type, Component, LockstepLocalInputs> check) {
            var context = Type.GetType("Global.SceneContext, Assembly-CSharp", true);
            var match = context.GetProperty("MatchInfo"); var previous = match.GetValue(null);
            var host = new GameObject("lockstep-wire-input-test");
            try {
                match.SetValue(null, new MatchedInfoDto { lockstepSimulationVersion = LockstepVersion.Supported });
                var type = Type.GetType("GameScene.StompConnector, Assembly-CSharp", true);
                var connector = host.AddComponent(type);
                if (type.GetField("_frameInfoHandler", Private).GetValue(connector) == null) type.GetMethod("Awake", Private).Invoke(connector, null);
                var connectionType = Type.GetType("GameScene.LockstepConnection, Assembly-CSharp", true);
                var connection = host.AddComponent(connectionType);
                type.GetMethod("BindLockstepPresentation").Invoke(connector, new object[] { connection });
                var start = new SessionStart { sessionId = "input", participants = new long[] { 7, 9 }, simulationVersion = LockstepVersion.Supported, tickRate = 20, initialStateJson = LockstepInitialStateTests.Snapshot };
                using (var sha = SHA256.Create()) start.initialHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(start.initialStateJson))).Replace("-", "").ToLowerInvariant();
                var session = new LockstepBattleSession("input", 7, LockstepVersion.Supported, initial => JsonCodec.Deserialize<BattleInitialState>(initial.initialStateJson).CreateRuntime(initial));
                session.Initialize(start);
                connectionType.GetField("battleSession", Private).SetValue(connection, session);
                connectionType.GetField("stream", Private).SetValue(connection, new ConfirmedInputStream("input"));
                session.Receive(new ConfirmedFrame { sessionId = "input", tick = 0, inputs = new[] { Array.Empty<CastCommand>(), Array.Empty<CastCommand>() } });
                check(type, connector, (LockstepLocalInputs)connectionType.GetField("localInputs", Private).GetValue(connection));
            } finally { UnityEngine.Object.DestroyImmediate(host); match.SetValue(null, previous); }
        }
        [Property("OriginalSimulationTest", "game/dto/tests.rs::input_use_magic")]
        [Test] public void InputUseMagic() => WithInputConnection((type, connector, local) => {
            var input = ReadInput("Data.CardUseInput", "{\"type\":\"useMagic\",\"magicId\":34,\"id\":7,\"position\":{\"x\":9.0,\"y\":0.0,\"z\":5.0}}");
            Assert.AreEqual(7, input.GetType().GetField("id").GetValue(input));
            Assert.AreEqual(new Vector3(9, 0, 5), input.GetType().GetField("position").GetValue(input));
            Assert.IsTrue((bool)type.GetMethod("TrySendCardUse").Invoke(connector, new[] { input }));
            var command = local.Prepare(1, 0).commands[0];
            Assert.AreEqual("cast", command.kind); Assert.AreEqual(34, command.magicId);
            Assert.AreEqual(9L << 32, command.xRaw); Assert.AreEqual(5L << 32, command.zRaw);
            Assert.IsTrue(local.TryAcknowledge(new InputResponse(true, InputResultCode.Success, "", 0, 32, 34), out var reply)); Assert.AreEqual(7, reply.Id);
            local.Clear();
            input.GetType().GetField("type").SetValue(input, "emote");
            Assert.IsFalse((bool)type.GetMethod("TrySendCardUse").Invoke(connector, new[] { input }));
            Assert.IsFalse((bool)type.GetMethod("TrySendCardSelection").Invoke(connector, new[] { input }));
            Assert.IsFalse((bool)type.GetMethod("TrySendCardUse").Invoke(connector, new object[] { null }));
            Assert.IsEmpty(local.Prepare(2, 0).commands);
        });
        [Property("OriginalSimulationTest", "game/dto/tests.rs::input_card_aim_and_retired_types")]
        [Test] public void InputCardAimAndRetiredTypes() => WithInputConnection((type, connector, local) => {
            foreach (string kind in new[] { "selectCard", "unselectCard" }) {
                string dto = kind == "selectCard" ? "Data.CardSelectRequestDto" : "Data.CardUnselectRequestDto";
                var input = ReadInput(dto, "{\"type\":\"" + kind + "\",\"magicId\":34,\"id\":7}");
                Assert.AreEqual(7, input.GetType().GetField("id").GetValue(input));
                Assert.IsTrue((bool)type.GetMethod("TrySendCardSelection").Invoke(connector, new[] { input }));
                var command = local.Prepare(1, 0).commands[0]; Assert.AreEqual(kind, command.kind); Assert.AreEqual(34, command.magicId);
                local.Clear();
                foreach (string retired in new[] { "toggleCard", "cancelCard", "emote", "useMagic" }) {
                    input.GetType().GetField("type").SetValue(input, retired);
                    Assert.IsFalse((bool)type.GetMethod("TrySendCardSelection").Invoke(connector, new[] { input }));
                }
                Assert.IsEmpty(local.Prepare(2, 0).commands); local.Clear();
            }
        });
        [TestCase(false)]
        [TestCase(true)]
        public void ConnectionFailureOrDestroyCallbackTerminatesTheBattleOnce(bool destroy) {
            var host = new GameObject("lockstep-termination-test");
            try {
                var type = Type.GetType("GameScene.LockstepConnection, Assembly-CSharp", true);
                var connection = host.AddComponent(type);
                // EditMode에서는 MonoBehaviour 수명 콜백을 직접 실행한다.
                type.GetMethod("Awake", Private).Invoke(connection, null);
                var initial = new SessionStart { sessionId = "termination", participants = new long[] { 7, 9 }, simulationVersion = LockstepVersion.Supported, tickRate = 20, initialStateJson = LockstepInitialStateTests.Snapshot };
                using (var sha = SHA256.Create()) initial.initialHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(initial.initialStateJson))).Replace("-", "").ToLowerInvariant();
                var session = new LockstepBattleSession(initial.sessionId, 7, LockstepVersion.Supported, start => JsonCodec.Deserialize<BattleInitialState>(start.initialStateJson).CreateRuntime(start));
                session.Initialize(initial);
                type.GetField("battleSession", Private).SetValue(connection, session);
                var runtime = session.Driver.Runtime; int terminated = 0; runtime.Terminated += () => terminated++;
                runtime.AdvanceConfirmedFrame(1); Assert.IsTrue(runtime.IsRunning);
                if (destroy) type.GetMethod("OnDestroy", Private).Invoke(connection, null);
                else type.GetMethod("Fail", Private).Invoke(connection, new object[] { "test connection failure" });
                Assert.IsTrue(runtime.IsTerminated); Assert.IsFalse(runtime.IsRunning); Assert.AreEqual(1, terminated);
                Assert.Throws<InvalidOperationException>(() => runtime.AdvanceConfirmedFrame(2));
                type.GetMethod("OnDestroy", Private).Invoke(connection, null); Assert.AreEqual(1, terminated);
            } finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        [Test] public void BotEmotesReachPresentationAndAreSuppressedDuringRecovery() {
            var host = new GameObject("lockstep-bot-emote-presentation-test");
            try {
                var type = Type.GetType("GameScene.LockstepConnection, Assembly-CSharp", true);
                var connection = host.AddComponent(type);
                type.GetField("memberId", Private).SetValue(connection, 7L);
                type.GetMethod("ConfigureSnapshotBattle").Invoke(connection, new object[] { null, null, null });
                var factory = (Func<SessionStart, BattleSessionRuntime>)type.GetField("createBattle", Private).GetValue(connection);
                var initial = new SessionStart { sessionId = "bot-emote", participants = new long[] { 7, 9 }, simulationVersion = LockstepVersion.Supported, tickRate = 20, initialStateJson = LockstepInitialStateTests.Snapshot };
                using (var sha = SHA256.Create()) initial.initialHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(initial.initialStateJson))).Replace("-", "").ToLowerInvariant();
                var session = new LockstepBattleSession(initial.sessionId, 7, LockstepVersion.Supported, factory);
                session.Initialize(initial);
                var received = new System.Collections.Generic.List<GameScene.Dto.ServerMessage>();
                type.GetEvent("PresentationReceived").AddEventHandler(connection, (Action<GameScene.Dto.ServerMessage>)received.Add);
                session.Driver.Runtime.Emotes.PublishBot(Master.RightPlayer, Emote.Greet);
                Assert.AreEqual(1, received.Count);
                var emote = (GameScene.Dto.EmoteInfo)received[0];
                Assert.AreEqual("RightPlayer", emote.side); Assert.AreEqual("Greet", emote.emote);
                type.GetField("recovering", Private).SetValue(connection, true);
                session.Driver.Runtime.Emotes.PublishBot(Master.RightPlayer, Emote.Cry);
                Assert.AreEqual(1, received.Count);
                type.GetField("recovering", Private).SetValue(connection, false);
                session.Driver.Runtime.Emotes.PublishBot(Master.RightPlayer, Emote.Laugh);
                Assert.AreEqual(2, received.Count);
            } finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        [UnityEngine.TestTools.UnityTest] public System.Collections.IEnumerator CompletionHttpRetriesWithAuthorizationAndPublishesOneResult() => CompletionHttp(false);
        [UnityEngine.TestTools.UnityTest] public System.Collections.IEnumerator SpectatorPollsStoredResultWithoutPostingCompletion() => CompletionHttp(true);
        private System.Collections.IEnumerator CompletionHttp(bool spectator) {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var host = new GameObject("lockstep-completion-http-test");
            var requests = new System.Collections.Generic.List<string>();
            string json = "{\"type\":\"result\",\"leftPlayer\":\"Draw\",\"rightPlayer\":\"Draw\",\"lastLeftPlayerMmr\":1000,\"lastRightPlayerMmr\":1000,\"newLeftPlayerMmr\":1000,\"newRightPlayerMmr\":1000}";
            var server = System.Threading.Tasks.Task.Run(async () => {
                for (int attempt = 0; attempt < 2; attempt++) {
                    using (var client = await listener.AcceptTcpClientAsync())
                    using (var network = client.GetStream()) {
                        var headers = new StringBuilder(); var one = new byte[1];
                        while (!headers.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal)) {
                            if (await network.ReadAsync(one, 0, 1) == 0) throw new System.IO.IOException("Request closed before headers");
                            headers.Append((char)one[0]);
                            if (headers.Length > 8192) throw new System.IO.IOException("Request headers too large");
                        }
                        requests.Add(headers.ToString());
                        string body = attempt == 0 ? "" : json;
                        byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 " + (attempt == 0 ? spectator ? "202 Accepted" : "503 Service Unavailable" : "200 OK")
                            + "\r\nContent-Type: application/json\r\nConnection: close\r\nContent-Length: " + Encoding.UTF8.GetByteCount(body) + "\r\n\r\n" + body);
                        await network.WriteAsync(response, 0, response.Length);
                    }
                }
            });
            System.Collections.IEnumerator routine = null;
            try {
                var type = Type.GetType("GameScene.LockstepConnection, Assembly-CSharp", true);
                var connection = host.AddComponent(type);
                type.GetField("baseUrl", Private).SetValue(connection, "http://127.0.0.1:" + ((System.Net.IPEndPoint)listener.LocalEndpoint).Port);
                type.GetField("token", Private).SetValue(connection, "local-test-token");
                type.GetField("sessionId", Private).SetValue(connection, "completion-http");
                type.GetField("completionPending", Private).SetValue(connection, true);
                if (spectator) type.GetMethod("ConfigureSpectator").Invoke(connection, null);
                var initial = new SessionStart { sessionId = "completion-http", participants = new long[] { 7, 9 }, simulationVersion = LockstepVersion.Supported, tickRate = 20, initialStateJson = LockstepInitialStateTests.Snapshot };
                using (var sha = SHA256.Create()) initial.initialHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(initial.initialStateJson))).Replace("-", "").ToLowerInvariant();
                var session = new LockstepBattleSession(initial.sessionId, 7, LockstepVersion.Supported, start => JsonCodec.Deserialize<BattleInitialState>(start.initialStateJson).CreateRuntime(start));
                session.Initialize(initial);
                for (uint tick = 0; tick < 40; tick++) session.Receive(new ConfirmedFrame { sessionId = initial.sessionId, tick = tick, inputs = new[] { Array.Empty<CastCommand>(), Array.Empty<CastCommand>() } });
                type.GetField("battleSession", Private).SetValue(connection, session);
                var messages = new System.Collections.Generic.List<GameScene.Dto.ServerMessage>();
                var failures = new System.Collections.Generic.List<string>();
                type.GetEvent("PresentationReceived").AddEventHandler(connection, (Action<GameScene.Dto.ServerMessage>)messages.Add);
                type.GetEvent("Stopped").AddEventHandler(connection, (Action<string>)failures.Add);
                routine = (System.Collections.IEnumerator)type.GetMethod("CompleteBattle", Private).Invoke(connection, null);
                double deadline = UnityEditor.EditorApplication.timeSinceStartup + 15;
                while (routine.MoveNext()) {
                    // EditMode에서도 실제 UnityWebRequest와 재시도 대기를 진행한다.
                    while ((routine.Current is AsyncOperation operation && !operation.isDone)
                        || (routine.Current is CustomYieldInstruction wait && wait.keepWaiting)) {
                        Assert.Less(UnityEditor.EditorApplication.timeSinceStartup, deadline, "Completion request timed out");
                        if (server.IsFaulted) throw server.Exception;
                        yield return null;
                    }
                }
                while (!server.IsCompleted) { Assert.Less(UnityEditor.EditorApplication.timeSinceStartup, deadline); yield return null; }
                server.GetAwaiter().GetResult();
                Assert.AreEqual(2, requests.Count);
                foreach (string request in requests) {
                    StringAssert.StartsWith(spectator ? "GET /api/lockstep/completion-http/result HTTP/1.1\r\n" : "POST /api/lockstep/completion-http/complete HTTP/1.1\r\n", request);
                    StringAssert.Contains("Authorization: Bearer local-test-token\r\n", request);
                }
                Assert.IsEmpty(failures); Assert.AreEqual(1, messages.Count);
                Assert.IsInstanceOf<GameScene.Dto.ResultInfo>(messages[0]);
                Assert.IsFalse((bool)type.GetField("completionPending", Private).GetValue(connection));
            } finally {
                (routine as IDisposable)?.Dispose(); listener.Stop(); UnityEngine.Object.DestroyImmediate(host);
            }
        }
        [Test] public void StoredResultRequiresCompletedMatchingBattleAndIsPublishedOnce() {
            var host = new GameObject("lockstep-stored-result-test");
            try {
                var type = Type.GetType("GameScene.LockstepConnection, Assembly-CSharp",true);
                var connection = host.AddComponent(type);
                var initial = new SessionStart { sessionId="stored-result", participants=new long[] {7,9}, simulationVersion=LockstepVersion.Supported, tickRate=20, initialStateJson=LockstepInitialStateTests.Snapshot };
                using (var sha=SHA256.Create()) initial.initialHash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(initial.initialStateJson))).Replace("-","").ToLowerInvariant();
                var session=new LockstepBattleSession(initial.sessionId,7,LockstepVersion.Supported,
                    start=>JsonCodec.Deserialize<BattleInitialState>(start.initialStateJson).CreateRuntime(start));
                session.Initialize(initial); type.GetField("battleSession",Private).SetValue(connection,session);
                var messages=new System.Collections.Generic.List<GameScene.Dto.ServerMessage>();
                Action<GameScene.Dto.ServerMessage> capture=messages.Add;
                type.GetEvent("PresentationReceived").AddEventHandler(connection,capture);
                var receive=type.GetMethod("ReceiveResult",Private);
                var result=new GameScene.Dto.ResultInfo { type="result",leftPlayer="Draw",rightPlayer="Draw",lastLeftPlayerMmr=1000,lastRightPlayerMmr=1100,newLeftPlayerMmr=1003,newRightPlayerMmr=1097 };
                string json=JsonCodec.Serialize(result);
                Assert.IsFalse((bool)receive.Invoke(connection,new object[] {json}));
                for(uint tick=0;tick<40;tick++) session.Receive(new ConfirmedFrame {sessionId=initial.sessionId,tick=tick,inputs=new[] {Array.Empty<CastCommand>(),Array.Empty<CastCommand>()}});
                var invalid=JObject.Parse(json); invalid["leftPlayer"]="Win";
                Assert.IsFalse((bool)receive.Invoke(connection,new object[] {invalid.ToString()}));
                invalid=JObject.Parse(json); invalid.Remove("newLeftPlayerMmr");
                Assert.IsFalse((bool)receive.Invoke(connection,new object[] {invalid.ToString()}));
                Assert.AreEqual(0,messages.Count);
                Assert.IsTrue((bool)receive.Invoke(connection,new object[] {json}));
                Assert.IsTrue((bool)receive.Invoke(connection,new object[] {json}));
                Assert.AreEqual(1,messages.Count);
                Assert.AreEqual(1003,((GameScene.Dto.ResultInfo)messages[0]).newLeftPlayerMmr);
                Assert.AreEqual(1097,((GameScene.Dto.ResultInfo)messages[0]).newRightPlayerMmr);
            } finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        [Test] public void RecoveryAcknowledgesPendingLocalInputOnlyOnce() {
            var host = new GameObject("lockstep-recovery-input-test");
            try {
                var type = Type.GetType("GameScene.LockstepConnection, Assembly-CSharp",true); var connection = host.AddComponent(type);
                type.GetField("memberId",Private).SetValue(connection,7L); type.GetField("recovering",Private).SetValue(connection,true);
                var local = (LockstepLocalInputs)type.GetField("localInputs",Private).GetValue(connection);
                Assert.IsTrue(local.Queue(1,0,0,77)); local.Prepare(5,0);
                var received = new System.Collections.Generic.List<GameScene.Dto.ServerMessage>();
                Action<GameScene.Dto.ServerMessage> capture = received.Add; type.GetEvent("PresentationReceived").AddEventHandler(connection,capture);
                var response = new InputResponse(true,InputResultCode.Success,"",4,LockstepLocalInputs.SimulationRequestId(5,0,0),1);
                var handled = type.GetMethod("OnInputHandled",Private);
                handled.Invoke(connection,new object[] { 9L,response }); Assert.AreEqual(0,received.Count);
                handled.Invoke(connection,new object[] { 7L,response });
                handled.Invoke(connection,new object[] { 7L,response });
                Assert.AreEqual(1,received.Count); Assert.AreEqual(77,((GameScene.Dto.MagicValidInfo)received[0]).id);
                Assert.IsTrue(local.Queue(1,0,0,77));
            } finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void RecoverySuppressesHistoryAndPublishesCurrentSnapshotBeforeCompletion(bool spectator) {
            var host = new GameObject("lockstep-recovery-presentation-test");
            try {
                var type = Type.GetType("GameScene.LockstepConnection, Assembly-CSharp",true); var connection = host.AddComponent(type);
                if (type.GetField("transport",Private).GetValue(connection) == null) type.GetMethod("Awake",Private).Invoke(connection,null);
                type.GetField("memberId",Private).SetValue(connection,spectator ? 99L : 7L);
                if (spectator) type.GetMethod("ConfigureSpectator").Invoke(connection,null);
                var received = new System.Collections.Generic.List<GameScene.Dto.ServerMessage>();
                var order = new System.Collections.Generic.List<string>();
                Action<GameScene.Dto.ServerMessage> capture = message => { received.Add(message); order.Add("snapshot"); };
                type.GetEvent("PresentationReceived").AddEventHandler(connection,capture);
                Action recovered = () => order.Add("recovered"); type.GetEvent("Recovered").AddEventHandler(connection,recovered);
                Action<BattleSessionRuntime> completed = _ => order.Add("completed"); type.GetEvent("BattleCompleted").AddEventHandler(connection,completed);
                type.GetMethod("ConfigureSnapshotBattle").Invoke(connection,new object[] { null,null,null });
                var factory = (Func<SessionStart,BattleSessionRuntime>)type.GetField("createBattle",Private).GetValue(connection);
                var initial = new SessionStart { sessionId = "recovery",participants = new long[] { 7,9 },simulationVersion = LockstepVersion.Supported,tickRate = 20,initialStateJson = LockstepInitialStateTests.Snapshot };
                using (var sha = SHA256.Create()) initial.initialHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(initial.initialStateJson))).Replace("-","").ToLowerInvariant();
                var session = new LockstepBattleSession("recovery",spectator ? 99L : 7L,LockstepVersion.Supported,factory,spectator); session.Initialize(initial);
                type.GetField("battleSession",Private).SetValue(connection,session);
                type.GetField("recovering",Private).SetValue(connection,true);
                ConfirmedFrame Frame(uint tick) => new ConfirmedFrame { sessionId = "recovery",tick = tick,inputs = new[] { Array.Empty<CastCommand>(),Array.Empty<CastCommand>() } };
                for (uint tick = 0; tick < 15; tick++) {
                    session.Driver.Runtime.Context.AddEvent(new BattleEvent("Beam",1,2,1));
                    session.Receive(Frame(tick));
                }
                Assert.AreEqual(0,received.Count);
                type.GetMethod("CompleteRecovery",Private).Invoke(connection,null);
                Assert.AreEqual(1,received.Count); CollectionAssert.AreEqual(new[] { "snapshot","recovered" },order);
                var snapshot = (GameScene.Dto.SyncFrameInfo)received[0];
                Assert.IsTrue(snapshot.suppressSpawnPresentation); Assert.AreEqual(15,snapshot.snapshotResponseDto.frame);
                Assert.AreEqual(session.Driver.Runtime.Context.Objects.Length,snapshot.snapshotResponseDto.objects.Length);
                Assert.AreEqual(spectator ? 0 : session.Driver.Runtime.Context.Players[Master.LeftPlayer].Mana,snapshot.updatedMana);
                if (spectator) {
                    type.GetField("stream",Private).SetValue(connection,new ConfirmedInputStream("recovery"));
                    Assert.IsFalse((bool)type.GetMethod("CanQueueCommands").Invoke(connection,new object[] { 1 }));
                    Assert.IsFalse((bool)type.GetMethod("QueueEmote").Invoke(connection,new object[] { "Cry" }));
                    Assert.IsFalse((bool)type.GetMethod("QueueCardSelection").Invoke(connection,new object[] { 7L,true }));
                    Assert.IsFalse((bool)type.GetMethod("QueueCast").Invoke(connection,new object[] { 7L,0L,0L,null }));
                    type.GetMethod("SendNext",Private).Invoke(connection,null);
                }
                Assert.AreEqual(0,snapshot.events.Count); Assert.AreEqual(0,snapshot.projectileDtos.Count);
                Assert.IsNull(JObject.Parse(JsonCodec.Serialize(snapshot))["suppressSpawnPresentation"]);
                type.GetMethod("CompleteRecovery",Private).Invoke(connection,null); Assert.AreEqual(1,received.Count);
                session.Receive(Frame(15)); Assert.AreEqual(2,received.Count);
                // 이미 끝난 경기의 이력도 마지막 snapshot을 전달한 뒤 종료를 알린다.
                session.Initialize(initial); received.Clear(); order.Clear();
                type.GetField("recovering",Private).SetValue(connection,true);
                var stream = new ConfirmedInputStream("recovery");
                for (uint tick = 0; tick < 40; tick++) stream.Enqueue(Frame(tick));
                type.GetField("stream",Private).SetValue(connection,stream);
                type.GetMethod("Drain",Private).Invoke(connection,null);
                CollectionAssert.AreEqual(new[] { "snapshot","recovered","completed" },order);
                Assert.AreEqual(1,received.Count);
                Assert.AreEqual(40,((GameScene.Dto.SyncFrameInfo)received[0]).snapshotResponseDto.frame);
                Assert.IsTrue((bool)type.GetProperty("IsStopped").GetValue(connection));
            } finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void PveSyncUsesRecoveredLocalStateWithoutAdvancingBattle(bool spectator) {
            var host = new GameObject("lockstep-pve-sync-test");
            try {
                var type = Type.GetType("GameScene.LockstepConnection, Assembly-CSharp",true);
                var connection = host.AddComponent(type);
                type.GetField("memberId",Private).SetValue(connection,spectator ? 99L : 7L);
                if (spectator) type.GetMethod("ConfigureSpectator").Invoke(connection,null);
                var received = new System.Collections.Generic.List<GameScene.Dto.ServerMessage>();
                Action<GameScene.Dto.ServerMessage> capture = received.Add;
                type.GetEvent("PresentationReceived").AddEventHandler(connection,capture);
                type.GetMethod("ConfigureSnapshotBattle").Invoke(connection,new object[] { null,null,null });
                var factory = (Func<SessionStart,BattleSessionRuntime>)type.GetField("createBattle",Private).GetValue(connection);
                var state = JsonCodec.Deserialize<BattleInitialState>(LockstepInitialStateTests.Snapshot);
                state.sessionType = "PVE"; state.scenarioId = 5;
                state.pve = JsonCodec.Deserialize<BattlePveSnapshot>(LockstepInitialStateTests.PveSnapshot);
                var json = JsonCodec.Serialize(state);
                var initial = new SessionStart { sessionId = "pve-sync", participants = new long[] { 7,9 },simulationVersion = LockstepVersion.Supported,tickRate = 20,initialStateJson = json };
                using (var sha = SHA256.Create()) initial.initialHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-","").ToLowerInvariant();
                var session = new LockstepBattleSession("pve-sync",spectator ? 99L : 7L,LockstepVersion.Supported,factory,spectator); session.Initialize(initial);
                type.GetField("battleSession",Private).SetValue(connection,session);
                var send = type.GetMethod("TrySendPveSync");
                Assert.IsFalse((bool)send.Invoke(connection,new object[] { 0 }));
                session.Receive(new ConfirmedFrame { sessionId = "pve-sync",tick = 0,inputs = new[] { Array.Empty<CastCommand>(),Array.Empty<CastCommand>() } });
                received.Clear(); var runtime = session.Driver.Runtime; long random = runtime.Context.Random.State;
                Assert.IsTrue((bool)send.Invoke(connection,new object[] { 0 }));
                Assert.AreEqual(3,received.Count);
                Assert.IsInstanceOf<GameScene.Dto.PveObjectiveInfo>(received[0]);
                Assert.AreEqual(1,((GameScene.Dto.PveScriptEventInfo)received[1]).seq);
                Assert.AreEqual("battle",((GameScene.Dto.PveStateInfo)received[2]).value);
                Assert.AreEqual(1,runtime.Context.FrameNumber); Assert.AreEqual(random,runtime.Context.Random.State);
                received.Clear(); Assert.IsTrue((bool)send.Invoke(connection,new object[] { 1 })); Assert.AreEqual(2,received.Count);
                received.Clear(); type.GetField("recovering",Private).SetValue(connection,true);
                Assert.IsFalse((bool)send.Invoke(connection,new object[] { 0 })); Assert.AreEqual(0,received.Count);
                type.GetField("recovering",Private).SetValue(connection,false); type.GetField("stopped",Private).SetValue(connection,true);
                Assert.IsFalse((bool)send.Invoke(connection,new object[] { 0 }));
            } finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        [Test] public void MatchResponseSelectsLockstepWithoutSerializingComputedFlag() {
            var legacy = JsonCodec.Deserialize<MatchedInfoDto>("{\"sessionId\":\"legacy\"}"); Assert.IsFalse(legacy.IsLockstep);
            var match = new MatchedInfoDto { sessionId = "match", server = "http://127.0.0.1:7777", lockstepSimulationVersion = "lockstep-v1" };
            string json = JsonCodec.Serialize(match); Assert.IsNull(JObject.Parse(json)["IsLockstep"]);
            Assert.IsTrue(JsonCodec.Deserialize<MatchedInfoDto>(json).IsLockstep);
        }
        [Test] public void SceneInputWaitsForFirstFrameAndRejectsFullQueueBeforeChangingSelection() {
            var contextType = Type.GetType("Global.SceneContext, Assembly-CSharp", true);
            var matchProperty = contextType.GetProperty("MatchInfo"); var previousMatch = matchProperty.GetValue(null);
            var host = new GameObject("lockstep-input-test");
            try {
                matchProperty.SetValue(null, new MatchedInfoDto { lockstepSimulationVersion = "lockstep-v1" });
                var connectorType = Type.GetType("GameScene.StompConnector, Assembly-CSharp", true);
                var connector = host.AddComponent(connectorType);
                if (connectorType.GetField("_frameInfoHandler", Private).GetValue(connector) == null) connectorType.GetMethod("Awake", Private).Invoke(connector, null);
                var connectionType = Type.GetType("GameScene.LockstepConnection, Assembly-CSharp", true);
                var connection = host.AddComponent(connectionType);
                connectorType.GetMethod("BindLockstepPresentation").Invoke(connector, new object[] { connection });
                var start = new SessionStart { sessionId = "test", participants = new long[] { 7, 9 }, simulationVersion = "lockstep-v1", tickRate = 20, initialStateJson = LockstepInitialStateTests.Snapshot };
                using (var sha = SHA256.Create()) start.initialHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(start.initialStateJson))).Replace("-", "").ToLowerInvariant();
                var session = new LockstepBattleSession("test", 7, "lockstep-v1", initial => JsonCodec.Deserialize<BattleInitialState>(initial.initialStateJson).CreateRuntime(initial));
                session.Initialize(start);
                connectionType.GetField("battleSession", Private).SetValue(connection, session);
                connectionType.GetField("stream", Private).SetValue(connection, new ConfirmedInputStream("test"));
                var capacity = connectorType.GetMethod("CanQueueBattleInputs");
                Assert.IsFalse((bool)capacity.Invoke(connector, new object[] { 1 }));
                session.Receive(new ConfirmedFrame { sessionId = "test", tick = 0, inputs = new[] { Array.Empty<CastCommand>(), Array.Empty<CastCommand>() } });
                Assert.IsTrue((bool)capacity.Invoke(connector, new object[] { 2 }));
                var local = (LockstepLocalInputs)connectionType.GetField("localInputs", Private).GetValue(connection);
                var sendEmote = connectorType.GetMethod("TrySendEmote");
                Assert.IsFalse((bool)sendEmote.Invoke(connector, new object[] { "cry" }));
                Assert.IsTrue((bool)sendEmote.Invoke(connector, new object[] { "Cry" }));
                var emote = local.Prepare(1, 0).commands[0];
                Assert.AreEqual("emote", emote.kind); Assert.AreEqual("Cry", emote.emote); Assert.AreEqual(0, emote.magicId);
                local.Clear();
                var castType = Type.GetType("Data.CardUseInput, Assembly-CSharp", true);
                var cast = Activator.CreateInstance(castType, new object[] { 7L, new Vector3(1.25f, 0, 2.5f) });
                Assert.IsTrue((bool)connectorType.GetMethod("TrySendCardUse").Invoke(connector, new[] { cast }));
                var batch = local.Prepare(1, 0); Assert.AreEqual(1, batch.commands.Length);
                Assert.AreEqual(5368709120L, batch.commands[0].xRaw); Assert.AreEqual(10737418240L, batch.commands[0].zRaw);
                Assert.IsTrue(local.TryAcknowledge(new InputResponse(true, InputResultCode.Success, "ok", 0, 32, 7), out var response));
                Assert.AreEqual(castType.GetField("id").GetValue(cast), response.Id);
                var invalidCast = Activator.CreateInstance(castType, new object[] { 7L, new Vector3(float.NaN, 0, 2) });
                Assert.IsFalse((bool)connectorType.GetMethod("TrySendCardUse").Invoke(connector, new[] { invalidCast }));
                local.Clear();
                var selectType = Type.GetType("Data.CardSelectRequestDto, Assembly-CSharp", true);
                var select = Activator.CreateInstance(selectType, new object[] { 7L });
                Assert.IsTrue((bool)connectorType.GetMethod("TrySendCardSelection").Invoke(connector, new[] { select }));
                Assert.AreEqual("selectCard", local.Prepare(2, 0).commands[0].kind); local.Clear();
                for (int i = 0; i < 15; i++) Assert.IsTrue(local.QueueSelection(7, true));
                Assert.IsTrue((bool)capacity.Invoke(connector, new object[] { 1 }));
                Assert.IsFalse((bool)capacity.Invoke(connector, new object[] { 2 }));
                Assert.IsTrue(local.QueueSelection(7, false));
                Assert.IsFalse((bool)capacity.Invoke(connector, new object[] { 1 }));
                local.Clear(); connectionType.GetField("recovering", Private).SetValue(connection, true);
                Assert.IsFalse((bool)capacity.Invoke(connector, new object[] { 1 }));
            } finally { UnityEngine.Object.DestroyImmediate(host); matchProperty.SetValue(null, previousMatch); }
        }
    }
}
