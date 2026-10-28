using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public abstract class DropBase : MobComponent, IDropMarker, ICollidable, IGaugeComponent {
        protected readonly int damage;
        public int Direction { get; protected set; } = -1;
        protected DropBase(SimObject owner, int damage) : base(owner, 1, 0) { this.damage = damage; }
        public override void OnDeath() { Direction = 0; Owner.Destroy(); }
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
        public override void Update() { base.Update(); Owner.SetPosition(Owner.Position + new Vector3(0, Direction * 5 * Context.DeltaTime, 0)); if (Owner.Position.Y < Fix64.Zero) Owner.Destroy(); }
        public void OnCollision(SimObject other) { }
        public virtual void OnCollisionWithEnemy(SimObject other) {
            var receivers = other.GetComponents<IDamageable>(); if (receivers.Length == 0) return;
            Direction = 0; Owner.SetStatus(ObjectStatus.Attack); other.SetStatus(ObjectStatus.Damaged);
            foreach (var receiver in receivers) receiver.OnDamaged(new Hit(damage, Owner.Id, Owner.Elements.Total)); Owner.Destroy();
        }
    }
    public sealed class Drop : DropBase { public Drop(SimObject owner, int damage) : base(owner, damage) { } }
    public sealed class Leafair : DropBase {
        private readonly int heal;
        private readonly Fix64 radius;
        public Leafair(SimObject owner, int damage, int heal, Fix64 radius) : base(owner, damage) { this.heal = heal; this.radius = radius; }
        public override void OnCollisionWithEnemy(SimObject other) { }
        public override void Update() {
            base.Update(); if (Owner.IsDestroyed) return;
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius)) {
                if (ReferenceEquals(target, Owner) || target.IsDestroyed) continue;
                if (Owner.Master != Master.None && target.Master == Owner.Master) {
                    var mobs = target.GetComponents<MobComponent>(); foreach (var mob in mobs) mob.OnDamaged(new Hit(-heal, 0, Element.None));
                    var lifetime = target.GetComponent<TimedSelfDestroyer>(); lifetime?.Recover(); if (mobs.Length == 0 && lifetime == null) continue;
                } else {
                    var receivers = target.GetComponents<IDamageable>(); if (receivers.Length == 0) continue;
                    target.SetStatus(ObjectStatus.Damaged); foreach (var receiver in receivers) receiver.OnDamaged(new Hit(damage, Owner.Id, Owner.Elements.Total));
                    target.GetComponent<IEffectReceiver>()?.Receive(VisualEffect.Snared);
                }
                Owner.SetStatus(ObjectStatus.Attack); Owner.Destroy(); return;
            }
        }
    }
    public static class DropPrefabs {
        public static void Register(SimulationContext context) {
            Register(context, "FireDrop", "fire_drop", Element.Fire); Register(context, "NatureDrop", "nature_drop", Element.Nature);
            Register(context, "RockDrop", "rock_drop", Element.Rock); Register(context, "WindDrop", "wind_drop", Element.Wind);
            Register(context, "MeteorDrop", "meteor_drop", Element.Fire, Element.Rock);
            context.RegisterPrefab("Leafair", owner => {
                Fix64 radius = context.GameParameters["leaf_drop.radius"]; int amount = (int)(context.GameParameters["leaf_drop.damage"].RawValue / (1L << 32));
                owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Nature); owner.AddComponent(new Leafair(owner, amount, amount, radius));
            });
        }
        private static void Register(SimulationContext context, string prefab, string key, params Element[] elements) {
            context.RegisterPrefab(prefab, owner => {
                owner.AddCollider(new CircleCollider(owner, context.GameParameters[key + ".radius"], true)); foreach (var element in elements) owner.Elements.AddNative(element);
                owner.AddComponentImmediately(new Drop(owner, (int)(context.GameParameters[key + ".damage"].RawValue / (1L << 32))));
            });
        }
    }
}
