using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class PveGateKeeperMob : MobComponent, IGaugeComponent, IPveObjectiveTarget {
        public PveGateKeeperMob(SimObject owner, int hp) : base(owner, hp, 0) { }
        public override bool IsObjectiveTarget => true;
        public override bool LeavesDeathField => false;
        public bool IsTerminal => Owner.IsDestroyed || Hp <= 0;
        public override void OnDeath() { Owner.Destroy(); }
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
    }
    public static class DimensionToadPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("DimensionToad", owner => {
                const string key = "dimension_toad"; Body(owner, key, false);
                owner.AddComponent(new NonAttackingCowardMob(owner, Integer(owner, key, "hp"), Value(owner, key, "speed"), 1, Value(owner, key, "attack_interval"), Value(owner, key, "detection_range"), Value(owner, key, "panic_duration"), 4));
                owner.AddComponent(new LimitedSequenceSpawner(owner, Value(owner, key, "spawn_interval"), 0, new[] { "FireTadpole", "LightningTadpole" }, true));
                owner.Elements.AddNative(Element.Fire); owner.Elements.AddNative(Element.Lightning); owner.AddComponent(new CommonEffectReceiver(owner));
            });
            context.RegisterPrefab("FireTadpole", owner => Tadpole(owner, "fire_tadpole", false));
            context.RegisterPrefab("LightningTadpole", owner => Tadpole(owner, "lightning_tadpole", true));
            context.RegisterPrefab("PveDimensionToad", owner => {
                const string key = "pve_dimension_toad"; Body(owner, key, true); owner.Elements.AddNative(Element.Fire); owner.Elements.AddNative(Element.Lightning);
                owner.AddComponent(new CommonEffectReceiver(owner)); owner.AddComponent(new PveGateKeeperMob(owner, Integer(owner, key, "hp")));
                owner.AddComponent(new LimitedSequenceSpawner(owner, Value(owner, key, "spawn_interval"), 0, new[] { "PveFireTadpole", "PveLightningTadpole" }, true));
            });
            context.RegisterPrefab("PveFireTadpole", owner => Tadpole(owner, "pve_fire_tadpole", false));
            context.RegisterPrefab("PveLightningTadpole", owner => Tadpole(owner, "pve_lightning_tadpole", true));
        }
        private static Fix64 Value(SimObject owner, string key, string name) => owner.Context.GameParameters[key + "." + name];
        private static int Integer(SimObject owner, string key, string name) => (int)(Value(owner, key, name).RawValue / (1L << 32));
        private static void Body(SimObject owner, string key, bool trigger) { owner.AddComponent(new RigidBody(owner, Integer(owner, key, "mass"))); owner.AddComponent(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, Value(owner, key, "radius"), trigger)); }
        private static void Tadpole(SimObject owner, string key, bool lightning) {
            Body(owner, key, false); owner.AddComponent(new MeleeAttackMob(owner, Integer(owner, key, "hp"), Value(owner, key, "speed"), 1, Integer(owner, key, "damage"), Value(owner, key, "attack_interval")));
            owner.AddComponent(new TimedSelfDestroyer(owner, Value(owner, key, "duration"))); owner.Elements.AddNative(lightning ? Element.Lightning : Element.Fire);
            if (lightning) { owner.AddComponent(new ElectricDeathEnergy(owner)); owner.AddComponent(new LightningSummonEffectReceiver(owner)); }
            else owner.AddComponent(new CommonEffectReceiver(owner));
        }
    }
}
