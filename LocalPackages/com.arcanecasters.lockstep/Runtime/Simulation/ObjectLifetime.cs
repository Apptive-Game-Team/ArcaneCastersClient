using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum GaugeCategory { Hp, Ttl }
    public readonly struct Gauge {
        public readonly Fix64 Value, MaxValue;
        public readonly GaugeCategory Category;
        public Gauge(Fix64 value, Fix64 maxValue, GaugeCategory category) { Value = value; MaxValue = maxValue; Category = category; }
    }
    public interface IGaugeComponent { Gauge GetGauge(); }
    public static class GaugeExtractor {
        public static Gauge[] Extract(SimObject owner) { var gauges = new System.Collections.Generic.List<Gauge>(); Merge(owner, gauges); return gauges.ToArray(); }
        public static void Merge(SimObject owner, System.Collections.Generic.List<Gauge> gauges) {
            foreach (var component in owner.GetComponents<IGaugeComponent>()) {
                var gauge = component.GetGauge(); if (gauge.MaxValue < 0) continue;
                int index = gauges.FindIndex(value => value.Category == gauge.Category); if (index >= 0) gauges.RemoveAt(index); gauges.Add(gauge);
            }
        }
    }
    public class TimedSelfDestroyer : SimComponent, IGaugeComponent {
        protected readonly Fix64 timeToLive;
        protected Fix64 elapsed;
        private bool frozen;
        public TimedSelfDestroyer(SimObject owner, Fix64 timeToLive) : base(owner) { this.timeToLive = timeToLive; }
        public override void Update() { if (frozen) { frozen = false; return; } elapsed += Context.DeltaTime; if (elapsed >= timeToLive) Owner.Destroy(); }
        public void Freeze() { frozen = true; }
        public void Recover() { elapsed = Fix64.Zero; Owner.ApplyUpdate(); }
        public void RecoverBy(Fix64 amount) { if (amount <= Fix64.Zero) return; elapsed = MathHelper.Max(Fix64.Zero, elapsed - amount); Owner.ApplyUpdate(); }
        public Gauge GetGauge() => new Gauge(timeToLive - elapsed, timeToLive, GaugeCategory.Ttl);
    }
    public sealed class DistanceSelfDestroyer : SimComponent {
        private readonly Vector3 origin;
        private readonly Fix64 distance;
        public DistanceSelfDestroyer(SimObject owner, Vector3 origin, Fix64 distance) : base(owner) { this.origin = origin; this.distance = distance; }
        public override void Update() { if (Vector3.Distance(Owner.Position, origin) >= distance) Owner.Destroy(); }
    }
}
