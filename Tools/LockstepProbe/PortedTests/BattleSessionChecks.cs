using System;
using System.Collections.Generic;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BattleSessionChecks {
        private sealed class Lookup : IMagicLookup {
            private readonly BoundMagic magic = new BoundMagic(1, "strike", (context, side, origin, target) => context.FindPlayer(side == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer).GetComponent<PlayerHealthComponent>().OnDamaged(new Hit(10, 0, Element.None)));
            public BoundMagic GetMagic(long id) => id == 1 ? magic : null;
            public BoundMagic ParseMagic(long user, long id) => GetMagic(id);
        }
        private sealed class Counters : IBotCounterScorer {
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
        }
        private sealed class PveSink : IPveSessionSink {
            public long LeftUserId { get; }
            public long RightUserId { get; }
            public PveSink(long left = -1, long right = 2) { LeftUserId = left; RightUserId = right; }
            public int Objectives;
            public void SendObjective(long id, PveObjectiveState state) { Objectives++; }
            public void SendScriptEvent(long id, PveScriptEvent message) { }
            public void SendState(long id, PveChannelState state) { }
        }
        internal static BattleSessionRuntime Create(SessionType type = SessionType.Practice, long leftUserId = -1, long rightUserId = 2, long? scenarioId = null, BotDecisionSourceFactory decisionFactory = null) {
            var c = new SimulationContext(1); c.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100)) { Mana = 50 }; c.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100)) { Mana = 50 };
            c.Players[Master.LeftPlayer].AddCard(1); c.Players[Master.LeftPlayer].AddCard(1);
            c.GameParameters["player.hp"] = 20; c.GameParameters["strike.mana_cost"] = 5; c.GameParameters["strike.range"] = 20; c.GameParameters["strike.damage"] = 10;
            StandardPrefabs.Register(c); c.InitializationFailed += (_, error) => throw error;
            return new BattleSessionRuntime(c, new SessionDefinition("test", leftUserId, rightUserId, 123, new long[] { 1, 1 }, new long[0], type, scenarioId: scenarioId), new Lookup(), new TestBotCatalog(new BotMagic(1, "strike")), new Counters(), _ => new BotPersona { Tier = BotTier.Elite, ThinkingTimeMs = 0, ReactionIntervalFrames = 1, CounterAggression = 0 }, _ => (Fix64)0.5m, o => o.GetComponent<MobComponent>()?.Hp, 100000, 10000, decisionFactory: decisionFactory);
        }
        private sealed class FailedModelSource : IBotDecisionSource {
            public readonly BotDecisionProtocolException Error = new BotDecisionProtocolException("conflicting confirmed model event");
            public InputDecision Think(BotEye eye, IDictionary<string, Fix64> parameters, Master side, bool overdue) => throw Error;
        }
        private static void CheckModelProtocolFailure() {
            var source = new FailedModelSource();
            var runtime = Create(decisionFactory: (side, generation, persona, fallback) => {
                Assert.Equal(Master.LeftPlayer, side); Assert.Equal(0L, generation); return source;
            });
            var driver = new LockstepBattleDriver(runtime);
            int terminated = 0; runtime.Terminated += () => terminated++;
            bool failed = false;
            try { driver.Receive(new ConfirmedFrame { sessionId = "test", tick = 0, inputs = new[] { Array.Empty<CastCommand>(), Array.Empty<CastCommand>() } }); }
            catch (BotDecisionProtocolException error) { failed = ReferenceEquals(source.Error, error); }
            Assert.True(failed); Assert.True(driver.Failed); Assert.True(runtime.IsTerminated);
            Assert.True(ReferenceEquals(source.Error, runtime.Failure)); Assert.Equal(1, terminated);
            failed = false;
            try { driver.Receive(new ConfirmedFrame { sessionId = "test", tick = 1, inputs = new[] { Array.Empty<CastCommand>(), Array.Empty<CastCommand>() } }); }
            catch (InvalidOperationException) { failed = true; }
            Assert.True(failed); Assert.Equal(1, terminated);
        }
        public static void Run() {
            CheckModelProtocolFailure();
            var failed = Create(SessionType.PVP);
            var firstError = new InvalidOperationException("verification fixture");
            failed.Context.SubmitAction("firstFailure", _ => throw firstError);
            failed.Context.SubmitAction("secondFailure", _ => throw new InvalidOperationException("later failure"));
            failed.AdvanceConfirmedFrame(1);
            Assert.True(ReferenceEquals(firstError, failed.Failure));
            failed.AdvanceConfirmedFrame(2);
            Assert.True(ReferenceEquals(firstError, failed.Failure));
            var traces = new List<string>[] { new List<string>(), new List<string>() };
            for (int run = 0; run < 2; run++) {
                var runtime = Create(); var c = runtime.Context; int ended = 0; runtime.Loop.HandleGameEnd = _ => ended++;
                Assert.True(runtime.Bots.HasBotAgent(Master.LeftPlayer)); Assert.True(!runtime.Bots.HasBotAgent(Master.RightPlayer));
                Assert.True(runtime.Emotes.ReceivePlayer(2, "Greet")); Assert.True(!runtime.Emotes.Cooldown.TryConsume(Master.RightPlayer));
                bool skipped = false; try { runtime.AdvanceConfirmedFrame(2); } catch (InvalidOperationException) { skipped = true; } Assert.True(skipped); Assert.Equal(0, c.FrameNumber);
                for (int frame = 1; frame <= 100 && !runtime.Loop.StopRequested; frame++) {
                    runtime.AdvanceConfirmedFrame(frame); traces[run].Add(c.FrameNumber + ":" + c.Players[Master.LeftPlayer].Mana + ":" + c.Players[Master.RightPlayer].Hp + ":" + c.PendingActionCount + ":" + c.Result.Ended);
                }
                Assert.True(runtime.Loop.StopRequested); Assert.Equal(1, ended); Assert.Equal((Master?)Master.RightPlayer, c.Result.Loser); Assert.Equal(44, c.Players[Master.LeftPlayer].Mana); Assert.Equal(1, c.Players[Master.LeftPlayer].Cards.Count);
                bool stopped = false; try { runtime.AdvanceConfirmedFrame(c.FrameNumber + 1); } catch (InvalidOperationException) { stopped = true; } Assert.True(stopped);
            }
            Assert.Sequence(traces[0], traces[1]);
            var pve = Create(SessionType.PVE); var sink = new PveSink();
            bool uninitialized = false; try { pve.AdvanceConfirmedFrame(1); } catch (InvalidOperationException) { uninitialized = true; } Assert.True(uninitialized);
            var registry = new PveScenarioRegistry(id => new PveScenario(new PveScenarioEvent[0], rules: new PveScenarioRules(PveWinCondition.Survive, 1)));
            pve.InitializePve(registry, sink); int pveEnded = 0; pve.Loop.HandleGameEnd = _ => pveEnded++;
            var completionOrder = new List<string>();
            pve.PveResultDecided += loser => { Assert.Equal<Master?>(Master.RightPlayer, loser); completionOrder.Add("decided"); };
            pve.PveResultReady += (user, result) => {
                Assert.Equal(false, pve.Loop.StopRequested); Assert.Equal("result", result.Type); Assert.Equal("Win", result.LeftPlayer); Assert.Equal("Lose", result.RightPlayer);
                Assert.Equal((short)0, result.Mmr.LastLeftMmr); Assert.Equal((short)0, result.Mmr.LastRightMmr); Assert.Equal((short)0, result.Mmr.NewLeftMmr); Assert.Equal((short)0, result.Mmr.NewRightMmr);
                completionOrder.Add("send:" + user);
            };
            bool duplicate = false; try { pve.InitializePve(registry, sink); } catch (InvalidOperationException) { duplicate = true; } Assert.True(duplicate);
            for (int frame = 1; frame <= 40; frame++) pve.AdvanceConfirmedFrame(frame);
            Assert.True(pve.Loop.StopRequested); Assert.Equal(1, pveEnded); Assert.Equal<Master?>(Master.RightPlayer, pve.Context.Result.Loser); Assert.Equal(4, sink.Objectives);
            Assert.Equal(false, pve.Bots.HasBotAgent(Master.LeftPlayer)); Assert.Equal(false, pve.Loop.HasTimeLimit);
            Assert.Sequence(new[] { "decided", "send:2", "send:-1" }, completionOrder);
            var draw = new BattleResultMessage(null, PveMatch.GameEndMmr()); Assert.Equal("Draw", draw.LeftPlayer); Assert.Equal("Draw", draw.RightPlayer);
            var defeat = new BattleResultMessage(Master.LeftPlayer, PveMatch.GameEndMmr()); Assert.Equal("Lose", defeat.LeftPlayer); Assert.Equal("Win", defeat.RightPlayer);
            CheckConfirmedBattle();
            CheckBattleRecovery();
            CheckSpectatorRecovery();
            CheckBattleContent();
            CheckContentRuntimeFactory();
        }
        private static void CheckContentRuntimeFactory() {
            var content = new BattleContent(new BattleContentSnapshot {
                schemaVersion = 1, parameters = new[] {
                    new BattleParameterRow { key = "player.hp", rawValue = 100L << 32 }, new BattleParameterRow { key = "player.max_mana", rawValue = 10L << 32 },
                    new BattleParameterRow { key = "game.duration", rawValue = 100000L << 32 }, new BattleParameterRow { key = "game.fever_duration", rawValue = 10000L << 32 }
                }, magics = new[] { new BattleMagicRow { id = 1, name = "lightning_explosion", element = "Lightning" } }, leftOwnedMagicIds = new long[] { 1 }, rightOwnedMagicIds = new long[] { 1 }
            }, 7, 9);
            var factory = new BattleRuntimeFactory(content, new Counters(), _ => new BotPersona(), _ => Fix64.Zero);
            var definition = new SessionDefinition("factory", 7, 9, 123, new long[] { 1, 1 }, new long[] { 1 });
            var a = factory.Create(definition); var b = factory.Create(definition);
            Assert.Equal(0, a.Context.Players[Master.LeftPlayer].Mana); Assert.Equal(0, a.Context.Players[Master.LeftPlayer].Cards.Count);
            for (int frame = 1; frame <= 20; frame++) { a.AdvanceConfirmedFrame(frame); b.AdvanceConfirmedFrame(frame); }
            Assert.Equal(4, a.Context.Players[Master.LeftPlayer].Mana); Assert.Equal(4, a.Context.Players[Master.RightPlayer].Mana);
            Assert.Sequence(new long[] { 1 }, a.Context.Players[Master.LeftPlayer].Cards); Assert.Sequence(new long[] { 1 }, a.Economy.Left.DrawnCards);
            Assert.Equal(100, a.Context.Players[Master.LeftPlayer].Hp); Assert.Equal(a.Context.Players[Master.LeftPlayer].Mana, b.Context.Players[Master.LeftPlayer].Mana);
            a.Context.GameParameters["player.hp"] = 1; Assert.Equal((Fix64)100, b.Context.GameParameters["player.hp"]);
            var pve = factory.Create(new SessionDefinition("pve", 7, 9, 123, new long[] { 1 }, new long[0], SessionType.PVE), scenarios: new PveScenarioRegistry(_ => new PveScenario(new PveScenarioEvent[0], rules: new PveScenarioRules(PveWinCondition.Survive, 1))), pveSink: new PveSink(7, 9));
            for (int frame = 1; frame <= 40; frame++) pve.AdvanceConfirmedFrame(frame);
            Assert.True(pve.Loop.StopRequested); Assert.True(pve.Context.FindPlayer(Master.RightPlayer) == null); Assert.Equal<Master?>(Master.RightPlayer, pve.Context.Result.Loser);
        }
        private static void CheckBattleContent() {
            BattleContentSnapshot Snapshot() => new BattleContentSnapshot {
                schemaVersion = 1,
                parameters = new[] { new BattleParameterRow { key = "player.hp", rawValue = 100L << 32 }, new BattleParameterRow { key = "spell.range", rawValue = (1L << 32) + 1 } },
                magics = new[] { new BattleMagicRow { id = 2, name = "water_explosion", element = "Water" }, new BattleMagicRow { id = 1, name = "lightning_explosion", element = "Lightning" } },
                leftOwnedMagicIds = new long[] { 1 }, rightOwnedMagicIds = new long[] { 2 }
            };
            var snapshot = Snapshot(); var content = new BattleContent(snapshot, 7, 9);
            snapshot.parameters[1].rawValue = 0; snapshot.magics[1].name = "changed"; snapshot.leftOwnedMagicIds[0] = 2;
            var first = new SimulationContext(0); var parser = content.Configure(first);
            Assert.Equal((1L << 32) + 1, first.GameParameters["spell.range"].RawValue);
            Assert.Equal("lightning_explosion", parser.ParseMagic(7, 1).Name); Assert.True(parser.ParseMagic(7, 2) == null);
            Assert.True(parser.ParseMagic(9, 2) != null); Assert.True(parser.ParseMagic(99, 1) == null);
            Assert.Equal(Element.Lightning, parser.GetMagic(1).Element); Assert.True(first.BossMagicParser.ParseBossMagic("lightning_explosion") != null);
            var second = new SimulationContext(0); content.Configure(second); first.GameParameters["spell.range"] = 0;
            Assert.Equal((1L << 32) + 1, second.GameParameters["spell.range"].RawValue);
            bool rejected = false; try { content.Configure(second); } catch (InvalidOperationException) { rejected = true; } Assert.True(rejected);
            foreach (Action<BattleContentSnapshot> invalidate in new Action<BattleContentSnapshot>[] {
                s => s.schemaVersion = 2, s => s.parameters[1].key = "player.hp", s => s.magics[1].id = 2,
                s => s.magics[0].element = "1", s => s.leftOwnedMagicIds[0] = 99
            }) {
                var bad = Snapshot(); invalidate(bad); rejected = false;
                try { new BattleContent(bad, 7, 9); } catch (System.IO.InvalidDataException) { rejected = true; } Assert.True(rejected);
            }
        }
        private static void CheckBattleRecovery() {
            int created = 0;
            var session = new LockstepBattleSession("test", 7, LockstepVersion.Supported, initial => { created++; return Create(SessionType.PVP, initial.participants[0], initial.participants[1]); });
            SessionStart Initial(string state = "{}") => new SessionStart { sessionId = "test", participants = new long[] { 7, 9 }, simulationVersion = LockstepVersion.Supported, tickRate = 20, initialStateJson = state, initialHash = SessionStart.Digest(System.Text.Encoding.UTF8.GetBytes(state)) };
            ConfirmedFrame Frame(uint tick) => new ConfirmedFrame { sessionId = "test", tick = tick, inputs = new[] { new CastCommand[0], new CastCommand[0] } };
            var unsupported = Initial(); unsupported.simulationVersion = "future-version";
            bool unsupportedRejected = false;
            try { unsupported.Validate("test", 7, "future-version"); } catch (System.IO.InvalidDataException) { unsupportedRejected = true; }
            Assert.True(unsupportedRejected);
            var botMatch = Initial(); botMatch.participants = new long[] { 7, -1 };
            Assert.Equal(0, botMatch.Validate("test", 7, LockstepVersion.Supported));
            bool botRejected = false;
            try { botMatch.Validate("test", -1, LockstepVersion.Supported); } catch (System.IO.InvalidDataException) { botRejected = true; }
            Assert.True(botRejected);
            foreach (string id in new[] { "123", "bot-123", "pve-456", "match_a" }) Assert.True(LockstepVersion.ValidSessionId(id));
            foreach (string id in new[] { "", "../frames", "pve/1", "pve?x=1", new string('a', 129) }) Assert.True(!LockstepVersion.ValidSessionId(id));
            session.Initialize(Initial()); session.Receive(Frame(0)); var prior = session.Driver.Runtime;
            session.Initialize(Initial()); Assert.Equal(0, session.Driver.Runtime.Context.FrameNumber); Assert.Equal(false, ReferenceEquals(prior, session.Driver.Runtime));
            Assert.True(prior.IsTerminated && !prior.IsRunning);
            session.Receive(Frame(0)); Assert.Equal(prior.Context.FrameNumber, session.Driver.Runtime.Context.FrameNumber); Assert.Equal(2, created);
            bool rejected = false; try { session.Initialize(Initial("{\"changed\":true}")); } catch (System.IO.InvalidDataException) { rejected = true; }
            Assert.True(rejected); Assert.Equal(2, created);
            var changedSides = Initial(); changedSides.participants = new long[] { 9, 7 }; rejected = false;
            try { session.Initialize(changedSides); } catch (System.IO.InvalidDataException) { rejected = true; } Assert.True(rejected); Assert.Equal(2, created);
            var invalid = new LockstepBattleSession("test", 7, LockstepVersion.Supported, _ => Create(SessionType.PVP)); rejected = false;
            try { invalid.Initialize(Initial()); } catch (System.IO.InvalidDataException) { rejected = true; } Assert.True(rejected); Assert.True(invalid.Driver == null);
        }
        private static void CheckSpectatorRecovery() {
            SessionStart Initial() => new SessionStart { sessionId = "test", participants = new long[] { 7, 9 },
                simulationVersion = LockstepVersion.Supported, tickRate = 20, initialStateJson = "{}",
                initialHash = SessionStart.Digest(System.Text.Encoding.UTF8.GetBytes("{}")) };
            BattleSessionRuntime Factory(SessionStart initial) => Create(SessionType.PVP, initial.participants[0], initial.participants[1]);
            var participant = new LockstepBattleSession("test", 7, LockstepVersion.Supported, Factory);
            var spectator = new LockstepBattleSession("test", 99, LockstepVersion.Supported, Factory, spectator: true);
            var outsider = new LockstepBattleSession("test", 99, LockstepVersion.Supported, Factory);
            bool rejected = false;
            try { outsider.Initialize(Initial()); } catch (System.IO.InvalidDataException) { rejected = true; }
            Assert.True(rejected && outsider.Driver == null);
            Assert.Equal(-1, Initial().Validate("test", 99, LockstepVersion.Supported, spectator: true));
            foreach (long invalidUser in new long[] { 0, -1 }) {
                rejected = false;
                try { Initial().Validate("test", invalidUser, LockstepVersion.Supported, spectator: true); }
                catch (System.IO.InvalidDataException) { rejected = true; }
                Assert.True(rejected);
            }
            participant.Initialize(Initial()); spectator.Initialize(Initial());
            var frames = new List<ConfirmedFrame>();
            for (uint tick = 0; tick < 24; tick++) {
                var frame = new ConfirmedFrame { sessionId = "test", tick = tick, inputs = new[] {
                    tick == 1 || tick == 2 ? new[] { new CastCommand { magicId = 1, xRaw = 17L << 32, zRaw = 5L << 32 } } : Array.Empty<CastCommand>(), Array.Empty<CastCommand>() } };
                frames.Add(frame); participant.Receive(frame); spectator.Receive(frame);
            }
            var prior = spectator.Driver.Runtime;
            spectator.Initialize(Initial()); Assert.True(prior.IsTerminated);
            foreach (var frame in frames) spectator.Receive(frame);
            Assert.True(participant.Driver.Runtime.Context.Result.Ended);
            Assert.Equal(participant.Driver.Runtime.Context.Result.Loser, spectator.Driver.Runtime.Context.Result.Loser);
            Assert.Equal(participant.Driver.Runtime.Context.FrameNumber, spectator.Driver.Runtime.Context.FrameNumber);
            foreach (Master side in new[] { Master.LeftPlayer, Master.RightPlayer }) {
                Assert.Equal(participant.Driver.Runtime.Context.Players[side].Hp, spectator.Driver.Runtime.Context.Players[side].Hp);
                Assert.Equal(participant.Driver.Runtime.Context.Players[side].Mana, spectator.Driver.Runtime.Context.Players[side].Mana);
            }
            foreach (Action<SessionStart> invalidate in new Action<SessionStart>[] {
                value => value.initialHash = "bad", value => value.tickRate = 30,
                value => value.participants = new long[] { 9, 7 }, value => value.simulationVersion = "future"
            }) {
                var invalid = Initial(); invalidate(invalid); rejected = false;
                try { spectator.Initialize(invalid); } catch (System.IO.InvalidDataException) { rejected = true; }
                Assert.True(rejected);
            }
        }
        private static void CheckConfirmedBattle() {
            var emotePeers = new[] { new LockstepBattleDriver(Create(SessionType.PVP, leftUserId: 7, rightUserId: 9)), new LockstepBattleDriver(Create(SessionType.PVP, leftUserId: 7, rightUserId: 9)) };
            var emoteEvents = new[] { new List<string>(), new List<string>() };
            for (int index = 0; index < 2; index++) {
                var events = emoteEvents[index];
                emotePeers[index].Runtime.Emotes.Sent += (side, emote) => events.Add(side + ":" + emote);
            }
            var queuedEmotes = new LockstepLocalInputs(); Assert.True(!queuedEmotes.QueueEmote("cry"));
            Assert.True(queuedEmotes.QueueEmote("Greet")); Assert.True(queuedEmotes.QueueEmote("Cry"));
            for (uint tick = 0; tick <= 60; tick++) {
                var commands = tick == 0 ? queuedEmotes.Prepare(0, 0).commands : tick == 60 ? new[] { new CastCommand { kind = "emote", emote = "Laugh" } } : Array.Empty<CastCommand>();
                var frame = new ConfirmedFrame { sessionId = "test", tick = tick, inputs = new[] { commands, Array.Empty<CastCommand>() } };
                foreach (var peer in emotePeers) { peer.Receive(frame); peer.Receive(frame); }
            }
            Assert.Sequence(new[] { "LeftPlayer:Greet", "LeftPlayer:Laugh" }, emoteEvents[0]); Assert.Sequence(emoteEvents[0], emoteEvents[1]);
            var peers = new[] { new LockstepBattleDriver(Create(SessionType.PVP)), new LockstepBattleDriver(Create(SessionType.PVP)) };
            ConfirmedFrame Frame(uint tick, bool cast = false) => new ConfirmedFrame { sessionId = "test", tick = tick, inputs = new[] {
                cast ? new[] { new CastCommand { magicId = 1, xRaw = 17L << 32, zRaw = 5L << 32 } } : new CastCommand[0], new CastCommand[0] } };
            var responses = new List<int>(); peers[0].InputHandled += (id, response) => { Assert.Equal(-1L, id); Assert.True(response.Valid); responses.Add(response.Id); };
            // 프레임 0에서 생성 대기 중인 플레이어를 반영한 뒤 공격한다.
            peers[0].Receive(Frame(1, true)); Assert.Equal(0, peers[0].Runtime.Context.FrameNumber);
            peers[0].Receive(Frame(0)); peers[0].Receive(Frame(1, true));
            peers[1].Receive(Frame(0)); peers[1].Receive(Frame(1, true));
            for (uint tick = 2; tick < 24; tick++) foreach (var peer in peers) peer.Receive(Frame(tick, tick == 2));
            Assert.Sequence(new[] { 32, 64 }, responses);
            foreach (var peer in peers) {
                Assert.True(peer.Runtime.Loop.StopRequested); Assert.Equal<Master?>(Master.RightPlayer, peer.Runtime.Context.Result.Loser);
                Assert.Equal(44, peer.Runtime.Context.Players[Master.LeftPlayer].Mana); Assert.Equal(0, peer.Runtime.Context.Players[Master.RightPlayer].Hp);
            }
            Assert.Equal(peers[0].Runtime.Context.FrameNumber, peers[1].Runtime.Context.FrameNumber);
            var simultaneous = new LockstepBattleDriver(Create(SessionType.PVP));
            simultaneous.Runtime.Context.Players[Master.RightPlayer].AddCard(1);
            simultaneous.Receive(Frame(0));
            var ordered = Frame(1, true);
            ordered.inputs[0] = new[] { ordered.inputs[0][0], ordered.inputs[0][0] };
            ordered.inputs[1] = new[] { new CastCommand { magicId = 1, xRaw = 1L << 32, zRaw = 5L << 32 } };
            var results = new List<InputResultCode>(); simultaneous.InputHandled += (id, response) => results.Add(response.ResultCode);
            simultaneous.Receive(ordered);
            Assert.Sequence(new[] { InputResultCode.Success, InputResultCode.Success, InputResultCode.FailGameEnded }, results);
            Assert.Equal<Master?>(Master.RightPlayer, simultaneous.Runtime.Context.Result.Loser);
            Assert.Equal(20, simultaneous.Runtime.Context.Players[Master.LeftPlayer].Hp);
            Assert.Equal(50, simultaneous.Runtime.Context.Players[Master.RightPlayer].Mana);
            var local = new LockstepLocalInputs(); var localDriver = new LockstepBattleDriver(Create(SessionType.PVP));
            var localResponses = new List<InputResponse>();
            localDriver.InputHandled += (user, response) => { if (user == localDriver.Runtime.Definition.LeftUserId && local.TryAcknowledge(response, out var ack)) localResponses.Add(ack); };
            localDriver.Receive(Frame(0)); Assert.True(local.Queue(1, 17L << 32, 5L << 32, 735));
            var localFrame = Frame(1); localFrame.inputs[0] = local.Prepare(1, 0).commands;
            localDriver.Receive(localFrame); localDriver.Receive(localFrame);
            Assert.Equal(1, localResponses.Count); Assert.Equal(735, localResponses[0].Id); Assert.Equal(InputResultCode.Success, localResponses[0].ResultCode);
            Assert.Equal(10, localDriver.Runtime.Context.Players[Master.RightPlayer].Hp);
            var selection = new LockstepBattleDriver(Create(SessionType.PVP)); selection.Receive(Frame(0));
            int selectionReplies = 0; selection.InputHandled += (_, __) => selectionReplies++;
            var selected = Frame(1); selected.inputs[0] = new[] { new CastCommand { kind = "selectCard", magicId = 1 } };
            selection.Receive(selected); var selectedPlayer = selection.Runtime.Context.FindPlayer(Master.LeftPlayer);
            Assert.True(selectedPlayer.HasEffect(VisualEffect.CardSelected)); Assert.Equal(50, selection.Runtime.Context.Players[Master.LeftPlayer].Mana);
            var unselected = Frame(2); unselected.inputs[0] = new[] { new CastCommand { kind = "unselectCard", magicId = 1 } };
            selection.Receive(unselected); Assert.True(!selectedPlayer.HasEffect(VisualEffect.CardSelected)); Assert.Equal(0, selectionReplies);
            var mixed = Frame(3); mixed.inputs[0] = new[] { new CastCommand { kind = "selectCard", magicId = 1 }, new CastCommand { kind = "cast", magicId = 1, xRaw = 17L << 32, zRaw = 5L << 32 } };
            selection.Receive(mixed); Assert.Equal(1, selectionReplies); Assert.True(!selectedPlayer.HasEffect(VisualEffect.CardSelected)); Assert.Equal(10, selection.Runtime.Context.Players[Master.RightPlayer].Hp);
            var conflict = Frame(1, true); conflict.inputs[0][0].zRaw = 4L << 32;
            bool rejected = false; try { peers[0].Receive(conflict); } catch (System.IO.InvalidDataException) { rejected = true; }
            Assert.True(rejected); Assert.True(peers[0].Failed);
        }
    }
}
