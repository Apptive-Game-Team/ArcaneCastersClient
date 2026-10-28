using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class AllyOverchargeProvider : SimComponent, ICollidable {
        public AllyOverchargeProvider(SimObject owner) : base(owner) { }
        public void OnCollision(SimObject other) { if (EffectApplication.IsFriendly(Owner, other)) LightningSummonEffectReceiver.Overcharge(other); }
        public void OnCollisionWithEnemy(SimObject other) { }
    }
    public static class ShotMagics {
        public static MagicImplementation Create(string prefab, Func<Master, Master> getMaster = null) => new MagicImplementation((context, master, origin, position) => {
            if (!origin.HasValue) return;
            var owner = context.Spawn(getMaster == null ? master : getMaster(master), prefab, origin.Value);
            var shot = owner.GetComponent<IShotTarget>() ?? throw new InvalidOperationException(prefab + " has no Shot component"); shot.SetTarget(position);
        });
        public static void Register(MagicRegistry registry) {
            registry.Register("boulder_strike", Create("BoulderStrike"));
            registry.Register("chain_lightning", Create("ChainLightning"));
            registry.Register("fire_shot", Create("FireShot"));
            registry.Register("lightning_shot", Create("ElectricShot"));
            registry.Register("rock_rolling", Create("RockRolling"));
            registry.Register("tide_call", Create("TideCall"));
            registry.Register("water_shot", Create("WaterShot"));
            registry.Register("wind_blade", Create("WindBlade"));
        }
    }
    public static class BasicShotPrefabs {
        public static void Register(SimulationContext context) {
            Register(context, "FireShot", "fire_shot", Element.Fire, VisualEffect.Burn);
            Register(context, "WaterShot", "water_shot", Element.Water, VisualEffect.Wet);
            Register(context, "ElectricShot", "electric_shot", Element.Lightning, VisualEffect.Shock);
        }
        private static void Register(SimulationContext context, string prefab, string key, Element element, VisualEffect effect) {
            context.RegisterPrefab(prefab, owner => {
                var p = context.GameParameters; owner.AddCollider(new CircleCollider(owner, p[key + ".radius"], true)); owner.Elements.AddNative(element);
                owner.AddComponentImmediately(new EffectProvider(owner, effect)); if (element == Element.Lightning) owner.AddComponentImmediately(new AllyOverchargeProvider(owner));
                owner.AddComponentImmediately(new Shot(owner, (int)(p[key + ".damage"].RawValue / (1L << 32)), p[key + ".speed"]));
            });
        }
    }
}
