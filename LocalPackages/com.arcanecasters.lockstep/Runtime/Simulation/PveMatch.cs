using System;
using System.Linq;

namespace ArcaneCasters.Simulation {
    public sealed class BattleResultMmr {
        public short LastLeftMmr { get; }
        public short LastRightMmr { get; }
        public short NewLeftMmr { get; }
        public short NewRightMmr { get; }
        public BattleResultMmr(short lastLeft = 0, short lastRight = 0, short newLeft = 0, short newRight = 0) {
            LastLeftMmr = lastLeft; LastRightMmr = lastRight; NewLeftMmr = newLeft; NewRightMmr = newRight;
        }
    }
    public sealed class BattleResultMessage {
        public string Type => "result";
        public string LeftPlayer { get; }
        public string RightPlayer { get; }
        public BattleResultMmr Mmr { get; }
        public BattleResultMessage(Master? loser, BattleResultMmr mmr) {
            LeftPlayer = !loser.HasValue ? "Draw" : loser == Master.LeftPlayer ? "Lose" : "Win";
            RightPlayer = !loser.HasValue ? "Draw" : loser == Master.RightPlayer ? "Lose" : "Win";
            Mmr = mmr ?? throw new ArgumentNullException(nameof(mmr));
        }
    }
    public sealed class PveScenarioRegistry {
        private readonly Func<long, PveScenario> source;
        public PveScenarioRegistry(Func<long, PveScenario> source) { this.source = source ?? throw new ArgumentNullException(nameof(source)); }
        public PveScenario GetScenario(long id) => source(id) ?? throw new ArgumentException("Unknown scenarioId: " + id);
    }
    public interface IPveSessionSink {
        long LeftUserId { get; }
        long RightUserId { get; }
        void SendObjective(long userId, PveObjectiveState state);
        void SendScriptEvent(long userId, PveScriptEvent message);
        void SendState(long userId, PveChannelState state);
    }
    public sealed class PveSync {
        private readonly PveMatch match;
        public PveSync(PveMatch match) { this.match = match ?? throw new ArgumentNullException(nameof(match)); }
        public void SendPveStateTo(SimulationContext context, long userId, int lastEventSeq) => match.SendPveStateTo(context,userId,lastEventSeq);
    }
    public sealed class PveMatch {
        public const long DefaultScenarioId = 1;
        public PveScenarioInstaller Installer { get; }
        public PveResultChecker ResultChecker { get; }
        public PveScriptSystem Script { get; }
        public PveObjectiveSystem Objectives { get; }
        public bool HasTimeLimit => false;
        public static BattleResultMmr GameEndMmr() => new BattleResultMmr();
        public static long[] UsersToMarkOnline(long leftUserId, long rightUserId) => new[] { leftUserId, rightUserId }.Where(id => id >= 0).ToArray();
        public PveMatch(PveScenarioRegistry registry, long? scenarioId, SimulationContext context, IPveSessionSink sink, PveScenarioComponents components) {
            var scenario = registry.GetScenario(scenarioId ?? DefaultScenarioId);
            Installer = new PveScenarioInstaller { Components = components }; Installer.Install(scenario.Installers, context);
            ResultChecker = new PveResultChecker(context.Result) { Runtime = Installer.Runtime };
            ResultChecker.SetObjectiveInstallerIds(scenario.ObjectiveInstallerIds); ResultChecker.ConfigureRules(scenario.Rules);
            Objectives = new PveObjectiveSystem(ResultChecker, sink.LeftUserId, sink.RightUserId, sink.SendObjective);
            Script = new PveScriptSystem { Scenario = scenario, Runtime = Installer.Runtime, SendScriptEvent = sink.SendScriptEvent };
            Script.Runner.Installer = Installer; Script.Runner.Components = components; Script.Runner.SendState = sink.SendState;
            Script.Runner.LeftUserId = sink.LeftUserId; Script.Runner.RightUserId = sink.RightUserId;
        }
        public void BeforeResultCheck(SimulationContext context, SessionType type) {
            if (type != SessionType.PVE) return;
            Script.Update(context); Objectives.Update(context);
        }
        public void SendPveStateTo(SimulationContext context, long userId, int lastEventSeq) {
            Objectives.SendCurrentTo(context, userId); Script.SendRecentEventsTo(context, userId, lastEventSeq); Script.SendStatesTo(userId);
        }
    }
}
