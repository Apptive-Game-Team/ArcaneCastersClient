using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PvePullParityTests {
        private const string Source = "game/service/system/pve_script_system.rs";
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0) { DeltaTime = (Fix64)0.5m };
            public readonly PveScenarioInstaller Installer = new PveScenarioInstaller();
            public readonly PveScriptSystem System = new PveScriptSystem();
            public Fixture() {
                Context.RegisterPrefab("Unit", owner => owner.AddComponent(new MeleeAttackMob(owner, 10, 2, 1, 1, 1)));
                Context.RegisterPrefab("Building", owner => owner.AddComponent(new DummyMob(owner, 10)));
                Installer.Install(new PveInstallObject[0], Context); System.Runtime = Installer.Runtime; System.Runner.Installer = Installer;
                System.Scenario = new PveScenario(new[] { new PveScenarioEvent("pull", PveTriggerType.FrameNumGte, 0, key: "key.pull", actions: new[] { new PveScenarioAction(PveActionType.PullPlayerUnits, "gate") }) });
            }
            public SimObject Unit(Master master, int x, bool moves = true) => Context.Spawn(master, moves ? "Unit" : "Building", new Vector3(x, 0, 3));
        }
        [PortedTest(Source)] public static void pull_player_units_drags_every_left_player_moving_unit_to_the_installer_then_destroys_it_and_spares_the_rest() {
            var f = new Fixture(); var gate = new SimObject(f.Context, Master.RightPlayer, "ZapMouse", new Vector3(14, 0, 5)); f.Installer.Runtime.Register("gate", gate);
            var summon = f.Unit(Master.LeftPlayer, 2); var building = f.Unit(Master.LeftPlayer, 3, false); var enemy = f.Unit(Master.RightPlayer, 10); f.Context.FlushObjects();
            f.System.Update(f.Context); summon.FlushComponents(); building.FlushComponents(); enemy.FlushComponents();
            Assert.Equal(0, summon.GetComponents<MobComponent>().Length); Assert.True(summon.HasComponent<PulledToPoint>());
            Assert.True(building.HasComponent<MobComponent>()); Assert.Equal(false, building.HasComponent<PulledToPoint>()); Assert.True(enemy.HasComponent<MobComponent>()); Assert.Equal(false, enemy.HasComponent<PulledToPoint>());
            var pull = summon.GetComponent<PulledToPoint>(); pull.Update(); Assert.Near((Fix64)6, summon.Position.X, (Fix64)0.001m); Assert.Equal(false, summon.IsDestroyed);
            pull.Update(); pull.Update(); Assert.Equal((Fix64)14, summon.Position.X); Assert.Equal((Fix64)5, summon.Position.Z); Assert.True(summon.IsDestroyed);
        }
        [PortedTest(Source)] public static void pull_player_units_destroys_summons_in_place_when_the_installer_is_missing() {
            var f = new Fixture(); var summon = f.Unit(Master.LeftPlayer, 2); f.Context.FlushObjects(); f.System.Update(f.Context); Assert.True(summon.IsDestroyed); Assert.Equal((Fix64)2, summon.Position.X);
        }
    }
}
