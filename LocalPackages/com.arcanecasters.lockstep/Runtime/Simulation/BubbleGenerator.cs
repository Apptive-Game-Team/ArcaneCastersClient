using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public abstract class TimedBehaviorMob : MobComponent, IIntervalAttacker, IGaugeComponent {
        public Stat AttackInterval { get; }
        public Func<bool> Behavior { get; set; }
        private Fix64 timer;
        protected TimedBehaviorMob(SimObject owner, int hp, Fix64 speed, Fix64 interval) : base(owner, hp, speed) { AttackInterval = new Stat(interval); }
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
        public override void Update() {
            base.Update(); timer += Context.DeltaTime; var interval = AttackInterval.Total;
            if (timer < interval) return;
            if (Behavior == null) throw new InvalidOperationException("timed behavior mob has no behavior");
            if (Behavior()) timer = IntervalTimer.CarryOver(timer, interval, Context.DeltaTime);
        }
    }
    public sealed class BubbleGeneratorMob : TimedBehaviorMob {
        private readonly Fix64 range, projectileSpeed;
        public BubbleGeneratorMob(SimObject owner, int hp, Fix64 interval, Fix64 range, Fix64 projectileSpeed) : base(owner, hp, 0, interval) {
            this.range = range; this.projectileSpeed = projectileSpeed; Behavior = ShootBubble;
        }
        public override void OnDeath() { Owner.Destroy(); }
        private bool ShootBubble() {
            SimObject closest = null; Fix64 closestDistance = Fix64.MaxValue;
            foreach (var target in Context.Objects) {
                if (ReferenceEquals(target, Owner) || !target.IsActive || target.Master != Owner.Master || !target.HasComponent<IEffectReceiver>() || !target.HasComponent<IDamageable>()) continue;
                if (!CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero, range)) continue;
                if (target.GetComponents<BaseStatusEffect>().Any(effect => effect.Key == StatusEffectKey.BubbleReceive)) continue;
                var distance = Vector3.Distance(Owner.Position, target.Position);
                if (closest == null || distance < closestDistance) { closest = target; closestDistance = distance; }
            }
            if (closest == null) return false;
            Context.AddEvent(new BattleEvent("WaterShot", Owner.Id, closest.Id, closestDistance / projectileSpeed));
            closest.GetComponent<IEffectReceiver>().Receive(VisualEffect.Bubble); Owner.SetStatus(ObjectStatus.Attack); return true;
        }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("BubbleGenerator", owner => {
                var p = context.GameParameters;
                owner.AddComponent(new RigidBody(owner, (int)(p["bubble_generator.mass"].RawValue / (1L << 32))));
                owner.AddCollider(new CircleCollider(owner, p["bubble_generator.radius"]));
                owner.AddComponent(new BubbleGeneratorMob(owner, (int)(p["bubble_generator.hp"].RawValue / (1L << 32)), p["bubble_generator.attack_interval"], p["bubble_generator.attack_range"], p["bubble_generator.projectile_speed"]));
                owner.AddComponent(new TimedSelfDestroyer(owner, p["bubble_generator.duration"])); owner.Elements.AddNative(Element.Water); owner.AddComponent(new BuildingEffectReceiver(owner));
            });
        }
    }
}
