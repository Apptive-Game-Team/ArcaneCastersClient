using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class ManaWellMob : MobComponent, IGaugeComponent, IFrenzyImmune, IMindControlImmune {
        public ManaWellMob(SimObject owner, int hp) : base(owner, hp, 0) { }
        private void UpdateCharge(Fix64 delta) {
            if (!Context.Players.TryGetValue(Owner.Master, out var player)) throw new InvalidOperationException("mana well has no player data");
            player.ManaCharger.UpdateManaCharge(delta);
        }
        public override void Start() { UpdateCharge((Fix64)0.8m); }
        public override void OnDestroy() { UpdateCharge((Fix64)(-0.8m)); }
        public override void OnDeath() { Owner.Destroy(); }
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("ManaWell", owner => {
                Fix64 Value(string key) => context.GameParameters["mana_well." + key];
                owner.AddCollider(new CircleCollider(owner, Value("radius"))); owner.AddComponentImmediately(new ManaWellMob(owner, (int)(Value("hp").RawValue / (1L << 32))));
                var lifetime = Value("hp") * Value("attack_interval") / Value("damage");
                owner.AddComponent(new TimedSelfDestroyer(owner, (int)(lifetime.RawValue / (1L << 32))));
                owner.Elements.AddNative(Element.Lightning); owner.Elements.AddNative(Element.Nature); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
