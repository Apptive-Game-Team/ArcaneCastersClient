using System;
using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class LightningTowerMob : TimedBehaviorMob {
        private readonly int damage, chainDamage, chainCount;
        private readonly Fix64 range, chainRadius;
        private IDetector detector;
        public LightningTowerMob(SimObject owner, int hp, int damage, int chainDamage, int chainCount, Fix64 interval, Fix64 range, Fix64 chainRadius) : base(owner, hp, 0, interval) {
            this.damage = damage; this.chainDamage = chainDamage; this.chainCount = chainCount; this.range = range; this.chainRadius = chainRadius; Behavior = Attack;
        }
        public override void Start() { detector = new ClosestEnemyDetector(Context, 3); }
        public override void OnDeath() { Owner.Destroy(); }
        private bool Attack() {
            if (detector == null) throw new InvalidOperationException("lightning tower attacked before start");
            var target = detector.Detect(Owner, candidate => CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, candidate.Position, candidate.FirstCircleRadius(false) ?? Fix64.Zero, range));
            if (target == null) return false;
            Strike(Owner, target, damage); var hit = new HashSet<SimObject> { target }; var source = target;
            for (int i = 0; i < chainCount; i++) {
                SimObject next = null; Fix64 closest = Fix64.MaxValue;
                foreach (var candidate in SimulationQueries.OverlapSphereAll(Context, source.Position, chainRadius)) {
                    if (!TargetRelation.CanAttack(Owner, candidate) || hit.Contains(candidate) || !candidate.HasComponent<IDamageable>()) continue;
                    var distance = Vector3.Distance(source.Position, candidate.Position);
                    if (next == null || distance < closest) { next = candidate; closest = distance; }
                }
                if (next == null) break;
                Strike(source, next, chainDamage); hit.Add(next); source = next;
            }
            Owner.SetStatus(ObjectStatus.Attack); return true;
        }
        private void Strike(SimObject source, SimObject target, int amount) {
            var duration = (Fix64)0.2m; Context.AddEvent(new BattleEvent("ElectricShot", source.Id, target.Id, duration));
            var hit = new Hit(amount, Owner.Id, Element.Lightning); foreach (var receiver in target.GetComponents<IDamageable>()) receiver.OnDamagedWithDelay(hit, duration);
        }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("ElectricTower", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["electric_tower." + name].RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddCollider(new CircleCollider(owner, p["electric_tower.radius"]));
                owner.AddComponent(new LightningTowerMob(owner, Integer("hp"), Integer("damage"), Integer("chain_damage"), Integer("chain_count"), p["electric_tower.attack_interval"], p["electric_tower.attack_range"], p["electric_tower.chain_radius"]));
                owner.AddComponent(new TimedSelfDestroyer(owner, p["electric_tower.duration"])); owner.Elements.AddNative(Element.Lightning); owner.AddComponent(new BuildingEffectReceiver(owner));
            });
        }
    }
}
