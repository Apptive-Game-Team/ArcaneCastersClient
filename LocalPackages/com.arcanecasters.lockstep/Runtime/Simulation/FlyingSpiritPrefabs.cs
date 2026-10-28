using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public class BubbleSpiritMob : BehaviorMob {
        public BubbleSpiritMob(SimObject owner, int hp, Fix64 speed, int damage, Fix64 interval, Fix64 range, Fix64 projectileSpeed) : base(owner, hp, speed, 2, interval, range, null) {
            Behavior = target => {
                var duration = Vector3.Distance(target.Position, owner.Position) / projectileSpeed;
                Context.AddEvent(new BattleEvent("BubbleShot", owner.Id, target.Id, duration));
                var hit = new Hit(damage, owner.Id, owner.Elements.Total);
                foreach (var candidate in SimulationQueries.OverlapSphereAll(Context, target.Position, 1)) {
                    if (!TargetRelation.CanAttack(owner, candidate) || (CombatRange.TargetMask(candidate.Position) & 2) == 0) continue;
                    candidate.GetComponent<IDamageable>()?.OnDamagedWithDelay(hit, duration);
                }
                owner.SetStatus(ObjectStatus.Attack); return true;
            };
        }
    }
    public static class FlyingSpiritPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("BubbleSpirit", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["bubble_spirit." + name].RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner, 3)); owner.AddCollider(new CircleCollider(owner, p["bubble_spirit.radius"]));
                owner.AddComponent(new BubbleSpiritMob(owner, Integer("hp"), p["bubble_spirit.speed"], Integer("damage"), p["bubble_spirit.attack_interval"], p["bubble_spirit.attack_range"], p["bubble_spirit.projectile_speed"]));
                owner.Elements.AddNative(Element.Water); owner.Elements.AddNative(Element.Wind); owner.AddComponent(new CommonEffectReceiver(owner));
            });
            context.RegisterPrefab("ThunderSpirit", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["thunder_spirit." + name].RawValue / (1L << 32));
                owner.AddComponentImmediately(new RigidBody(owner, Integer("mass"))); owner.AddComponentImmediately(new ZPhysics(owner, 3)); owner.AddCollider(new CircleCollider(owner, p["thunder_spirit.radius"]));
                owner.AddComponentImmediately(new ProjectileRangeAttackMob(owner, Integer("hp"), p["thunder_spirit.speed"], 1, Integer("damage"), p["thunder_spirit.attack_interval"], p["thunder_spirit.attack_range"], "ElectricShot", (Fix64)0.5m));
                owner.Elements.AddNative(Element.Lightning); owner.Elements.AddNative(Element.Wind); owner.AddComponent(new ElectricDeathEnergy(owner)); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
