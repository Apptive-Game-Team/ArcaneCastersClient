using System;
using System.Collections.Generic;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum PveTriggerType { FrameNumGte, SecondsGte, InstallerHpPercentLte, InstallerDestroyed }
    public enum PveActionType { SpawnWave, InstallObject, SetSpawner, SetBgm, PullPlayerUnits }
    public sealed class PveScenarioAction {
        public PveActionType Type { get; }
        public string InstallerId { get; }
        public string Prefab { get; }
        public string BgmKey { get; }
        public int Count { get; }
        public int X { get; }
        public int Z { get; }
        public int? MaxHp { get; }
        public Fix64? Interval { get; }
        public PveScenarioAction(PveActionType type, string installerId = null, string prefab = null, int count = 0, int x = 0, int z = 0, int? maxHp = null, Fix64? interval = null, string bgmKey = null) {
            Type = type; InstallerId = installerId; Prefab = prefab; Count = count; X = x; Z = z; MaxHp = maxHp; Interval = interval; BgmKey = bgmKey;
        }
        public void Run(PveActionRunner runner, SimulationContext context) {
            switch (Type) {
                case PveActionType.SpawnWave: runner.SpawnWave(context, Prefab, Count, X, Z); break;
                case PveActionType.InstallObject: runner.InstallObject(context, InstallerId, Prefab, X, Z, MaxHp); break;
                case PveActionType.SetSpawner: runner.SetSpawner(InstallerId, Prefab, Count, Interval); break;
                case PveActionType.SetBgm: runner.SetBgm(BgmKey); break;
                case PveActionType.PullPlayerUnits: runner.PullPlayerUnits(context, InstallerId); break;
                default: throw new ArgumentOutOfRangeException(nameof(Type));
            }
        }
    }
    public sealed class PveScenarioEvent {
        public readonly string Id, TargetInstallerId, SpeakerInstallerId, Key;
        public readonly PveTriggerType TriggerType;
        public readonly int Value;
        public IReadOnlyList<string> Lines { get; }
        public IReadOnlyList<PveScenarioAction> Actions { get; }
        public PveScenarioEvent(string id, PveTriggerType triggerType, int value, string target = null, string speaker = null, string key = null, IEnumerable<string> lines = null, IEnumerable<PveScenarioAction> actions = null) {
            Id = id; TriggerType = triggerType; Value = value; TargetInstallerId = target; SpeakerInstallerId = speaker; Key = key;
            Lines = Array.AsReadOnly((lines ?? new string[0]).ToArray()); Actions = Array.AsReadOnly((actions ?? new PveScenarioAction[0]).ToArray());
        }
    }
    public sealed class PveShield {
        public readonly string InstallerId, SourceInstallerId;
        public PveShield(string installerId, string sourceInstallerId) { InstallerId = installerId; SourceInstallerId = sourceInstallerId; }
    }
    public sealed class PveScenario {
        public IReadOnlyList<string> ObjectiveInstallerIds { get; }
        public IReadOnlyList<PveInstallObject> Installers { get; }
        public IReadOnlyList<PveScenarioEvent> Events { get; }
        public IReadOnlyList<PveShield> Shields { get; }
        public PveScenarioRules Rules { get; }
        public PveScenario(IEnumerable<PveScenarioEvent> events, IEnumerable<string> objectives = null, IEnumerable<PveInstallObject> installers = null, PveScenarioRules rules = null, IEnumerable<PveShield> shields = null) {
            Events = Array.AsReadOnly(events.ToArray()); ObjectiveInstallerIds = Array.AsReadOnly((objectives ?? new string[0]).ToArray());
            Installers = Array.AsReadOnly((installers ?? new PveInstallObject[0]).ToArray()); Rules = rules ?? new PveScenarioRules(); Shields = Array.AsReadOnly((shields ?? new PveShield[0]).ToArray());
        }
    }
    public sealed class PveScriptEvent {
        public readonly string Key;
        public readonly int SpeakerObjectId, Seq;
        public IReadOnlyList<string> Lines { get; }
        public PveScriptEvent(string key, int speaker, IEnumerable<string> lines, int seq) { Key = key; SpeakerObjectId = speaker; Lines = Array.AsReadOnly(lines.ToArray()); Seq = seq; }
    }
    public sealed class PveScriptSystem {
        public PveScenario Scenario { get; set; }
        public PveRuntimeState Runtime { get; set; }
        public PveActionRunner Runner { get; } = new PveActionRunner();
        public Action<long, PveScriptEvent> SendScriptEvent { get; set; }
        public event Action<string, Exception> Failed;
        private readonly HashSet<string> fired = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<SimObject> shieldAttached = new HashSet<SimObject>();
        private readonly List<KeyValuePair<int, PveScriptEvent>> sent = new List<KeyValuePair<int, PveScriptEvent>>();
        private int lastSeq;
        private void Report(string id, Exception error) { try { Failed?.Invoke(id, error); } catch { } }
        private bool Satisfied(PveScenarioEvent spec, SimulationContext context) {
            switch (spec.TriggerType) {
                case PveTriggerType.FrameNumGte: return unchecked(context.FrameNumber * 20) >= unchecked(spec.Value * context.TickRate);
                case PveTriggerType.SecondsGte: return context.FrameNumber >= unchecked(spec.Value * context.TickRate);
                case PveTriggerType.InstallerDestroyed:
                    var target = Runner.Installer?.GetInstalledObject(spec.TargetInstallerId); return target != null && PveObjectiveTarget.IsTerminal(target);
                case PveTriggerType.InstallerHpPercentLte:
                    var owner = Runner.Installer?.GetInstalledObject(spec.TargetInstallerId); var mob = owner == null ? null : PveScenarioInstaller.FindHealthMob(owner);
                    if (mob == null || mob.MaxHp <= 0 || mob.Hp <= 0) return false;
                    return ((long)mob.Hp * 200 + mob.MaxHp) / ((long)mob.MaxHp * 2) <= spec.Value;
                default: throw new ArgumentOutOfRangeException(nameof(spec.TriggerType));
            }
        }
        private void AttachShields() {
            if (Runner.Components == null) return;
            foreach (var row in Scenario.Shields) {
                var target = Runner.Installer.GetInstalledObject(row.InstallerId);
                if (target == null || target.IsDestroyed || !shieldAttached.Add(target)) continue;
                var sources = Scenario.Shields.Where(item => item.InstallerId == row.InstallerId).Select(item => item.SourceInstallerId).ToArray();
                var runtime = Runner.Installer.Runtime;
                Runner.Components.AddShield(target, () => sources.Any(id => { var source = runtime?.GetInstalledGameObject(id); return source != null && !PveObjectiveTarget.IsTerminal(source); }));
            }
        }
        private void Prune(SimulationContext context) { sent.RemoveAll(item => unchecked(context.FrameNumber - item.Key) > unchecked(10 * context.TickRate)); }
        public void SendRecentEventsTo(SimulationContext context, long userId, int lastEventSeq) {
            Prune(context); if (SendScriptEvent == null) return;
            foreach (var item in sent) if (item.Value.Seq > lastEventSeq) SendScriptEvent(userId, item.Value);
        }
        public void SendStatesTo(long userId) => Runner.SendStatesTo(userId);
        private void SendDialogue(PveScenarioEvent spec, SimulationContext context) {
            if (spec.Lines.Count == 0) return;
            var message = new PveScriptEvent(spec.Key ?? "", Runtime?.GetInstalledObjectId(spec.SpeakerInstallerId) ?? -1, spec.Lines, unchecked(++lastSeq));
            Prune(context); sent.Add(new KeyValuePair<int, PveScriptEvent>(context.FrameNumber, message));
            SendScriptEvent?.Invoke(Runner.LeftUserId, message); SendScriptEvent?.Invoke(Runner.RightUserId, message);
        }
        public void Update(SimulationContext context) {
            if (Scenario == null) return;
            if (Runner.Installer != null) { Runner.Installer.FinishPendingSetup(); AttachShields(); }
            foreach (var spec in Scenario.Events) {
                if (fired.Contains(spec.Id)) continue;
                bool satisfied;
                try { satisfied = Satisfied(spec, context); } catch (Exception error) { fired.Add(spec.Id); Report(spec.Id, error); continue; }
                if (!satisfied) continue;
                fired.Add(spec.Id);
                try { SendDialogue(spec, context); } catch (Exception error) { Report(spec.Id, error); }
                foreach (var action in spec.Actions) { try { action.Run(Runner, context); } catch (Exception error) { Report(spec.Id, error); } }
            }
        }
    }
}
