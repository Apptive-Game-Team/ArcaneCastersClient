using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class WindBladeShot : Shot {
        private readonly HashSet<int> pierced = new HashSet<int>();
        private int currentDamage;
        public WindBladeShot(SimObject owner, int damage, Fix64 speed) : base(owner, damage, speed) { currentDamage = damage; }
        public override void OnCollisionWithEnemy(SimObject other) {
            if (ReferenceEquals(other, Owner) || pierced.Contains(other.Id) || Owner.Master != Master.None && other.Master == Owner.Master || !other.HasComponent<IDamageable>()) return;
            pierced.Add(other.Id); other.SetStatus(ObjectStatus.Damaged);
            foreach (var receiver in other.GetComponents<IDamageable>()) receiver.OnDamaged(new Hit(currentDamage, Owner.Id, Owner.Elements.Total));
            currentDamage = unchecked(currentDamage * 2) / 3;
        }
    }
    public sealed class ChainShot : Shot {
        private int chainDamage, chainCount;
        private readonly Fix64 radius;
        private readonly int minimum;
        private Vector3? chainDirection;
        private readonly HashSet<int> hits = new HashSet<int>();
        private bool active = true;
        private Fix64 timer;
        public ChainShot(SimObject owner, int damage, Fix64 speed, Fix64 radius, int minimum) : base(owner, damage, speed) { chainDamage = damage; this.radius = radius; this.minimum = minimum; }
        // 원본의 숨겨진 direction 필드를 유지한다. 상속된 Direction은 null이다.
        public override void SetTarget(Vector3 target) {
            var offset = target - Owner.Position; var length = offset.Length();
            chainDirection = length == Fix64.Zero ? Vector3.Zero : new Vector3(offset.X / length, offset.Y / length, offset.Z / length);
        }
        public override void OnCollisionWithEnemy(SimObject other) {
            if (!active || other.Master == Owner.Master || !other.HasComponent<IDamageable>() || hits.Contains(other.Id)) return;
            other.SetStatus(ObjectStatus.Damaged);
            foreach (var receiver in other.GetComponents<IDamageable>()) receiver.OnDamaged(new Hit(chainDamage, Owner.Id, Owner.Elements.Total));
            chainDamage = System.Math.Max(chainDamage / 2, minimum); chainCount++; active = false;
            if (chainCount >= 5) Owner.Destroy(); hits.Add(other.Id);
            var next = SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius).FirstOrDefault(target => !ReferenceEquals(target, Owner) && target.Master != Owner.Master && !hits.Contains(target.Id) && target.HasComponent<IDamageable>());
            if (next == null) { Owner.Destroy(); return; }
            Owner.SetPosition(other.Position); SetTarget(next.Position);
        }
        public override void Update() {
            if (!active) { if (timer >= (Fix64)0.12m) { active = true; timer = Fix64.Zero; return; } timer += Context.DeltaTime; return; }
            if (chainDirection.HasValue && chainDirection.Value != Vector3.Zero) Owner.SetPosition(Owner.Position + chainDirection.Value * (speed * Context.DeltaTime));
        }
    }
    public sealed class BoulderStrikeShot : Shot {
        private readonly int collisionDamage;
        private readonly Fix64 knockbackSpeed, knockbackDuration;
        private bool hit;
        public BoulderStrikeShot(SimObject owner, int damage, Fix64 speed, int collisionDamage, Fix64 knockbackSpeed, Fix64 knockbackDuration) : base(owner, damage, speed) { this.collisionDamage = collisionDamage; this.knockbackSpeed = knockbackSpeed; this.knockbackDuration = knockbackDuration; }
        public override void OnCollisionWithEnemy(SimObject other) {
            if (hit || ReferenceEquals(other, Owner) || !other.HasComponent<IDamageable>()) return;
            hit = true; other.SetStatus(ObjectStatus.Damaged);
            foreach (var receiver in other.GetComponents<IDamageable>()) receiver.OnDamaged(new Hit(damage, Owner.Id, Owner.Elements.Total));
            if (Direction.HasValue) BoulderStrikeKnockback.Apply(other, Owner, Direction.Value, knockbackSpeed, knockbackDuration, collisionDamage);
            Owner.SetStatus(ObjectStatus.Attack); Owner.Destroy();
        }
    }
    public sealed class BoulderStrikeKnockback : SimComponent, ICollidable {
        private SimObject source;
        private Vector3 direction;
        private Fix64 speed, remaining;
        private int collisionDamage;
        private bool resolved;
        private BoulderStrikeKnockback(SimObject owner) : base(owner) { }
        public static bool Apply(SimObject target, SimObject source, Vector3 direction, Fix64 speed, Fix64 duration, int damage) {
            var body = target.GetComponent<RigidBody>(); if (body == null || ForcedMovement.MassMultiplier(body.Mass) == Fix64.Zero) return false;
            var component = target.GetComponent<BoulderStrikeKnockback>() ?? target.GetPendingComponents<BoulderStrikeKnockback>().FirstOrDefault();
            if (component == null) { component = new BoulderStrikeKnockback(target); target.AddComponent(component); }
            direction.Y = Fix64.Zero; component.direction = direction.LengthSquared() == Fix64.Zero ? Vector3.Zero : Vector3.Normalize(direction);
            component.source = source; component.speed = speed; component.remaining = duration; component.collisionDamage = damage; component.resolved = false; return true;
        }
        private bool Enemy(SimObject other) => source != null && other.IsActive && TargetRelation.CanAttack(source, other) && other.HasComponent<IDamageable>();
        private static void Damage(SimObject target, Hit hit) { if (!target.HasComponent<IDamageable>()) return; target.SetStatus(ObjectStatus.Damaged); foreach (var receiver in target.GetComponents<IDamageable>()) receiver.OnDamaged(hit); }
        public void OnCollisionWithEnemy(SimObject other) { }
        public void OnCollision(SimObject other) {
            if (resolved || ReferenceEquals(other, Owner) || !(other.Prefab == "Wall" || other.Prefab == "RockRemnant" || Enemy(other))) return;
            resolved = true; var hit = new Hit(collisionDamage, source?.Id ?? 0, Element.Rock, Element.Wind); Damage(Owner, hit);
            if (Enemy(other)) Damage(other, hit);
            var position = (Owner.Position + other.Position) / 2; Context.AddEvent(new BattleEvent("BoulderStrikeImpact", position, position, (Fix64)0.35m)); Owner.RemoveComponent(this);
        }
        public override void Start() { Owner.GetComponent<IStunnableBehavior>()?.SetStun(remaining); }
        public override void Update() {
            Fix64 dt = Context.DeltaTime; var body = Owner.GetComponent<RigidBody>();
            if (resolved || dt <= Fix64.Zero || remaining <= Fix64.Zero || body == null) { Owner.RemoveComponent(this); return; }
            body.AddVelocity(direction * (speed * ForcedMovement.MassMultiplier(body.Mass) * MathHelper.Min(remaining / dt, Fix64.One)));
            remaining -= dt; if (remaining <= Fix64.Zero) Owner.RemoveComponent(this);
        }
    }
}
