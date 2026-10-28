using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class BattleRuntimeFactory {
        private readonly BattleContent content;
        private readonly IBotCounterScorer counters;
        private readonly Func<long, BotPersona> personas;
        private readonly Func<long, Fix64> noviceProgress;
        private readonly BotDecisionSourceFactory decisionFactory;
        public BattleRuntimeFactory(BattleContent content, IBotCounterScorer counters, Func<long, BotPersona> personas,
            Func<long, Fix64> noviceProgress, BotDecisionSourceFactory decisionFactory = null) {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            this.counters = counters ?? throw new ArgumentNullException(nameof(counters));
            this.personas = personas ?? throw new ArgumentNullException(nameof(personas));
            this.noviceProgress = noviceProgress ?? throw new ArgumentNullException(nameof(noviceProgress)); this.decisionFactory = decisionFactory;
        }
        public BattleSessionRuntime Create(SessionDefinition definition, IBattleFrameOutput output = null, PveScenarioRegistry scenarios = null, IPveSessionSink pveSink = null) {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!content.MatchesParticipants(definition.LeftUserId, definition.RightUserId)) throw new ArgumentException("Battle content participants do not match the session.");
            if (definition.Type == SessionType.PVE && (scenarios == null || pveSink == null)) throw new ArgumentException("PVE requires scenario content and a session sink.");
            if (definition.Type == SessionType.PVE && (pveSink.LeftUserId != definition.LeftUserId || pveSink.RightUserId != definition.RightUserId)) throw new ArgumentException("PVE sink participants do not match the session.");
            var context = new SimulationContext(0); context.SetTickRate(20);
            var parser = content.Configure(context);
            int maxMana = Integer(context, "player.max_mana"), duration = Integer(context, "game.duration"), fever = Integer(context, "game.fever_duration");
            context.Players.Add(Master.LeftPlayer, new PlayerData(new ManaCharger(maxMana)));
            context.Players.Add(Master.RightPlayer, new PlayerData(new ManaCharger(maxMana)));
            StandardPrefabs.Register(context);
            // 필수 콘텐츠 누락으로 파괴된 객체만 남긴 채 매치를 시작하지 않는다.
            context.InitializationFailed += (owner, error) => throw new InvalidOperationException("Battle prefab initialization failed: " + owner.Prefab, error);
            var runtime = new BattleSessionRuntime(context, definition, parser, new ParsedBotMagicCatalog(parser, context), counters, personas, noviceProgress,
                owner => owner.GetComponent<MobComponent>()?.Hp, duration, fever, definition.Type != SessionType.PVE, output, decisionFactory);
            if (definition.Type == SessionType.PVE) runtime.InitializePve(scenarios, pveSink);
            return runtime;
        }
        private static int Integer(SimulationContext context, string key) => checked((int)(context.GameParameters[key].RawValue / (1L << 32)));
    }
}
