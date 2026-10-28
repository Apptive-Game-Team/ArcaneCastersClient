using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class BeanProjectile : SimComponent, ICollidable {
        private readonly Fix64 speed;
        private Vector3 origin;
        private Vector3? direction;
        private int damage;
        private Fix64 maxDistance;
        private bool spent;
        public BeanProjectile(SimObject owner, Fix64 speed) : base(owner) { this.speed = speed; }
        public void Launch(Vector3 target, int damage, Fix64 maxDistance) {
            origin = Owner.Position;
            var offset = GroundVector.Of(target - origin); var length = offset.Length();
            direction = length == Fix64.Zero ? Vector3.Zero : new Vector3(offset.X / length, 0, offset.Z / length);
            this.damage = damage; this.maxDistance = maxDistance;
        }
        public override void Update() {
            if (!direction.HasValue || spent) return;
            Owner.SetPosition(Owner.Position + direction.Value * (speed * Context.DeltaTime));
            if (Vector3.Distance(Owner.Position, origin) >= maxDistance) { spent = true; Owner.Destroy(); }
        }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) {
            if (spent || !other.IsActive || !TargetRelation.CanAttack(Owner, other) || CombatRange.TargetMask(other.Position) != 1) return;
            var receivers = other.GetComponents<IDamageable>(); if (receivers.Length == 0) return;
            spent = true; Owner.SetStatus(ObjectStatus.Attack); other.SetStatus(ObjectStatus.Damaged);
            var hit = new Hit(damage, Owner.Id, Owner.Elements.Total);
            foreach (var receiver in receivers) receiver.OnDamaged(hit);
            Owner.Destroy();
        }
    }
    public sealed class BeanShooterMob : BehaviorMob {
        public BeanShooterMob(SimObject owner, int hp, Fix64 speed, int damage, Fix64 interval, Fix64 range) : base(owner, hp, speed, 1, interval, range, null) {
            Behavior = target => {
                if (!target.IsActive) return false;
                var bean = Context.Spawn(owner.Master, "Bean", owner.Position);
                var projectile = bean.GetComponent<BeanProjectile>() ?? throw new System.InvalidOperationException("the bean prefab carries a BeanProjectile");
                projectile.Launch(GroundVector.Of(target.Position), damage, AttackRange * (Fix64)1.5m);
                owner.SetStatus(ObjectStatus.Attack); return true;
            };
        }
    }
    public static class BeanPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("Bean", owner => {
                owner.AddCollider(new CircleCollider(owner, context.GameParameters["bean.radius"], true)); owner.Elements.AddNative(Element.Nature);
                owner.AddComponent(new BeanProjectile(owner, context.GameParameters["bean.speed"]));
            });
            context.RegisterPrefab("BeanShooter", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["bean_shooter." + name].RawValue / (1L << 32));
                owner.AddComponentImmediately(new RigidBody(owner, Integer("mass"))); owner.AddComponentImmediately(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, p["bean_shooter.radius"]));
                owner.AddComponentImmediately(new BeanShooterMob(owner, Integer("hp"), p["bean_shooter.speed"], Integer("damage"), p["bean_shooter.attack_interval"], p["bean_shooter.attack_range"]));
                owner.Elements.AddNative(Element.Nature); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
