using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public static class DropMagics {
        public static MagicImplementation Create(string prefab, Fix64? initialHeight = null) => new MagicImplementation((context, master, origin, position) => {
            position.Y = initialHeight ?? (Fix64)10; context.Spawn(master, prefab, position);
        });
        public static void Register(MagicRegistry registry) {
            registry.Register("fire_drop", Create("FireDrop"));
            registry.Register("frenzy_totem", Create("FrenzyTotem"));
            registry.Register("leafair", Create("Leafair"));
            registry.Register("lightning_drop", Create("LightningCloud", 0));
            registry.Register("meteor_shower", Create("MeteorShower"));
            registry.Register("nature_drop", Create("NatureDrop"));
            registry.Register("rallying_totem", Create("RallyingTotem"));
            registry.Register("rock_drop", Create("RockDrop"));
            registry.Register("wind_drop", Create("WindDrop"));
            registry.Register("chicken_commando", new MagicImplementation((context, master, origin, position) => {
                position.Y = context.GameParameters["chicken_commando.spawn_height"]; context.Spawn(master, "ChickenCommando", position);
            }));
        }
    }
    public sealed class FrenzyTotem : SimComponent, ICollidable {
        private readonly Fix64 speed, range, duration;
        public bool Triggered { get; private set; }
        public FrenzyTotem(SimObject owner, Fix64 speed, Fix64 range, Fix64 duration) : base(owner) { this.speed = speed; this.range = range; this.duration = duration; }
        public override void Update() {
            Owner.SetPosition(Owner.Position + new Vector3(0, -speed * Context.DeltaTime, 0));
            if (Owner.Position.Y < 0) Trigger();
        }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) { Trigger(); }
        private void Trigger() {
            if (Triggered) return; Triggered = true; Owner.SetStatus(ObjectStatus.Attack);
            foreach (var target in Context.Objects) {
                if (!target.IsActive || !CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero, range)) continue;
                target.GetComponent<IEffectReceiver>()?.Receive(VisualEffect.Frenzy, duration);
            }
            Owner.Destroy();
        }
    }
    public static class SpecialDropPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("FrenzyTotem", owner => {
                owner.AddCollider(new CircleCollider(owner, context.GameParameters["frenzy_totem.radius"], true)); owner.Elements.AddNative(Element.None);
                owner.AddComponentImmediately(new FrenzyTotem(owner, context.GameParameters["frenzy_totem.speed"], context.GameParameters["frenzy_totem.attack_range"], context.GameParameters["frenzy_totem.buff_duration"]));
            });
            context.RegisterPrefab("MeteorShower", owner => {
                owner.Elements.AddNative(Element.Fire); owner.Elements.AddNative(Element.Rock);
                // 원본 duration/radius는 정수 매개변수다.
                var duration = (Fix64)(context.GameParameters["meteor_shower.duration"].RawValue / (1L << 32));
                var radius = (Fix64)(context.GameParameters["meteor_shower.radius"].RawValue / (1L << 32));
                owner.AddComponentImmediately(new RandomAreaSpawner(owner, "MeteorDrop", duration, context.GameParameters["meteor_shower.attack_interval"], radius));
            });
        }
    }
}
