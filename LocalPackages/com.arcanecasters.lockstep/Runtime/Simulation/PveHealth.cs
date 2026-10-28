using System;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IPveHealthMob {
        int Hp { get; }
        int MaxHp { get; }
        void OverrideMaxHp(int value);
        bool IsObjectiveTarget { get; }
        bool IsSpawner { get; }
    }
    public interface IPveObjectiveTarget { bool IsTerminal { get; } }
    public static class PveObjectiveTarget {
        public static bool IsTerminal(SimObject owner) => owner == null || owner.IsDestroyed || owner.GetComponents<IPveObjectiveTarget>().Any(target => target.IsTerminal);
    }
    public sealed class PveShieldComponent : SimComponent, IDamageImmunity {
        private readonly Func<bool> shieldUp;
        private bool showing;
        public PveShieldComponent(SimObject owner, Func<bool> shieldUp) : base(owner) { this.shieldUp = shieldUp; }
        public bool IsShieldUp => shieldUp();
        public bool IsImmuneTo(Hit hit) => hit.Damage > 0 && IsShieldUp;
        private void SyncEffect() {
            bool up = IsShieldUp; if (up == showing) return; showing = up;
            if (up) Owner.AddEffect(VisualEffect.Bubble); else Owner.RemoveEffect(VisualEffect.Bubble);
        }
        public override void Start() { SyncEffect(); }
        public override void Update() { SyncEffect(); }
        public override void OnDestroy() { if (showing) { Owner.RemoveEffect(VisualEffect.Bubble); showing = false; } }
    }
    public class PveScenarioComponents {
        public void RemoveLifetimeComponents(SimObject owner) { foreach (var lifetime in owner.GetComponents<TimedSelfDestroyer>()) owner.RemoveComponent(lifetime); }
        public void RemoveSpawner(SimObject owner) { var existing = owner.GetComponent<Spawner>(); if (existing != null) owner.RemoveComponent(existing); }
        public virtual void AddSpawner(SimObject owner, string prefab, Fix64 interval, int count) { if (prefab != null) owner.AddComponent(new Spawner(owner, 0, prefab, interval, false, count)); }
        public void AddShield(SimObject owner, Func<bool> shieldUp) { owner.AddComponent(new PveShieldComponent(owner, shieldUp)); }
    }
}
