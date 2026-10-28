using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class ChickenCommandoMob : MeleeAttackMob {
        public ChickenCommandoMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval) : base(owner, hp, speed, mask, damage, interval) { }
        public override void Update() { if (Owner.Position.Y > (Fix64)0.05m) return; base.Update(); }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("ChickenCommando", owner => {
                var p = context.GameParameters;
                owner.AddComponent(new RigidBody(owner, (int)(p["chicken_commando.mass"].RawValue / (1L << 32))));
                // 원본 f32::MAX와 같이 낙하 피해 판정을 비활성화한다.
                owner.AddComponent(new ZPhysics(owner) { Gravity = p["chicken_commando.fall_gravity"], FallThreshold = Fix64.MaxValue });
                owner.AddCollider(new CircleCollider(owner, p["chicken_commando.radius"]));
                owner.AddComponent(new ChickenCommandoMob(owner, (int)(p["chicken_commando.hp"].RawValue / (1L << 32)), p["chicken_commando.speed"], 1, (int)(p["chicken_commando.damage"].RawValue / (1L << 32)), p["chicken_commando.attack_interval"]));
                owner.Elements.AddNative(Element.Wind); owner.AddComponent(new CommonEffectReceiver(owner));
            });
        }
    }
}
