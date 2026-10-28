using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class AuraParityTests {
        private const string Source = "game/domain/object/component/effect/statuseffect/idle_aura_status_effect.rs";
        private static SimObject Owner() { var c = new SimulationContext(); c.RegisterPrefab("Player", _ => { }); return c.Spawn(Master.LeftPlayer, "Player", Vector3.Zero); }
        [PortedTest(Source)] public static void an_attack_turns_the_idle_aura_into_an_attack_aura() {
            var o = Owner(); var aura = new IdleAuraStatusEffect(o, Element.Fire); o.AddComponentImmediately(aura); Assert.True(o.HasEffect(VisualEffect.FireIdleAura)); o.SetStatus(ObjectStatus.Attack);
            Assert.True(!o.HasEffect(VisualEffect.FireIdleAura) && o.HasEffect(VisualEffect.FireAttackAura)); var pending = o.GetPendingComponents<AttackAuraStatusEffect>().ToArray(); Assert.Equal(1, pending.Length); Assert.Equal(Element.Fire, pending[0].Element);
        }
        [PortedTest(Source)] public static void a_canceled_aura_stops_listening() { var o = Owner(); var aura = new IdleAuraStatusEffect(o, Element.Water); o.AddComponentImmediately(aura); aura.Cancel(); o.SetStatus(ObjectStatus.Attack); Assert.Equal(0, o.GetPendingComponents<AttackAuraStatusEffect>().Count()); }
        public static void CheckLifecycle() {
            foreach (var element in new[] { Element.Fire, Element.Water, Element.Nature, Element.Lightning, Element.Rock, Element.Wind }) {
                var o = Owner(); var idle = new IdleAuraStatusEffect(o, element); o.AddComponentImmediately(idle); o.Context.DeltaTime = 100; idle.Update(); Assert.Equal(Fix64.MaxValue, idle.Remaining);
                o.SetStatus(ObjectStatus.Attack); var attack = o.GetPendingComponents<AttackAuraStatusEffect>().Single(); var duplicate = new IdleAuraStatusEffect(o, element); o.AddComponentImmediately(duplicate); o.SetStatus(ObjectStatus.Attack); Assert.Equal(1, o.GetPendingComponents<AttackAuraStatusEffect>().Count());
                o.FlushComponents(); o.Context.DeltaTime = (Fix64)0.3m; attack.Update(); o.FlushComponents(); Assert.Equal(0, o.GetComponents<AttackAuraStatusEffect>().Count()); Assert.True(!o.HasEffect((VisualEffect)System.Enum.Parse(typeof(VisualEffect), element + "AttackAura")));
                var hindered = new IdleAuraStatusEffect(o, element); o.AddComponentImmediately(hindered); o.SetStatus(ObjectStatus.Hindered); o.SetStatus(ObjectStatus.Attack); Assert.Equal(0, o.GetPendingComponents<AttackAuraStatusEffect>().Count());
            }
        }
    }
}
