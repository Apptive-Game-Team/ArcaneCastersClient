using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    // 확정 모델 요청·결과 replay slice를 실제 driver/runtime/source로 검사한다. 원본 대응 수를 늘리는 검사가 아니다.
    public static class ModelDecisionChecks {
        private const string Session = "model";
        private const uint Deadline = 10;
        private const string StandardPlayKey = "p0";

        private sealed class Lookup : IMagicLookup {
            private readonly BoundMagic cheap = new BoundMagic(1, "cheap", (context, side, origin, target) =>
                context.FindPlayer(side == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer).GetComponent<PlayerHealthComponent>().OnDamaged(new Hit(10, 0, Element.None)));
            private readonly BoundMagic dear = new BoundMagic(2, "dear", (context, side, origin, target) => { });
            public BoundMagic GetMagic(long id) => id == 1 ? cheap : id == 2 ? dear : null;
            public BoundMagic ParseMagic(long user, long id) => GetMagic(id);
        }
        private sealed class Counters : IBotCounterScorer {
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
        }

        private static BattleSessionRuntime Create(BotBrainType brain, BotDecisionSourceFactory decisions) {
            var c = new SimulationContext(1);
            c.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100)) { Mana = 50 };
            c.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100)) { Mana = 50 };
            c.Players[Master.LeftPlayer].AddCard(1); c.Players[Master.LeftPlayer].AddCard(1);
            c.Players[Master.LeftPlayer].AddCard(1); c.Players[Master.LeftPlayer].AddCard(2);
            c.GameParameters["player.hp"] = 400;
            c.GameParameters["cheap.mana_cost"] = 5; c.GameParameters["cheap.range"] = 20; c.GameParameters["cheap.damage"] = 10;
            // 손패의 가장 비싼 카드를 끝까지 못 쓰게 두어 wait 선택지가 유지되고, 모델에게 물을 상황이 계속 생긴다.
            c.GameParameters["dear.mana_cost"] = 900; c.GameParameters["dear.range"] = 20; c.GameParameters["dear.damage"] = 30;
            StandardPrefabs.Register(c); c.InitializationFailed += (_, error) => throw error;
            return new BattleSessionRuntime(c, new SessionDefinition(Session, -1, 2, 123, new long[] { 1, 1, 1, 2 }, new long[0], SessionType.Practice),
                new Lookup(), new TestBotCatalog(new BotMagic(1, "cheap"), new BotMagic(2, "dear")), new Counters(),
                _ => new BotPersona { Tier = BotTier.Elite, ThinkingTimeMs = 0, ReactionIntervalFrames = 1, CounterAggression = 0, BrainType = brain },
                _ => (Fix64)0.5m, owner => owner.GetComponent<MobComponent>()?.Hp, 100000, 10000, true, null, decisions);
        }
        private const long SessionSeed = 123;
        private static LockstepBattleDriver Driver(BotBrainType brain, uint interval = 4) =>
            new LockstepBattleDriver(Create(brain, ModelBotDecisions.Confirmed(SessionSeed, interval)), modelProtocol: true);

        private static ConfirmedFrame Frame(uint tick, params ModelEvent[] events) => new ConfirmedFrame {
            sessionId = Session, tick = tick, inputs = new[] { Array.Empty<CastCommand>(), Array.Empty<CastCommand>() },
            modelEvents = events.Length == 0 ? null : events
        };
        private static ModelEvent Accept(ModelRequest request, uint deadline = Deadline) => new ModelEvent {
            kind = ModelProtocol.Accepted, slot = request.slot, generation = request.generation, sequence = request.sequence,
            observationTick = request.observationTick, digest = request.digest, payloadHash = request.payloadHash,
            terminalTick = request.observationTick + deadline
        };
        private static ModelEvent Reject(ModelRequest request, string reason) => new ModelEvent {
            kind = ModelProtocol.Rejected, slot = request.slot, generation = request.generation, sequence = request.sequence,
            observationTick = request.observationTick, digest = request.digest, payloadHash = request.payloadHash, reason = reason
        };
        private static ModelEvent Complete(ModelRequest request, string playKey, string aimKey = null, string reason = null) => new ModelEvent {
            kind = ModelProtocol.Completed, slot = request.slot, generation = request.generation, sequence = request.sequence,
            observationTick = request.observationTick, playKey = playKey, aimKey = aimKey, reason = reason
        };
        private static ModelEvent Failure(ModelRequest request, string reason = "provider") => new ModelEvent {
            kind = ModelProtocol.Failed, slot = request.slot, generation = request.generation, sequence = request.sequence,
            observationTick = request.observationTick, reason = reason
        };
        private static ModelEvent BackOff(ModelRequest request, uint resumeTick) => new ModelEvent {
            kind = ModelProtocol.BackedOff, slot = request.slot, generation = request.generation, sequence = request.sequence,
            observationTick = request.observationTick, resumeTick = resumeTick, reason = "rate limited"
        };

        // 테스트가 서버 역할을 맡아 수락 기록과 두 tick 뒤의 결과를 같은 확정 스트림에 넣는다.
        private sealed class Session2 {
            private readonly Dictionary<uint, List<ModelEvent>> scheduled = new Dictionary<uint, List<ModelEvent>>();
            public readonly LockstepBattleDriver Driver;
            public readonly List<string> Trace = new List<string>();
            public int Requests, Results;
            public Func<ModelRequest, ModelEvent> Answer;
            public uint AnswerDelay = 2;
            public uint AcceptDeadline = Deadline;
            public Session2(LockstepBattleDriver driver) { Driver = driver; }
            public ModelRequest[] Manifest(uint tick) => Driver.Runtime.Models.PendingManifest(tick);
            public void Advance(uint tick) {
                var events = new List<ModelEvent>();
                if (scheduled.TryGetValue(tick, out var due)) { events.AddRange(due); scheduled.Remove(tick); }
                foreach (var request in Manifest(tick) ?? Array.Empty<ModelRequest>()) {
                    Requests++;
                    events.Add(Accept(request, AcceptDeadline));
                    uint at = tick + AnswerDelay;
                    if (!scheduled.TryGetValue(at, out var list)) scheduled[at] = list = new List<ModelEvent>();
                    var answer = Answer(request);
                    if (answer != null) { list.Add(answer); Results++; }
                }
                Driver.Receive(Frame(tick, events.ToArray()));
                Record();
            }
            private void Record() {
                var c = Driver.Runtime.Context; var agent = Driver.Runtime.Bots.Left;
                var decision = agent?.LastDecision;
                Trace.Add(c.FrameNumber + ":" + c.Players[Master.LeftPlayer].Mana + ":" + c.Players[Master.RightPlayer].Hp
                    + ":" + (decision == null ? "-" : decision.MagicId + "@" + decision.Target.X.RawValue + "," + decision.Target.Z.RawValue + "/" + decision.RuleId));
            }
        }

        private static void Expect<T>(Action action) where T : Exception {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name);
        }
        private static void ExpectProtocolFailure(Session2 session, Action action) {
            int terminated = 0; session.Driver.Runtime.Terminated += () => terminated++;
            Expect<BotDecisionProtocolException>(action);
            Assert.True(session.Driver.Failed); Assert.True(session.Driver.Runtime.IsTerminated);
            Assert.True(session.Driver.Runtime.Failure is BotDecisionProtocolException); Assert.Equal(1, terminated);
            // 실패 후의 프레임은 더 이상 받지 않는다.
            Expect<InvalidOperationException>(() => session.Driver.Receive(Frame(999)));
        }

        // 두 replica가 같은 확정 스트림에서 같은 결과에 이른다. 벽시계도 HTTP도 거치지 않는다.
        private static void CheckCastReplaySlice() {
            var runs = new List<string>[] { new List<string>(), new List<string>() };
            int requests = 0, results = 0;
            for (int run = 0; run < 2; run++) {
                var session = new Session2(Driver(BotBrainType.Openrouter)) {
                    Answer = request => Complete(request, StandardPlayKey)
                };
                for (uint tick = 0; tick < 60; tick++) session.Advance(tick);
                runs[run] = session.Trace; requests = session.Requests; results = session.Results;
                Assert.True(session.Driver.Runtime.Failure == null);
            }
            Assert.True(requests >= 3); Assert.Equal(requests, results);
            Assert.Sequence(runs[0], runs[1]);
            Assert.True(runs[0][runs[0].Count - 1].Contains("llm.openrouter"));
        }

        // 거부·실패·backoff는 그 확정 프레임에서 규칙 brain으로 돌아가고, wait는 아무것도 시전하지 않는다.
        private static void CheckCastFallbackAndWait() {
            var rejected = new Session2(Driver(BotBrainType.Openrouter)) { Answer = _ => null };
            for (uint tick = 0; tick < 6; tick++) {
                var manifest = rejected.Manifest(tick);
                var events = new List<ModelEvent>();
                foreach (var request in manifest ?? Array.Empty<ModelRequest>()) events.Add(Reject(request, "executor full"));
                rejected.Driver.Receive(Frame(tick, events.ToArray()));
            }
            Assert.True(rejected.Driver.Runtime.Bots.Left.LastDecision != null);
            Assert.Equal(BotBrain.ValueRule, rejected.Driver.Runtime.Bots.Left.LastDecision.RuleId);

            var waiting = new Session2(Driver(BotBrainType.Openrouter)) {
                Answer = request => Complete(request, ModelProtocol.WaitPlayKey)
            };
            for (uint tick = 0; tick < 12; tick++) waiting.Advance(tick);
            Assert.True(waiting.Requests >= 2);
            Assert.Equal(400, waiting.Driver.Runtime.Context.Players[Master.RightPlayer].Hp);
        }

        // JEV 계열은 요청이 대기 중에도 현재 stance로 계속 판단한다.
        private static void CheckStanceReplaySlice() {
            var runs = new List<string>[] { new List<string>(), new List<string>() };
            var stances = new List<ModelStance>[] { new List<ModelStance>(), new List<ModelStance>() };
            Session2 session = null;
            for (int run = 0; run < 2; run++) {
                session = new Session2(Driver(BotBrainType.Jev)) {
                    AnswerDelay = 3,
                    Answer = request => Complete(request, ModelDecisionSource.StanceKey(request.sequence % 2 == 0 ? ModelStance.Attack : ModelStance.Defend))
                };
                for (uint tick = 0; tick < 40; tick++) {
                    session.Advance(tick);
                    stances[run].Add(((ModelDecisionSource)session.Driver.Runtime.Bots.Left.DecisionSource).Stance);
                }
                runs[run] = session.Trace;
            }
            Assert.Sequence(runs[0], runs[1]); Assert.Sequence(stances[0], stances[1]);
            // 확정 응답이 stance를 실제로 바꾼다.
            Assert.True(stances[0].Contains(ModelStance.Attack)); Assert.True(stances[0].Contains(ModelStance.Defend));
            // defend는 규칙 brain의 판단을 그대로 쓰고, attack은 원본 후보에서 고른 선택지를 실제로 시전한다.
            bool defended = false, attacked = false;
            foreach (string line in runs[0]) {
                defended |= line.Contains(BotBrain.ValueRule) || line.Contains(BotBrain.CycleRule);
                attacked |= line.Contains("llm.jev.attack");
            }
            Assert.True(defended); Assert.True(attacked);
            Assert.True(session.Driver.Runtime.Context.Players[Master.RightPlayer].Hp < 400);
        }

        private static void CheckDroppedManifest() {
            var session = new Session2(Driver(BotBrainType.Openrouter)) { Answer = _ => null };
            uint tick = 0;
            while (session.Manifest(tick) == null) { session.Driver.Receive(Frame(tick)); tick++; }
            // 요청이 생긴 tick의 수락 기록을 빠뜨리면 재생을 거부한다.
            uint missing = tick;
            ExpectProtocolFailure(session, () => session.Driver.Receive(Frame(missing)));
        }

        private static void CheckExtraManifest() {
            var session = new Session2(Driver(BotBrainType.Openrouter)) { Answer = _ => null };
            var invented = new ModelRequest {
                slot = 0, generation = 0, sequence = 7, observationTick = 1,
                digest = new string('a', 64), payloadHash = new string('b', 64)
            };
            session.Driver.Receive(Frame(0));
            ExpectProtocolFailure(session, () => session.Driver.Receive(Frame(1, Accept(invented))));
        }

        private static void CheckConflictingDigest() {
            var session = new Session2(Driver(BotBrainType.Openrouter)) { Answer = _ => null };
            uint tick = 0;
            while (session.Manifest(tick) == null) { session.Driver.Receive(Frame(tick)); tick++; }
            var request = session.Manifest(tick)[0];
            var tampered = new ModelRequest {
                slot = request.slot, generation = request.generation, sequence = request.sequence,
                observationTick = request.observationTick, payloadHash = request.payloadHash,
                digest = (request.digest[0] == 'a' ? "b" : "a") + request.digest.Substring(1)
            };
            uint at = tick;
            ExpectProtocolFailure(session, () => session.Driver.Receive(Frame(at, Accept(tampered))));
        }

        // 진짜 관측 digest에 다른 provider 입력 payload를 붙인 기록은 거부한다.
        private static void CheckMutatedPayloadWithGenuineDigest() {
            var session = new Session2(Driver(BotBrainType.Openrouter)) { Answer = _ => null };
            uint tick = 0;
            while (session.Manifest(tick) == null) { session.Driver.Receive(Frame(tick)); tick++; }
            var request = session.Manifest(tick)[0];
            var mutated = new ModelRequest {
                slot = request.slot, generation = request.generation, sequence = request.sequence,
                observationTick = request.observationTick, digest = request.digest,
                payloadHash = (request.payloadHash[0] == 'a' ? "b" : "a") + request.payloadHash.Substring(1)
            };
            uint at = tick;
            ExpectProtocolFailure(session, () => session.Driver.Receive(Frame(at, Accept(mutated))));
        }

        // payload는 그 관측의 criteria/state/aims와 결속되고 본문을 바꾸면 hash와 digest가 함께 바뀐다.
        private static void CheckPayloadBindsTheObservedMenu() {
            var session = new Session2(Driver(BotBrainType.Openrouter)) { Answer = _ => null };
            uint tick = 0;
            while (session.Manifest(tick) == null) { session.Driver.Receive(Frame(tick)); tick++; }
            var request = session.Manifest(tick)[0];
            Assert.Equal(ModelProtocol.CastPolicy, request.payload.policy);
            Assert.Equal(ModelRequestPayload.Hash(request.payload), request.payloadHash);
            // criteria는 선택지 키와 wait, state는 실제 관측 board를 그대로 싣는다.
            bool wait = false, play = false;
            foreach (var option in request.payload.criteria) {
                wait |= option.key == ModelProtocol.WaitPlayKey;
                play |= option.key == StandardPlayKey;
            }
            Assert.True(wait); Assert.True(play);
            Assert.Sequence(new[] { "side", "ourHp", "enemyHp", "mana", "pressure", "enemyNearOurBase", "hand",
                "handSlotsUsed", "enemyUnits", "ourUnits", "unitGuide" }, request.payload.state.Select(entry => entry.key));
            // 본문 한 글자만 달라도 hash가 달라진다.
            var reworded = ModelRequestPayload.Copy(request.payload);
            reworded.criteria[0].text += "!";
            Assert.True(ModelRequestPayload.Hash(reworded) != request.payloadHash);
            // 상한을 넘는 payload는 거부한다.
            var huge = ModelRequestPayload.Copy(request.payload);
            huge.criteria[0].text = new string('x', ModelProtocol.MaxOptionTextLength + 1);
            Expect<InvalidDataException>(() => ModelRequestPayload.Copy(huge));
            var unknown = ModelRequestPayload.Copy(request.payload);
            unknown.policy = "other";
            Expect<InvalidDataException>(() => ModelRequestPayload.Copy(unknown));
            var forged = ModelRequest.Copy(request);
            forged.payload.criteria[0].text += "!";
            Expect<InvalidDataException>(() => ModelRequest.Copy(forged));
        }

        // JEV는 stance 선택지 본문을 묻고 조준 질문은 싣지 않는다.
        private static void CheckStancePayloadAsksForAPlan() {
            var session = new Session2(Driver(BotBrainType.Jev)) { Answer = _ => null };
            uint tick = 0;
            while (session.Manifest(tick) == null) { session.Driver.Receive(Frame(tick)); tick++; }
            var request = session.Manifest(tick)[0];
            Assert.Equal(ModelProtocol.StancePolicy, request.payload.policy);
            Assert.Sequence(new[] { "attack", "defend", "save" }, request.payload.criteria.Select(option => option.key));
            Assert.Equal(0, request.payload.aims.Length);
        }

        // 관측 State는 construction 이후 바뀌지 않는다. 바깥 caller가 같은 map이나 중첩 값을 바꿔도 과거 요청과 digest가 그대로다.
        private static void CheckObservedStateIsAFrozenSnapshot() {
            var eye = BenchEye(50, 1, 2);
            var row = new ModelJsonMap(); row.Put("type", "FireSlime"); row.Put("hp", 20L);
            var rows = new List<ModelJson> { ModelJson.Of(row) };
            var state = new ModelJsonMap();
            state.Put("enemyNearOurBase", false);
            state.Put("hand", ModelJson.Of(rows));
            var play = BenchPlay("p0", 1, 5, 4, "cheap (damage spell, cost 5)", baseDamage: (Fix64)10);
            var menu = new ModelCandidateMenu(new[] { play }, "wait here", state);
            string before = menu.State.Write();
            string digest = ModelDigest.Compute("bench", 0, 0, 0, 1, eye, menu);
            var payload = ModelRequestPayload.Of(ModelProtocol.CastPolicy, menu, menu.Criteria());
            string payloadHash = ModelRequestPayload.Hash(payload);

            // 원본 map, 중첩 목록, 중첩 map을 모두 바꿔 본다.
            state.Put("enemyNearOurBase", true);
            state.Put("mana", 99L);
            rows.Add(ModelJson.Of(new ModelJsonMap()));
            row.Put("hp", 1L);
            Assert.Equal(before, menu.State.Write());
            Assert.Equal(digest, ModelDigest.Compute("bench", 0, 0, 0, 1, eye, menu));
            Assert.Equal(payloadHash, ModelRequestPayload.Hash(ModelRequestPayload.Of(ModelProtocol.CastPolicy, menu, menu.Criteria())));
            // 노출된 중첩 값도 얼어 있다.
            Assert.True(menu.State.IsFrozen);
            Expect<InvalidOperationException>(() => menu.State.Put("mana", 1L));
            Expect<InvalidOperationException>(() => menu.State.Get("hand").AsList[0].AsMap.Put("hp", 2L));
            Assert.Equal(1, menu.State.Get("hand").AsList.Count);
        }

        private static void CheckTerminalDeadline() {
            var session = new Session2(Driver(BotBrainType.Openrouter)) { Answer = _ => null, AcceptDeadline = 2 };
            uint tick = 0;
            while (session.Manifest(tick) == null) { session.Driver.Receive(Frame(tick)); tick++; }
            var request = session.Manifest(tick)[0];
            session.Driver.Receive(Frame(tick, Accept(request, 2)));
            session.Driver.Receive(Frame(tick + 1));
            uint overdue = tick + 2;
            ExpectProtocolFailure(session, () => session.Driver.Receive(Frame(overdue)));
        }

        private static void CheckDuplicateResultIsIdempotent() {
            var session = new Session2(Driver(BotBrainType.Openrouter)) { Answer = _ => null };
            uint tick = 0;
            while (session.Manifest(tick) == null) { session.Driver.Receive(Frame(tick)); tick++; }
            var request = session.Manifest(tick)[0];
            session.Driver.Receive(Frame(tick, Accept(request)));
            var completed = Complete(request, StandardPlayKey);
            session.Driver.Receive(Frame(tick + 1, completed));
            int hp = session.Driver.Runtime.Context.Players[Master.RightPlayer].Hp;
            // 같은 결과의 재전송은 멱등하다. 경기는 계속 진행한다.
            session.Driver.Receive(Frame(tick + 2, completed));
            session.Driver.Receive(Frame(tick + 3, completed));
            Assert.True(!session.Driver.Failed); Assert.True(!session.Driver.Runtime.IsTerminated);
            Assert.True(session.Driver.Runtime.Context.Players[Master.RightPlayer].Hp <= hp);
        }

        private static void CheckGenerationReplacement() {
            var session = new Session2(Driver(BotBrainType.Openrouter)) { Answer = _ => null };
            uint tick = 0;
            while (session.Manifest(tick) == null) { session.Driver.Receive(Frame(tick)); tick++; }
            var request = session.Manifest(tick)[0];
            Assert.Equal(0L, request.generation);
            session.Driver.Receive(Frame(tick, Accept(request)));
            var runtime = session.Driver.Runtime;
            var first = runtime.Bots.Left;
            runtime.Bots.RemoveBotAgent(Master.LeftPlayer);
            runtime.Bots.NewBotAgent(runtime.Context, Master.LeftPlayer, -1, (Fix64)0.5m);
            Assert.True(!ReferenceEquals(first, runtime.Bots.Left));
            // 교체 전 generation의 결과는 새 봇에 적용하지 않고, 그 요청의 deadline도 더 보지 않는다.
            session.Driver.Receive(Frame(tick + 1, Complete(request, StandardPlayKey)));
            Assert.True(runtime.Bots.Left.LastDecision == null);
            var answers = new List<ModelEvent>();
            for (uint next = tick + 2; next <= tick + Deadline + 2; next++) {
                var events = new List<ModelEvent>(answers); answers.Clear();
                foreach (var pending in session.Manifest(next) ?? Array.Empty<ModelRequest>()) {
                    Assert.Equal(1L, pending.generation);
                    events.Add(Accept(pending));
                    answers.Add(Complete(pending, StandardPlayKey));
                }
                session.Driver.Receive(Frame(next, events.ToArray()));
            }
            Assert.True(!session.Driver.Failed); Assert.True(!session.Driver.Runtime.IsTerminated);
            // 새 봇의 generation보다 앞선 generation을 가리키는 이벤트는 프로토콜 오류다.
            uint last = tick + Deadline + 3;
            var future = new ModelRequest {
                slot = 0, generation = 9, sequence = 0, observationTick = last,
                digest = new string('c', 64), payloadHash = new string('d', 64)
            };
            var tail = new List<ModelEvent>(answers);
            foreach (var pending in session.Manifest(last) ?? Array.Empty<ModelRequest>()) tail.Add(Accept(pending));
            tail.Add(Accept(future));
            ExpectProtocolFailure(session, () => session.Driver.Receive(Frame(last, tail.ToArray())));
        }

        // v1 경기는 모델 필드를 거부하고, v2 경기는 사람 입력과 분리해 deep copy·fingerprint·manifest에 포함한다.
        private static void CheckVersionSeparation() {
            Assert.True(LockstepVersion.IsSupported(LockstepVersion.Supported));
            Assert.True(LockstepVersion.IsSupported(LockstepVersion.SupportedModel));
            Assert.True(!LockstepVersion.IsSupported("lockstep-v3"));
            Assert.True(!LockstepVersion.UsesModelProtocol(LockstepVersion.Supported));
            var sample = new ModelEvent { kind = ModelProtocol.Failed, slot = 0, generation = 0, sequence = 0, observationTick = 1, reason = "provider" };
            var legacy = new ConfirmedInputStream(Session);
            Expect<InvalidDataException>(() => legacy.Enqueue(Frame(0, sample)));
            legacy.Enqueue(Frame(0)); Assert.True(legacy.TryTake(out _));

            var model = new ConfirmedInputStream(Session, modelProtocol: true);
            var mutable = new ModelEvent { kind = ModelProtocol.Failed, slot = 0, generation = 0, sequence = 0, observationTick = 1, reason = "provider" };
            model.Enqueue(Frame(0));
            Assert.True(model.TryTake(out _));
            model.Enqueue(Frame(1, mutable));
            mutable.reason = "changed";
            Assert.True(model.TryTake(out var taken) && taken.modelEvents[0].reason == "provider");
            // fingerprint가 모델 이벤트를 포함하므로 같은 tick의 다른 이벤트는 충돌이다.
            Expect<InvalidDataException>(() => model.Enqueue(Frame(1, mutable)));
            mutable.reason = "provider";
            model.Enqueue(Frame(1, mutable));
            Expect<InvalidDataException>(() => model.Enqueue(Frame(2, mutable, mutable)));
            foreach (Action<ModelEvent> invalidate in new Action<ModelEvent>[] {
                item => item.kind = "unknown", item => item.slot = 2, item => item.observationTick = 0,
                item => item.playKey = "p0", item => item.digest = new string('a', 64), item => item.terminalTick = 5,
                item => item.resumeTick = 5, item => item.reason = new string('x', ModelProtocol.MaxReasonLength + 1),
                item => item.payloadHash = new string('a', 64)
            }) {
                var bad = ModelEvent.Copy(mutable); invalidate(bad);
                Expect<InvalidDataException>(() => ModelEvent.Copy(bad));
            }
            var accepted = new ModelEvent {
                kind = ModelProtocol.Accepted, observationTick = 4,
                digest = new string('a', 64), payloadHash = new string('b', 64), terminalTick = 9
            };
            Assert.Equal(9u, ModelEvent.Copy(accepted).terminalTick);
            accepted.terminalTick = 4; Expect<InvalidDataException>(() => ModelEvent.Copy(accepted));
            accepted.terminalTick = 9; accepted.payloadHash = null;
            Expect<InvalidDataException>(() => ModelEvent.Copy(accepted));

            // v1은 빈 배열도 필드가 붙은 것으로 보고 거부하고, 필드가 없는 프레임만 받는다.
            var strict = new ConfirmedInputStream(Session);
            var empty = Frame(0); empty.modelEvents = Array.Empty<ModelEvent>();
            Expect<InvalidDataException>(() => strict.Enqueue(empty));
            var permissive = new ConfirmedInputStream(Session, modelProtocol: true);
            permissive.Enqueue(empty);
            Assert.True(permissive.TryTake(out var none) && none.modelEvents.Length == 0);
            var absent = new ConfirmedInputStream(Session);
            absent.Enqueue(Frame(0));
            Assert.True(absent.TryTake(out var bare) && bare.modelEvents == null);

            var manifest = new[] { new ModelRequest { slot = 0, generation = 0, sequence = 0, observationTick = 2, digest = new string('a', 64) } };
            var legacyInputs = new LockstepLocalInputs();
            Assert.True(legacyInputs.Prepare(1, 0).modelRequests == null);
            Expect<ArgumentOutOfRangeException>(() => legacyInputs.Prepare(2, 0, manifest));
            var modelInputs = new LockstepLocalInputs(modelProtocol: true);
            Assert.True(modelInputs.Prepare(1, 0).modelRequests == null);
            Expect<ArgumentOutOfRangeException>(() => modelInputs.Prepare(2, 0, Array.Empty<ModelRequest>()));
            Assert.Sequence(manifest, modelInputs.Prepare(2, 0, manifest).modelRequests);
            Assert.Sequence(manifest, modelInputs.Prepare(2, 0, null).modelRequests);
        }

        // ---- 분기별 규칙 brain·후보 호출 횟수와 난수 순서 ----

        private sealed class SpyFallback : IBotDecisionSource {
            public readonly BotBrain Brain;
            public readonly List<BotEye> Eyes = new List<BotEye>();
            public SpyFallback(BotBrain brain) { Brain = brain; }
            public int Calls => Eyes.Count;
            public InputDecision Think(BotEye eye, IDictionary<string, Fix64> parameters, Master side, bool overdue) {
                Eyes.Add(eye); return Brain.Think(eye, parameters, side, overdue);
            }
        }
        private sealed class SpyBuilder : IModelCandidateBuilder {
            private readonly IModelCandidateBuilder inner;
            public int Calls;
            public bool LastStandardWasNull;
            public SpyBuilder(IModelCandidateBuilder inner) { this.inner = inner; }
            public ModelCandidateMenu Build(BotEye eye, IDictionary<string, Fix64> parameters, Master side, InputDecision standard) {
                Calls++; LastStandardWasNull = standard == null;
                return inner.Build(eye, parameters, side, standard);
            }
        }
        private sealed class ScriptedBuilder : IModelCandidateBuilder {
            public Func<ModelCandidateMenu> Menu;
            public ModelCandidateMenu Build(BotEye eye, IDictionary<string, Fix64> parameters, Master side, InputDecision standard) => Menu();
        }

        private static Dictionary<string, Fix64> Parameters() => new Dictionary<string, Fix64> {
            ["player.max_mana"] = 120, ["cheap.mana_cost"] = 5, ["cheap.range"] = 20, ["cheap.damage"] = 10,
            ["dear.mana_cost"] = 900, ["dear.range"] = 20, ["dear.damage"] = 30
        };
        private static TestBotCatalog BenchCatalog() => new TestBotCatalog(new BotMagic(1, "cheap"), new BotMagic(2, "dear"));
        private static BotEye BenchEye(int mana, params long[] cards) => new BotEye(new[] {
            new BotVisibleObject(11, Master.RightPlayer, "FireSlime", new BEPUutilities.Vector3(10, 0, 5), 25, true, true),
            new BotVisibleObject(12, Master.RightPlayer, "Player", new BEPUutilities.Vector3(17, 0, 5), 100, false, true)
        }, cards, mana, 100, Fix64.One);
        private static BotBrain BenchBrain(long seed) => new BotBrain(BenchCatalog(), new Counters(),
            new BotPersona { Tier = BotTier.Elite, ThinkingTimeMs = 0, ReactionIntervalFrames = 1, CounterAggression = 0 },
            new JavaRandom(seed), deckCards: () => new long[] { 1, 2 }, opponentProgress: (Fix64)0.5m);

        private sealed class Bench {
            public uint Tick;
            public readonly ModelEventLedger Ledger;
            public readonly SpyFallback Fallback;
            public readonly SpyBuilder Builder;
            public readonly ModelDecisionSource Source;
            private readonly Dictionary<string, Fix64> parameters = Parameters();
            public Bench(ModelDecisionPolicy policy, IModelCandidateBuilder inner, uint interval = 10, long seed = 4242) {
                Ledger = new ModelEventLedger("bench", () => (int)Tick);
                Ledger.Enable();
                Fallback = new SpyFallback(BenchBrain(seed));
                Builder = new SpyBuilder(inner);
                Source = new ModelDecisionSource(Fallback, Builder, policy,
                    policy == ModelDecisionPolicy.Stance ? "llm.jev" : "llm.openrouter", interval);
                Ledger.Attach(Master.LeftPlayer, 0, Source);
                Tick = 1;
            }
            public InputDecision Think(BotEye eye) => Source.Think(eye, parameters, Master.LeftPlayer, false);
            public ModelRequest Request(uint tick) => Ledger.PendingManifest(tick)[0];
            public void Confirm(uint tick, params ModelEvent[] events) => Ledger.Receive(Frame(tick, events));
            public void Expect(int fallback, int build) { Assert.Equal(fallback, Fallback.Calls); Assert.Equal(build, Builder.Calls); }
        }

        private static ModelCandidateBuilder RealBuilder() =>
            new ModelCandidateBuilder(BenchCatalog(), new Counters(), () => new JavaRandom(7));
        // 실제 builder가 만드는 state와 같은 모양이다. urgent와 가장 비싼 손패 비용만 분기에 쓰인다.
        private static ModelJsonMap BenchState(bool urgent, params int[] handCosts) {
            var state = new ModelJsonMap();
            state.Put("enemyNearOurBase", urgent);
            var hand = new List<ModelJson>();
            foreach (int cost in handCosts) {
                var card = new ModelJsonMap(); card.Put("magic", "cheap"); card.Put("cost", cost); card.Put("copies", 1L);
                hand.Add(ModelJson.Of(card));
            }
            state.Put("hand", ModelJson.Of(hand));
            return state;
        }
        private static ModelCandidateMenu SummonMenu() {
            var aims = new List<ModelCandidateAim> {
                new ModelCandidateAim("near_high", "high lane", new BEPUutilities.Vector3(4, 0, 8)),
                new ModelCandidateAim("near_low", "low lane", new BEPUutilities.Vector3(4, 0, 2))
            };
            var play = new ModelCandidatePlay("summon", 1, "cheap", 5, new BEPUutilities.Vector3(4, 0, 5),
                "cheap (summon, cost 5)", true, Fix64.Zero, Fix64.Zero, aims);
            return new ModelCandidateMenu(new[] { play }, "wait here", BenchState(false, 5, 900));
        }
        private static ModelCandidateMenu AlternativeMenu() {
            var play = new ModelCandidatePlay("alt", 1, "cheap", 5, new BEPUutilities.Vector3(9, 0, 5),
                "cheap (damage spell, cost 5)", false, (Fix64)10, (Fix64)10);
            return new ModelCandidateMenu(new[] { play }, "wait here", BenchState(false, 5, 900));
        }

        private static void CheckBranchCallCounts() {
            var eye = BenchEye(50, 1, 2);
            var real = RealBuilder();

            // ask: 규칙 brain 1회 + 후보 1회. pending과 최소 간격은 둘 다 0회다.
            var cast = new Bench(ModelDecisionPolicy.Cast, real);
            Assert.True(cast.Think(eye) == null); cast.Expect(1, 1);
            Assert.True(cast.Think(eye) == null); cast.Expect(1, 1);
            var asked = cast.Request(1);
            cast.Confirm(1, Accept(asked));
            cast.Tick = 2;
            Assert.True(cast.Think(eye) == null); cast.Expect(1, 1);
            cast.Confirm(2, Complete(asked, StandardPlayKey, reason: "provider chose the standard play"));
            // 표준 선택지 재조준은 규칙 brain 1회, 후보 0회다.
            var applied = cast.Think(eye); cast.Expect(2, 1);
            Assert.True(applied != null); Assert.Equal("llm.openrouter", applied.RuleId);
            Assert.Equal("provider chose the standard play", applied.Reason);
            cast.Tick = 3;
            Assert.True(cast.Think(eye) == null); cast.Expect(2, 1);
            // 같은 seed의 규칙 brain을 기록된 관측으로만 같은 횟수 돌리면 이후 판단이 일치한다. 난수 수열이 그대로다.
            Assert.Sequence(new[] { eye, eye }, cast.Fallback.Eyes);
            var reference = BenchBrain(4242);
            var parameters = Parameters();
            for (int i = 0; i < cast.Fallback.Calls; i++) reference.Think(eye, parameters, Master.LeftPlayer, false);
            var probe = BenchEye(70, 1, 2);
            Assert.Equal(Trace(reference.Think(probe, parameters, Master.LeftPlayer, false)),
                Trace(cast.Fallback.Brain.Think(probe, parameters, Master.LeftPlayer, false)));

            // 대기 응답은 규칙 brain과 후보를 전혀 건드리지 않는다.
            var waiting = Prepared(real, eye, out var waitingRequest);
            waiting.Confirm(2, Complete(waitingRequest, ModelProtocol.WaitPlayKey));
            Assert.True(waiting.Think(eye) == null); waiting.Expect(1, 1);

            // 손패에서 사라진 카드도 0회다.
            var gone = Prepared(real, eye, out var goneRequest);
            gone.Confirm(2, Complete(goneRequest, StandardPlayKey));
            Assert.True(gone.Think(BenchEye(50, 2)) == null); gone.Expect(1, 1);

            // 모델이 고른 소환 위치를 쓰는 분기도 0회다.
            var aimed = new Bench(ModelDecisionPolicy.Cast, new ScriptedBuilder { Menu = SummonMenu });
            Assert.True(aimed.Think(eye) == null); aimed.Expect(1, 1);
            var aimedRequest = aimed.Request(1);
            aimed.Confirm(1, Accept(aimedRequest)); aimed.Tick = 2;
            aimed.Confirm(2, Complete(aimedRequest, "summon", "near_low"));
            var kept = aimed.Think(eye); aimed.Expect(1, 1);
            Assert.True(kept != null); Assert.Equal((Fix64)2, kept.Target.Z);

            // 표준 선택지가 아니면 규칙 brain 1회 뒤 후보를 다시 만든다.
            var fresh = new Bench(ModelDecisionPolicy.Cast, new ScriptedBuilder { Menu = AlternativeMenu });
            Assert.True(fresh.Think(eye) == null); fresh.Expect(1, 1);
            var freshRequest = fresh.Request(1);
            fresh.Confirm(1, Accept(freshRequest)); fresh.Tick = 2;
            fresh.Confirm(2, Complete(freshRequest, "alt"));
            Assert.True(fresh.Think(eye) != null); fresh.Expect(2, 2);

            // 실패는 규칙 brain 1회, 후보 0회로 그 프레임을 규칙 brain이 판단한다.
            var failed = Prepared(real, eye, out var failedRequest);
            failed.Confirm(2, Failure(failedRequest));
            var ruled = failed.Think(eye); failed.Expect(2, 1);
            Assert.True(ruled != null); Assert.Equal(BotBrain.ValueRule, ruled.RuleId);

            // backoff 구간도 규칙 brain 1회, 후보 0회다.
            var backing = Prepared(real, eye, out var backingRequest);
            backing.Confirm(2, BackOff(backingRequest, 9));
            Assert.True(backing.Think(eye) != null); backing.Expect(2, 1);
            backing.Tick = 3;
            Assert.True(backing.Think(eye) != null); backing.Expect(3, 1);

            // 선택지가 없으면 규칙 brain 1회, 후보 1회로 끝난다.
            var idle = new Bench(ModelDecisionPolicy.Cast, real);
            Assert.True(idle.Think(BenchEye(50)) == null); idle.Expect(1, 1);

            // JEV는 규칙 brain을 먼저 1회 부르고 후보는 standard:null로 만든다.
            var stance = new Bench(ModelDecisionPolicy.Stance, real);
            Assert.True(stance.Think(eye) != null); stance.Expect(1, 1);
            Assert.True(stance.Builder.LastStandardWasNull);
            var stanceRequest = stance.Request(1);
            stance.Confirm(1, Accept(stanceRequest)); stance.Tick = 2;
            stance.Confirm(2, BackOff(stanceRequest, 9));
            // backoff 중에는 후보를 만들지 않는다.
            Assert.True(stance.Think(eye) != null); stance.Expect(2, 1);
        }
        private static Bench Prepared(IModelCandidateBuilder inner, BotEye eye, out ModelRequest request) {
            var bench = new Bench(ModelDecisionPolicy.Cast, inner);
            bench.Think(eye);
            request = bench.Request(1);
            bench.Confirm(1, Accept(request));
            bench.Tick = 2;
            return bench;
        }
        private static string Trace(InputDecision decision) => decision == null ? "-"
            : decision.MagicId + "@" + decision.Target.X.RawValue + "," + decision.Target.Z.RawValue + "/" + decision.RuleId;

        // ---- terminal 신원과 멱등 ----

        private static void CheckTerminalIntegrity() {
            var eye = BenchEye(50, 1, 2);
            var real = RealBuilder();

            // 같은 본문만 멱등하다. 다른 kind나 다른 play는 충돌이다.
            var bench = Prepared(real, eye, out var request);
            var completed = Complete(request, StandardPlayKey, reason: "chosen");
            bench.Confirm(2, completed);
            bench.Tick = 3; bench.Confirm(3, Complete(request, StandardPlayKey, reason: "chosen"));
            bench.Tick = 4;
            Expect<BotDecisionProtocolException>(() => bench.Confirm(4, Failure(request)));
            Expect<BotDecisionProtocolException>(() => bench.Confirm(4, Complete(request, StandardPlayKey, reason: "other")));
            Expect<BotDecisionProtocolException>(() => bench.Confirm(4, Complete(request, ModelProtocol.WaitPlayKey)));

            // terminal은 수락된 관측 tick과 묶여 있다.
            var bound = Prepared(real, eye, out var boundRequest);
            var drifted = Complete(boundRequest, StandardPlayKey);
            drifted.observationTick = boundRequest.observationTick + 1;
            Expect<BotDecisionProtocolException>(() => bound.Confirm(2, drifted));

            // 수락된 적이 없는 신원은 위조다.
            var forged = Prepared(real, eye, out var forgedRequest);
            var invented = Complete(forgedRequest, StandardPlayKey);
            invented.sequence = forgedRequest.sequence + 5;
            Expect<BotDecisionProtocolException>(() => forged.Confirm(2, invented));

            // 교체된 generation의 진짜 늦은 결과는 버리고 deadline도 보지 않는다.
            var replaced = Prepared(real, eye, out var staleRequest);
            replaced.Ledger.Detach(Master.LeftPlayer);
            var next = new ModelDecisionSource(new SpyFallback(BenchBrain(1)), real, ModelDecisionPolicy.Cast, "llm.openrouter");
            replaced.Ledger.Attach(Master.LeftPlayer, 1, next);
            replaced.Confirm(2, Complete(staleRequest, StandardPlayKey));
            for (uint tick = 3; tick <= staleRequest.observationTick + Deadline + 2; tick++) { replaced.Tick = tick; replaced.Confirm(tick); }
            // 그 신원의 두 번째 결과도 위조가 아니라 재전송이므로 멱등하게 무시한다.
            replaced.Tick++; replaced.Confirm(replaced.Tick, Complete(staleRequest, StandardPlayKey));

            // 같은 신원을 두 번 수락하는 기록은 거부한다.
            var twice = Prepared(real, eye, out var twiceRequest);
            Expect<BotDecisionProtocolException>(() => twice.Confirm(2, Accept(twiceRequest)));
        }

        // ---- 후보 동결과 digest ----

        private static ModelCandidatePlay BenchPlay(string key, long magicId, int manaCost, decimal x, string description,
            bool summon = false, Fix64 baseDamage = default, Fix64 unitDamage = default, IReadOnlyList<ModelCandidateAim> aims = null) =>
            new ModelCandidatePlay(key, magicId, "cheap", manaCost, new BEPUutilities.Vector3((Fix64)x, 0, 5), description,
                summon, baseDamage, unitDamage, aims);

        private static void CheckFrozenCandidates() {
            var aims = new List<ModelCandidateAim> { new ModelCandidateAim("one", "first", new BEPUutilities.Vector3(3, 0, 3)) };
            var play = BenchPlay("p0", 1, 5, 4, "cheap (damage spell, cost 5)", baseDamage: (Fix64)10, aims: aims);
            aims.Add(new ModelCandidateAim("two", "second", new BEPUutilities.Vector3(6, 0, 6)));
            Assert.Equal(1, play.Aims.Count);
            var plays = new List<ModelCandidatePlay> { play };
            var menu = new ModelCandidateMenu(plays, "wait here", BenchState(false, 5, 900));
            plays.Add(BenchPlay("p1", 2, 900, 5, "dear (damage spell, cost 900)"));
            Assert.Equal(1, menu.Plays.Count);
            // attack/save 분류는 선택지에서 끌어내므로 메뉴 밖의 후보가 끼어들 수 없다.
            Assert.True(ReferenceEquals(menu.Plays[0], menu.AttackPlay));
            Assert.Equal(900, menu.DearestManaCost);
            Assert.True(menu.SavePlay == null);
            Assert.True(!menu.Urgent);

            var duplicateAims = new[] { new ModelCandidateAim("one", "a", BEPUutilities.Vector3.Zero), new ModelCandidateAim("one", "b", BEPUutilities.Vector3.Up) };
            Expect<ArgumentException>(() => BenchPlay("p0", 1, 1, 0, "x", aims: duplicateAims));
            Expect<ArgumentException>(() => BenchPlay("p0", 1, 1, 0, "x", aims: new ModelCandidateAim[] { null }));
            Expect<ArgumentException>(() => new ModelCandidateMenu(new[] { play, play }, null, BenchState(false)));
            Expect<ArgumentException>(() => new ModelCandidateMenu(new ModelCandidatePlay[] { null }, null, BenchState(false)));

            var eye = BenchEye(50, 1, 2);
            string Digest(BotEye observed, ModelCandidateMenu built) => ModelDigest.Compute("bench", 0, 0, 0, 1, observed, built);
            string baseline = Digest(eye, new ModelCandidateMenu(new[] { play }, "wait here", BenchState(false, 5, 900)));
            Assert.Equal(baseline, Digest(eye, new ModelCandidateMenu(new[] { play }, "wait here", BenchState(false, 5, 900))));
            // attack/save 분류를 바꾸는 state와 선택지 본문이 모두 digest에 묶인다.
            Assert.True(baseline != Digest(eye, new ModelCandidateMenu(new[] { play }, "wait here", BenchState(true, 5, 900))));
            Assert.True(baseline != Digest(eye, new ModelCandidateMenu(new[] { play }, "wait here", BenchState(false, 5, 5))));
            Assert.True(baseline != Digest(eye, new ModelCandidateMenu(new[] { play }, null, BenchState(false, 5, 900))));
            Assert.True(baseline != Digest(eye, new ModelCandidateMenu(
                new[] { BenchPlay("p0", 1, 5, 4, "cheap (damage spell, cost 5) reworded", baseDamage: (Fix64)10, aims: play.Aims) },
                "wait here", BenchState(false, 5, 900))));
            // 관측 시각은 봇의 이동 추정을 바꾼다.
            var later = new BotEye(eye.Objects, eye.Cards, eye.Mana, eye.EnemyPlayerHp, (Fix64)2);
            Assert.True(baseline != Digest(later, new ModelCandidateMenu(new[] { play }, "wait here", BenchState(false, 5, 900))));
            var timeless = new BotEye(eye.Objects, eye.Cards, eye.Mana, eye.EnemyPlayerHp);
            Assert.True(baseline != Digest(timeless, new ModelCandidateMenu(new[] { play }, "wait here", BenchState(false, 5, 900))));
        }

        // ---- 이벤트 본문 계약 ----

        private static void CheckEventContract() {
            var terminal = new ModelEvent { kind = ModelProtocol.Failed, slot = 0, generation = 0, sequence = 0, observationTick = 1, reason = "provider" };
            Assert.Equal("provider", ModelEvent.Copy(terminal).reason);
            // manifest가 아닌 이벤트의 digest 자리는 비어 있어야 한다. 잘못된 문자열도 통과하면 안 된다.
            terminal.digest = "not-a-digest";
            Expect<InvalidDataException>(() => ModelEvent.Copy(terminal));
            terminal.digest = new string('a', 64);
            Expect<InvalidDataException>(() => ModelEvent.Copy(terminal));
            terminal.digest = null;
            terminal.reason = "";
            Expect<InvalidDataException>(() => ModelEvent.Copy(terminal));
            terminal.reason = new string('x', ModelProtocol.MaxReasonLength);
            Assert.Equal(ModelProtocol.MaxReasonLength, ModelEvent.Copy(terminal).reason.Length);

            // 완료 기록은 provider의 선택 사유를 보존하고, 수락 통지는 사유를 담지 않는다.
            var completed = new ModelEvent { kind = ModelProtocol.Completed, observationTick = 1, playKey = "p0", reason = "because" };
            Assert.Equal("because", ModelEvent.Copy(completed).reason);
            var accepted = new ModelEvent { kind = ModelProtocol.Accepted, observationTick = 1, digest = new string('a', 64), terminalTick = 5, reason = "no" };
            Expect<InvalidDataException>(() => ModelEvent.Copy(accepted));

            // 화면용 사유는 원본과 같은 120자에서 자른다.
            var eye = BenchEye(50, 1, 2);
            var bench = Prepared(RealBuilder(), eye, out var request);
            string verbose = new string('r', ModelProtocol.MaxReasonLength);
            bench.Confirm(2, Complete(request, StandardPlayKey, reason: verbose));
            var decision = bench.Think(eye);
            Assert.True(decision != null);
            Assert.Equal(ModelProtocol.MaxDecisionReasonLength, decision.Reason.Length);
        }

        // ---- 묶인 카드 설명문과 세션별 배치 seed ----

        // 기본 생산 경로가 묶인 설명문을 실제 선택지 본문에 넣는다.
        private static void CheckBundledCardTextReachesTheMenu() {
            var catalog = new TestBotCatalog(new BotMagic(1, "wind_totem", summoning: new SummonedBodies("WindTotem")));
            var parameters = new Dictionary<string, Fix64> { ["wind_totem.mana_cost"] = 5, ["wind_totem.range"] = 6 };
            var eye = new BotEye(new[] {
                new BotVisibleObject(11, Master.RightPlayer, "FireSlime", new BEPUutilities.Vector3(14, 0, 5), 25, true, true)
            }, new long[] { 1 }, 50, 100, Fix64.One);
            // descriptions를 주지 않으면 묶인 사본을 쓴다.
            var menu = ModelCandidateBuilder.Cards(catalog, new Counters(), () => new JavaRandom(7))
                .Build(eye, parameters, Master.LeftPlayer, null);
            string description = menu.Describe("wind_totem");
            Assert.True(description != null);
            Assert.True(description.Contains("Places a wind totem that repeatedly pushes enemy ground units"));
            Assert.True(MagicDescriptions.Bundled.Count > 50);
            // 묶인 사본은 한 번만 읽고 같은 객체를 돌려준다.
            Assert.True(ReferenceEquals(MagicDescriptions.Bundled, MagicDescriptions.Bundled));
        }

        // 배치 seed는 확정 snapshot의 randomSeed와 side·generation만으로 정해진다.
        private static void CheckPlacementSeedFollowsTheSessionSeed() {
            long left0 = ModelBotDecisions.PlacementSeed(777, Master.LeftPlayer, 0);
            Assert.Equal(left0, ModelBotDecisions.PlacementSeed(777, Master.LeftPlayer, 0));
            Assert.True(left0 != ModelBotDecisions.PlacementSeed(778, Master.LeftPlayer, 0));
            Assert.True(left0 != ModelBotDecisions.PlacementSeed(777, Master.RightPlayer, 0));
            Assert.True(left0 != ModelBotDecisions.PlacementSeed(777, Master.LeftPlayer, 1));

            // 원본 supplier 계약: 배치마다 새 generator이므로 같은 seed는 같은 첫 수를 준다.
            var supplier = ModelBotDecisions.PlacementRandom(777, Master.LeftPlayer, 0);
            var first = supplier(); var second = supplier();
            Assert.True(!ReferenceEquals(first, second));
            Assert.Equal(first.NextFraction(), second.NextFraction());
            // 다른 세션 seed는 독립된 수열을 준다.
            Assert.True(ModelBotDecisions.PlacementRandom(777, Master.LeftPlayer, 0)().NextFraction()
                != ModelBotDecisions.PlacementRandom(778, Master.LeftPlayer, 0)().NextFraction());

            // 같은 세션 seed의 두 replica는 같은 배치를, 다른 세션 seed는 다른 배치를 만든다.
            string Placement(long sessionSeed) {
                var catalog = new TestBotCatalog(new BotMagic(1, "cheap", summoning: new SummonedBodies("FireSlime")));
                var parameters = new Dictionary<string, Fix64> { ["cheap.mana_cost"] = 5, ["cheap.range"] = 6 };
                // 위협이 없는 판에서만 배치 난수가 쓰인다.
                var eye = new BotEye(Array.Empty<BotVisibleObject>(), new long[] { 1 }, 50, 100, Fix64.One);
                var menu = new ModelCandidateBuilder(catalog, new Counters(),
                    ModelBotDecisions.PlacementRandom(sessionSeed, Master.LeftPlayer, 0)).Build(eye, parameters, Master.LeftPlayer, null);
                var play = menu.Plays[0];
                return play.Target.X.RawValue + "," + play.Target.Z.RawValue;
            }
            Assert.Equal(Placement(777), Placement(777));
            Assert.True(Placement(777) != Placement(778));

            // v1 경기는 모델 소스를 만들지 않으므로 배치 seed가 전혀 쓰이지 않는다.
            var rule = ModelBotDecisions.Confirmed(777)(Master.LeftPlayer, 0,
                new BotPersona { BrainType = BotBrainType.Rule }, new RuleDecisionSource(BenchBrain(1)));
            Assert.True(rule is RuleDecisionSource);
        }

        // ---- generation 증가와 Bind 실패 ----

        private sealed class RefusingSource : IModelDecisionSource {
            public InputDecision Think(BotEye eye, IDictionary<string, Fix64> parameters, Master side, bool overdue) => null;
            public void Bind(ModelRequestChannel channel) => throw new BotDecisionProtocolException("bind fixture");
            public void Deliver(ModelEvent confirmed) { }
        }
        private static void CheckGenerationMonotonic() {
            var real = RealBuilder();
            var eye = BenchEye(50, 1, 2);
            var bench = new Bench(ModelDecisionPolicy.Cast, real);
            // 같은 generation을 다시 붙이면 sequence가 0부터 재사용되므로 거부한다.
            Expect<InvalidOperationException>(() => bench.Ledger.Attach(Master.LeftPlayer, 0, new ModelDecisionSource(new SpyFallback(BenchBrain(1)), real, ModelDecisionPolicy.Cast, "llm.openrouter")));
            // 한 소스는 하나의 generation에만 결속한다.
            Expect<BotDecisionProtocolException>(() => bench.Ledger.Attach(Master.LeftPlayer, 1, bench.Source));

            // Bind가 실패하면 슬롯은 그대로이므로 같은 generation으로 다시 붙일 수 있다.
            var retry = new Bench(ModelDecisionPolicy.Cast, real);
            retry.Ledger.Detach(Master.LeftPlayer);
            Expect<BotDecisionProtocolException>(() => retry.Ledger.Attach(Master.LeftPlayer, 1, new RefusingSource()));
            var second = new ModelDecisionSource(new SpyFallback(BenchBrain(7)), real, ModelDecisionPolicy.Cast, "llm.openrouter", 10);
            retry.Ledger.Attach(Master.LeftPlayer, 1, second);
            retry.Tick = 1;
            Assert.True(second.Think(eye, Parameters(), Master.LeftPlayer, false) == null);
            var request = retry.Ledger.PendingManifest(1)[0];
            Assert.Equal(1L, request.generation); Assert.Equal(0L, request.sequence);
        }

        private static void CheckRejectedBackoff() {
            var eye = BenchEye(50, 1, 2);
            var bench = new Bench(ModelDecisionPolicy.Cast, RealBuilder(), interval: 1);
            bench.Think(eye);
            var request = bench.Request(1);
            var rejected = Reject(request, "provider is backing off"); rejected.resumeTick = 9;
            var copied = ModelEvent.Copy(rejected);
            Assert.Equal(9U, copied.resumeTick);
            bench.Confirm(1, copied);
            Assert.Equal(9U, bench.Ledger.ResumeTick(0));
            bench.Tick = 2; Assert.True(bench.Think(eye) != null); bench.Expect(2, 1);
            bench.Tick = 3; Assert.True(bench.Think(eye) != null); bench.Expect(3, 1);
            bench.Tick = 9; bench.Think(eye); bench.Expect(4, 2);
            Assert.True(bench.Ledger.PendingManifest(9) != null);
#if NET8_0_OR_GREATER
            var json = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            var wire = System.Text.Json.JsonSerializer.Serialize(new InputBatch {
                tick = 9, commands = Array.Empty<CastCommand>(), modelRequests = bench.Ledger.PendingManifest(9)
            }, json);
            var batch = System.Text.Json.JsonSerializer.Deserialize<InputBatch>(wire, json);
            var restored = ModelRequest.Copy(batch.modelRequests[0]);
            Assert.Equal(ModelRequestPayload.Hash(restored.payload), restored.payloadHash);
            Assert.True(restored.payload.criteria.Length > 0 && restored.payload.state.Length > 0);
#endif
            copied.resumeTick = copied.observationTick;
            bool rejectedTick = false;
            try { ModelEvent.Copy(copied); } catch (InvalidDataException) { rejectedTick = true; }
            Assert.True(rejectedTick);
        }

        public static void Run() {
            CheckRejectedBackoff();
            CheckVersionSeparation();
            CheckEventContract();
            CheckFrozenCandidates();
            CheckBranchCallCounts();
            CheckTerminalIntegrity();
            CheckGenerationMonotonic();
            CheckBundledCardTextReachesTheMenu();
            CheckPlacementSeedFollowsTheSessionSeed();
            CheckObservedStateIsAFrozenSnapshot();
            CheckPayloadBindsTheObservedMenu();
            CheckStancePayloadAsksForAPlan();
            CheckMutatedPayloadWithGenuineDigest();
            CheckCastReplaySlice();
            CheckCastFallbackAndWait();
            CheckStanceReplaySlice();
            CheckDroppedManifest();
            CheckExtraManifest();
            CheckConflictingDigest();
            CheckTerminalDeadline();
            CheckDuplicateResultIsIdempotent();
            CheckGenerationReplacement();
        }
    }
}
