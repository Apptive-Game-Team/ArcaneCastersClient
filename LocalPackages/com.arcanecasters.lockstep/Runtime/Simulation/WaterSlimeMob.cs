using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class WaterSlimeRangeAttackMob : ProjectileRangeAttackMob {
        public WaterSlimeRangeAttackMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range)
            : base(owner, hp, speed, mask, damage, interval, range, "WaterShot", (Fix64)0.5m) { }
        public static void RegisterPrefabs(SimulationContext context) {
            context.RegisterPrefab("WaterSlime", owner => {
                Fix64 Value(string key) => context.GameParameters["water_slime." + key]; int Integer(string key) => (int)(Value(key).RawValue / (1L << 32));
                owner.AddComponentImmediately(new RigidBody(owner, Integer("mass"))); owner.AddComponentImmediately(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, Value("radius")));
                owner.AddComponentImmediately(new WaterSlimeRangeAttackMob(owner, Integer("hp"), Value("speed"), 1, Integer("damage"), Value("attack_interval"), context.GameParameters["aqua_archer.attack_range"]));
                owner.AddComponentImmediately(new PathSpawner(owner, "WaterField", 1)); owner.Elements.AddNative(Element.Water); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
