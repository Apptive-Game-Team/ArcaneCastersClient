namespace ArcaneCasters.Simulation {
    public static class ElementalNests {
        public static FireSlimeInitializer EmberSpiritInitializer() => new FireSlimeInitializer("ember_spirit");
        public static void Register(SimulationContext context) {
            RegisterPair(context, "ElectricSummon", "electric_summon", "ElectricSlime", "electric_slime", Element.Lightning);
            RegisterPair(context, "FireSummon", "fire_summon", "FireSlime", "fire_slime", Element.Fire);
            RegisterPair(context, "RockSummon", "rock_summon", "RockSlime", "rock_slime", Element.Rock);
            RegisterPair(context, "WindSummon", "wind_summon", "WindSlime", "wind_slime", Element.Wind);
        }
        private static void RegisterPair(SimulationContext context, string nest, string nestKey, string slime, string slimeKey, Element element) {
            context.RegisterPrefab(nest, owner => {
                var p = context.GameParameters; owner.AddCollider(new CircleCollider(owner, p[nestKey + ".radius"], true)); owner.Elements.AddNative(element);
                owner.AddComponentImmediately(new Spawner(owner, (int)(p[nestKey + ".hp"].RawValue / (1L << 32)), slime));
            });
            RegisterSlime(context, slime, slimeKey, element);
        }
        private static void RegisterSlime(SimulationContext context, string slime, string slimeKey, Element element) {
            if (element == Element.Fire) context.RegisterPrefab(slime, new FireSlimeInitializer(slimeKey).Initialize);
            else context.RegisterPrefab(slime, owner => InitializeSlime(owner, slimeKey, element));
        }
        internal static void InitializeSlime(SimObject owner, string slimeKey, Element element) {
            var p = owner.Context.GameParameters; int Integer(string name) => (int)(p[slimeKey + "." + name].RawValue / (1L << 32));
            owner.AddComponentImmediately(new RigidBody(owner, Integer("mass"))); owner.AddComponentImmediately(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, p[slimeKey + ".radius"]));
            if (element == Element.Fire) owner.Elements.AddNative(element);
            owner.AddComponentImmediately(new Slime(owner, Integer("hp"), p[slimeKey + ".speed"], 1, Integer("damage"), p[slimeKey + ".attack_interval"]));
            if (element != Element.Fire) owner.Elements.AddNative(element);
            if (element == Element.Fire) owner.AddComponentImmediately(new PathSpawner(owner, "FireField", 1));
            if (element == Element.Lightning) {
                owner.AddComponent(new ElectricDeathEnergy(owner)); owner.AddComponentImmediately(new LightningSummonEffectReceiver(owner));
            } else owner.AddComponentImmediately(new CommonEffectReceiver(owner));
        }
    }
    public sealed class FireSlimeInitializer {
        public string Prefab => "FireSlime";
        private readonly string parameterKey;
        public FireSlimeInitializer(string parameterKey = "fire_slime") { this.parameterKey = parameterKey; }
        public void Initialize(SimObject owner) => ElementalNests.InitializeSlime(owner, parameterKey, Element.Fire);
    }
}
