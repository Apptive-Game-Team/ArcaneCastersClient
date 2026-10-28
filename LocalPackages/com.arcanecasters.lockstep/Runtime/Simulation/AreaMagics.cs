using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class OvergrowthExplosion : SimComponent {
        private readonly Fix64 radius;
        private readonly int quantity, damage;
        private Fix64 counter;
        private bool running = true;
        public OvergrowthExplosion(SimObject owner, Fix64 radius, int quantity, int damage) : base(owner) { this.radius = radius; this.quantity = Math.Max(1, quantity); this.damage = damage; }
        public override void Update() {
            if (!running) return;
            if (counter < (Fix64)0.5m) { counter += Context.DeltaTime; return; }
            if (Owner.Master != Master.None) {
                var hit = new Hit(damage, Owner.Id, Owner.Elements.Total);
                foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius)) {
                    if (ReferenceEquals(target, Owner) || target.IsDestroyed || target.Master == Owner.Master || target.Master == Master.None) continue;
                    var receivers = target.GetComponents<IDamageable>(); if (receivers.Length == 0) continue;
                    target.SetStatus(ObjectStatus.Damaged); foreach (var receiver in receivers) receiver.OnDamaged(hit);
                    target.GetComponent<IEffectReceiver>()?.Receive(VisualEffect.Snared);
                }
            }
            if (SeedSpiritEvolver.EvolveAlliedSeedSpirits(Owner, radius, "TreeGolem") == 0) {
                var center = (Fix64)(quantity - 1) / 2;
                for (int i = 0; i < quantity; i++) Context.Spawn(Owner.Master, "SeedSpirit", Owner.Position + new Vector3(((Fix64)i - center) / 2, 0, 0));
            }
            running = false; Owner.Destroy();
        }
    }
    public sealed class RazorGale : SimComponent {
        private readonly Fix64 radius, interval;
        private readonly Hit hit;
        private Fix64 counter;
        public RazorGale(SimObject owner, int damage, Fix64 radius, Fix64 interval) : base(owner) { this.radius = radius; this.interval = interval; hit = new Hit(damage, owner.Id, owner.Elements.Total); }
        public override void Update() {
            counter += Context.DeltaTime; if (counter < interval) return; counter = IntervalTimer.CarryOver(counter, interval, Context.DeltaTime);
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius)) {
                if (ReferenceEquals(target, Owner)) continue;
                var receivers = target.GetComponents<IDamageable>(); if (receivers.Length == 0) continue;
                target.SetStatus(ObjectStatus.Damaged); foreach (var receiver in receivers) receiver.OnDamaged(hit);
            }
        }
    }
    public static class AreaMagics {
        public static MagicImplementation Create(string prefab, Func<Master, Master> getMaster = null) => new MagicImplementation((context, master, origin, aim) => context.Spawn(getMaster == null ? master : getMaster(master), prefab, aim), isExplosionMagic: true);
        public static void Register(MagicRegistry registry) {
            registry.Register("lightning_explosion", Create("ElectricExplode")); registry.Register("magma_explosion", Create("MagmaExplosion"));
            registry.Register("overgrowth", Create("Overgrowth")); registry.Register("razor_gale", Create("RazorGale")); registry.Register("sand_storm", Create("SandStorm"));
            registry.Register("shock_overload", Create("ShockOverload")); registry.Register("water_explosion", Create("WaterExplosion")); registry.Register("wind_explosion", Create("WindExplode", _ => Master.None));
            RemnantAndVineMagics.Register(registry);
        }
        public static void RegisterPrefabs(SimulationContext context) {
            context.RegisterPrefab("MagmaExplosion", owner => {
                var p = context.GameParameters; var radius = p["magma_explosion.radius"]; owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Fire);
                owner.AddComponentImmediately(new EffectProvider(owner, VisualEffect.Burn)); owner.AddComponentImmediately(new Explode(owner, Integer(context, "magma_explosion.damage"), radius));
            });
            context.RegisterPrefab("WaterExplosion", owner => {
                var p = context.GameParameters; var radius = p["water_explosion.radius"]; owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Water);
                // 원본 WaterExplosion은 Burn 공급자를 사용한다.
                owner.AddComponent(new EffectProvider(owner, VisualEffect.Burn)); owner.AddComponent(new WaterExplode(owner, Integer(context, "water_explosion.damage"), radius, p["water_explosion.z_force"]));
            });
            context.RegisterPrefab("Overgrowth", owner => {
                var radius = context.GameParameters["overgrowth.radius"]; owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Nature);
                owner.AddComponent(new OvergrowthExplosion(owner, radius, Integer(context, "overgrowth.quantity"), Integer(context, "overgrowth.damage")));
            });
            context.RegisterPrefab("RazorGale", owner => {
                var p = context.GameParameters; var radius = p["razor_gale.radius"]; owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Wind);
                owner.AddComponent(new RazorGale(owner, Integer(context, "razor_gale.damage"), radius, p["razor_gale.attack_interval"])); owner.AddComponent(new TimedSelfDestroyer(owner, p["razor_gale.duration"]));
            });
            context.RegisterPrefab("SandStorm", owner => {
                var p = context.GameParameters; owner.AddCollider(new CircleCollider(owner, p["sand_storm.radius"], true)); owner.Elements.AddNative(Element.Rock); owner.Elements.AddNative(Element.Wind);
                owner.AddComponentImmediately(new EffectProvider(owner, VisualEffect.Sandstorm)); owner.AddComponentImmediately(new TimedSelfDestroyer(owner, p["sand_storm.duration"]));
            });
        }
        private static int Integer(SimulationContext c, string key) => (int)(c.GameParameters[key].RawValue / (1L << 32));
    }
}
