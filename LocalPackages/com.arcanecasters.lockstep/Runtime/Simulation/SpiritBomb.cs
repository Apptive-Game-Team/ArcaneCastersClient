using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class SpiritBombChannel : SimComponent {
        private readonly Vector3 direction;
        private readonly Fix64 length;
        private readonly int totalDamage;
        private Fix64 elapsed;
        private long positiveSteps;
        private int ticksApplied;
        public SpiritBombChannel(SimObject owner, Vector3 target, int totalDamage, Fix64 length) : base(owner) { direction = VineMagics.Normalize(target - owner.Position); this.length = length; this.totalDamage = totalDamage; }
        public static Fix64 VisualWidth(int damage) => damage <= 100 ? (Fix64)0.25m : damage >= 1000 ? Fix64.One : (Fix64)0.25m + (Fix64)(damage - 100) / 900 * (Fix64)0.75m;
        private RayHit? FindClosest(List<SimObject> excluded) {
            foreach (var hit in RayQueries.RaycastAll(Context, Owner.Position, direction, length))
                if (!excluded.Contains(hit.Object) && TargetRelation.CanAttack(Owner, hit.Object) && hit.Object.HasComponent<IDamageable>()) return hit;
            return null;
        }
        private void Beam(Fix64 distance) { Context.AddEvent(new BattleEvent("SpiritBombBeam", Owner.Id, Owner.Position + direction * distance, (Fix64)1.05m, VisualWidth(totalDamage))); }
        private void ApplyTick(int index) {
            var excluded = new List<SimObject>(); var hit = FindClosest(excluded); if (!hit.HasValue) { Beam(length); return; }
            int remaining = totalDamage / 4 + (index < totalDamage % 4 ? 1 : 0); if (remaining == 0) { Beam(hit.Value.Distance); return; }
            while (hit.HasValue && remaining > 0) {
                var target = hit.Value.Object; excluded.Add(target); Beam(hit.Value.Distance); var mob = target.GetComponent<MobComponent>(); int before = mob?.Hp ?? 0;
                var attack = new Hit(remaining, Owner.Id, Element.Lightning, Element.Nature); foreach (var receiver in target.GetComponents<IDamageable>()) receiver.OnDamaged(attack);
                if (mob == null) return;
                int after = mob.Hp, lost = before - after; bool killed = after <= 0 || target.IsDying || target.IsDestroyed;
                if (lost <= 0 || !killed) return;
                remaining = StormStagMob.JavaRound((Fix64)remaining * System.Math.Max(0, -after) / lost); hit = FindClosest(excluded);
            }
        }
        public override void Update() {
            elapsed += Context.DeltaTime; if (Context.DeltaTime > 0) positiveSteps++; if (direction == Vector3.Zero) return;
            while (ticksApplied < 4) {
                var due = (Fix64)0.4m + ticksApplied;
                // 20Hz delta의 양자화 누적으로 명목상 피해 tick이 한 프레임 늦어지는 것을 막는다.
                if (elapsed < due && due.RawValue - elapsed.RawValue >= positiveSteps) break;
                ApplyTick(ticksApplied); ticksApplied++;
            }
            if (ticksApplied == 4) Owner.RemoveComponent(this);
        }
    }
    public static class SpiritBombMagic {
        public static void Register(MagicRegistry registry) {
            registry.Register("spirit_bomb", new MagicImplementation((context, master, origin, position) => {
                var player = context.Objects.FirstOrDefault(o => o.IsActive && o.Master == master && o.Prefab == "Player"); if (player == null || position == player.Position) return;
                int absorbed = 0;
                foreach (var ally in context.Objects) {
                    if (!ally.IsActive || ReferenceEquals(ally, player) || ally.Master != master || ally.Prefab == "Player") continue;
                    var mob = ally.GetComponent<MobComponent>(); if (mob == null) continue;
                    int drained = mob.DrainHpAboveFraction((Fix64)0.5m); if (drained == 0) continue;
                    absorbed = unchecked(absorbed + drained); context.AddEvent(new BattleEvent("ElectricAbsorb", ally.Id, player.Id, (Fix64)0.35m));
                }
                int damage = StormStagMob.JavaRound((Fix64)absorbed * 7 / 10); player.AddComponent(new SpiritBombChannel(player, position, damage, context.GameParameters["spirit_bomb.range"]));
            }));
        }
    }
}
