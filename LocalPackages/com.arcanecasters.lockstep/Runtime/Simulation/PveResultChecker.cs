using System.Collections.Generic;
using System.Linq;

namespace ArcaneCasters.Simulation {
    public enum PveWinCondition { DestroyObjectives, Survive }
    public sealed class PveScenarioRules {
        public readonly PveWinCondition WinCondition;
        public readonly int? SurviveSeconds;
        public PveScenarioRules(PveWinCondition winCondition = PveWinCondition.DestroyObjectives, int? surviveSeconds = null) { WinCondition = winCondition; SurviveSeconds = surviveSeconds; }
    }
    public sealed class PveResultChecker {
        private readonly BattleResult result;
        private string[] objectives = new string[0];
        private readonly HashSet<int> seen = new HashSet<int>();
        private bool cleared, failed;
        public PveRuntimeState Runtime { get; set; }
        public PveWinCondition WinCondition { get; private set; }
        public int SurviveSeconds { get; private set; }
        public Master? Loser => result.Loser;
        public bool IsEnd => result.Ended;
        public int ObjectivesTotal => objectives.Length;
        public PveResultChecker(BattleResult result = null) { this.result = result ?? new BattleResult(); }
        public void SetEnd() => result.SetEnd();
        public void SetLoser(Master loser) => result.SetLoser(loser);
        public void SetCleared() { cleared = true; SetLoser(Master.RightPlayer); }
        public void SetFailed() { failed = true; SetLoser(Master.LeftPlayer); }
        public void SetObjectiveInstallerIds(IEnumerable<string> ids) { objectives = ids.ToArray(); seen.Clear(); }
        public void ConfigureRules(PveScenarioRules rules = null) {
            rules = rules ?? new PveScenarioRules(); WinCondition = rules.WinCondition;
            SurviveSeconds = WinCondition == PveWinCondition.Survive ? rules.SurviveSeconds ?? 0 : 0;
        }
        public int GetSurviveFrameThreshold(int tickRate) => WinCondition == PveWinCondition.Survive ? unchecked(SurviveSeconds * tickRate) : int.MaxValue;
        public int CountObjectivesRemaining() {
            int remaining = 0;
            foreach (var id in objectives) {
                var owner = Runtime?.GetInstalledGameObject(id);
                if (owner == null) { remaining++; continue; }
                if (seen.Contains(owner.Id)) continue;
                if (PveObjectiveTarget.IsTerminal(owner)) { seen.Add(owner.Id); continue; }
                remaining++;
            }
            return remaining;
        }
        private bool AllTerminal() => objectives.Length > 0 && Runtime != null && CountObjectivesRemaining() == 0;
        public bool CheckResult(SimulationContext context) {
            if (!cleared && !failed && Loser == Master.LeftPlayer) SetFailed();
            if (!cleared && !failed) {
                if (WinCondition == PveWinCondition.Survive) { if (context.FrameNumber >= GetSurviveFrameThreshold(context.TickRate) || AllTerminal()) SetCleared(); }
                else if (AllTerminal()) SetCleared();
            }
            return cleared || failed;
        }
    }
}
