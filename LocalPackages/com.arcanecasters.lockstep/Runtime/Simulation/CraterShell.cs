using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class CraterShell : SimComponent {
        private readonly Fix64 speed, radius;
        private Vector3 origin;
        private Vector3? impact;
        private Hit hit;
        private Fix64 duration, elapsed;
        private bool spent;
        public CraterShell(SimObject owner, Fix64 speed, Fix64 radius) : base(owner) { this.speed = speed; this.radius = radius; }
        public void Launch(Vector3 target, int damage, SimObject shooter) {
            origin = Owner.Position; impact = GroundVector.Of(target);
            duration = MathHelper.Max((Fix64)0.1m, Vector3.Distance(GroundVector.Of(origin), impact.Value) / MathHelper.Max((Fix64)0.1m, speed)); hit = new Hit(damage, shooter.Id, shooter.Elements.Total);
        }
        public override void Update() {
            if (!impact.HasValue || spent) return;
            elapsed += Context.DeltaTime; var progress = MathHelper.Clamp(elapsed / duration, 0, 1);
            var position = origin + (impact.Value - origin) * progress; position.Y += 10 * progress * (1 - progress); Owner.SetPosition(position);
            if (progress < 1) return;
            spent = true;
            foreach (var enemy in SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius)) {
                if (!enemy.IsActive || enemy.IsDying || !TargetRelation.CanAttack(Owner, enemy) || (CombatRange.TargetMask(enemy.Position) & 1) == 0) continue;
                foreach (var receiver in enemy.GetComponents<IDamageable>()) receiver.OnDamaged(hit);
            }
            Context.Spawn(Owner.Master, "CraterExplosion", impact.Value); Owner.Destroy();
        }
    }
    public static class CraterShellPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("CraterExplosion", owner => owner.AddComponent(new TimedSelfDestroyer(owner, (Fix64)0.6m)));
            context.RegisterPrefab("CraterShell", owner => {
                owner.Elements.AddNative(Element.Fire); owner.Elements.AddNative(Element.Rock);
                owner.AddComponent(new CraterShell(owner, context.GameParameters["crater_shell.speed"], context.GameParameters["crater_shell.radius"]));
            });
        }
    }
}
