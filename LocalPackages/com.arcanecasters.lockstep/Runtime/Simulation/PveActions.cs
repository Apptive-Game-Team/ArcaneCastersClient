using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class PulledToPoint : SimComponent {
        private readonly Vector3 destination;
        private readonly Fix64 duration;
        private Vector3 start;
        private Fix64 elapsed;
        public PulledToPoint(SimObject owner, Vector3 destination) : this(owner, destination, (Fix64)1.5m) { }
        public PulledToPoint(SimObject owner, Vector3 destination, Fix64 duration) : base(owner) { this.destination = destination; this.duration = duration; }
        public override void Start() { start = Owner.Position; }
        public override void Update() {
            if (Owner.IsDestroyed) return;
            elapsed += Context.DeltaTime;
            var progress = MathHelper.Min(elapsed / duration, Fix64.One);
            Owner.SetPosition(new Vector3(start.X + (destination.X - start.X) * progress, Owner.Position.Y, start.Z + (destination.Z - start.Z) * progress));
            Owner.ApplyUpdate(); if (progress >= Fix64.One) Owner.Destroy();
        }
    }
    public sealed class PveActionRunner {
        public PveScenarioInstaller Installer { get; set; }
        public PveScenarioComponents Components { get; set; }
        public PveStateStore States { get; } = new PveStateStore();
        public Action<long, PveChannelState> SendState { get; set; }
        public long LeftUserId { get; set; }
        public long RightUserId { get; set; }
        public void SpawnWave(SimulationContext context, string prefab, int count, int x, int z) {
            count = Math.Max(1, count); Fix64 center = (Fix64)(count - 1) / 2;
            for (int i = 0; i < count; i++) context.Spawn(Master.RightPlayer, prefab, new Vector3(x + ((Fix64)i - center) / 2, 0, z));
        }
        public void InstallObject(SimulationContext context, string id, string prefab, int x, int z, int? maxHp) {
            Installer?.InstallOne(new PveInstallObject(id, prefab, Master.RightPlayer, new Vector3(x, 0, z), maxHp), context);
        }
        public void SetSpawner(string id, string prefab, int count, Fix64? interval = null) {
            var target = Installer?.GetInstalledObject(id); if (target == null || Components == null) return;
            Components.RemoveSpawner(target); if (count > 0) Components.AddSpawner(target, prefab, interval ?? (Fix64)2, count);
        }
        public void SetBgm(string key) {
            if (SendState == null) return;
            var changed = States.Set(PveStateStore.Bgm, key); if (changed == null) return;
            SendState(LeftUserId, changed); SendState(RightUserId, changed);
        }
        public void SendStatesTo(long userId) { if (SendState != null) foreach (var state in States.Snapshot()) SendState(userId, state); }
        public void PullPlayerUnits(SimulationContext context, string id) {
            var destination = Installer?.GetInstalledObject(id);
            if (destination != null && destination.IsDestroyed) destination = null;
            foreach (var unit in context.Objects) {
                if (!unit.IsActive || unit.Master != Master.LeftPlayer || unit.IsDying || PriorityEnemyDetector.CategoryOf(unit) != TargetCategory.Unit) continue;
                if (destination == null) { unit.Destroy(); continue; }
                foreach (var mob in unit.GetComponents<MobComponent>()) unit.RemoveComponent(mob);
                unit.AddComponent(new PulledToPoint(unit, destination.Position));
            }
        }
    }
}
