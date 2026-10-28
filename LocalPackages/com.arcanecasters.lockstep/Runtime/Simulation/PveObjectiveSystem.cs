using System;

namespace ArcaneCasters.Simulation {
    public sealed class PveObjectiveState {
        public string Type => "pveObjective";
        public readonly PveWinCondition WinCondition;
        public readonly int SurviveSeconds, RemainingSeconds, ObjectivesTotal, ObjectivesRemaining;
        public PveObjectiveState(PveWinCondition condition, int surviveSeconds, int remainingSeconds, int total, int remaining) {
            WinCondition = condition; SurviveSeconds = surviveSeconds; RemainingSeconds = remainingSeconds; ObjectivesTotal = total; ObjectivesRemaining = remaining;
        }
    }
    public sealed class PveObjectiveSystem {
        private readonly PveResultChecker checker;
        private readonly long leftUserId, rightUserId;
        private readonly Action<long, PveObjectiveState> send;
        private PveObjectiveState lastSent;
        public PveObjectiveSystem(PveResultChecker checker, long leftUserId, long rightUserId, Action<long, PveObjectiveState> send) {
            this.checker = checker; this.leftUserId = leftUserId; this.rightUserId = rightUserId; this.send = send;
        }
        public PveObjectiveState Build(SimulationContext context) {
            if (checker.WinCondition == PveWinCondition.Survive) {
                int remainingFrames = Math.Max(0, unchecked(checker.GetSurviveFrameThreshold(context.TickRate) - context.FrameNumber));
                int remainingSeconds = unchecked(remainingFrames + context.TickRate - 1) / context.TickRate;
                return new PveObjectiveState(checker.WinCondition, checker.SurviveSeconds, remainingSeconds, 0, 0);
            }
            return new PveObjectiveState(checker.WinCondition, 0, 0, checker.ObjectivesTotal, checker.CountObjectivesRemaining());
        }
        public void SendCurrentTo(SimulationContext context, long userId) { send(userId, Build(context)); }
        public void Update(SimulationContext context) {
            var next = Build(context);
            if (lastSent != null && lastSent.RemainingSeconds == next.RemainingSeconds && lastSent.ObjectivesRemaining == next.ObjectivesRemaining) return;
            send(leftUserId, next); send(rightUserId, next); lastSent = next;
        }
    }
}
