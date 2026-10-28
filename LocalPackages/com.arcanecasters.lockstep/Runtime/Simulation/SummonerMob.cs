using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class SummonerMob : BehaviorMob {
        public SummonerMob(SimObject owner, int hp, Fix64 speed, int mask, Fix64 interval, Fix64 range, string prefab)
            : base(owner, hp, speed, mask, interval, range, null) {
            Behavior = target => {
                var position = target.Position; var offset = new Vector3(position.X - owner.Position.X, 0, position.Z - owner.Position.Z);
                var maximum = range + (owner.FirstCircleRadius(false) ?? Fix64.Zero); var distance = offset.Length();
                if (distance > maximum) position = owner.Position + new Vector3(offset.X / distance, 0, offset.Z / distance) * maximum;
                Context.Spawn(owner.Master, prefab, position); return true;
            };
        }
        public static void RegisterPrefabs(SimulationContext context) {
            context.RegisterPrefab("MagmaSpirit", owner => {
                Fix64 Value(string key) => context.GameParameters["magma_spirit." + key]; int Integer(string key) => (int)(Value(key).RawValue / (1L << 32));
                owner.AddComponentImmediately(new RigidBody(owner, Integer("mass"))); owner.AddComponentImmediately(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, Value("radius")));
                owner.AddComponentImmediately(new SummonerMob(owner, Integer("hp"), Value("speed"), 1, Value("attack_interval"), Value("attack_range"), "MagmaFist"));
                owner.Elements.AddNative(Element.Fire); owner.Elements.AddNative(Element.Rock); owner.AddComponent(new RockDeathRemnant(owner, RockRemnantSize.Medium));
                owner.AddComponentImmediately(new CommonEffectReceiver(owner)); owner.AddComponent(new AreaEffectProvider(owner, 1, Value("sub_attack_range"), VisualEffect.Burn));
            });
            context.RegisterPrefab("MagmaFist", owner => {
                Fix64 Value(string key) => context.GameParameters["magma_fist." + key];
                owner.AddCollider(new CircleCollider(owner, Value("radius"), true)); owner.Elements.AddNative(Element.Fire); owner.Elements.AddNative(Element.Rock);
                owner.AddComponent(new EffectProvider(owner, VisualEffect.Burn)); owner.AddComponent(new TimedSelfDestroyer(owner, Value("duration")));
                owner.AddComponent(new OnStartAttacker(owner, Value("radius"), (int)(Value("damage").RawValue / (1L << 32))));
            });
        }
    }
}
