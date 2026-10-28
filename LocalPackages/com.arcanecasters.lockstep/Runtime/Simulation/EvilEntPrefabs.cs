using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class PveEvilEntMob : EvilEntMob, IPveObjectiveTarget {
        private Fix64 retargetTimer;
        public override bool IsObjectiveTarget => true;
        public override bool LeavesDeathField => false;
        public bool IsTerminal => Owner.IsDestroyed || Hp <= 0;
        public PveEvilEntMob(SimObject owner, int hp, int mask, int damage, Fix64 interval, Fix64 range, Fix64 projectileSpeed, int subDamage, Fix64 subRange, Fix64 subInterval, Fix64 massLimit)
            : base(owner, hp, 0, mask, damage, interval, range, projectileSpeed, subDamage, subRange, subInterval, massLimit) { }
        public override void Update() {
            if (Target != null && !IsInGrabSequence) {
                retargetTimer += Context.DeltaTime;
                if (retargetTimer >= 1) {
                    retargetTimer = 0; var closest = Detector.Detect(Owner);
                    if (closest != null && !ReferenceEquals(closest, Target) && IsValidTarget(closest)) { Target = closest; TargetRadius = closest.Colliders.FirstOrDefault(c => c.CircleRadius.HasValue)?.CircleRadius ?? Fix64.Zero; }
                }
            }
            base.Update();
        }
    }
    public static class EvilEntPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("EvilEnt", owner => Initialize(owner, false)); context.RegisterPrefab("PveEvilEnt", owner => Initialize(owner, true));
        }
        private static void Initialize(SimObject owner, bool boss) {
            var p = owner.Context.GameParameters; string key = boss ? "pve_evil_ent" : "evil_ent"; Fix64 Value(string name) => p[key + "." + name]; int Integer(string name) => (int)(Value(name).RawValue / (1L << 32));
            owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, Value("radius"), boss));
            if (boss) { owner.Elements.AddNative(Element.Nature); owner.Elements.AddNative(Element.Fire); owner.AddComponent(new CommonEffectReceiver(owner)); }
            EvilEntMob mob = boss ? new PveEvilEntMob(owner, Integer("hp"), 3, Integer("damage"), Value("attack_interval"), Value("attack_range"), Value("projectile_speed"), Integer("sub_damage"), Value("sub_attack_range"), Value("sub_attack_interval"), Value("pull_mass_limit"))
                : new EvilEntMob(owner, Integer("hp"), Value("speed"), 3, Integer("damage"), Value("attack_interval"), Value("attack_range"), Value("projectile_speed"), Integer("sub_damage"), Value("sub_attack_range"), Value("sub_attack_interval"), Value("pull_mass_limit"));
            owner.AddComponent(mob);
            if (!boss) { owner.Elements.AddNative(Element.Nature); owner.Elements.AddNative(Element.Fire); owner.AddComponent(new CommonEffectReceiver(owner)); }
        }
    }
}
