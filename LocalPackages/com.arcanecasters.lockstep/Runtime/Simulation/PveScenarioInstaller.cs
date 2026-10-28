using System;
using System.Collections.Generic;
using BEPUutilities;

namespace ArcaneCasters.Simulation {
    public sealed class PveInstallObject {
        public readonly string InstallerId, Prefab;
        public readonly Master Master;
        public readonly Vector3 Position;
        public readonly int? MaxHp;
        public PveInstallObject(string installerId, string prefab, Master master, Vector3 position, int? maxHp = null) {
            InstallerId = installerId; Prefab = prefab; Master = master; Position = position; MaxHp = maxHp;
        }
    }
    public sealed class PveRuntimeState {
        private readonly Dictionary<string, SimObject> installed = new Dictionary<string, SimObject>(StringComparer.Ordinal);
        public int GetInstalledObjectId(string installerId) => GetInstalledGameObject(installerId)?.Id ?? -1;
        public SimObject GetInstalledGameObject(string installerId) => installerId != null && installed.TryGetValue(installerId, out var owner) ? owner : null;
        public void Register(string installerId, SimObject owner) { installed[installerId] = owner; }
    }
    public sealed class PveScenarioInstaller {
        private readonly List<KeyValuePair<SimObject, int?>> pending = new List<KeyValuePair<SimObject, int?>>();
        public PveRuntimeState Runtime { get; private set; }
        public PveScenarioComponents Components { get; set; }
        public void Install(IEnumerable<PveInstallObject> installers, SimulationContext context) {
            Runtime = new PveRuntimeState();
            foreach (var item in installers) InstallOne(item, context);
        }
        public SimObject InstallOne(PveInstallObject item, SimulationContext context) {
            if (Runtime == null) Runtime = new PveRuntimeState();
            var owner = context.Spawn(item.Master, item.Prefab, item.Position);
            ApplyMaxHpOverride(owner, item.MaxHp); Runtime.Register(item.InstallerId, owner); return owner;
        }
        public SimObject GetInstalledObject(string installerId) => string.IsNullOrWhiteSpace(installerId) ? null : Runtime?.GetInstalledGameObject(installerId);
        public void ApplyMaxHpOverride(SimObject owner, int? maxHp) {
            pending.Add(new KeyValuePair<SimObject, int?>(owner, maxHp)); FinishPendingSetup();
        }
        public void FinishPendingSetup() {
            var batch = pending.ToArray(); pending.Clear();
            var waiting = new List<KeyValuePair<SimObject, int?>>();
            foreach (var item in batch) {
                var owner = item.Key;
                if (owner.IsDestroyed) continue;
                if (owner.Status == ObjectStatus.Initializing) { waiting.Add(item); continue; }
                Components?.RemoveLifetimeComponents(owner);
                if (item.Value.HasValue) FindHealthMob(owner)?.OverrideMaxHp(item.Value.Value);
            }
            pending.InsertRange(0, waiting);
        }
        public static IPveHealthMob FindHealthMob(SimObject owner) {
            IPveHealthMob fallback = null;
            foreach (var mob in owner.GetComponents<IPveHealthMob>()) {
                if (mob.IsObjectiveTarget) return mob;
                if (fallback == null && !mob.IsSpawner) fallback = mob;
            }
            return fallback;
        }
    }
}
