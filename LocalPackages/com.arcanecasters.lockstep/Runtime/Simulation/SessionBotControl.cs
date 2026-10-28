using System;
using System.Collections.Generic;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class SessionBotControl : IBotControl, IBotAgentSource {
        private readonly SimulationContext context;
        private readonly IBotMagicCatalog catalog;
        private readonly IBotCounterScorer counters;
        private readonly Func<long, BotPersona> personas;
        private readonly BotDecisionSourceFactory decisionFactory;
        private readonly ModelEventLedger models;
        private readonly Func<Master, bool> cooldown;
        private readonly long seed;
        private readonly long[][] decks;
        private readonly long[] generations = new long[2];
        private int nextRequestId;
        public BotAgent Left { get; private set; }
        public BotAgent Right { get; private set; }
        public event Action<BotThought> Thought;
        public event Action<Master, Emote> EmoteSent;
        // persona와 덱은 경기 시작 시 확정된 데이터다. 프레임 안에서 네트워크/DB를 조회하지 않는다.
        public SessionBotControl(SimulationContext context, IBotMagicCatalog catalog, IBotCounterScorer counters, Func<long, BotPersona> personas, IReadOnlyList<long> leftDeck, IReadOnlyList<long> rightDeck, long seed, Func<Master, bool> cooldown, BotDecisionSourceFactory decisionFactory = null, ModelEventLedger models = null) {
            this.context = context ?? throw new ArgumentNullException(nameof(context)); this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog)); this.counters = counters ?? throw new ArgumentNullException(nameof(counters));
            this.personas = personas ?? throw new ArgumentNullException(nameof(personas)); this.cooldown = cooldown ?? throw new ArgumentNullException(nameof(cooldown));
            decks = new[] { leftDeck.ToArray(), rightDeck.ToArray() }; this.seed = seed; this.decisionFactory = decisionFactory; this.models = models;
        }
        private static int Index(Master side) => side == Master.LeftPlayer ? 0 : side == Master.RightPlayer ? 1 : throw new ArgumentOutOfRangeException(nameof(side));
        public void NewBotAgent(SimulationContext gameContext, Master side, long participantId, Fix64 opponentNoviceProgress) {
            if (!ReferenceEquals(context, gameContext)) throw new ArgumentException("Bot control belongs to another simulation.", nameof(gameContext));
            int index = Index(side); var persona = Copy(personas(participantId) ?? new BotPersona());
            if (persona.BrainType != BotBrainType.Rule && decisionFactory == null) throw new InvalidOperationException("A synchronized decision source is required for model bots.");
            long generation = generations[index];
            long brainSeed = unchecked(seed + (index + 1) * 0x632BE59BD9B4E019L + generation * 0x1E3779B97F4A7C15L);
            var brain = new BotBrain(catalog, counters, persona, new JavaRandom(brainSeed), deckCards: () => Array.AsReadOnly(decks[index]), opponentProgress: opponentNoviceProgress);
            IBotDecisionSource source = new RuleDecisionSource(brain);
            if (decisionFactory != null) source = decisionFactory(side, generation, persona, source) ?? throw new InvalidOperationException("Bot decision factory returned no source.");
            // 교체된 봇은 이전 generation의 대기 요청을 버리고 새 generation으로 확정 이벤트를 받는다.
            if (decisionFactory != null) models?.Attach(side, generation, source);
            var agent = new BotAgent(source, side, persona, context.GameParameters, new FrameMillisClock(context), () => unchecked(nextRequestId++), new BotEmoteDirector(persona.Temperament, new JavaRandom(brainSeed ^ 0x5DEECE66DL)), cooldown);
            agent.Thought += thought => Thought?.Invoke(thought); agent.EmoteSent += (owner, emote) => EmoteSent?.Invoke(owner, emote);
            if (side == Master.LeftPlayer) Left = agent; else Right = agent;
            generations[index] = generation + 1;
        }
        public bool HasBotAgent(Master side) => side == Master.LeftPlayer ? Left != null : side == Master.RightPlayer && Right != null;
        public void RemoveBotAgent(Master side) {
            if (side == Master.LeftPlayer) Left = null; else if (side == Master.RightPlayer) Right = null; else return;
            models?.Detach(side);
        }
        public void OnOpponentEmote(Master botSide, Emote emote) {
            var agent = botSide == Master.LeftPlayer ? Left : botSide == Master.RightPlayer ? Right : null;
            agent?.OnOpponentEmote(emote);
        }
        private static BotPersona Copy(BotPersona p) => new BotPersona {
            UserId = p.UserId, Name = p.Name, Tier = p.Tier, ThinkingTimeMs = p.ThinkingTimeMs, ReactionIntervalFrames = p.ReactionIntervalFrames,
            CounterAggression = p.CounterAggression, Enabled = p.Enabled, Hospitality = p.Hospitality, Temperament = p.Temperament, BrainType = p.BrainType, LlmModel = p.LlmModel
        };
    }
}
