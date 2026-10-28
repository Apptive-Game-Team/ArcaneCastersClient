using System;
using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IMindControlImmune { }
    public sealed class MindControlShot : SimComponent, ICollidable {
        private readonly Fix64 speed;
        private Vector3? direction;
        public MindControlShot(SimObject owner, Fix64 speed) : base(owner) { this.speed = speed; }
        public void SetTarget(Vector3 target) { var offset = target - Owner.Position; direction = offset.LengthSquared() == Fix64.Zero ? Vector3.Zero : Vector3.Normalize(offset); }
        public override void Update() { if (direction.HasValue) Owner.SetPosition(Owner.Position + direction.Value * (speed * Context.DeltaTime)); }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) {
            if (ReferenceEquals(other, Owner) || other.Master == Owner.Master || other.Master == Master.None || !other.HasComponent<MobComponent>() || other.HasComponent<IMindControlImmune>()) return;
            other.SetMaster(other.Master == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer); other.SetStatus(ObjectStatus.Idle); Owner.Destroy();
        }
    }
    public interface IHitTracker { bool MarkIfFirstHit(int id); }
    public sealed class VineHitTracker : IHitTracker {
        private readonly HashSet<int> hits = new HashSet<int>();
        public bool MarkIfFirstHit(int id) => hits.Add(id);
        public int HitCount => hits.Count;
    }
    public static class VineSpawnContext {
        public static VineHitTracker CurrentTracker(SimulationContext context) => context.ActiveVineTracker;
        public static T RunWithTracker<T>(SimulationContext context, VineHitTracker tracker, Func<T> action) {
            var previous = context.ActiveVineTracker; context.ActiveVineTracker = tracker;
            try { return action(); } finally { context.ActiveVineTracker = previous; }
        }
    }
    public sealed class OnStartAttacker : SimComponent {
        private readonly Fix64 range;
        private readonly int damage;
        private readonly IHitTracker tracker;
        public OnStartAttacker(SimObject owner, Fix64 range, int damage, IHitTracker tracker = null) : base(owner) { this.range = range; this.damage = damage; this.tracker = tracker; }
        public override void Start() {
            var hit = new Hit(damage, Owner.Id, Owner.Elements.Total);
            foreach (var target in Context.Objects) {
                if (ReferenceEquals(target, Owner) || !target.IsActive || !target.HasComponent<MobComponent>() || Owner.Master != Master.None && target.Master == Owner.Master) continue;
                if (!CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero, range)) continue;
                if (tracker != null && !tracker.MarkIfFirstHit(target.Id)) continue;
                target.GetComponent<MobComponent>().OnDamaged(hit);
            }
        }
    }
    public sealed class SequentialLineSpawner : SimComponent {
        private readonly Master master;
        private readonly string prefab;
        private readonly Vector3 start, direction;
        private readonly Fix64 spacing, interval;
        private readonly int total, startFrame;
        private readonly VineHitTracker tracker;
        private int nextIndex = 1;
        public SequentialLineSpawner(SimObject owner, Master master, string prefab, Vector3 start, Vector3 direction, Fix64 spacing, Fix64 interval, int total, VineHitTracker tracker) : base(owner) {
            this.master = master; this.prefab = prefab; this.start = start; this.direction = direction.LengthSquared() == Fix64.Zero ? Vector3.Zero : Vector3.Normalize(direction);
            this.spacing = spacing; this.interval = interval; this.total = total; this.tracker = tracker; startFrame = Context.FrameNumber;
        }
        private SequentialLineSpawner(SimObject owner, SequentialLineSpawner previous) : base(owner) {
            master = previous.master; prefab = previous.prefab; start = previous.start; direction = previous.direction; spacing = previous.spacing; interval = previous.interval;
            total = previous.total; tracker = previous.tracker; startFrame = previous.startFrame; nextIndex = previous.nextIndex;
        }
        public override void Update() {
            Fix64 elapsed = (Context.FrameNumber - startFrame) * Context.DeltaTime; SimObject last = null;
            while (nextIndex < total && elapsed >= interval * nextIndex) {
                var position = start + direction * (spacing * nextIndex);
                last = VineSpawnContext.RunWithTracker(Context, tracker, () => Context.Spawn(master, prefab, position)); nextIndex++;
            }
            if (nextIndex >= total) { Owner.RemoveComponent(this); return; }
            if (last != null && !last.IsDestroyed) { last.AddComponent(new SequentialLineSpawner(last, this)); Owner.RemoveComponent(this); }
        }
    }
    public static class VineLine {
        public static void Spawn(SimulationContext context, Master master, Vector3 origin, Vector3 direction) {
            int count = (int)(context.GameParameters["vine_toss.vine_count"].RawValue / (1L << 32));
            Fix64 spacing = context.GameParameters["vine_toss.vine_spacing"], interval = context.GameParameters["vine_toss.vine_spawn_interval"];
            var tracker = new VineHitTracker(); var position = origin + direction * spacing;
            var first = VineSpawnContext.RunWithTracker(context, tracker, () => context.Spawn(master, "Vine", position));
            first.AddComponent(new SequentialLineSpawner(first, master, "Vine", position, direction, spacing, interval, count, tracker));
        }
    }
    public sealed class VineToss : Shot {
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("VineToss", owner => {
                owner.Elements.AddNative(Element.Nature);
                owner.AddComponentImmediately(new VineToss(owner));
            });
        }
        public VineToss(SimObject owner) : base(owner, 0, Fix64.Zero) { }
        public override void SetTarget(Vector3 target) {
            var offset = target - Owner.Position;
            var direction = VineMagics.Normalize(offset);
            if (direction != Vector3.Zero) VineLine.Spawn(Context, Owner.Master, Owner.Position, direction);
            Owner.Destroy();
        }
    }
}
