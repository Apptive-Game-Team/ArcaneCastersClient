using System;
using System.Threading;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IBotThinkExecutor { void Submit(Action job); }
    public sealed class SynchronousBotThinkExecutor : IBotThinkExecutor { public void Submit(Action job) => job(); }
    public interface IBotAgentSource { BotAgent Left { get; } BotAgent Right { get; } }
    public sealed class BotAgentSlots : IBotAgentSource, IBotControl {
        private readonly Func<SimulationContext, Master, long, Fix64, BotAgent> factory;
        public BotAgent Left { get; set; }
        public BotAgent Right { get; set; }
        public BotAgentSlots(Func<SimulationContext, Master, long, Fix64, BotAgent> factory = null) { this.factory = factory; }
        public void NewBotAgent(SimulationContext context, Master side, long participantId, Fix64 progress) {
            if (factory == null) throw new InvalidOperationException("Bot agent factory is not configured.");
            Set(side, factory(context, side, participantId, progress));
        }
        public void RemoveBotAgent(Master side) => Set(side, null);
        public bool HasBotAgent(Master side) => side == Master.LeftPlayer ? Left != null : side == Master.RightPlayer && Right != null;
        private void Set(Master side, BotAgent agent) {
            if (side == Master.LeftPlayer) Left = agent;
            else if (side == Master.RightPlayer) Right = agent;
            else throw new ArgumentOutOfRangeException(nameof(side));
        }
    }
    public sealed class BotAgentSystem : IGameSystem {
        private readonly IBotAgentSource agents;
        private readonly IBotThinkExecutor executor;
        private readonly Func<SimObject, int?> mobHp;
        private readonly IMillisClock clock;
        private readonly int[] processing = new int[2];
        public bool LeftProcessing => Volatile.Read(ref processing[0]) != 0;
        public bool RightProcessing => Volatile.Read(ref processing[1]) != 0;
        public event Action<Master, Exception> ThinkFailed;
        // lockstep에서는 기본 동기 실행으로 좌측→우측 순서를 고정한다.
        public BotAgentSystem(IBotAgentSource agents, Func<SimObject, int?> mobHp, IMillisClock clock, IBotThinkExecutor executor = null) {
            this.agents = agents ?? throw new ArgumentNullException(nameof(agents)); this.mobHp = mobHp ?? throw new ArgumentNullException(nameof(mobHp));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock)); this.executor = executor ?? new SynchronousBotThinkExecutor();
        }
        public void Update(SimulationContext context) {
            var left = agents.Left; var right = agents.Right;
            Submit(left, context, Master.LeftPlayer, 0); Submit(right, context, Master.RightPlayer, 1);
            long now = clock.NowMillis; left?.UpdateEmote(context, now); right?.UpdateEmote(context, now);
        }
        private void Submit(BotAgent agent, SimulationContext context, Master side, int slot) {
            if (agent == null || !agent.ShouldProcess(context.DeltaTime)) return;
            if (Interlocked.CompareExchange(ref processing[slot], 1, 0) != 0) return;
            try {
                var eye = BotEye.Observe(context, side, mobHp, (Fix64)context.FrameNumber / context.TickRate);
                var sender = context.ActionSender();
                executor.Submit(() => {
                    try { agent.OnTick(eye, sender); }
                    catch (Exception error) { ThinkFailed?.Invoke(side, error); }
                    finally { Volatile.Write(ref processing[slot], 0); }
                });
            } catch { Volatile.Write(ref processing[slot], 0); throw; }
        }
    }
}
