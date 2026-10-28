using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class RockGolemMob : MeleeAttackMob {
        public RockGolemMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval) : base(owner, hp, speed, mask, damage, interval) {
            Behavior = target => {
                var receiver = target.GetComponent<IDamageable>(); if (receiver == null) return false;
                receiver.OnDamaged(new Hit(damage, 0, owner.Elements.Total));
                var direction = GroundVector.Of(target.Position - owner.Position); if (direction.LengthSquared() != Fix64.Zero) direction.Normalize();
                target.GetComponent<IEffectReceiver>()?.ReceiveDirectional(VisualEffect.Knockback, direction, (Fix64)0.25m);
                owner.SetStatus(ObjectStatus.Attack); return true;
            };
        }
    }
    public static class RockUnitPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("MiniRock", owner => Initialize(owner, "mini_rock", false));
            context.RegisterPrefab("RockGolem", owner => Initialize(owner, "rock_golem", true));
            context.RegisterPrefab("WallGolem", owner => Initialize(owner, "wall_golem", true, RockRemnantSize.Medium));
        }
        private static void Initialize(SimObject owner, string key, bool golem, RockRemnantSize remnantSize = RockRemnantSize.Small) {
            var p = owner.Context.GameParameters;
            owner.AddComponentImmediately(new RigidBody(owner, (int)(p[key + ".mass"].RawValue / (1L << 32)))); owner.AddComponentImmediately(new ZPhysics(owner));
            owner.AddCollider(new CircleCollider(owner, p[key + ".radius"]));
            int hp = (int)(p[key + ".hp"].RawValue / (1L << 32)), damage = (int)(p[key + ".damage"].RawValue / (1L << 32));
            if (golem) owner.AddComponentImmediately(new RockGolemMob(owner, hp, p[key + ".speed"], 1, damage, p[key + ".attack_interval"]));
            else owner.AddComponentImmediately(new Slime(owner, hp, p[key + ".speed"], 1, damage, p[key + ".attack_interval"]));
            owner.Elements.AddNative(Element.Rock);
            if (golem) owner.AddComponent(new RockDeathRemnant(owner, remnantSize));
            owner.AddComponentImmediately(new CommonEffectReceiver(owner));
        }
    }
}
