using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public class CloudDragonMob : ProjectileRangeAttackMob {
        private readonly Fix64 cooldown;
        private Fix64 timer;
        private long positiveSteps;
        public CloudDragonMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range, Fix64 cooldown)
            : base(owner, hp, speed, mask, damage, interval, range, "WaterShot", (Fix64)0.5m) { this.cooldown = cooldown; }
        protected virtual void FireChainLightning(SimObject target) {
            var shot = Context.Spawn(Owner.Master, "ChainLightning", Owner.Position);
            var component = shot.GetComponent<IShotTarget>() ?? throw new System.InvalidOperationException("ChainLightning prefab has no Shot component");
            component.SetTarget(target.Position); Owner.SetStatus(ObjectStatus.Attack);
        }
        public override void Update() {
            base.Update(); var dt = Context.DeltaTime; timer = MathHelper.Min(cooldown + dt, timer + dt); if (dt > 0) positiveSteps++;
            // 각 양수 delta 변환에서 잘리는 raw 1 미만의 누적 상한 안에서만 시간 경계를 보정한다.
            if (timer < cooldown && cooldown.RawValue - timer.RawValue >= positiveSteps || !IsValidTarget(Target)) return;
            FireChainLightning(Target); timer = IntervalTimer.CarryOver(timer, cooldown, dt); positiveSteps = 0;
        }
        public static void RegisterPrefabs(SimulationContext context) {
            context.RegisterPrefab("ChainLightning", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["chain_lightning." + name].RawValue / (1L << 32));
                owner.AddCollider(new CircleCollider(owner, p["chain_lightning.radius"], true)); owner.Elements.AddNative(Element.Lightning);
                owner.AddComponentImmediately(new EffectProvider(owner, VisualEffect.Shock)); owner.AddComponentImmediately(new ChainShot(owner, Integer("damage"), Integer("speed"), p["chain_lightning.attack_range"], Integer("min_damage")));
            });
            context.RegisterPrefab("CloudDragon", owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p["cloud_dragon." + name].RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner, 3)); owner.AddCollider(new CircleCollider(owner, p["cloud_dragon.radius"]));
                owner.AddComponent(new CloudDragonMob(owner, Integer("hp"), p["cloud_dragon.speed"], 3, Integer("damage"), p["cloud_dragon.attack_interval"], p["cloud_dragon.attack_range"], p["cloud_dragon.chain_lightning_cooldown"]));
                owner.Elements.AddNative(Element.Water); owner.Elements.AddNative(Element.Wind); owner.Elements.AddNative(Element.Lightning); owner.AddComponent(new CommonEffectReceiver(owner));
                owner.AddComponent(new AreaEffectProvider(owner, 1, p["cloud_dragon.attack_range"], VisualEffect.Wet));
            });
        }
    }
}
