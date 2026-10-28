using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class AreaEffectProvider : SimComponent {
        private readonly Fix64 interval, radius;
        private readonly VisualEffect effect;
        private Fix64 counter;
        public AreaEffectProvider(SimObject owner, Fix64 interval, Fix64 radius, VisualEffect effect) : base(owner) { this.interval = interval; this.radius = radius; this.effect = effect; }
        public override void Update() {
            counter += Context.DeltaTime; if (counter < interval) return;
            counter = IntervalTimer.CarryOver(counter, interval, Context.DeltaTime);
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius)) target.GetComponent<IEffectReceiver>()?.Receive(new EffectApplication(effect, Owner));
        }
    }
    public class SprayingAttacker : BehaviorMob {
        public SprayingAttacker(SimObject owner, int hp, Fix64 speed, int mask, Fix64 interval, Fix64 range, int damage, VisualEffect effect, string projectile) : base(owner, hp, speed, mask, interval, range, null) {
            Behavior = target => {
                var direction = Normalize(target.Position - owner.Position); var normalized = Normalize(direction); var radius = AttackRange / 2;
                var start = owner.Position + normalized * (radius * 2);
                Context.AddEvent(new BattleEvent(projectile, start, start + normalized, (Fix64)0.5m));
                var hit = new Hit(damage, owner.Id, owner.Elements.Total); var mobs = new List<MobComponent>();
                // 원본 분사는 진영이나 상태를 거르지 않으며 겹치는 두 구의 첫 Mob만 중복 제거한다.
                for (int index = 0; index < 2; index++) {
                    var center = owner.Position + normalized * (radius + index * 2 * radius);
                    foreach (var candidate in SimulationQueries.OverlapSphereAll(Context, center, radius)) {
                        var mob = candidate.GetComponent<MobComponent>(); if (mob != null && !mobs.Contains(mob)) mobs.Add(mob);
                    }
                }
                if (mobs.Count == 0) return false;
                foreach (var mob in mobs) mob.OnDamaged(hit);
                foreach (var mob in mobs) mob.Owner.GetComponent<IEffectReceiver>()?.Receive(new EffectApplication(effect, owner));
                return true;
            };
        }
        private static Vector3 Normalize(Vector3 value) { var length = value.Length(); return length == Fix64.Zero ? Vector3.Zero : new Vector3(value.X / length, value.Y / length, value.Z / length); }
    }
    public static class FireSpiritPrefab {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("FireSpirit", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["fire_spirit." + name].RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, p["fire_spirit.radius"]));
                owner.AddComponent(new SprayingAttacker(owner, Integer("hp"), p["fire_spirit.speed"], 1, p["fire_spirit.attack_interval"], p["fire_spirit.attack_range"], Integer("damage"), VisualEffect.Burn, "SprayedFlame"));
                owner.Elements.AddNative(Element.Fire); owner.Elements.AddNative(Element.Wind); owner.AddComponent(new CommonEffectReceiver(owner));
                owner.AddComponent(new AreaEffectProvider(owner, 1, p["fire_spirit.sub_attack_range"], VisualEffect.Burn));
            });
        }
    }
}
