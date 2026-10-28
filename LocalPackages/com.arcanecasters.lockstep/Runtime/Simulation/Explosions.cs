using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public static class ExplosionPrefabs {
        public static void Register(SimulationContext context) {
            RegisterElement(context, "FireExplode", "fire_explode", Element.Fire, VisualEffect.Burn);
            RegisterElement(context, "RockExplode", "rock_explode", Element.Rock, VisualEffect.None);
            RegisterElement(context, "WindExplode", "wind_explode", Element.Wind, VisualEffect.Knockback);
            RegisterElement(context, "WaterExplode", "water_explode", Element.Water, VisualEffect.Wet);
            RegisterElement(context, "LeafExplode", "leaf_explode", Element.Nature, VisualEffect.Snared);
            RegisterElement(context, "ElectricExplode", "electric_explode", Element.Lightning, VisualEffect.Shock);
            context.RegisterPrefab("ShockOverload", owner => {
                var radius = context.GameParameters["shock_overload.radius"]; owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Lightning);
                owner.AddComponent(new ShockOverloadExplosion(owner, (int)(context.GameParameters["shock_overload.damage"].RawValue / (1L << 32)), radius));
            });
        }
        private static void RegisterElement(SimulationContext context, string prefab, string key, Element element, VisualEffect effect) {
            context.RegisterPrefab(prefab, owner => {
                var radius = context.GameParameters[key + ".radius"]; owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(element);
                if (effect == VisualEffect.Knockback) owner.AddComponentImmediately(new KnockbackEffectProvider(owner, effect));
                else if (effect != VisualEffect.None) owner.AddComponentImmediately(new EffectProvider(owner, effect));
                // WaterExplode prefab도 원본 initializer에서는 기본 Explode를 사용한다.
                owner.AddComponentImmediately(new Explode(owner, (int)(context.GameParameters[key + ".damage"].RawValue / (1L << 32)), radius));
            });
        }
    }
    public abstract class ExplosionBase : SimComponent, IAreaExplode {
        protected readonly int damage;
        protected readonly Fix64 delay;
        protected readonly Hit hit;
        protected Fix64 counter;
        protected bool running = true;
        public Fix64 Radius { get; }
        protected ExplosionBase(SimObject owner, int damage, Fix64 radius, Fix64 delay) : base(owner) { this.damage = damage; Radius = radius; this.delay = delay; hit = new Hit(damage, owner.Id, owner.Elements.Total); }
        public virtual void HandleGameObject(SimObject target) {
            var receivers = target.GetComponents<IDamageable>(); if (receivers.Length == 0) return;
            target.SetStatus(ObjectStatus.Damaged); foreach (var receiver in receivers) receiver.OnDamaged(hit);
        }
        public override void Update() {
            if (!running) return;
            // 원본은 시간을 더하기 전 검사하므로 경계에 도달한 다음 update에 폭발한다.
            if (counter < delay) { counter += Context.DeltaTime; return; }
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, Radius)) if (!ReferenceEquals(target, Owner)) HandleGameObject(target);
            running = false; Owner.Destroy();
        }
    }
    public sealed class Explode : ExplosionBase {
        public Explode(SimObject owner, int damage) : this(owner, damage, 3, (Fix64)0.5m) { }
        public Explode(SimObject owner, int damage, Fix64 radius) : this(owner, damage, radius, (Fix64)0.5m) { }
        public Explode(SimObject owner, int damage, Fix64 radius, Fix64 delay) : base(owner, damage, radius, delay) { }
    }
    public sealed class WaterExplode : ExplosionBase {
        private readonly Fix64 force;
        public WaterExplode(SimObject owner, int damage, Fix64 radius, Fix64 force) : base(owner, damage, radius, (Fix64)0.5m) { this.force = force; }
        public override void HandleGameObject(SimObject target) { base.HandleGameObject(target); target.GetComponent<ZPhysics>()?.AddImpulseZ(force * ForcedMovement.MassMultiplier(target)); }
    }
    public sealed class ShockOverloadExplosion : ExplosionBase {
        private readonly List<SimObject> marked = new List<SimObject>();
        private bool primary;
        public ShockOverloadExplosion(SimObject owner, int damage, Fix64 radius) : base(owner, damage, radius, (Fix64)0.5m) { }
        public override void Update() {
            if (!running) return; counter += Context.DeltaTime;
            if (!primary) {
                if (counter < delay) return;
                foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, Radius)) {
                    if (!TargetRelation.CanAttack(Owner, target) || !target.HasComponent<IDamageable>()) continue;
                    HandleGameObject(target); marked.Add(target); target.AddEffect(VisualEffect.Shock);
                }
                primary = true; counter = IntervalTimer.CarryOver(counter, delay, Context.DeltaTime); return;
            }
            if (counter < Fix64.One) return;
            int secondaryDamage = System.Math.Max(1, (int)(Fix64.Floor((Fix64)damage / 2 + (Fix64)0.5m).RawValue / (1L << 32)));
            var secondaryHit = new Hit(secondaryDamage, Owner.Id, Owner.Elements.Total);
            foreach (var target in marked) {
                target.RemoveEffect(VisualEffect.Shock); if (target.IsDestroyed) continue;
                var center = target.Position; Context.AddEvent(new BattleEvent("ShockOverloadSecondary", center, center, (Fix64)0.8m));
                foreach (var affected in SimulationQueries.OverlapSphereAll(Context, center, Radius / 2)) {
                    if (!TargetRelation.CanAttack(Owner, affected)) continue;
                    var receivers = affected.GetComponents<IDamageable>(); if (receivers.Length == 0) continue;
                    affected.SetStatus(ObjectStatus.Damaged); foreach (var receiver in receivers) receiver.OnDamaged(secondaryHit);
                }
            }
            running = false; Owner.Destroy();
        }
    }
}
