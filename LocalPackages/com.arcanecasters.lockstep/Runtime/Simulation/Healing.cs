using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class Totem : MobComponent, IGaugeComponent, IFrenzyImmune, IMindControlImmune {
        private readonly Hit hit;
        private readonly Fix64 cooldown, range;
        private Fix64 timer;
        public Totem(SimObject owner, int hp, int damage, Fix64 interval, Fix64 range, int targetMask) : base(owner, hp, 0) {
            hit = new Hit(damage, owner.Id, Element.Nature); cooldown = interval; this.range = range;
        }
        public override void OnDeath() { Owner.Destroy(); }
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
        public override void Update() {
            base.Update(); timer += Context.DeltaTime; if (timer < cooldown) return;
            timer = IntervalTimer.CarryOver(timer, cooldown, Context.DeltaTime);
            foreach (var target in Context.Objects) {
                // 원본은 저장한 targetMask를 사용하지 않으며 자신도 회복 대상이다.
                if (!target.IsActive || target.Master != Owner.Master || !CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero, range)) continue;
                target.GetComponent<MobComponent>()?.ApplyDamage(hit);
            }
            Owner.SetStatus(ObjectStatus.Attack);
        }
    }
    internal sealed class SelfAttackTimer {
        private readonly SimObject owner;
        private readonly Hit hit;
        private readonly Fix64 interval;
        private Fix64 counter;
        private MobComponent mob;
        public SelfAttackTimer(SimObject owner, Hit hit, Fix64 interval) { this.owner = owner; this.hit = hit; this.interval = interval; }
        public void Start() { mob = owner.GetComponent<MobComponent>(); }
        public void Update() {
            counter += owner.Context.DeltaTime; if (counter <= interval) return;
            counter = IntervalTimer.CarryOver(counter, interval, owner.Context.DeltaTime);
            if (mob == null) throw new InvalidOperationException("self attacker has no mob");
            mob.OnDamaged(hit);
        }
    }
    public sealed class SelfAttacker : SimComponent {
        private readonly SelfAttackTimer timer;
        public SelfAttacker(SimObject owner, Hit hit, Fix64 interval) : base(owner) { timer = new SelfAttackTimer(owner, hit, interval); }
        public override void Start() { timer.Start(); }
        public override void Update() { timer.Update(); }
    }
    public sealed class SelfHealer : SimComponent {
        private readonly SelfAttackTimer timer;
        public SelfHealer(SimObject owner, int amount, Fix64 interval, Element element) : base(owner) { timer = new SelfAttackTimer(owner, new Hit(-amount, 0, element), interval); }
        public override void Start() { timer.Start(); }
        public override void Update() { timer.Update(); }
    }
}
