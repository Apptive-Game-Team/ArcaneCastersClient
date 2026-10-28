using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class BombSpriteBomb : SimComponent {
        private readonly int damage;
        private readonly Fix64 fallSpeed, radius;
        private Vector3? start, target;
        private Fix64 duration, elapsed;
        private bool exploded;
        public BombSpriteBomb(SimObject owner, int damage, Fix64 fallSpeed, Fix64 radius) : base(owner) { this.damage = damage; this.fallSpeed = fallSpeed; this.radius = radius; }
        public void SetTarget(Vector3 position) { start = Owner.Position; target = GroundVector.Of(position); duration = MathHelper.Max((Fix64)0.1m, Vector3.Distance(start.Value, target.Value) / fallSpeed); }
        public override void Update() {
            if (!start.HasValue || !target.HasValue || exploded) return;
            elapsed += Context.DeltaTime; var progress = MathHelper.Clamp(elapsed / duration, 0, 1); Owner.SetPosition(SimulationVector.Lerp(start.Value, target.Value, progress));
            if (progress < 1) return;
            exploded = true; Owner.SetStatus(ObjectStatus.Attack); Context.Spawn(Owner.Master, "BombSpriteExplosion", Owner.Position);
            var hit = new Hit(damage, Owner.Id, Owner.Elements.Total);
            foreach (var candidate in SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius)) {
                if (!candidate.IsActive || !TargetRelation.CanAttack(Owner, candidate)) continue;
                var damageables = candidate.GetComponents<IDamageable>(); if (damageables.Length == 0) continue;
                candidate.SetStatus(ObjectStatus.Damaged); foreach (var receiver in damageables) receiver.OnDamaged(hit);
            }
            Owner.Destroy();
        }
    }
    public class BombSpriteMob : BehaviorMob {
        public BombSpriteMob(SimObject owner, int hp, Fix64 speed, Fix64 interval, Fix64 range) : base(owner, hp, speed, 1, interval, range, null, true) {
            Behavior = target => {
                if (!target.IsActive) return false;
                var bomb = Context.Spawn(owner.Master, "BombSpriteBomb", owner.Position);
                var flight = bomb.GetComponent<BombSpriteBomb>() ?? throw new System.InvalidOperationException("BombSpriteBomb prefab has no BombSpriteBomb component");
                flight.SetTarget(GroundVector.Of(target.Position)); owner.SetStatus(ObjectStatus.Attack); return true;
            };
        }
    }
    public static class BombSpritePrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("BombSpriteExplosion", owner => owner.AddComponent(new TimedSelfDestroyer(owner, (Fix64)0.6m)));
            context.RegisterPrefab("BombSpriteBomb", owner => {
                var p = context.GameParameters; owner.Elements.AddNative(Element.Wind);
                owner.AddComponent(new BombSpriteBomb(owner, (int)(p["bomb_sprite_bomb.damage"].RawValue / (1L << 32)), p["bomb_sprite_bomb.speed"], p["bomb_sprite_bomb.radius"]));
            });
            context.RegisterPrefab("BombSprite", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["bomb_sprite." + name].RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner, 3)); owner.AddCollider(new CircleCollider(owner, p["bomb_sprite.radius"]));
                owner.AddComponent(new BombSpriteMob(owner, Integer("hp"), p["bomb_sprite.speed"], p["bomb_sprite.attack_interval"], p["bomb_sprite.attack_range"]));
                owner.AddComponent(new CommonEffectReceiver(owner)); owner.Elements.AddNative(Element.Wind);
            });
        }
    }
}
