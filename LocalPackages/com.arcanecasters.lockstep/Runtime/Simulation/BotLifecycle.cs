using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IBotControl {
        void NewBotAgent(SimulationContext context, Master side, long participantId, Fix64 opponentNoviceProgress);
        void RemoveBotAgent(Master side);
        bool HasBotAgent(Master side);
    }
    public sealed class BotLifecycle {
        private readonly SimulationContext context;
        private readonly IBotControl control;
        private readonly Func<long, Fix64> noviceProgress;
        public BotLifecycle(SimulationContext context, IBotControl control, Func<long, Fix64> noviceProgress) {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.control = control; this.noviceProgress = noviceProgress ?? throw new ArgumentNullException(nameof(noviceProgress));
        }
        private void NewAgent(Master side, long participantId) {
            if (control == null) return;
            var opponent = side == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer;
            control.NewBotAgent(context, side, participantId, noviceProgress(context.UserIds[opponent]));
        }
        public void InitializePractice() {
            foreach (var side in new[] { Master.LeftPlayer, Master.RightPlayer })
                if (context.UserIds.TryGetValue(side, out var id) && id < 0) NewAgent(side, id);
        }
        public bool HasBotAgent(Master side) => control != null && control.HasBotAgent(side);
        private Master? Side(long userId) {
            foreach (var side in new[] { Master.LeftPlayer, Master.RightPlayer })
                if (context.UserIds.TryGetValue(side, out var id) && id == userId) return side;
            return null;
        }
        // 확정된 연결 상태 입력을 소비하는 프레임에서만 호출한다.
        public void ActivateForUser(long userId) {
            var side = Side(userId); if (!side.HasValue || HasBotAgent(side.Value)) return;
            NewAgent(side.Value, context.UserIds[side.Value]);
        }
        public void DeactivateForUser(long userId) {
            var side = Side(userId);
            if (!side.HasValue || context.UserIds[side.Value] < 0 || !HasBotAgent(side.Value)) return;
            control.RemoveBotAgent(side.Value);
        }
    }
    public sealed class FeverTimeSystem : IGameSystem {
        private readonly Func<bool> isFeverTime;
        private bool started;
        public FeverTimeSystem(Func<bool> isFeverTime) { this.isFeverTime = isFeverTime ?? throw new ArgumentNullException(nameof(isFeverTime)); }
        public void Update(SimulationContext context) {
            if (started || !isFeverTime()) return;
            foreach (var side in new[] { Master.LeftPlayer, Master.RightPlayer })
                if (context.CardDecks.TryGetValue(side, out var deck)) deck.Fever();
            context.Players[Master.LeftPlayer].ManaCharger.UpdateManaCharge(Fix64.One);
            context.Players[Master.RightPlayer].ManaCharger.UpdateManaCharge(Fix64.One);
            started = true;
        }
    }
}
