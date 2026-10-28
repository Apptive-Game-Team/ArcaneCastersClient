using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public static class SimulationQueries {
        public static SimObject[] OverlapBoxAll(SimulationContext context, Vector3 center, Vector3 size) {
            Vector3 half = size / 2, min = center - half, max = center + half;
            return context.Objects.Where(owner => owner.Position.X >= min.X && owner.Position.X <= max.X && owner.Position.Y >= min.Y && owner.Position.Y <= max.Y && owner.Position.Z >= min.Z && owner.Position.Z <= max.Z).ToArray();
        }
        public static SimObject[] OverlapSphereAll(SimulationContext context, Vector3 center, Fix64 radius) {
            var result = new List<SimObject>();
            foreach (var owner in context.Objects) {
                // 원본 질의는 상태나 trigger로 거르지 않고 첫 충돌체만 확인한다.
                var collider = owner.Colliders.FirstOrDefault();
                if (collider is CircleCollider circle && Vector3.Distance(center, circle.Position) < radius + circle.Radius
                    || collider is EdgeCollider edge && edge.OverlapsSphere(center, radius)) result.Add(owner);
            }
            return result.ToArray();
        }
    }
    public interface IShotTarget { void SetTarget(Vector3 target); }
    public interface IDirectionalShot { Vector3? Direction { get; } }
    public class Shot : SimComponent, ICollidable, IShotTarget, IDirectionalShot {
        protected readonly int damage;
        protected readonly Fix64 speed;
        private bool exploded;
        public Vector3? Direction { get; protected set; }
        public Shot(SimObject owner, int damage, Fix64 speed) : base(owner) { this.damage = damage; this.speed = speed; }
        public virtual void SetTarget(Vector3 target) {
            var offset = target - Owner.Position; var length = offset.Length();
            // 역수를 먼저 양자화하면 축 방향도 1보다 짧아져 사거리 경계에 늦게 도달한다.
            Direction = length == Fix64.Zero ? Vector3.Zero : new Vector3(offset.X / length, offset.Y / length, offset.Z / length);
        }
        public override void Update() { if (Direction.HasValue) Owner.SetPosition(Owner.Position + Direction.Value * (speed * Context.DeltaTime)); }
        public virtual void OnCollision(SimObject other) { }
        public virtual void OnCollisionWithEnemy(SimObject other) {
            if (exploded || !other.HasComponent<IDamageable>()) return;
            exploded = true; Direction = Vector3.Zero; Owner.SetStatus(ObjectStatus.Attack);
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, 1)) {
                if (ReferenceEquals(target, Owner) || target.Master == Owner.Master) continue;
                var damageables = target.GetComponents<IDamageable>(); if (damageables.Length == 0) continue;
                target.SetStatus(ObjectStatus.Damaged);
                foreach (var receiver in damageables) receiver.OnDamaged(new Hit(damage, Owner.Id, Owner.Elements.Total));
            }
            Owner.Destroy();
        }
    }
    public sealed class PushShot : Shot {
        private readonly HashSet<int> damaged = new HashSet<int>();
        public PushShot(SimObject owner, int damage, Fix64 speed) : base(owner, damage, speed) { }
        public override void OnCollisionWithEnemy(SimObject other) {
            if (ReferenceEquals(other, Owner) || other.Master == Owner.Master || !other.HasComponent<IDamageable>()) return;
            if (Direction.HasValue) TimedMassPush.Apply(other, Owner, Direction, speed, (Fix64)0.5m);
            if (!damaged.Add(other.Id)) return;
            other.SetStatus(ObjectStatus.Damaged);
            foreach (var receiver in other.GetComponents<IDamageable>()) receiver.OnDamaged(new Hit(damage, Owner.Id, Owner.Elements.Total));
        }
    }
    public sealed class TimedMassPush : SimComponent {
        private readonly SimObject source;
        public Vector3 Direction { get; private set; }
        private Fix64 speed, remaining;
        private TimedMassPush(SimObject owner, SimObject source, Vector3 direction, Fix64 speed, Fix64 duration) : base(owner) { this.source = source; Refresh(direction, speed, duration); }
        public static void Apply(SimObject target, SimObject source, Vector3? direction, Fix64 speed, Fix64 duration) {
            if (target == null || source == null || !direction.HasValue || speed <= Fix64.Zero || duration <= Fix64.Zero) return;
            var body = target.GetComponent<RigidBody>(); if (body == null || ForcedMovement.MassMultiplier(body.Mass) == Fix64.Zero) return;
            var existing = target.GetComponents<TimedMassPush>().Concat(target.GetPendingComponents<TimedMassPush>()).FirstOrDefault(push => ReferenceEquals(push.source, source));
            if (existing != null) existing.Refresh(direction.Value, speed, duration); else target.AddComponent(new TimedMassPush(target, source, direction.Value, speed, duration));
        }
        private void Refresh(Vector3 direction, Fix64 speed, Fix64 duration) { Direction = direction.LengthSquared() == Fix64.Zero ? Vector3.Zero : Vector3.Normalize(direction); this.speed = speed; remaining = duration; }
        public override void Update() {
            Fix64 dt = Context.DeltaTime; var body = Owner.GetComponent<RigidBody>();
            if (dt <= Fix64.Zero || remaining <= Fix64.Zero || body == null) { Owner.RemoveComponent(this); return; }
            Fix64 fraction = MathHelper.Min(remaining / dt, Fix64.One);
            body.AddVelocity(Direction * (speed * ForcedMovement.MassMultiplier(body.Mass) * fraction)); remaining -= dt;
            if (remaining <= Fix64.Zero) Owner.RemoveComponent(this);
        }
    }
    public class ProjectileRangeAttackMob : BehaviorMob {
        public ProjectileRangeAttackMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range, string projectile, Fix64 duration)
            : base(owner, hp, speed, mask, interval, range, AttackBehavior(owner, damage, projectile, duration)) { }
        public static System.Func<SimObject, bool> AttackBehavior(SimObject owner, int damage, string projectile, Fix64 duration) => target => {
            owner.Context.AddEvent(new BattleEvent(projectile, owner.Id, target.Id, duration));
            var receiver = target.GetComponent<IDamageable>();
            if (receiver != null) { receiver.OnDamaged(new Hit(damage, owner.Id, owner.Elements.Total)); owner.SetStatus(ObjectStatus.Attack); }
            return true;
        };
    }
    public class EffectProvideProjectileRangeAttackMob : ProjectileRangeAttackMob {
        public EffectProvideProjectileRangeAttackMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range, VisualEffect effect, string projectile, Fix64 duration)
            : base(owner, hp, speed, mask, damage, interval, range, projectile, duration) {
            var attack = Behavior; Behavior = target => { attack(target); target.GetComponent<IEffectReceiver>()?.Receive(new EffectApplication(effect, owner)); return true; };
        }
    }
}
