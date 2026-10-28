using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class WaterFieldEffectReceiver : SimComponent, IEffectReceiver, ICollidable {
        public WaterFieldEffectReceiver(SimObject owner) : base(owner) { }
        public void Receive(VisualEffect effect) { if (effect != VisualEffect.Shock) return; Context.Spawn(Master.None, "ElectricField", Owner.Position); Owner.Destroy(); }
        public void Receive(EffectApplication application) { Receive(application.Effect); }
        public void Receive(VisualEffect effect, Fix64 duration) { Receive(effect); }
        public void ReceiveDirectional(VisualEffect effect, Vector3 direction, Fix64 proximity) { }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) { }
    }
    public static class SpecialShotPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("WaterField", owner => FieldPrefabs.Initialize(owner, "water_field", Element.Water, VisualEffect.Wet, field => field.AddComponentImmediately(new WaterFieldEffectReceiver(field))));
            context.RegisterPrefab("WindBlade", owner => {
                var p = context.GameParameters; owner.AddCollider(new CircleCollider(owner, p["wind_blade.radius"], true)); owner.Elements.AddNative(Element.Wind);
                owner.AddComponentImmediately(new WindBladeShot(owner, (int)(p["wind_blade.damage"].RawValue / (1L << 32)), p["wind_blade.speed"]));
            });
            context.RegisterPrefab("RockRolling", owner => {
                var p = context.GameParameters; owner.AddCollider(new CircleCollider(owner, p["rock_rolling.radius"], true)); owner.Elements.AddNative(Element.Rock);
                owner.AddComponentImmediately(new RollingRock(owner, (int)(p["rock_rolling.damage"].RawValue / (1L << 32)), p["rock_rolling.speed"], p["rock_rolling.radius"]));
            });
            context.RegisterPrefab("BoulderStrike", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["boulder_strike." + name].RawValue / (1L << 32));
                owner.AddCollider(new CircleCollider(owner, p["boulder_strike.radius"], true)); owner.Elements.AddNative(Element.Rock); owner.Elements.AddNative(Element.Wind);
                owner.AddComponent(new BoulderStrikeShot(owner, Integer("damage"), p["boulder_strike.speed"], Integer("sub_damage"), p["boulder_strike.push_force"], (Fix64)0.65m));
            });
            context.RegisterPrefab("TideCall", owner => {
                var p = context.GameParameters; owner.AddCollider(new CircleCollider(owner, p["tide_call.radius"], true)); owner.Elements.AddNative(Element.Water); owner.AddComponentImmediately(new EffectProvider(owner, VisualEffect.Wet));
                owner.AddComponentImmediately(new PushShot(owner, (int)(p["tide_call.damage"].RawValue / (1L << 32)), p["tide_call.speed"]));
                owner.AddComponentImmediately(new PathSpawner(owner, "WaterField", (Fix64)0.5m)); owner.AddComponentImmediately(new TimedSelfDestroyer(owner, p["tide_call.duration"]));
            });
        }
    }
}
