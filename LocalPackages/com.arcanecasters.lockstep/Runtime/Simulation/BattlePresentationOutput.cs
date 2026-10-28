using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcaneCasters.Simulation {
    public sealed class BattlePresentationFrame {
        public int Frame, TickRate, RemainingTime, Mana, LeftHp, RightHp;
        public bool IsSync, IsRecovery;
        public long[] Cards;
        public ObjectSnapshot[] Created, Updated, Objects;
        public BattleEvent[] Events;
    }
    public interface IBattleOutputInitializer {
        void Initialize(SimulationContext context, BattleEconomySystem economy, int durationMillis);
    }
    // 원본처럼 프레임 시작에 변경분을 인계하고, 끝에서 동기화 스냅샷과 함께 전달한다.
    public sealed class BattlePresentationOutput : IBattleFrameOutput, IBattleOutputInitializer, IDisposable {
        public static BattlePresentationFrame RecoverySnapshot(SimulationContext context, Master? viewer, int durationMillis) {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (viewer.HasValue && viewer != Master.LeftPlayer && viewer != Master.RightPlayer) throw new ArgumentOutOfRangeException(nameof(viewer));
            PlayerData player = null;
            if (viewer.HasValue) context.Players.TryGetValue(viewer.Value,out player);
            return new BattlePresentationFrame {
                Frame = context.FrameNumber, TickRate = context.TickRate, IsSync = true, IsRecovery = true,
                RemainingTime = (int)((durationMillis - (long)context.FrameNumber * 1000 / context.TickRate) / 1000),
                Mana = player?.Mana ?? 0, Cards = player?.Cards.ToArray() ?? Array.Empty<long>(),
                LeftHp = context.Players.TryGetValue(Master.LeftPlayer,out var left) ? left.Hp : 0,
                RightHp = context.Players.TryGetValue(Master.RightPlayer,out var right) ? right.Hp : 0,
                Objects = context.Objects.Select(owner => new ObjectSnapshot(owner)).ToArray(),
                Created = Array.Empty<ObjectSnapshot>(), Updated = Array.Empty<ObjectSnapshot>(), Events = Array.Empty<BattleEvent>()
            };
        }
        private readonly Master? viewer;
        private readonly Action<BattlePresentationFrame> publish;
        private readonly List<ObjectSnapshot> created = new List<ObjectSnapshot>();
        private readonly List<ObjectSnapshot> updated = new List<ObjectSnapshot>();
        private readonly Dictionary<int, int> indices = new Dictionary<int, int>();
        private SimulationContext context;
        private BattleEconomySystem economy;
        private int durationMillis;
        private BattlePresentationFrame frame;
        internal BattlePresentationFrame CurrentFrame => frame;
        internal void SkipFrame() { created.Clear(); updated.Clear(); indices.Clear(); frame = null; }
        public BattlePresentationOutput(Master? viewer, Action<BattlePresentationFrame> publish) {
            if (viewer.HasValue && viewer != Master.LeftPlayer && viewer != Master.RightPlayer) throw new ArgumentOutOfRangeException(nameof(viewer));
            this.viewer = viewer; this.publish = publish ?? throw new ArgumentNullException(nameof(publish));
        }
        public void Initialize(SimulationContext context, BattleEconomySystem economy, int durationMillis) {
            if (this.context != null) throw new InvalidOperationException("Output already initialized.");
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy)); this.durationMillis = durationMillis;
            context.Created += OnCreated; context.Updated += OnUpdated;
        }
        private void OnCreated(SimObject owner) => created.Add(new ObjectSnapshot(owner));
        private void OnUpdated(SimObject owner) {
            if (indices.TryGetValue(owner.Id, out var index)) updated[index] = new ObjectSnapshot(owner, updated[index].Gauges);
            else { indices.Add(owner.Id, updated.Count); updated.Add(new ObjectSnapshot(owner)); }
        }
        public void BeginFrame(SimulationContext current) {
            if (!ReferenceEquals(context, current)) throw new InvalidOperationException("Output context mismatch.");
            BeginFrame(current, current.DrainEvents());
        }
        internal void BeginFrame(SimulationContext current, BattleEvent[] events) {
            if (!ReferenceEquals(context, current)) throw new InvalidOperationException("Output context mismatch.");
            var player = viewer == Master.LeftPlayer ? economy.Left : viewer == Master.RightPlayer ? economy.Right : null;
            frame = new BattlePresentationFrame {
                Frame = current.FrameNumber, TickRate = current.TickRate,
                RemainingTime = (int)((durationMillis - (long)current.FrameNumber * 1000 / current.TickRate) / 1000),
                Mana = player?.Mana ?? 0, Cards = player?.DrawnCards.ToArray() ?? Array.Empty<long>(),
                LeftHp = current.Players.TryGetValue(Master.LeftPlayer, out var left) ? left.Hp : 0,
                RightHp = current.Players.TryGetValue(Master.RightPlayer, out var right) ? right.Hp : 0,
                Created = created.ToArray(), Updated = updated.ToArray(), Events = (BattleEvent[])events.Clone(), Objects = Array.Empty<ObjectSnapshot>()
            };
            created.Clear(); updated.Clear(); indices.Clear();
        }
        public void BuildSnapshot(SimulationContext current) {
            frame.IsSync = true;
            frame.Objects = current.Objects.Select(owner => new ObjectSnapshot(owner)).ToArray();
            frame.Cards = viewer.HasValue && current.Players.TryGetValue(viewer.Value, out var player) ? player.Cards.ToArray() : Array.Empty<long>();
        }
        public void EndFrame(SimulationContext current) { var ready = frame; frame = null; publish(ready); }
        public void Dispose() {
            if (context == null) return;
            context.Created -= OnCreated; context.Updated -= OnUpdated;
            context = null; created.Clear(); updated.Clear(); indices.Clear(); frame = null;
        }
    }
}
