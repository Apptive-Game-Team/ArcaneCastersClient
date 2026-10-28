using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class BattleSessionRuntime {
        public SimulationContext Context { get; }
        public SessionDefinition Definition { get; }
        public SessionEmotes Emotes { get; }
        public SessionBotControl Bots { get; }
        public ModelEventLedger Models { get; }
        public BotLifecycle BotLifecycle { get; }
        public BattleFrameLoop Loop { get; }
        public int DurationMillis { get; }
        public Exception Failure { get; private set; }
        public PveMatch Pve { get; private set; }
        public BattleEconomySystem Economy { get; } = new BattleEconomySystem();
        public event Action<Master?> PveResultDecided;
        public event Action<long, BattleResultMessage> PveResultReady;
        private int lastFrame;
        private bool advancing, closeRequested;
        public bool HasStarted { get; private set; }
        public bool IsTerminated { get; private set; }
        public bool IsRunning => HasStarted && !IsTerminated;
        public event Action Terminated;
        public void Close() {
            closeRequested = true;
            if (!advancing) Terminate();
        }
        // 확정 입력이나 모델 프로토콜 검증이 실패하면 그 오류를 남기고 종료한다. 검증기는 실패한 재생 결과를 저장하면 안 된다.
        public void Fail(Exception error) {
            Failure ??= error ?? throw new ArgumentNullException(nameof(error));
            Close();
        }
        private void Terminate() {
            if (IsTerminated) return;
            IsTerminated = true;
            // 한 구독자의 오류가 나머지 정리나 최초 프레임 오류를 가리지 않게 한다.
            foreach (var callback in Terminated?.GetInvocationList() ?? Array.Empty<Delegate>()) {
                try { ((Action)callback)(); } catch (Exception error) { Failure ??= error; }
            }
        }
        // 콘텐츠/플레이어/prefab 구성 이후, 확정된 세션 정의로 런타임 서비스를 연결한다.
        public BattleSessionRuntime(SimulationContext context, SessionDefinition definition, IMagicLookup magicLookup, IBotMagicCatalog botCatalog, IBotCounterScorer counters, Func<long, BotPersona> personas, Func<long, Fix64> noviceProgress, Func<SimObject, int?> mobHp, int durationMillis, int feverMillis, bool createRightPlayer = true, IBattleFrameOutput output = null, BotDecisionSourceFactory decisionFactory = null) {
            if (durationMillis <= 0 || feverMillis < 0 || feverMillis > durationMillis) throw new ArgumentOutOfRangeException(nameof(durationMillis));
            Context = context ?? throw new ArgumentNullException(nameof(context)); Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            DurationMillis = durationMillis;
            definition.InitializeIdentityAndDecks(context);
            context.Random = new JavaRandom(definition.RandomSeed);
            context.InputHandler = new MagicInputHandler(magicLookup);
            context.CardSelectVisualizer = new CardSelectVisualizer(magicLookup);
            context.InsertService(new BotInputHandlerService(new BotMagicInputAdapter(context.InputHandler)));
            var clock = new FrameMillisClock(context); Emotes = new SessionEmotes(context);
            var timer = new GameTimer(durationMillis, feverMillis, clock);
            Models = new ModelEventLedger(definition.SessionId, () => context.FrameNumber);
            Bots = new SessionBotControl(context, botCatalog, counters, personas, definition.LeftDeck, definition.RightDeck, definition.RandomSeed, side => Emotes.Cooldown.TryConsume(side), decisionFactory, Models);
            context.InsertService(Bots); context.InsertService(Emotes); context.InsertService(Models); Bots.EmoteSent += Emotes.PublishBot;
            BotLifecycle = new BotLifecycle(context, Bots, noviceProgress);
            var systems = new BattleFrameSystems {
                Economy = Economy,
                Bots = new BotAgentSystem(Bots, mobHp, clock),
                Fever = new FeverTimeSystem(() => timer.Fever), Output = output
            };
            // 실행을 계속하는 기존 오류 격리와 별개로, 검증기는 실패한 재생 결과를 저장하면 안 된다.
            context.Actions.Failed += (_, error) => Failure ??= error;
            ((ComponentUpdateSystem)systems.Components).Failed += (_, error) => Failure ??= error;
            ((BattlePhysicsSystem)systems.Physics).CollisionFailed += (_, error) => Failure ??= error;
            ((BotAgentSystem)systems.Bots).ThinkFailed += (_, error) => {
                Failure ??= error;
                // 이 런타임의 동기 봇 실행에서 확정 기록 오류는 현재 프레임을 즉시 중단한다.
                if (error is BotDecisionProtocolException) throw error;
            };
            Loop = new BattleFrameLoop(context, systems, () => timer.Ended) { HasTimeLimit = definition.Type != SessionType.PVE };
            (output as IBattleOutputInitializer)?.Initialize(context, Economy, durationMillis);
            BattleFieldInitializer.Spawn(context, definition, createRightPlayer);
            if (definition.Type == SessionType.Practice) BotLifecycle.InitializePractice();
        }
        public void InitializePve(PveScenarioRegistry registry, IPveSessionSink sink) {
            if (Definition.Type != SessionType.PVE || lastFrame != 0 || Pve != null) throw new InvalidOperationException("PVE must be initialized once before the first confirmed frame.");
            Pve = new PveMatch(registry, Definition.ScenarioId, Context, sink, new PveScenarioComponents());
            Pve.Script.Failed += (_, error) => Failure ??= error;
            Context.InsertService(new PveSync(Pve));
            Loop.BeforeResultCheck += context => { Pve.BeforeResultCheck(context, Definition.Type); Pve.ResultChecker.CheckResult(context); };
            Loop.GameEnded += context => {
                PveResultDecided?.Invoke(context.Result.Loser);
                context.Result.Broadcast(sink.LeftUserId, sink.RightUserId, PveMatch.GameEndMmr(), (id, result) => PveResultReady?.Invoke(id, result));
            };
        }
        public void AdvanceConfirmedFrame(int frame) {
            if (IsTerminated || closeRequested) throw new InvalidOperationException("The battle runtime has terminated.");
            if (advancing) throw new InvalidOperationException("A battle frame is already executing.");
            if (Definition.Type == SessionType.PVE && Pve == null) throw new InvalidOperationException("PVE scenario is not initialized.");
            if (frame != lastFrame + 1) throw new InvalidOperationException("Confirmed battle frames must be consecutive.");
            if (Loop.StopRequested) throw new InvalidOperationException("The battle loop has stopped.");
            HasStarted = true; advancing = true;
            try {
                Context.FrameNumber = frame; Loop.Update(); lastFrame = frame;
            } catch (Exception error) {
                Failure ??= error; closeRequested = true; throw;
            } finally {
                advancing = false;
                if (closeRequested || Loop.StopRequested) Terminate();
            }
        }
    }
}
