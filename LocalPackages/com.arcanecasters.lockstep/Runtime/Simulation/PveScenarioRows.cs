using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class PveInstallerRow {
        public string InstallerId, PrefabType, Master;
        public int PositionX, PositionY, PositionZ;
        public int? MaxHp;
    }
    public sealed class PveActionRow {
        public string ActionType, InstallerId, PrefabType, BgmKey;
        public int? Count, PositionX, PositionZ, MaxHp;
        public Fix64? IntervalSeconds;
    }
    public sealed class PveEventRow {
        public string EventId, TriggerType, TargetInstallerId, SpeakerInstallerId, MessageKey;
        public int TriggerValue;
        public List<string> Lines = new List<string>();
        public List<PveActionRow> Actions = new List<PveActionRow>();
    }
    public sealed class PveRulesRow { public string WinCondition; public int? SurviveSeconds; }
    public sealed class PveShieldRow { public string InstallerId, SourceInstallerId; }
    public sealed class PveScenarioRows {
        public List<string> Objectives = new List<string>();
        public List<PveInstallerRow> Installers = new List<PveInstallerRow>();
        public List<PveEventRow> Events = new List<PveEventRow>();
        public List<PveShieldRow> Shields = new List<PveShieldRow>();
        public PveRulesRow Rules;
        public PveScenario Build() {
            var rules = Rules == null ? new PveScenarioRules() : new PveScenarioRules(ParseEnum<PveWinCondition>(Rules.WinCondition), Rules.SurviveSeconds);
            if (Installers.Count == 0 && Objectives.Count == 0 && rules.WinCondition != PveWinCondition.Survive) return null;
            var installers = Installers.Select(row => new PveInstallObject(row.InstallerId, ParsePrefab(row.PrefabType), ParseEnum<Master>(row.Master), new Vector3(row.PositionX, row.PositionY, row.PositionZ), row.MaxHp)).ToArray();
            var events = Events.Select(row => new PveScenarioEvent(row.EventId, ParseEnum<PveTriggerType>(row.TriggerType), row.TriggerValue, row.TargetInstallerId, row.SpeakerInstallerId, row.MessageKey, row.Lines, row.Actions.Select(ConvertAction))).ToArray();
            return new PveScenario(events, Objectives, installers, rules, Shields.Select(row => new PveShield(row.InstallerId, row.SourceInstallerId)));
        }
        private static T ParseEnum<T>(string value) where T : struct {
            if (Enum.GetNames(typeof(T)).Contains(value)) return (T)Enum.Parse(typeof(T), value);
            throw new ArgumentException("No enum constant " + typeof(T).Name + "." + value);
        }
        private static string ParsePrefab(string value) {
            if (PrefabCatalog.Names.Contains(value)) return value;
            throw new ArgumentException("No enum constant PrefabType." + value);
        }
        private static ArgumentException Missing(PveActionRow row, string column) => new ArgumentException(row.ActionType + " action is missing column " + column);
        private static int Required(int? value, PveActionRow row, string column) => value ?? throw Missing(row, column);
        private static string Required(string value, PveActionRow row, string column) => value ?? throw Missing(row, column);
        public static PveScenarioAction ConvertAction(PveActionRow row) {
            switch (row.ActionType) {
                case "SpawnWave": {
                    var prefab = ParsePrefab(Required(row.PrefabType, row, "prefab_type"));
                    return new PveScenarioAction(PveActionType.SpawnWave, prefab: prefab, count: Required(row.Count, row, "count"), x: Required(row.PositionX, row, "position_x"), z: Required(row.PositionZ, row, "position_z"));
                }
                case "InstallObject": {
                    var id = Required(row.InstallerId, row, "installer_id"); var prefab = ParsePrefab(Required(row.PrefabType, row, "prefab_type"));
                    return new PveScenarioAction(PveActionType.InstallObject, id, prefab, x: Required(row.PositionX, row, "position_x"), z: Required(row.PositionZ, row, "position_z"), maxHp: row.MaxHp);
                }
                case "SetSpawner": {
                    var id = Required(row.InstallerId, row, "installer_id"); int count = Required(row.Count, row, "count");
                    return new PveScenarioAction(PveActionType.SetSpawner, id, row.PrefabType == null ? null : ParsePrefab(row.PrefabType), count, interval: row.IntervalSeconds);
                }
                case "PullPlayerUnits": return new PveScenarioAction(PveActionType.PullPlayerUnits, row.InstallerId);
                case "PlayBgm": return new PveScenarioAction(PveActionType.SetBgm, bgmKey: row.BgmKey);
                default: throw new ArgumentException("Unknown action_type: " + row.ActionType);
            }
        }
    }
}
