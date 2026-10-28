namespace ArcaneCasters.Simulation {
    public enum RockRemnantSize { Small, Medium }
    public static class RockRemnantSizes {
        public static string RemnantPrefab(this RockRemnantSize size) => size == RockRemnantSize.Small ? "RockRemnant" : "MediumRockRemnant";
        public static string SummonedPrefab(this RockRemnantSize size) => size == RockRemnantSize.Small ? "MiniRock" : "RockGolem";
        public static string BlastRemainderPrefab(this RockRemnantSize size) => size == RockRemnantSize.Small ? null : "RockRemnant";
        public static RockRemnantSize? FromPrefab(string prefab) => prefab == "RockRemnant" ? RockRemnantSize.Small : prefab == "MediumRockRemnant" ? RockRemnantSize.Medium : (RockRemnantSize?)null;
    }
    public sealed class RockDeathRemnant : SimComponent, ICombatDeathListener {
        private readonly RockRemnantSize size;
        private bool consumed;
        public RockDeathRemnant(SimObject owner, RockRemnantSize size = RockRemnantSize.Small) : base(owner) { this.size = size; }
        public void OnCombatDeath() { if (consumed) return; consumed = true; Context.Spawn(Owner.Master, size.RemnantPrefab(), Owner.Position); }
    }
    public static class RockRemnantPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("RockRemnant", owner => {
                owner.AddCollider(new CircleCollider(owner, context.GameParameters["mini_rock.radius"]));
                owner.AddComponentImmediately(new StaticObstacle(owner)); owner.AddComponentImmediately(new TimedSelfDestroyer(owner, 20)); owner.Elements.AddNative(Element.Rock);
            });
            context.RegisterPrefab("MediumRockRemnant", owner => {
                owner.AddCollider(new CircleCollider(owner, context.GameParameters["medium_rock_remnant.radius"]));
                owner.AddComponent(new StaticObstacle(owner)); owner.AddComponent(new TimedSelfDestroyer(owner, 20)); owner.Elements.AddNative(Element.Rock);
            });
            context.RegisterPrefab("GroundTower", owner => {
                var p = context.GameParameters;
                owner.AddComponentImmediately(new RigidBody(owner, (int)(p["ground_tower.mass"].RawValue / (1L << 32)))); owner.AddCollider(new CircleCollider(owner, p["ground_tower.radius"]));
                owner.AddComponent(new DummyMob(owner, (int)(p["ground_tower.hp"].RawValue / (1L << 32))));
                owner.AddComponentImmediately(new Tower(owner, (int)(p["ground_tower.damage"].RawValue / (1L << 32)), 2, p["ground_tower.attack_interval"], p["ground_tower.attack_range"]));
                owner.AddComponent(new TimedSelfDestroyer(owner, p["ground_tower.duration"])); owner.AddComponent(new RockDeathRemnant(owner));
                owner.Elements.AddNative(Element.Rock); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
