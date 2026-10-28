using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class ElectricDeathEnergy : SimComponent, ICombatDeathListener {
        private bool consumed;
        public ElectricDeathEnergy(SimObject owner) : base(owner) { }
        public void OnCombatDeath() {
            if (consumed) return; consumed = true;
            var position = Owner.Position; var radius = Context.GameParameters["electric_field.radius"]; var duration = Context.GameParameters["electric_field.duration"];
            SimObject closest = null; var distance = Fix64.MaxValue;
            foreach (var candidate in Context.Objects) {
                if (ReferenceEquals(candidate, Owner) || !candidate.IsActive || candidate.Master != Owner.Master || !candidate.Elements.NativeHas(Element.Lightning)
                    || !candidate.HasComponent<MobComponent>() || !candidate.HasComponent<IDamageable>() || !candidate.HasComponent<ElectricDeathEnergy>()) continue;
                var next = Vector3.Distance(candidate.Position, position); if (next > radius) continue;
                if (closest == null || next < distance) { closest = candidate; distance = next; }
            }
            if (closest == null) return;
            OverchargeStatusEffect.Apply(closest, duration); Context.AddEvent(new BattleEvent("ElectricAbsorb", position, closest.Id, (Fix64)0.35m));
        }
    }
}
