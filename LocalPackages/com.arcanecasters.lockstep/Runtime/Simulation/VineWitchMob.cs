using System.Collections.Generic;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class VineWitchMob : PveBossMob {
        private readonly IVineFan fan;
        private bool rageActivated;
        private int remaining;
        private Fix64 timer;
        private long positiveSteps;
        public bool HasVineFan => fan != null;
        public VineWitchMob(SimObject owner, int hp, Fix64 speed, int mask, Fix64 interval, Fix64 range, IEnumerable<IBossMagic> magics, IVineFan fan) : base(owner, hp, speed, mask, interval, range, magics) { this.fan = fan; }
        public override void OnDamaged(Hit hit) {
            base.OnDamaged(hit);
            if (!rageActivated && Hp > 0 && Hp <= MaxHp / 2) { rageActivated = true; remaining = 4; timer = (Fix64)0.45m; }
        }
        public override bool CastMagic(SimObject target) {
            if (remaining > 0) {
                if (fan != null) return true;
                remaining = 0;
            }
            return base.CastMagic(target);
        }
        public override void Update() {
            base.Update(); if (remaining <= 0 || fan == null) return;
            var target = Target;
            if (target == null || target.IsDestroyed) {
                target = Detector.Detect(Owner); if (target == null || target.IsDestroyed) return;
                Target = target; TargetRadius = target.Colliders.FirstOrDefault(c => c.CircleRadius.HasValue)?.CircleRadius ?? Fix64.Zero;
            }
            if (!CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero, AttackRange)) return;
            timer += Context.DeltaTime; if (Context.DeltaTime > 0) positiveSteps++;
            var interval = (Fix64)0.45m;
            while (remaining > 0) {
                if (timer < interval && interval.RawValue - timer.RawValue >= positiveSteps) break;
                timer -= interval; fan.Run(Context, Owner.Master, Owner.Position, target.Position); remaining--;
            }
        }
    }
}
