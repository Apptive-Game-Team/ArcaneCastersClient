using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class FireworkLauncher : SimComponent {
        private sealed class Shot { public Vector3 Position; public Fix64 Remaining; }
        private readonly List<Shot> shots = new List<Shot>();
        private readonly Fix64 interval, offset;
        private Fix64 elapsed;
        public FireworkLauncher(SimObject owner, Fix64 interval, Fix64 offset) : base(owner) { this.interval = MathHelper.Max((Fix64)0.05m, interval); this.offset = offset; }
        public override void Update() {
            var landed = new List<Vector3>();
            foreach (var shot in shots) { shot.Remaining -= Context.DeltaTime; if (shot.Remaining <= 0) landed.Add(shot.Position); }
            shots.RemoveAll(shot => shot.Remaining <= 0);
            foreach (var position in landed) Context.Spawn(Owner.Master, "FireworkShell", position);
            elapsed += Context.DeltaTime;
            while (elapsed >= interval) {
                elapsed -= interval; if (Owner.Master == Master.None) continue;
                var start = Owner.Position; var end = start + (Owner.Master == Master.LeftPlayer ? Vector3.Right : Vector3.Left) * offset;
                var duration = MathHelper.Max((Fix64)0.05m, offset / 6);
                Context.AddEvent(new BattleEvent("FireworkShell", start, end, duration)); shots.Add(new Shot { Position = end, Remaining = duration });
            }
        }
    }
    public static class FireworkPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("FireworkTower", owner => {
                var p = context.GameParameters;
                owner.AddComponentImmediately(new RigidBody(owner, (int)(p["firework_tower.mass"].RawValue / (1L << 32)))); owner.AddCollider(new CircleCollider(owner, p["firework_tower.radius"]));
                owner.AddComponent(new DummyMob(owner, (int)(p["firework_tower.hp"].RawValue / (1L << 32))));
                owner.AddComponent(new FireworkLauncher(owner, p["firework_tower.attack_interval"], p["firework_tower.attack_offset"])); owner.AddComponent(new TimedSelfDestroyer(owner, p["firework_tower.duration"]));
                owner.AddComponentImmediately(new BuildingEffectReceiver(owner)); owner.Elements.AddNative(Element.Fire);
            });
            context.RegisterPrefab("FireworkShell", owner => {
                var p = context.GameParameters; var radius = p["firework_shell.radius"];
                owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Fire);
                owner.AddComponent(new TimedSelfDestroyer(owner, p["firework_shell.duration"])); owner.AddComponent(new OnStartAttacker(owner, radius, (int)(p["firework_shell.damage"].RawValue / (1L << 32))));
            });
        }
    }
}
