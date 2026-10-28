using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class ThunderBirdMob : BehaviorMob {
        private readonly int damage;
        private Fix64 selfRadius;
        public ThunderBirdMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range) : base(owner, hp, speed, mask, interval, range, null, true) {
            this.damage = damage;
            Behavior = target => {
                var mob = target.GetComponent<MobComponent>(); if (mob == null) return false;
                if (owner.Position.Y < 3) SetState(new FloatingState(this)); else SetState(new AttackingState(this, mob)); return true;
            };
        }
        private static Fix64 Radius(SimObject owner) => owner.Colliders.FirstOrDefault(c => c.CircleRadius.HasValue)?.CircleRadius ?? throw new InvalidOperationException("No value present");
        public override void Start() { base.Start(); selfRadius = Radius(Owner); }
        public sealed class AttackingState : IMobState {
            private readonly ThunderBirdMob mob;
            private readonly MobComponent target;
            private Vector3 start;
            private Fix64 targetRadius;
            public AttackingState(ThunderBirdMob mob, MobComponent target) { this.mob = mob; this.target = target; }
            public void OnEnter() { mob.Owner.GetComponent<ZPhysics>()?.LockHover(mob.SourceId); start = mob.Owner.Position; targetRadius = Radius(target.Owner); }
            public void OnExit() { mob.Owner.GetComponent<ZPhysics>()?.UnlockHover(mob.SourceId); }
            public void OnUpdate() {
                if (target.Owner.IsDestroyed) { mob.SetState(new FloatingState(mob)); return; }
                if (Vector3.Distance(target.Owner.Position, mob.Owner.Position) - targetRadius - mob.selfRadius > (Fix64)0.5m) {
                    var progress = MathHelper.Clamp(mob.DiveProgress(start, target.Owner.Position.Y), 0, 1);
                    var next = start + (target.Owner.Position - start) * progress; next.Y = mob.Owner.Position.Y; mob.Owner.SetPosition(next); return;
                }
                var amount = (Fix64)mob.damage * (start.Y - mob.Owner.Position.Y) / (Fix64)2.5m;
                target.OnDamaged(new Hit((int)(amount.RawValue / (1L << 32)), mob.Owner.Id, mob.Owner.Elements.Total)); mob.SetState(new FloatingState(mob));
            }
        }
        public sealed class FloatingState : IMobState {
            private readonly ThunderBirdMob mob;
            public FloatingState(ThunderBirdMob mob) { this.mob = mob; }
            public void OnEnter() { }
            public void OnExit() { }
            public void OnUpdate() { if (mob.Owner.Position.Y >= 3) mob.SetIdle(); }
        }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("ThunderBird", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["thunder_bird." + name].RawValue / (1L << 32));
                owner.AddComponentImmediately(new RigidBody(owner, Integer("mass"))); owner.AddComponentImmediately(new ZPhysics(owner, 3)); owner.AddCollider(new CircleCollider(owner, p["thunder_bird.radius"]));
                owner.AddComponentImmediately(new ThunderBirdMob(owner, Integer("hp"), p["thunder_bird.speed"], 1, Integer("damage"), p["thunder_bird.attack_interval"], p["thunder_bird.attack_range"]));
                owner.Elements.AddNative(Element.Lightning); owner.AddComponent(new ElectricDeathEnergy(owner)); owner.AddComponentImmediately(new LightningSummonEffectReceiver(owner));
            });
        }
    }
}
