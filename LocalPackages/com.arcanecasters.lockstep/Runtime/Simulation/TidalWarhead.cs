using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class TidalWarheadProjectile : SimComponent, ICollidable {
        private readonly int damage;
        private readonly Fix64 speed, radius;
        private SimObject target;
        private Vector3? lastPosition;
        private bool aerial, exploded;
        public TidalWarheadProjectile(SimObject owner, int damage, Fix64 speed, Fix64 radius) : base(owner) { this.damage = damage; this.speed = speed; this.radius = radius; }
        public void SetTarget(SimObject value, bool targetsAir) { target = value; lastPosition = value.Position; aerial = targetsAir; }
        private void Explode() {
            if (exploded) return;
            exploded = true; Owner.SetStatus(ObjectStatus.Attack); Context.Spawn(Owner.Master, "TidalWarheadExplosion", Owner.Position);
            var hit = new Hit(aerial ? damage : StormStagMob.JavaRound((Fix64)damage / 2), Owner.Id, Element.Water);
            foreach (var candidate in SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius)) {
                if (!candidate.IsActive || !TargetRelation.CanAttack(Owner, candidate)) continue;
                var receivers = candidate.GetComponents<IDamageable>(); if (receivers.Length == 0) continue;
                candidate.SetStatus(ObjectStatus.Damaged); foreach (var receiver in receivers) receiver.OnDamaged(hit);
            }
            Owner.Destroy();
        }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) { if (!exploded && other.HasComponent<IDamageable>()) Explode(); }
        public override void Update() {
            if (!lastPosition.HasValue || exploded) return;
            if (target != null && target.IsActive) lastPosition = target.Position;
            var offset = lastPosition.Value - Owner.Position; var travel = speed * Context.DeltaTime;
            if (offset.Length() <= travel) { Owner.SetPosition(lastPosition.Value); Explode(); return; }
            Owner.SetPosition(Owner.Position + VineMagics.Normalize(offset) * travel);
        }
    }
    public static class TidalWarheadMagic {
        public static SimObject FindClosestTarget(SimulationContext context, SimObject player, Vector3 aim, int mask) => context.Objects
            .Where(o => o.IsActive && TargetRelation.CanAttack(player, o) && o.HasComponent<IDamageable>() && (CombatRange.TargetMask(o.Position) & mask) != 0)
            .OrderBy(o => Vector3.Distance(o.Position, aim)).FirstOrDefault();
        public static SimObject SelectTarget(SimulationContext context, SimObject player, Vector3 aim) => FindClosestTarget(context, player, aim, 2) ?? FindClosestTarget(context, player, aim, 1);
        public static void Register(MagicRegistry registry) {
            registry.Register("tidal_warhead", new MagicImplementation((context, master, origin, aim) => {
                var player = context.Objects.FirstOrDefault(o => o.IsActive && o.Master == master && o.Prefab == "Player"); if (player == null) return;
                var target = SelectTarget(context, player, aim); if (target == null) return;
                bool aerial = CombatRange.TargetMask(target.Position) == 2; var missile = context.Spawn(master, aerial ? "TidalWarhead" : "GroundTidalWarhead", player.Position);
                var flight = missile.GetComponent<TidalWarheadProjectile>() ?? throw new System.InvalidOperationException("TidalWarhead prefab has no TidalWarheadProjectile component"); flight.SetTarget(target, aerial);
            }));
        }
        public static void RegisterPrefabs(SimulationContext context) {
            foreach (var name in new[] { "TidalWarhead", "GroundTidalWarhead" }) context.RegisterPrefab(name, owner => {
                var p = context.GameParameters; owner.AddCollider(new CircleCollider(owner, (Fix64)0.25m, true)); owner.Elements.AddNative(Element.Water);
                owner.AddComponent(new TidalWarheadProjectile(owner, (int)(p["tidal_warhead.damage"].RawValue / (1L << 32)), p["tidal_warhead.speed"], p["tidal_warhead.radius"]));
            });
            context.RegisterPrefab("TidalWarheadExplosion", owner => owner.AddComponent(new TimedSelfDestroyer(owner, (Fix64)0.6m)));
        }
    }
}
