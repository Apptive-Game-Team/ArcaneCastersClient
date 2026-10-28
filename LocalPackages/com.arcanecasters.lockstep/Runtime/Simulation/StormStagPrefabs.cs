using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public static class StormStagPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("StormStag", owner => {
                Fix64 Value(string key) => context.GameParameters["storm_stag." + key]; int Integer(string key) => (int)(Value(key).RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, Value("radius")));
                owner.AddComponent(new MovementSpeedTracker(owner, Value("speed"))); owner.AddComponent(new StormStagMob(owner, Integer("hp"), Value("speed"), 1, Integer("damage"), Value("acceleration"), Value("detection_range"), Value("panic_duration")));
                owner.Elements.AddNative(Element.Lightning); owner.AddComponent(new CommonEffectReceiver(owner)); owner.AddComponent(new OverchargeStatusEffect(owner, Fix64.Zero, true));
            });
        }
    }
}
