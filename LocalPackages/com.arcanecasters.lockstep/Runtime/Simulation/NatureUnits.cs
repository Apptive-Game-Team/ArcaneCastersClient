using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class Slime : MeleeAttackMob {
        public Slime(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval) : base(owner, hp, speed, mask, damage, interval) { }
    }
    public sealed class PathSpawner : SimComponent {
        private readonly string prefab;
        private readonly Fix64 interval;
        private Vector3 last;
        public PathSpawner(SimObject owner, string prefab, Fix64 interval) : base(owner) { this.prefab = prefab; this.interval = interval; last = owner.Position; }
        public void MoveWithUnmovedObject(Vector3 previous, Vector3 position) { if (last == previous) last = position; }
        public override void Update() { if (Vector3.Distance(Owner.Position, last) < interval) return; last = Owner.Position; Context.Spawn(Master.None, prefab, Owner.Position); }
    }
    public sealed class LeafFieldEffectReceiver : SimComponent, IEffectReceiver, ICollidable {
        public LeafFieldEffectReceiver(SimObject owner) : base(owner) { }
        public void Receive(VisualEffect effect) { if (effect != VisualEffect.Burn) return; Context.Spawn(Master.None, "FireField", Owner.Position); Owner.Destroy(); }
        public void Receive(EffectApplication application) { Receive(application.Effect); }
        public void Receive(VisualEffect effect, Fix64 duration) { Receive(effect); }
        public void ReceiveDirectional(VisualEffect effect, Vector3 direction, Fix64 proximity) { }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) { }
    }
    public static class FieldPrefabs {
        public static void RegisterElectricField(SimulationContext context) { context.RegisterPrefab("ElectricField", owner => Initialize(owner, "electric_field", Element.Lightning, VisualEffect.Shock)); }
        public static void Initialize(SimObject owner, string key, Element element, VisualEffect effect, Action<SimObject> extra = null) {
            owner.SetPosition(GroundVector.Of(owner.Position));
            owner.AddCollider(new CircleCollider(owner, owner.Context.GameParameters[key + ".radius"], true)); owner.Elements.AddNative(element);
            owner.AddComponentImmediately(new EffectProvider(owner, effect)); extra?.Invoke(owner);
            owner.AddComponentImmediately(new TimedSelfDestroyer(owner, owner.Context.GameParameters[key + ".duration"]));
        }
        public static void RegisterNatureFields(SimulationContext context) {
            context.RegisterPrefab("FireField", owner => Initialize(owner, "fire_field", Element.Fire, VisualEffect.Burn));
            context.RegisterPrefab("LeafField", owner => Initialize(owner, "leaf_field", Element.Nature, VisualEffect.Snared, field => {
                field.AddComponentImmediately(new EffectProvider(field, VisualEffect.LeafFieldHeal)); field.AddComponentImmediately(new LeafFieldEffectReceiver(field));
            }));
        }
    }
    public static class NatureUnitPrefabs {
        private static Fix64 Value(SimObject owner, string key, string name) => owner.Context.GameParameters[key + "." + name];
        private static int Integer(SimObject owner, string key, string name) => (int)(Value(owner, key, name).RawValue / (1L << 32));
        private static void Body(SimObject owner, string key) {
            owner.AddComponentImmediately(new RigidBody(owner, Integer(owner, key, "mass"))); owner.AddComponentImmediately(new ZPhysics(owner));
            owner.AddCollider(new CircleCollider(owner, Value(owner, key, "radius")));
        }
        private static void LeafSlime(SimObject owner, string key) {
            Body(owner, key); owner.AddComponentImmediately(new Slime(owner, Integer(owner, key, "hp"), Value(owner, key, "speed"), 1, Integer(owner, key, "damage"), Value(owner, key, "attack_interval")));
            owner.AddComponentImmediately(new PathSpawner(owner, "LeafField", 1)); owner.Elements.AddNative(Element.Nature); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
        }
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("RepairTotem", owner => {
                const string key = "repair_totem"; owner.AddComponent(new RigidBody(owner, Integer(owner, key, "mass")));
                owner.AddCollider(new CircleCollider(owner, Value(owner, key, "radius")));
                owner.AddComponent(new DummyMob(owner, Integer(owner, key, "hp"))); owner.AddComponent(new RepairAura(owner, Value(owner, key, "effect_radius")));
                owner.AddComponent(new TimedSelfDestroyer(owner, Value(owner, key, "duration"))); owner.AddComponent(new BuildingEffectReceiver(owner)); owner.Elements.AddNative(Element.Nature);
            });
            context.RegisterPrefab("LifeTree", owner => {
                const string key = "life_tree"; owner.AddComponentImmediately(new RigidBody(owner, Integer(owner, key, "mass")));
                owner.AddCollider(new CircleCollider(owner, Value(owner, key, "radius"), true));
                owner.AddComponentImmediately(new Totem(owner, Integer(owner, key, "hp"), Integer(owner, key, "damage"), Value(owner, key, "attack_interval"), Value(owner, key, "effect_radius"), 1));
                owner.Elements.AddNative(Element.Nature); owner.AddComponentImmediately(new TimedSelfDestroyer(owner, Integer(owner, key, "duration")));
                owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
            context.RegisterPrefab("SeedNest", owner => {
                const string key = "seed_nest"; owner.AddCollider(new CircleCollider(owner, Value(owner, key, "radius"))); owner.Elements.AddNative(Element.Nature);
                owner.AddComponent(new Spawner(owner, Integer(owner, key, "hp"), "SeedSpirit", Value(owner, key, "attack_interval"), true, 1));
                owner.AddComponent(new TimedSelfDestroyer(owner, Value(owner, key, "duration"))); owner.AddComponent(new CommonEffectReceiver(owner));
            });
            context.RegisterPrefab("LeafSlime", owner => LeafSlime(owner, "leaf_slime"));
            context.RegisterPrefab("SeedSpirit", owner => LeafSlime(owner, "seed_spirit"));
            context.RegisterPrefab("VineSpirit", owner => {
                const string key = "vine_spirit"; Body(owner, key);
                owner.AddComponentImmediately(new EffectProvideProjectileRangeAttackMob(owner, Integer(owner, key, "hp"), Value(owner, key, "speed"), 3, Integer(owner, key, "damage"), Value(owner, key, "attack_interval"), Value(owner, key, "attack_range"), VisualEffect.Snared, "NatureShot", (Fix64)0.5m));
                owner.Elements.AddNative(Element.Nature); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
