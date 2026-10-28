using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PveShieldParityTests {
        private const string Source = "tests/pve_shield.rs";
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0);
            public readonly PveScenarioInstaller Installer = new PveScenarioInstaller();
            public Fixture() { Context.RegisterPrefab("ZapMouse", owner => owner.AddComponentImmediately(new DummyMob(owner, 100))); Installer.Components = new PveScenarioComponents(); Installer.Install(new PveInstallObject[0], Context); }
            public SimObject Install(string id) => Installer.InstallOne(new PveInstallObject(id, "ZapMouse", Master.RightPlayer, Vector3.Zero), Context);
            public PveScriptSystem System(PveShield[] shields, params PveScenarioEvent[] events) {
                var system = new PveScriptSystem { Scenario = new PveScenario(events, new[] { "boss" }, shields: shields), Runtime = Installer.Runtime };
                system.Runner.Installer = Installer; system.Runner.Components = new PveScenarioComponents(); return system;
            }
            public void Tick(PveScriptSystem system) {
                system.Update(Context);
                foreach (var owner in Context.PendingObjects.Where(owner => !owner.IsDestroyed)) owner.FlushComponents();
                foreach (var owner in Context.PendingObjects.Where(owner => !owner.IsDestroyed)) foreach (var shield in owner.GetComponents<PveShieldComponent>()) shield.Update();
            }
        }
        private static DummyMob Mob(SimObject owner) => owner.GetComponent<DummyMob>();
        private static void Hit(SimObject owner, int amount) => Mob(owner).OnDamaged(new Hit(amount, 0, Element.None));
        private static bool Bubble(SimObject owner) => owner.Effects.Contains(VisualEffect.Bubble);
        private static PveShield[] Shields() => new[] { new PveShield("boss", "pillar") };
        [PortedTest(Source)] public static void shield_is_down_while_no_source_has_been_installed() {
            var f = new Fixture(); var boss = f.Install("boss"); f.Tick(f.System(Shields())); Hit(boss, 10); Assert.Equal(90, Mob(boss).Hp); Assert.Equal(false, Bubble(boss));
        }
        [PortedTest(Source)] public static void shield_blocks_every_damage_path_while_a_source_is_alive() {
            var f = new Fixture(); var boss = f.Install("boss"); f.Install("pillar"); f.Tick(f.System(Shields()));
            Hit(boss, 10); Mob(boss).OnDamagedWithDelay(new Hit(10, 0, Element.Fire), (Fix64)0.5m); Mob(boss).ApplyDamage(new Hit(10, 0, Element.Fire));
            f.Context.DeltaTime = 1; new DotStatusEffect(boss, 1, 10, Element.Fire, StatusEffectKey.DotDealBurn).Update();
            Assert.Equal(100, Mob(boss).Hp); Assert.True(Bubble(boss));
            Mob(boss).Update(); Assert.Equal(100, Mob(boss).Hp);
        }
        [PortedTest(Source)] public static void healing_is_not_blocked() {
            var f = new Fixture(); var boss = f.Install("boss"); f.Install("pillar"); f.Tick(f.System(Shields())); Mob(boss).DrainHpAboveFraction((Fix64)0.5m); Assert.Equal(50, Mob(boss).Hp); Mob(boss).ApplyDamage(new Hit(-5, 0, Element.None)); Assert.Equal(55, Mob(boss).Hp);
        }
        [PortedTest(Source)] public static void destroying_the_only_source_drops_the_shield_and_boss_can_be_killed() {
            var f = new Fixture(); var boss = f.Install("boss"); var pillar = f.Install("pillar"); var system = f.System(Shields()); f.Tick(system); Hit(boss, 10); Assert.Equal(100, Mob(boss).Hp);
            pillar.Destroy(); f.Tick(system); Hit(boss, 30); Assert.Equal(70, Mob(boss).Hp); Assert.Equal(false, Bubble(boss)); Hit(boss, 70); Assert.True(Mob(boss).Hp <= 0);
        }
        [PortedTest(Source)] public static void a_source_installed_later_by_an_install_object_action_raises_the_shield_again() {
            var f = new Fixture(); var boss = f.Install("boss"); var pillar = f.Install("pillar");
            var respawn = new PveScenarioEvent("respawn", PveTriggerType.InstallerDestroyed, 0, target: "pillar", key: "k", actions: new[] { new PveScenarioAction(PveActionType.InstallObject, "pillar", "ZapMouse") });
            var system = f.System(Shields(), respawn); f.Tick(system); pillar.Destroy(); f.Tick(system);
            Assert.Equal(false, ReferenceEquals(pillar, f.Installer.GetInstalledObject("pillar"))); Hit(boss, 10); Assert.Equal(100, Mob(boss).Hp); Assert.True(Bubble(boss));
        }
        [PortedTest(Source)] public static void shield_stays_up_while_another_source_is_alive() {
            var f = new Fixture(); var boss = f.Install("boss"); var a = f.Install("pillar_a"); f.Install("pillar_b");
            var system = f.System(new[] { new PveShield("boss", "pillar_a"), new PveShield("boss", "pillar_b") }); f.Tick(system); a.Destroy(); f.Tick(system); Hit(boss, 10); Assert.Equal(100, Mob(boss).Hp); Assert.True(Bubble(boss));
        }
        [PortedTest(Source)] public static void a_source_that_is_an_objective_itself_can_be_damaged_and_the_shielded_boss_still_counts() {
            var f = new Fixture(); var boss = f.Install("boss"); var pillar = f.Install("pillar"); f.Tick(f.System(Shields())); Hit(pillar, 10); Assert.Equal(90, Mob(pillar).Hp); Assert.Equal(false, PveObjectiveTarget.IsTerminal(boss)); Hit(boss, 50); Assert.Equal(false, PveObjectiveTarget.IsTerminal(boss)); Assert.Equal(100, Mob(boss).Hp);
        }
        [PortedTest(Source)] public static void scenario_without_shield_rows_takes_damage_as_before() {
            var f = new Fixture(); var boss = f.Install("boss"); f.Install("pillar"); f.Tick(f.System(new PveShield[0])); Hit(boss, 10); Mob(boss).ApplyDamage(new Hit(10, 0, Element.None)); Assert.Equal(80, Mob(boss).Hp); Assert.Equal(false, Bubble(boss));
        }
    }
}
