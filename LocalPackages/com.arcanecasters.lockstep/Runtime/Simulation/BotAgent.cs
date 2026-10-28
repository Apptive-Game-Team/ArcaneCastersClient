using System;
using System.Collections.Generic;
using FixMath.NET;
using BEPUutilities;

namespace ArcaneCasters.Simulation {
    public sealed class BotThought {
        public readonly Master Side;
        public readonly string RuleId, Reason;
        public readonly Vector3 Target;
        public IReadOnlyList<long> Cards { get; }
        public BotThought(Master side, InputDecision decision) {
            Side = side; RuleId = decision.RuleId; Reason = decision.Reason; Target = decision.Target;
            Cards = Array.AsReadOnly(decision.MagicId == 0 ? Array.Empty<long>() : new[] { decision.MagicId });
        }
    }
    public sealed class BotAgent {
        public const long ThoughtIntervalMillis = 10000;
        private readonly object gate = new object();
        private readonly BotAction action = new BotAction();
        private readonly IDictionary<string, Fix64> parameters;
        private readonly IMillisClock clock;
        private readonly Func<int> nextRequestId;
        private readonly CastDeadline deadline;
        private readonly BotEmoteDirector emotes;
        private readonly Func<Master, bool> consumeEmoteCooldown;
        private readonly IntervalTimer reaction = new IntervalTimer();
        private InputDecision pending, last;
        private long readyAt, nextThoughtAt;
        public IBotDecisionSource DecisionSource { get; }
        public BotPersona Persona { get; }
        public Master Side { get; }
        public event Action<BotThought> Thought;
        public event Action<Master, Emote> EmoteSent;
        public InputDecision LastDecision { get { lock (gate) return last; } }
        public bool HasReadyPendingDecision { get { lock (gate) return pending != null && clock.NowMillis >= readyAt; } }
        // 요청 번호는 세션이 공급한다. 다른 경기의 실행 순서가 번호를 바꾸지 않는다.
        public BotAgent(IBotDecisionSource source, Master side, BotPersona persona, IDictionary<string, Fix64> parameters, IMillisClock clock, Func<int> nextRequestId, BotEmoteDirector emotes = null, Func<Master, bool> consumeEmoteCooldown = null) {
            DecisionSource = source ?? throw new ArgumentNullException(nameof(source)); Side = side;
            Persona = persona ?? throw new ArgumentNullException(nameof(persona)); this.parameters = parameters;
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock)); this.nextRequestId = nextRequestId ?? throw new ArgumentNullException(nameof(nextRequestId));
            deadline = new CastDeadline(persona.Tier, clock.NowMillis); nextThoughtAt = clock.NowMillis + ThoughtIntervalMillis;
            if (emotes != null && consumeEmoteCooldown == null) throw new ArgumentNullException(nameof(consumeEmoteCooldown));
            this.emotes = emotes; this.consumeEmoteCooldown = consumeEmoteCooldown;
        }
        public void OnOpponentEmote(Emote emote) { lock (gate) emotes?.OnOpponentEmote(emote, clock.NowMillis); }
        public void UpdateEmote(SimulationContext context, long nowMillis) {
            lock (gate) {
                if (emotes == null) return;
                var enemy = Side == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer;
                var emote = emotes.NextEmote(nowMillis, context.Players[Side].Hp, context.Players[enemy].Hp);
                if (emote.HasValue && consumeEmoteCooldown(Side)) EmoteSent?.Invoke(Side, emote.Value);
            }
        }
        public bool ShouldProcess(Fix64 deltaTime) {
            lock (gate) {
                bool due = reaction.Advance(deltaTime, Persona.ReactionIntervalSeconds) > 0;
                return clock.NowMillis >= nextThoughtAt || HasReadyPendingDecision || (pending == null && due);
            }
        }
        public void OnTick(BotEye eye, GameActionSender<SimulationContext> sender) {
            lock (gate) {
                if (DispatchIfReady(sender)) return;
                if (clock.NowMillis >= nextThoughtAt) Publish(last ?? new InputDecision(0, new Vector3(Side == Master.LeftPlayer ? 1 : 17, 0, 5), "idle.observing", "No action has been selected yet; observing the battlefield."));
                if (pending != null) return;
                var decision = DecisionSource.Think(eye, parameters, Side, deadline.Overdue(clock.NowMillis));
                if (decision == null) return;
                pending = decision; readyAt = clock.NowMillis + Persona.NormalizedThinkingTimeMs;
                DispatchIfReady(sender);
            }
        }
        private bool DispatchIfReady(GameActionSender<SimulationContext> sender) {
            if (pending == null || clock.NowMillis < readyAt) return false;
            var decision = pending; pending = null; last = decision;
            action.UseCard(sender, new BotInputRequest(nextRequestId(), decision.MagicId, decision.Target), Side);
            deadline.RecordCast(clock.NowMillis); Publish(decision); return true;
        }
        private void Publish(InputDecision decision) { nextThoughtAt = clock.NowMillis + ThoughtIntervalMillis; Thought?.Invoke(new BotThought(Side, decision)); }
    }
}
