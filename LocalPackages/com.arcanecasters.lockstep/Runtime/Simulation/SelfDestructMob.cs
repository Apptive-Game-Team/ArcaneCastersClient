using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class SelfDestructMob : BehaviorMob, ICollidable {
        private readonly int damage;
        private readonly Fix64 explosionRange;
        private readonly Fix64? altitudeThreshold;
        private Fix64 selfRadius;
        private bool exploded;
        public override bool FallsOnDeath => false;
        public SelfDestructMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range, bool verticalRangeIgnored, Fix64? altitudeThreshold = null)
            : base(owner, hp, speed, mask, 0, 0, null, verticalRangeIgnored) {
            this.damage = damage; explosionRange = range; this.altitudeThreshold = altitudeThreshold;
            Behavior = target => { var mob = target.GetComponent<MobComponent>(); if (mob == null) return false; SetState(new AttackingState(this, mob)); return true; };
        }
        private static Fix64 Radius(SimObject owner) => owner.Colliders.FirstOrDefault(c => c.CircleRadius.HasValue)?.CircleRadius ?? throw new InvalidOperationException("No value present");
        public override void Start() { base.Start(); selfRadius = Radius(Owner); AttackRange = selfRadius + (Fix64)0.3m; }
        public override void Update() {
            base.Update();
            if (altitudeThreshold.HasValue && Owner.Position.Y <= altitudeThreshold.Value) { Explode(); Owner.Destroy(); }
        }
        private void Explode() {
            if (exploded) return; exploded = true; var hit = new Hit(damage, Owner.Id, Owner.Elements.Total);
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, explosionRange)) {
                if (ReferenceEquals(target, Owner) || !target.IsActive || !TargetRelation.CanAttack(Owner, target)) continue;
                target.GetComponent<MobComponent>()?.OnDamaged(hit);
            }
        }
        public override void OnDeath() { Explode(); base.OnDeath(); }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) {
            if (other.HasComponent<MobComponent>() && TargetRelation.CanAttack(Owner, other)) { Explode(); Owner.Destroy(); }
        }
        public sealed class AttackingState : IMobState {
            private readonly SelfDestructMob mob;
            private readonly MobComponent target;
            private Fix64 targetRadius;
            private Vector3 start;
            public AttackingState(SelfDestructMob mob, MobComponent target) { this.mob = mob; this.target = target; }
            public void OnEnter() { mob.Owner.GetComponent<ZPhysics>()?.LockHover(mob.SourceId); start = mob.Owner.Position; targetRadius = Radius(target.Owner); }
            public void OnExit() { mob.Owner.GetComponent<ZPhysics>()?.UnlockHover(mob.SourceId); }
            public void OnUpdate() {
                if (target.Owner.IsDestroyed) { mob.SetIdle(); return; }
                var progress = MathHelper.Clamp(mob.DiveProgress(start, target.Owner.Position.Y), 0, 1);
                var position = start + (target.Owner.Position - start) * progress; position.Y = mob.Owner.Position.Y; mob.Owner.SetPosition(position);
                if (Vector3.Distance(mob.Owner.Position, target.Owner.Position) - targetRadius - mob.selfRadius <= (Fix64)0.3m) { mob.Explode(); mob.Owner.Destroy(); }
            }
        }
        public static void RegisterWindSpirit(SimulationContext context) {
            context.RegisterPrefab("WindSpirit", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["wind_spirit." + name].RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner, 3)); owner.AddCollider(new CircleCollider(owner, p["wind_spirit.radius"]));
                owner.AddComponent(new SelfDestructMob(owner, Integer("hp"), p["wind_spirit.speed"], 2, Integer("damage"), p["wind_spirit.attack_interval"], p["wind_spirit.attack_range"], true, 1));
                owner.Elements.AddNative(Element.Wind); owner.AddComponent(new CommonEffectReceiver(owner));
            });
        }
    }
}
