using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class TowerbackMob : BehaviorMob {
        private readonly Fix64 airRange, chaseRange;
        private readonly int airDamage;
        private readonly IDetector airDetector;
        private Fix64 checkTimer;
        public TowerbackMob(SimObject owner, int hp, Fix64 speed, int groundDamage, Fix64 groundRange, int airDamage, Fix64 airRange, Fix64 interval) : base(owner, hp, speed, 3, interval, groundRange, null) {
            this.airRange = airRange; chaseRange = airRange * (Fix64)1.5m; this.airDamage = airDamage;
            airDetector = new ClosestEnemyDetector(Context, 2); Detector = new AirFirstDetector(this);
            Behavior = target => {
                var receiver = target.GetComponent<IDamageable>(); if (receiver == null) return false;
                if (IsAir(target)) {
                    foreach (var victim in SimulationQueries.OverlapSphereAll(Context, target.Position, 1)) {
                        if (TargetRelation.CanAttack(Owner, victim)) victim.GetComponent<IDamageable>()?.OnDamagedWithDelay(new Hit(this.airDamage, Owner.Id, Owner.Elements.Total), (Fix64)0.2m);
                    }
                    Context.AddEvent(new BattleEvent("RockShot", Owner.Id, target.Id, (Fix64)0.2m));
                } else receiver.OnDamaged(new Hit(groundDamage, Owner.Id, Owner.Elements.Total));
                Owner.SetStatus(ObjectStatus.Attack); return true;
            };
        }
        private static bool IsAir(SimObject target) => CombatRange.TargetMask(target.Position) == 2;
        private bool InChaseRange(SimObject target) => CombatRange.HorizontalEdgeDistance(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero) <= chaseRange;
        public override Fix64 AttackRangeFor(SimObject target) => IsAir(target) ? airRange : AttackRange;
        public override bool IsValidTarget(SimObject target) => base.IsValidTarget(target) && (!IsAir(target) || InChaseRange(target));
        public override void Update() {
            checkTimer += Context.DeltaTime;
            if (checkTimer > Fix64.One) {
                checkTimer = Fix64.Zero;
                if (Target != null && CurrentState is AttackState && !IsAir(Target)) {
                    var air = airDetector.Detect(Owner, InChaseRange);
                    if (air != null) { TargetRadius = air.FirstCircleRadius(false) ?? Fix64.Zero; Target = air; SetMove(); }
                }
            }
            base.Update();
        }
        private sealed class AirFirstDetector : IDetector {
            private readonly TowerbackMob mob;
            private readonly IDetector ground;
            public AirFirstDetector(TowerbackMob mob) { this.mob = mob; ground = new ClosestEnemyDetector(mob.Context, 1); }
            public SimObject Detect(SimObject source, Func<SimObject, bool> filter = null) => mob.airDetector.Detect(source, candidate => mob.InChaseRange(candidate) && (filter == null || filter(candidate))) ?? ground.Detect(source, filter);
        }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("Towerback", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["towerback." + name].RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, p["towerback.radius"])); owner.Elements.AddNative(Element.Rock);
                owner.AddComponent(new TowerbackMob(owner, Integer("hp"), p["towerback.sub_speed"], Integer("sub_damage"), p["towerback.sub_attack_range"], Integer("damage"), p["towerback.attack_range"], p["towerback.attack_interval"])); owner.AddComponent(new CommonEffectReceiver(owner));
            });
        }
    }
}
