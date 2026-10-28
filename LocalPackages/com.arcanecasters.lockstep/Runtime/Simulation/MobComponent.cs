using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public readonly struct BattleEvent {
        public readonly string Type;
        public readonly int SourceId, TargetId;
        public readonly Fix64 Duration, Width;
        public readonly Vector3? StartPosition, EndPosition;
        public BattleEvent(string type, int sourceId, int targetId, Fix64 duration = default, Fix64 width = default) { Type = type; SourceId = sourceId; TargetId = targetId; Duration = duration; Width = width; StartPosition = EndPosition = null; }
        public BattleEvent(string type, Vector3 start, Vector3 end, Fix64 duration, Fix64 width = default) { Type = type; SourceId = TargetId = 0; StartPosition = start; EndPosition = end; Duration = duration; Width = width; }
        public BattleEvent(string type, Vector3 start, int targetId, Fix64 duration) { Type = type; SourceId = 0; TargetId = targetId; StartPosition = start; EndPosition = null; Duration = duration; Width = Fix64.Zero; }
        public BattleEvent(string type, int sourceId, Vector3 end, Fix64 duration, Fix64 width) { Type = type; SourceId = sourceId; TargetId = 0; StartPosition = null; EndPosition = end; Duration = duration; Width = width; }
    }
    public interface IDamageable { void OnDamaged(Hit hit); void OnDamagedWithDelay(Hit hit, Fix64 delay); }
    public interface IDamageImmunity { bool IsImmuneTo(Hit hit); }
    public interface IDamageInterceptor { bool BeforeDamage(Hit hit); }
    public interface IElementReaction { void OnAttacked(Element element); }
    public interface ICombatDeathListener { void OnCombatDeath(); }
    public interface ISpeedHolder { Stat Speed { get; } }
    public interface IDropMarker { }
    public interface IPlayerHealth { }
    public interface IFrenzyState { bool IsFrenzied { get; } }
    public static class TargetRelation {
        public static bool CanAttack(SimObject source, SimObject target) => !ReferenceEquals(source, target) && !target.IsDying
            && (source.GetComponents<IFrenzyState>().Any(state => state.IsFrenzied) || source.Master != target.Master);
    }

    public abstract class MobComponent : SimComponent, IDamageable, ISpeedHolder, IPveHealthMob {
        public virtual bool IsObjectiveTarget => false;
        public virtual bool IsSpawner => false;
        private readonly List<KeyValuePair<Hit, Fix64>> delayed = new List<KeyValuePair<Hit, Fix64>>();
        public int Hp { get; protected set; }
        public int MaxHp { get; protected set; }
        public Stat Speed { get; }
        public virtual bool FallsOnDeath => true;
        public virtual bool LeavesDeathField => true;
        protected MobComponent(SimObject owner, int maxHp, Fix64 speed) : base(owner) { Hp = MaxHp = maxHp; Speed = new Stat(speed); }
        public abstract void OnDeath();
        public void OnDamagedWithDelay(Hit hit, Fix64 delay) { delayed.Add(new KeyValuePair<Hit, Fix64>(hit, delay)); }
        public override void Update() {
            var due = new List<Hit>();
            for (int i = 0; i < delayed.Count;) {
                var item = delayed[i]; Fix64 remaining = item.Value - Context.DeltaTime;
                if (remaining <= Fix64.Zero) { due.Add(item.Key); delayed.RemoveAt(i); }
                else { delayed[i] = new KeyValuePair<Hit, Fix64>(item.Key, remaining); i++; }
            }
            foreach (var hit in due) OnDamaged(hit);
        }
        private bool Immune(Hit hit) => Owner.GetComponents<IDamageImmunity>().Any(component => component.IsImmuneTo(hit));
        public virtual void OnDamaged(Hit hit) {
            if (Owner.IsDying || Immune(hit)) return;
            foreach (var interceptor in Owner.GetComponents<IDamageInterceptor>()) if (interceptor.BeforeDamage(hit)) return;
            foreach (var effect in Owner.GetComponents<IElementReaction>()) foreach (var element in hit.Elements) effect.OnAttacked(element);
            ApplyDamage(hit);
        }
        public virtual void ApplyDamage(Hit hit) {
            if (Owner.IsDying || Immune(hit)) return;
            if (hit.AttackerId != 0 && hit.Damage > 0) Context.AddEvent(new BattleEvent("hit", hit.AttackerId, Owner.Id));
            var remaining = (Fix64)Hp - (Fix64)hit.Damage * ElementalChart.Average(hit.Elements, Owner.Elements.Total);
            Hp = (int)(remaining.RawValue / (1L << 32));
            Owner.ApplyUpdate();
            if (Hp <= 0 && !Owner.IsDestroyed) {
                if (!AerialDeathFall.TryStart(this)) CompleteDeath();
            } else if (Hp > MaxHp) Hp = MaxHp;
        }
        public void CompleteDeath() {
            foreach (var listener in Owner.GetComponents<ICombatDeathListener>()) listener.OnCombatDeath();
            if (LeavesDeathField) DeathField.Spawn(Owner);
            OnDeath();
        }
        public void OverrideMaxHp(int value) { if (value > 0) { MaxHp = Hp = value; Owner.ApplyUpdate(); } }
        public int DrainHpAboveFraction(Fix64 fraction) {
            int floor = (int)Fix64.Ceiling((Fix64)MaxHp * MathHelper.Clamp(fraction, Fix64.Zero, Fix64.One));
            int drained = Math.Max(0, Hp - floor);
            if (drained > 0) { Hp -= drained; Owner.ApplyUpdate(); }
            return drained;
        }
    }

    public static class DeathField {
        private static readonly Element[] Candidates = { Element.Fire, Element.Water, Element.Nature, Element.Lightning };
        public static void Spawn(SimObject deceased) {
            if (deceased.Context.SuppressDeathFields || deceased.HasComponent<IDropMarker>() || deceased.HasComponent<IPlayerHealth>()) return;
            var candidates = Candidates.Where(deceased.Elements.NativeHas).ToArray();
            if (candidates.Length == 0) return;
            Element chosen = candidates[deceased.Context.Random.NextBound(candidates.Length)];
            string prefab;
            switch (chosen) {
                case Element.Fire: prefab = "FireField"; break;
                case Element.Water: prefab = "WaterField"; break;
                case Element.Nature: prefab = "LeafField"; break;
                case Element.Lightning: prefab = "ElectricField"; break;
                default: throw new InvalidOperationException();
            }
            var position = deceased.Position; position.Y = Fix64.Zero;
            deceased.Context.Spawn(deceased.Master, prefab, position);
        }
    }

    public sealed class AerialDeathFall : SimComponent {
        private readonly MobComponent mob;
        private readonly ZPhysics physics;
        private bool landed;
        private AerialDeathFall(MobComponent mob, ZPhysics physics) : base(mob.Owner) { this.mob = mob; this.physics = physics; }
        public override bool ActiveWhileDying => true;
        public static bool TryStart(MobComponent mob) {
            var owner = mob.Owner; var physics = owner.GetComponent<ZPhysics>();
            if (!mob.FallsOnDeath || physics == null || !physics.IsAerial || owner.Position.Y - physics.GroundY <= (Fix64)0.01m) return false;
            owner.SetStatus(ObjectStatus.Dying); owner.AddComponent(new AerialDeathFall(mob, physics)); return true;
        }
        public override void Start() { physics.LockHover(SourceId); }
        public override void Update() {
            if (landed || Owner.Position.Y - physics.GroundY > (Fix64)0.01m) return;
            landed = true; mob.CompleteDeath();
        }
    }

    // 공중 유닛의 hover와 낙하 피해 규칙. 충돌 해결은 PhysicsWorld가 담당한다.
    public sealed class ZPhysics : SimComponent {
        private readonly Fix64 hoverY;
        private readonly bool hover;
        private readonly HashSet<long> hoverLocks = new HashSet<long>();
        public Fix64 YVelocity { get; private set; }
        public Fix64 GroundY { get; set; }
        public Fix64 Gravity { get; set; } = (Fix64)4;
        public Fix64 FallThreshold { get; set; } = (Fix64)0.01m;
        public Fix64 FallDamageVelocityUnit { get; set; } = (Fix64)4;
        public ZPhysics(SimObject owner, Fix64? hoverHeight = null) : base(owner) { hover = hoverHeight.HasValue; hoverY = hoverHeight ?? Fix64.Zero; }
        public bool CanHover => hover && hoverLocks.Count == 0;
        public bool IsAerial => hover && hoverY > GroundY;
        public void LockHover(long source) { hoverLocks.Add(source); }
        public void UnlockHover(long source) { hoverLocks.Remove(source); }
        public void AddImpulseZ(Fix64 force) { YVelocity += force; }
        public void ApplyHover() {
            var p = Owner.Position; if (p.Y == hoverY) return;
            Fix64 delta = Context.DeltaTime;
            p.Y = Fix64.Abs(p.Y - hoverY) < delta ? hoverY : p.Y + delta * (hoverY > p.Y ? 1 : -1);
            Owner.SetPosition(p);
        }
        public void ApplyZForce() {
            YVelocity -= Gravity * Context.DeltaTime;
            var p = Owner.Position; Fix64 y = p.Y + YVelocity * Context.DeltaTime;
            if (y < GroundY) {
                y = GroundY;
                if (YVelocity < Fix64.Zero && Fix64.Abs(p.Y - GroundY) > FallThreshold) {
                    int damage = (int)Fix64.Floor(Fix64.Abs(YVelocity) / FallDamageVelocityUnit);
                    Owner.GetComponent<MobComponent>()?.ApplyDamage(new Hit(damage, 0, Element.None));
                }
                YVelocity = Fix64.Zero;
            }
            if (p.Y != y) { p.Y = y; Owner.SetPosition(p); }
        }
    }
}
