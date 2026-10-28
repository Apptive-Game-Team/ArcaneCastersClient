using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public static class CorruptedSpiritPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("PveCorruptedVineSpirit", owner => Initialize(owner, false));
            context.RegisterPrefab("PveCorruptedWindSpirit", owner => Initialize(owner, true));
        }
        private static void Initialize(SimObject owner, bool wind) {
            string key = wind ? "pve_corrupted_wind_spirit" : "pve_corrupted_vine_spirit";
            var p = owner.Context.GameParameters; Fix64 Value(string name) => p[key + "." + name]; int Integer(string name) => (int)(Value(name).RawValue / (1L << 32));
            owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(wind ? new ZPhysics(owner, 3) : new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, Value("radius")));
            if (wind) owner.AddComponent(new SelfDestructMob(owner, Integer("hp"), Value("speed"), 3, Integer("damage"), Value("attack_interval"), Value("attack_range"), true, 1));
            else owner.AddComponent(new MeleeAttackMob(owner, Integer("hp"), Value("speed"), 1, Integer("damage"), Value("attack_interval")));
            owner.Elements.AddNative(wind ? Element.Wind : Element.Nature); owner.AddComponent(new CommonEffectReceiver(owner));
        }
    }
}
