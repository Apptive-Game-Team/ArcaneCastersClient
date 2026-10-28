using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class CraterCarrierMob : BehaviorMob {
        private readonly int damage;
        private readonly Fix64 setup, recovery;
        private bool deployed, finishingStun;
        public CraterCarrierMob(SimObject owner, int hp, Fix64 speed, int damage, Fix64 interval, Fix64 range, Fix64 setup, Fix64 recovery) : base(owner, hp, speed, 1, interval, range, null) { this.damage = damage; this.setup = setup; this.recovery = recovery; Behavior = Fire; }
        private bool Fire(SimObject target) {
            if (!Owner.IsActive || Owner.IsDying || !IsValidTarget(target) || !WithinAttackRange(AttackRange)) return false;
            var shell = Context.Spawn(Owner.Master, "CraterShell", Owner.Position + new Vector3(0, (Fix64)0.8m, 0));
            shell.GetComponent<CraterShell>().Launch(target.Position, damage, Owner); Owner.SetStatus(ObjectStatus.Attack); return true;
        }
        private static bool IsStun(IMobState state) => state is StunState || state is CarrierStunState;
        public override void SetState(IMobState next) {
            if (IsStun(CurrentState) && !finishingStun && !IsStun(next)) return;
            if (CurrentState is RecoveryState && !IsStun(next)) return;
            if (next is AttackState) next = new DeploymentState(this);
            if (deployed && !(next is DeploymentState || next is SiegeAttackState || next is RecoveryState) && !IsStun(next)) next = new RecoveryState(this);
            base.SetState(next);
        }
        public override void SetStun(Fix64 duration) { SetState(new CarrierStunState(this, duration)); }
        public override void SetIdle() { finishingStun = true; try { base.SetIdle(); } finally { finishingStun = false; } }
        private sealed class PhaseTimer {
            private Fix64 elapsed;
            private long positiveSteps;
            public bool Advance(Fix64 delta, Fix64 duration) {
                elapsed += delta;
                if (delta > Fix64.Zero) positiveSteps++;
                // 각 양수 delta 변환에서 버린 값은 raw 1 미만이다. 누적 상한 안의 경계만 보정한다.
                return elapsed >= duration || duration.RawValue - elapsed.RawValue < positiveSteps;
            }
        }
        private sealed class DeploymentState : State {
            private readonly CraterCarrierMob owner; private readonly PhaseTimer timer = new PhaseTimer();
            public DeploymentState(CraterCarrierMob owner) : base(owner) { this.owner = owner; }
            public override void OnEnter() { owner.deployed = true; owner.Owner.SetStatus(ObjectStatus.Deploying); }
            public override void OnUpdate() {
                owner.Owner.SetStatus(ObjectStatus.Deploying); var target = owner.Target;
                if (!owner.IsValidTarget(target) || !owner.WithinAttackRange(owner.AttackRange)) { owner.SetState(new IdleState(owner)); return; }
                if (timer.Advance(owner.Context.DeltaTime, owner.setup)) { owner.DirectState(new SiegeAttackState(owner)); owner.Fire(target); }
            }
        }
        private void DirectState(IMobState state) { base.SetState(state); }
        private sealed class SiegeAttackState : State {
            private readonly AttackState attack;
            public SiegeAttackState(CraterCarrierMob owner) : base(owner) { attack = new AttackState(owner); }
            public override void OnEnter() { Mob.Owner.SetStatus(ObjectStatus.Deployed); }
            public override void OnUpdate() { Mob.Owner.SetStatus(ObjectStatus.Deployed); attack.OnUpdate(); }
            public override void OnExit() { attack.OnExit(); }
        }
        private sealed class RecoveryState : State {
            private readonly CraterCarrierMob owner; private readonly PhaseTimer timer = new PhaseTimer();
            public RecoveryState(CraterCarrierMob owner) : base(owner) { this.owner = owner; }
            public override void OnEnter() { owner.Owner.SetStatus(ObjectStatus.Recovering); }
            public override void OnUpdate() {
                owner.Owner.SetStatus(ObjectStatus.Recovering);
                if (timer.Advance(owner.Context.DeltaTime, owner.recovery)) { owner.deployed = false; owner.Owner.SetStatus(ObjectStatus.Idle); owner.DirectState(new IdleState(owner)); }
            }
        }
        private sealed class CarrierStunState : State {
            private readonly CraterCarrierMob owner; private readonly StunState stun;
            public CarrierStunState(CraterCarrierMob owner, Fix64 duration) : base(owner) { this.owner = owner; stun = new StunState(owner, duration); }
            public override void OnEnter() { owner.Owner.SetStatus(owner.deployed ? ObjectStatus.Recovering : ObjectStatus.Idle); }
            public override void OnUpdate() { OnEnter(); owner.finishingStun = true; try { stun.OnUpdate(); } finally { owner.finishingStun = false; } }
            public override void OnExit() { stun.OnExit(); }
        }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("CraterCarrier", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["crater_carrier." + name].RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, p["crater_carrier.radius"]));
                owner.AddComponent(new CraterCarrierMob(owner, Integer("hp"), p["crater_carrier.speed"], Integer("damage"), p["crater_carrier.attack_interval"], p["crater_carrier.attack_range"], p["crater_carrier.setup_duration"], p["crater_carrier.recovery_duration"]));
                owner.Elements.AddNative(Element.Fire); owner.Elements.AddNative(Element.Rock); owner.AddComponent(new CommonEffectReceiver(owner)); owner.AddComponent(new RockDeathRemnant(owner));
            });
        }
    }
}
