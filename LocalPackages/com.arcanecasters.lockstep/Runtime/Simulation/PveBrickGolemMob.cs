using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class PveBrickGolemMob : BehaviorMob, IPveObjectiveTarget, IGaugeComponent {
        private readonly int damage, subDamage;
        private readonly Fix64 effectRadius, subInterval, subRange;
        private Fix64 slamTimer, sweepTimer;
        private readonly List<KeyValuePair<Vector3, Fix64>> pending = new List<KeyValuePair<Vector3, Fix64>>();
        public override bool IsObjectiveTarget => true;
        public override bool LeavesDeathField => false;
        public bool IsTerminal => Owner.IsDestroyed || Hp <= 0;
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
        public PveBrickGolemMob(SimObject owner, int hp, int damage, Fix64 interval, Fix64 range, Fix64 effectRadius, int subDamage, Fix64 subInterval, Fix64 subRange)
            : base(owner, hp, 0, 1, interval, range, null) { this.damage = damage; this.effectRadius = effectRadius; this.subDamage = subDamage; this.subInterval = subInterval; this.subRange = subRange; LockState(); }
        public override void Start() { }
        private static Vector3 Ground(Vector3 p) => new Vector3(p.X, 0, p.Z);
        private static Vector3 Normalize(Vector3 p) { var length = p.Length(); return length == 0 ? Vector3.Zero : new Vector3(p.X / length, p.Y / length, p.Z / length); }
        private SimObject Closest(Fix64 range) => Detector.Detect(Owner, other => CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, other.Position, other.FirstCircleRadius(false) ?? Fix64.Zero, range));
        private MobComponent[] Enemies(Vector3 center, Fix64 radius) => SimulationQueries.OverlapSphereAll(Context, center, radius).Where(o => TargetRelation.CanAttack(Owner, o)).Select(o => o.GetComponent<MobComponent>()).Where(m => m != null).ToArray();
        private void Project(string type, Vector3 start, Vector3 end, Fix64 duration) { Context.AddEvent(new BattleEvent(type, start, end, duration)); Owner.SetStatus(ObjectStatus.Attack); }
        public override void Update() {
            base.Update(); if (IsTerminal) return; var dt = Context.DeltaTime; var landed = new List<Vector3>();
            for (int i = 0; i < pending.Count;) { var item = pending[i]; var remaining = item.Value - dt; if (remaining <= 0) { landed.Add(item.Key); pending.RemoveAt(i); } else { pending[i] = new KeyValuePair<Vector3, Fix64>(item.Key, remaining); i++; } }
            foreach (var spot in landed) { Project("BrickGolemSlam", Owner.Position, spot, (Fix64)0.3m); var hit = new Hit(damage, Owner.Id, Owner.Elements.Total); foreach (var mob in Enemies(spot, effectRadius)) mob.OnDamaged(hit); }
            slamTimer += dt;
            if (slamTimer >= AttackInterval.Total) { var target = Closest(AttackRange); if (target != null) { slamTimer = IntervalTimer.CarryOver(slamTimer, AttackInterval.Total, dt); var spot = Ground(target.Position); Project("BrickGolemSlamWarning", spot, spot, 1); pending.Add(new KeyValuePair<Vector3, Fix64>(spot, 1)); } }
            sweepTimer += dt;
            if (sweepTimer >= subInterval) { var target = Closest(subRange); if (target != null) { sweepTimer = IntervalTimer.CarryOver(sweepTimer, subInterval, dt); Sweep(target); } }
        }
        private void Sweep(SimObject target) {
            var direction = Normalize(Ground(target.Position) - Ground(Owner.Position)); if (direction == Vector3.Zero) direction = Vector3.Right;
            Project("BrickGolemSweep", Owner.Position, Ground(Owner.Position) + direction * subRange, (Fix64)0.4m);
            var hit = new Hit(subDamage, Owner.Id, Owner.Elements.Total);
            foreach (var mob in Enemies(Owner.Position, subRange)) {
                var toward = Normalize(Ground(mob.Owner.Position) - Ground(Owner.Position)); bool center = toward == Vector3.Zero;
                if (!center && Vector3.Dot(toward, direction) < (Fix64)0.5m) continue;
                mob.OnDamaged(hit); mob.Owner.GetComponent<IEffectReceiver>()?.ReceiveDirectional(VisualEffect.Knockback, center ? direction : toward, (Fix64)0.75m);
            }
        }
        public static void RegisterPrefabs(SimulationContext context) {
            context.RegisterPrefab("PveBrickGolem", owner => {
                Fix64 Value(string key) => context.GameParameters["pve_brick_golem." + key]; int Integer(string key) => (int)(Value(key).RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, Value("radius"), true)); owner.Elements.AddNative(Element.Rock); owner.AddComponent(new CommonEffectReceiver(owner));
                owner.AddComponent(new PveBrickGolemMob(owner, Integer("hp"), Integer("damage"), Value("attack_interval"), Value("attack_range"), Value("effect_radius"), Integer("sub_damage"), Value("sub_attack_interval"), Value("sub_attack_range")));
            });
        }
    }
}
