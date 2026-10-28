using System;
using System.IO;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    [Serializable] public sealed class BattlePveInstaller {
        public string installerId, prefabType, master;
        public int positionX, positionY, positionZ;
        public int? maxHp;
    }
    [Serializable] public sealed class BattlePveAction {
        public string actionType, installerId, prefabType, bgmKey;
        public int? count, positionX, positionZ, maxHp;
        public long? intervalRaw;
        internal PveActionRow ToRow() => new PveActionRow {
            ActionType = actionType, InstallerId = installerId, PrefabType = prefabType, BgmKey = bgmKey, Count = count,
            PositionX = positionX, PositionZ = positionZ, MaxHp = maxHp, IntervalSeconds = intervalRaw.HasValue ? Fix64.FromRaw(intervalRaw.Value) : (Fix64?)null
        };
    }
    [Serializable] public sealed class BattlePveEvent {
        public string eventId, triggerType, targetInstallerId, speakerInstallerId, messageKey;
        public int triggerValue;
        public string[] lines;
        public BattlePveAction[] actions;
    }
    [Serializable] public sealed class BattlePveShield { public string installerId, sourceInstallerId; }
    [Serializable] public sealed class BattlePveRules { public string winCondition; public int? surviveSeconds; }
    [Serializable] public sealed class BattlePveSnapshot {
        public long scenarioId;
        public string[] objectives;
        public BattlePveInstaller[] installers;
        public BattlePveEvent[] events;
        public BattlePveShield[] shields;
        public BattlePveRules rules;
        public PveScenario Build() {
            if (objectives == null || installers == null || events == null || shields == null
                || objectives.Any(id => id == null) || installers.Any(row => row == null) || shields.Any(row => row == null)
                || events.Any(row => row == null || row.lines == null || row.actions == null || row.actions.Any(action => action == null)))
                throw new InvalidDataException("Invalid PVE scenario snapshot.");
            var rows = new PveScenarioRows {
                Objectives = objectives.ToList(),
                Installers = installers.Select(row => new PveInstallerRow { InstallerId = row.installerId, PrefabType = row.prefabType, Master = row.master, PositionX = row.positionX, PositionY = row.positionY, PositionZ = row.positionZ, MaxHp = row.maxHp }).ToList(),
                Events = events.Select(row => new PveEventRow { EventId = row.eventId, TriggerType = row.triggerType, TriggerValue = row.triggerValue, TargetInstallerId = row.targetInstallerId, SpeakerInstallerId = row.speakerInstallerId, MessageKey = row.messageKey, Lines = row.lines.ToList(), Actions = row.actions.Select(action => action.ToRow()).ToList() }).ToList(),
                Shields = shields.Select(row => new PveShieldRow { InstallerId = row.installerId, SourceInstallerId = row.sourceInstallerId }).ToList(),
                Rules = rules == null ? null : new PveRulesRow { WinCondition = rules.winCondition, SurviveSeconds = rules.surviveSeconds }
            };
            return rows.Build() ?? throw new InvalidDataException("PVE scenario snapshot has no scenario.");
        }
    }
}
