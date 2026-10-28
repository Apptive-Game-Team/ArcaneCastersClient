using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class PlayerPriorityEnemyDetector : IDetector {
        private readonly IDetector players, closest;
        public PlayerPriorityEnemyDetector(SimulationContext context, int mask) {
            players = new PriorityEnemyDetector(context, mask, Fix64.MaxValue, TargetCategory.Player);
            closest = new ClosestEnemyDetector(context, mask);
        }
        public SimObject Detect(SimObject source, Func<SimObject, bool> filter = null) => players.Detect(source, filter) ?? closest.Detect(source, filter);
    }
    public sealed class PlayerPrioMob : MeleeAttackMob {
        public PlayerPrioMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval) : base(owner, hp, speed, mask, damage, interval) { }
        public override void Start() { base.Start(); Detector = new PlayerPriorityEnemyDetector(Context, TargetMask); }
    }
    public sealed class BuildingPrioMob : MeleeAttackMob {
        public BuildingPrioMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval) : base(owner, hp, speed, mask, damage, interval) { }
        public override void Start() { base.Start(); Detector = new PriorityEnemyDetector(Context, TargetMask, Fix64.MaxValue, TargetCategory.Building, TargetCategory.Player); }
    }
    public static class PriorityUnitPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("ArcaneInfantry", owner => {
                var p = context.GameParameters;
                int Integer(string name) => (int)(p["arcane_infantry." + name].RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner));
                owner.AddCollider(new CircleCollider(owner, p["arcane_infantry.radius"], false));
                owner.AddComponent(new AttackMob(owner, Integer("hp"), p["arcane_infantry.speed"], 1, Integer("damage"), p["arcane_infantry.attack_interval"], p["arcane_infantry.attack_range"]));
                owner.Elements.AddNative(Element.None); owner.AddComponent(new CommonEffectReceiver(owner));
            });
            context.RegisterPrefab("StormRider", owner => Initialize(owner, "storm_rider"));
            context.RegisterPrefab("ZapMouse", owner => Initialize(owner, "zap_mouse"));
            context.RegisterPrefab("TreeGolem", owner => Initialize(owner, "tree_golem"));
        }
        private static void Initialize(SimObject owner, string key) {
            var p = owner.Context.GameParameters;
            int Integer(string name) => (int)(p[key + "." + name].RawValue / (1L << 32));
            bool rider = key == "storm_rider", tree = key == "tree_golem";
            void Add(SimComponent component) { if (rider) owner.AddComponentImmediately(component); else owner.AddComponent(component); }
            Add(new RigidBody(owner, Integer("mass"))); Add(new ZPhysics(owner));
            owner.AddCollider(new CircleCollider(owner, p[key + ".radius"], false));
            int hp = Integer("hp"), damage = Integer("damage"); var speed = p[key + ".speed"]; var interval = p[key + ".attack_interval"];
            if (rider) Add(new PlayerPrioMob(owner, hp, speed, 1, damage, interval));
            else if (tree) Add(new MeleeAttackMob(owner, hp, speed, 1, damage, interval));
            else Add(new BuildingPrioMob(owner, hp, speed, 1, damage, interval));
            if (tree) {
                Add(new SelfHealer(owner, Integer("heal_amount"), p[key + ".heal_interval"], Element.Nature));
                Add(new PathSpawner(owner, "LeafField", Fix64.One));
                owner.Elements.AddNative(Element.Nature);
            } else {
                owner.Elements.AddNative(Element.Lightning);
                if (rider) owner.Elements.AddNative(Element.Water);
                owner.AddComponent(new ElectricDeathEnergy(owner));
            }
            if (key == "zap_mouse") Add(new LightningSummonEffectReceiver(owner));
            else Add(new CommonEffectReceiver(owner));
        }
    }
}
