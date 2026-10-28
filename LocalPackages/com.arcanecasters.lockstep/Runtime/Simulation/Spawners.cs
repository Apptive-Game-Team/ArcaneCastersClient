using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class CraterSpawner : SimComponent {
        private readonly Fix64 interval;
        private Fix64 elapsed;
        public CraterSpawner(SimObject owner, Fix64 interval) : base(owner) { this.interval = MathHelper.Max((Fix64)0.05m, interval); }
        public override void Update() {
            elapsed += Context.DeltaTime;
            while (elapsed >= interval) { elapsed -= interval; Context.Spawn(Owner.Master, "CraterEmber", Owner.Position); }
        }
    }
    public sealed class Spawner : MobComponent, IGaugeComponent {
        public override bool IsSpawner => true;
        public static readonly Fix64 DefaultBurstSpacing = (Fix64)0.5m;
        private readonly string prefab;
        private readonly Fix64 interval;
        private readonly bool destructible;
        private readonly int count;
        private Fix64 counter;
        public Spawner(SimObject owner, int hp, string prefab) : this(owner, hp, prefab, 2, true, 1) { }
        public Spawner(SimObject owner, int hp, string prefab, Fix64 interval, bool destructible, int count) : base(owner, hp, 0) {
            this.prefab = prefab; this.interval = MathHelper.Max((Fix64)0.1m, interval); this.destructible = destructible; this.count = Math.Max(1, count);
        }
        public override void OnDeath() { }
        public override void OnDamaged(Hit hit) {
            if (!destructible) return;
            Hp -= hit.Damage; Owner.ApplyUpdate(); if (Hp <= 0) Owner.Destroy();
        }
        public Gauge GetGauge() => destructible ? new Gauge(Hp, MaxHp, GaugeCategory.Hp) : new Gauge(0, -1, GaugeCategory.Hp);
        public override void Update() {
            base.Update(); counter += Context.DeltaTime;
            if (counter < interval) return;
            counter = IntervalTimer.CarryOver(counter, interval, Context.DeltaTime);
            var position = Owner.Position; Fix64 center = (Fix64)(count - 1) / 2;
            for (int i = 0; i < count; i++) Context.Spawn(Owner.Master, prefab, position + new Vector3(((Fix64)i - center) * DefaultBurstSpacing, 0, 0));
        }
    }
    public sealed class LimitedSequenceSpawner : SimComponent {
        private readonly Fix64 interval;
        private readonly int maximum;
        private readonly string[] prefabs;
        private Fix64 elapsed;
        private long positiveSteps;
        private int spawned;
        public LimitedSequenceSpawner(SimObject owner, Fix64 interval, int maximum, string[] prefabs, bool immediate = false) : base(owner) {
            this.interval = MathHelper.Max((Fix64)0.1m, interval); this.maximum = maximum; this.prefabs = (string[])prefabs.Clone(); elapsed = immediate ? this.interval : Fix64.Zero;
        }
        public override void Update() {
            if ((maximum > 0 && spawned >= maximum) || prefabs.Length == 0) return;
            elapsed += Context.DeltaTime; if (Context.DeltaTime > 0) positiveSteps++;
            if (elapsed < interval && interval.RawValue - elapsed.RawValue >= positiveSteps) return;
            elapsed = IntervalTimer.CarryOver(elapsed, interval, Context.DeltaTime); positiveSteps = 0;
            int slot = spawned % 6;
            // 원본은 두 번째 오프셋을 지상 Z가 아닌 높이 Y에 적용한다.
            var offset = new Vector3((Fix64)(slot % 3 - 1) * (Fix64)0.35m, ((Fix64)(slot / 3) - (Fix64)0.5m) * (Fix64)0.35m, 0);
            Context.Spawn(Owner.Master, prefabs[spawned % prefabs.Length], Owner.Position + offset); spawned++;
        }
    }
    public sealed class RandomAreaSpawner : TimedSelfDestroyer {
        private readonly string prefab;
        private readonly Fix64 interval, radius;
        private Fix64 spawnElapsed;
        public RandomAreaSpawner(SimObject owner, string prefab, Fix64 lifetime, Fix64 interval, Fix64 radius) : base(owner, lifetime) { this.prefab = prefab; this.interval = interval; this.radius = radius; }
        public override void Update() {
            base.Update(); spawnElapsed += Context.DeltaTime; if (spawnElapsed < interval) return;
            spawnElapsed -= interval;
            // 원본의 각도 범위 0..PI와 난수 소비 순서를 유지한다.
            Fix64 angle = Context.Random.NextFraction() * Fix64.Pi;
            Fix64 distance = Fix64.Sqrt(Context.Random.NextFraction()) * radius;
            var center = Owner.Position;
            Context.Spawn(Owner.Master, prefab, new Vector3(center.X + distance * Fix64.Cos(angle), 10, center.Z + distance * Fix64.Sin(angle)));
        }
    }
}
