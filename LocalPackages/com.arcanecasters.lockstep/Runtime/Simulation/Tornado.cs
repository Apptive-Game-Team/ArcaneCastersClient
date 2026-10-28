using System;
using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class Tornado : SimComponent, ICollidable {
        private sealed class Victim { public SimObject Object; public Fix64 Angle, Orbit; }
        private readonly List<Victim> victims = new List<Victim>();
        private readonly int damage;
        private readonly Fix64 speed, interval;
        private Fix64 timer;
        public Vector3? Direction { get; private set; }
        public Tornado(SimObject owner, Fix64 speed, int damage, Fix64 radius, Fix64 interval) : base(owner) { this.speed = speed; this.damage = damage; this.interval = interval; SetTarget(new Vector3(9, 0, 5)); }
        public void SetTarget(Vector3 target) {
            var offset = target - Owner.Position; var length = offset.Length(); Direction = length == Fix64.Zero ? Vector3.Zero : new Vector3(offset.X / length, offset.Y / length, offset.Z / length);
        }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) {
            if (!other.HasComponent<IDamageable>() || victims.Exists(v => ReferenceEquals(v.Object, other))) return;
            var offset = Owner.Position - other.Position;
            victims.Add(new Victim { Object = other, Angle = Fix64.Atan2(offset.Z, offset.X), Orbit = MathHelper.Max((Fix64)0.1m, offset.Length()) });
        }
        public void ApplyDamage() {
            var snapshot = victims.ToArray(); var amount = damage + 2 * snapshot.Length;
            foreach (var victim in snapshot) {
                var receiver = victim.Object.GetComponent<IDamageable>() ?? throw new InvalidOperationException("NullPointerException: a tornado victim has no Damageable");
                receiver.OnDamaged(new Hit(amount, Owner.Id, Element.Wind));
            }
        }
        public override void Update() {
            if (!Direction.HasValue) return;
            var dt = Context.DeltaTime; Owner.SetPosition(Owner.Position + Direction.Value * (speed * dt)); var center = Owner.Position;
            int count = victims.Count;
            for (int i = 0; i < count; i++) {
                var victim = victims[i]; var angle = victim.Angle - Fix64.Pi * 2 * dt; var y = victim.Object.Position.Y;
                if (y < 5) y = MathHelper.Min(5, y + 8 * dt);
                victim.Object.SetPosition(new Vector3(center.X + Fix64.Cos(angle) * victim.Orbit, y, center.Z + Fix64.Sin(angle) * victim.Orbit)); victim.Angle = angle;
            }
            timer += dt; if (timer >= interval) { ApplyDamage(); timer = IntervalTimer.CarryOver(timer, interval, dt); }
        }
    }
    public static class SpecialSpawnPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("EarthCall", owner => { owner.AddComponent(new TimedSelfDestroyer(owner, 1)); owner.Elements.AddNative(Element.Rock); });
            context.RegisterPrefab("TornadoStrike", owner => {
                var p = context.GameParameters; var radius = p["tornado_strike.radius"]; owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Nature); owner.Elements.AddNative(Element.Wind);
                owner.AddComponentImmediately(new Tornado(owner, p["tornado_strike.speed"], (int)(p["tornado_strike.damage"].RawValue / (1L << 32)), radius, p["tornado_strike.attack_interval"])); owner.AddComponentImmediately(new TimedSelfDestroyer(owner, p["tornado_strike.duration"]));
            });
        }
    }
}
