using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ManaWellParityTests {
        private static SimulationContext Context() {
            var c = new SimulationContext(1); c.Players.Add(Master.LeftPlayer, new PlayerData(new ManaCharger(100))); c.Players.Add(Master.RightPlayer, new PlayerData(new ManaCharger(100))); return c;
        }
        [PortedTest("tests/mob_simple.rs")] public static void a_mana_well_starts_and_stops_for_a_player_side() {
            var c = Context(); var owner = new SimObject(c, Master.LeftPlayer, "ZapMouse", Vector3.Zero); var well = new ManaWellMob(owner, 10); well.Start(); well.OnDestroy();
        }
        public static void CheckResourceLifecycle() {
            var c = Context(); ManaWellMob.Register(c); c.GameParameters["mana_well.radius"] = 1; c.GameParameters["mana_well.hp"] = 10; c.GameParameters["mana_well.attack_interval"] = 2; c.GameParameters["mana_well.damage"] = 3;
            var first = c.Spawn(Master.LeftPlayer, "ManaWell", new Vector3(3, 0, 3)); var second = c.Spawn(Master.LeftPlayer, "ManaWell", new Vector3(4, 0, 3));
            Assert.True(!first.IsDestroyed && !second.IsDestroyed); Assert.True(first.HasComponent<IFrenzyImmune>() && first.HasComponent<IMindControlImmune>()); Assert.True(first.Elements.Has(Element.Lightning) && first.Elements.Has(Element.Nature));
            Assert.Equal((Fix64)6, first.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
            var left = c.Players[Master.LeftPlayer]; var right = c.Players[Master.RightPlayer];
            left.ManaCharger.ChargeMana(left, (Fix64)0.25m); right.ManaCharger.ChargeMana(right, (Fix64)0.25m); Assert.Equal(2, left.Mana); Assert.Equal(1, right.Mana);
            first.Destroy(); Assert.Equal(Fix64.One + (Fix64)0.8m, left.ManaCharger.ChargeValue.Total);
            c.DeltaTime = 6; second.GetComponent<TimedSelfDestroyer>().Update(); Assert.True(second.IsDestroyed); Assert.Equal(Fix64.One, left.ManaCharger.ChargeValue.Total);
            var ownerless = new ManaWellMob(new SimObject(c, Master.None, "ManaWell", Vector3.Zero), 10); bool failed = false;
            try { ownerless.Start(); } catch (System.InvalidOperationException) { failed = true; } Assert.True(failed);
        }
    }
}
