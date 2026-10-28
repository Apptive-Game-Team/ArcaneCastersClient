using System;

namespace ArcaneCasters.Simulation {
    // 동일 틱의 이벤트를 한 번 인계하고, 플레이어와 관전자별 비공개 데이터를 분리한다.
    public sealed class BattleSessionOutput : IBattleFrameOutput, IBattleOutputInitializer, IDisposable {
        private readonly BattlePresentationOutput left, right;
        private readonly BattlePresentationOutput spectator;
        private bool spectatorActive;
        private readonly Func<bool> hasSpectators;
        private SimulationContext context;
        public BattlePresentationFrame LeftFrame { get; private set; }
        public BattlePresentationFrame RightFrame { get; private set; }
        public BattlePresentationFrame SpectatorFrame { get; private set; }
        public BattleSessionOutput(long leftId, long rightId, Action<long, BattlePresentationFrame> publish, Func<bool> hasSpectators = null) {
            if (publish == null) throw new ArgumentNullException(nameof(publish)); this.hasSpectators = hasSpectators;
            left = new BattlePresentationOutput(Master.LeftPlayer, frame => { LeftFrame = frame; publish(leftId, frame); });
            right = new BattlePresentationOutput(Master.RightPlayer, frame => { RightFrame = frame; publish(rightId, frame); });
            spectator = new BattlePresentationOutput(null, frame => { SpectatorFrame = frame; publish(0, frame); });
        }
        public void Initialize(SimulationContext context, BattleEconomySystem economy, int durationMillis) {
            if (this.context != null) throw new InvalidOperationException("Output already initialized.");
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            left.Initialize(context, economy, durationMillis); right.Initialize(context, economy, durationMillis);
            spectator.Initialize(context, economy, durationMillis);
        }
        public void BeginFrame(SimulationContext current) {
            if (!ReferenceEquals(context, current)) throw new InvalidOperationException("Output context mismatch.");
            LeftFrame = null; RightFrame = null; SpectatorFrame = null;
            spectatorActive = hasSpectators?.Invoke() == true;
            var events = current.DrainEvents(); left.BeginFrame(current, events); right.BeginFrame(current, events);
            if (spectatorActive) spectator.BeginFrame(current, events); else spectator.SkipFrame();
            LeftFrame = left.CurrentFrame; RightFrame = right.CurrentFrame; SpectatorFrame = spectator.CurrentFrame;
        }
        public void BuildSnapshot(SimulationContext current) { left.BuildSnapshot(current); right.BuildSnapshot(current); if (spectatorActive) spectator.BuildSnapshot(current); }
        public void EndFrame(SimulationContext current) { left.EndFrame(current); right.EndFrame(current); if (spectatorActive) spectator.EndFrame(current); }
        public void Dispose() { left.Dispose(); right.Dispose(); spectator.Dispose(); context = null; }
    }
}
