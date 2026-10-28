using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class CraterEmber : SimComponent, ICollidable {
        private readonly int damage;
        private readonly Fix64 maximum;
        private Vector3 start, direction;
        private Fix64 elapsed;
        private bool resolved;
        public Vector3 LandingPosition { get; private set; }
        public Fix64 TravelDuration { get; private set; }
        public CraterEmber(SimObject owner, int damage, Fix64 maximum) : base(owner) { this.damage = damage; this.maximum = MathHelper.Max(1, maximum); }
        public override void Start() {
            start = Owner.Position; var angle = Context.Random.NextFraction() * Fix64.Pi * 2;
            if (maximum <= 1) throw new ArgumentOutOfRangeException(nameof(maximum), "bound must be greater than origin");
            var distance = 1 + Context.Random.NextFloatFraction() * (maximum - 1);
            direction = new Vector3(Fix64.Cos(angle), 0, Fix64.Sin(angle)); LandingPosition = start + direction * distance; TravelDuration = distance / 4;
        }
        public override void Update() {
            if (resolved) return;
            elapsed += Context.DeltaTime;
            if (elapsed >= TravelDuration) {
                resolved = true; Owner.SetPosition(GroundVector.Of(LandingPosition)); Context.Spawn(Master.None, "FireField", Owner.Position); Owner.Destroy(); return;
            }
            var progress = elapsed / TravelDuration; var position = start + direction * (4 * elapsed);
            Owner.SetPosition(new Vector3(position.X, MathHelper.Max(0, 12 * progress * (1 - progress)), position.Z));
        }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) {
            if (resolved || ReferenceEquals(other, Owner) || other.Master == Owner.Master) return;
            var mob = other.GetComponent<MobComponent>(); if (mob == null) return;
            other.SetStatus(ObjectStatus.Damaged); mob.OnDamaged(new Hit(damage, Owner.Id, Owner.Elements.Total)); resolved = true; Owner.Destroy();
        }
    }
    public static class CraterPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("Crater", owner => {
                var p = context.GameParameters;
                owner.AddComponent(new RigidBody(owner, (int)(p["crater.mass"].RawValue / (1L << 32)))); owner.AddCollider(new CircleCollider(owner, p["crater.radius"]));
                owner.AddComponent(new DummyMob(owner, (int)(p["crater.hp"].RawValue / (1L << 32)))); owner.AddComponent(new CraterSpawner(owner, p["crater.attack_interval"]));
                owner.AddComponent(new TimedSelfDestroyer(owner, p["crater.duration"])); owner.Elements.AddNative(Element.Fire); owner.AddComponent(new BuildingEffectReceiver(owner));
            });
            context.RegisterPrefab("CraterEmber", owner => {
                var p = context.GameParameters; owner.AddCollider(new CircleCollider(owner, p["crater_ember.radius"], true)); owner.Elements.AddNative(Element.Fire);
                owner.AddComponent(new CraterEmber(owner, (int)(p["crater_ember.damage"].RawValue / (1L << 32)), p["crater_ember.attack_range"]));
            });
        }
    }
}
