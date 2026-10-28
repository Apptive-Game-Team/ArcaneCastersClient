using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class FlameLauncher : SimComponent, IIntervalAttacker {
        public Stat AttackInterval { get; }
        private readonly Fix64 range;
        private Fix64 timer;
        public FlameLauncher(SimObject owner, Fix64 interval, Fix64 range) : base(owner) { AttackInterval = new Stat(interval); this.range = range; }
        public override void Update() {
            timer += Context.DeltaTime; var interval = AttackInterval.Total; if (timer < interval) return;
            timer = IntervalTimer.CarryOver(timer, interval, Context.DeltaTime); if (Owner.Master == Master.None) return;
            var origin = Owner.Position; var forward = Owner.Master == Master.LeftPlayer ? Vector3.Right : Vector3.Left;
            var flame = Context.Spawn(Owner.Master, "DragonFlame", origin); var shot = flame.GetComponent<IShotTarget>();
            if (shot == null) throw new InvalidOperationException("NullPointerException: the DragonFlame prefab has no Shot");
            shot.SetTarget(origin + forward * range); flame.AddComponent(new DistanceSelfDestroyer(flame, origin, range)); Owner.SetStatus(ObjectStatus.Attack);
        }
    }
    public static class DragonTowerPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("DragonTower", owner => {
                var p = context.GameParameters;
                owner.AddComponentImmediately(new RigidBody(owner, (int)(p["dragon_tower.mass"].RawValue / (1L << 32)))); owner.AddCollider(new CircleCollider(owner, p["dragon_tower.radius"]));
                owner.AddComponent(new DummyMob(owner, (int)(p["dragon_tower.hp"].RawValue / (1L << 32)))); owner.AddComponentImmediately(new FlameLauncher(owner, p["dragon_tower.attack_interval"], p["dragon_tower.attack_range"]));
                owner.AddComponent(new TimedSelfDestroyer(owner, p["dragon_tower.duration"])); owner.Elements.AddNative(Element.Fire); owner.AddComponent(new CommonEffectReceiver(owner));
            });
            context.RegisterPrefab("DragonFlame", owner => {
                var p = context.GameParameters; owner.AddCollider(new CircleCollider(owner, p["dragon_flame.radius"], true)); owner.Elements.AddNative(Element.Fire);
                owner.AddComponentImmediately(new EffectProvider(owner, VisualEffect.Burn)); owner.AddComponentImmediately(new Shot(owner, (int)(p["dragon_flame.damage"].RawValue / (1L << 32)), p["dragon_flame.speed"]));
            });
        }
    }
}
