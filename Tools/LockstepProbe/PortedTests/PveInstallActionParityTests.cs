using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;

namespace ArcaneCasters.Parity {
    public static class PveInstallActionParityTests {
        private const string Source = "tests/pve_engine.rs";
        private sealed class FailingSpawnerComponents : PveScenarioComponents {
            public override void AddSpawner(SimObject owner, string prefab, FixMath.NET.Fix64 interval, int count) {
                throw new System.InvalidOperationException("Injected spawner failure");
            }
        }
        [PortedTest(Source)] public static void a_failing_action_does_not_stop_the_next_action() {
            var f = new Fixture(
                new PveScenarioAction(PveActionType.SetSpawner,"boss","ZapMouse",2),
                new PveScenarioAction(PveActionType.SpawnWave,prefab:"ZapMouse",count:2,x:14,z:7));
            f.Install("boss","PveNatureSlimeNest"); int before = f.Context.PendingObjects.Length;
            f.System.Runner.Components = new FailingSpawnerComponents();
            int failures = 0; f.System.Failed += (id,error) => {
                Assert.Equal("e1",id); Assert.Equal("Injected spawner failure",error.Message); failures++;
            };
            f.Update(); Assert.Equal(1,failures); Assert.Equal(before + 2,f.Context.PendingObjects.Length);
            var wave = f.Context.PendingObjects.Skip(before).ToArray();
            Assert.True(wave.All(owner => owner.Prefab == "ZapMouse" && owner.Master == Master.RightPlayer && owner.Position.Z == 7));
            f.Update(); Assert.Equal(1,failures); Assert.Equal(before + 2,f.Context.PendingObjects.Length);
        }
        private sealed class TerminalBoss : SimComponent, IPveHealthMob, IPveObjectiveTarget {
            public int Hp { get; private set; } = 1000;
            public int MaxHp { get; private set; } = 1000;
            public bool IsObjectiveTarget => true;
            public bool IsSpawner => false;
            public bool IsTerminal => Hp <= 0;
            public TerminalBoss(SimObject owner) : base(owner) { }
            public void OverrideMaxHp(int value) { if (value > 0) Hp = MaxHp = value; }
            public void Damage(int value) { Hp -= value; }
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0);
            public readonly PveScenarioInstaller Installer = new PveScenarioInstaller { Components = new PveScenarioComponents() };
            public readonly PveScriptSystem System = new PveScriptSystem();
            public Fixture(params PveScenarioAction[] actions) {
                Context.RegisterPrefab("ZapMouse", owner => owner.AddComponent(new DummyMob(owner, 100)));
                Context.RegisterPrefab("FireSlime", owner => { owner.AddComponent(new DummyMob(owner, 100)); owner.AddComponent(new TimedSelfDestroyer(owner, 1)); });
                Context.RegisterPrefab("PveNatureSlimeNest", owner => { owner.AddComponent(new Spawner(owner, 0, "ZapMouse", 2, false, 1)); owner.AddComponent(new TerminalBoss(owner)); });
                Installer.Install(new PveInstallObject[0], Context); System.Runtime = Installer.Runtime; System.Runner.Installer = Installer; System.Runner.Components = new PveScenarioComponents();
                System.Scenario = new PveScenario(new[] { new PveScenarioEvent("e1", PveTriggerType.FrameNumGte, 0, actions: actions) });
            }
            public SimObject Install(string id, string prefab = "ZapMouse", int? hp = null) => Installer.InstallOne(new PveInstallObject(id, prefab, Master.RightPlayer, Vector3.Zero, hp), Context);
            public void Update() => System.Update(Context);
        }
        [PortedTest(Source)] public static void spawn_wave_spawns_count_objects_spread_on_x_belonging_to_right_player() {
            var f = new Fixture(new PveScenarioAction(PveActionType.SpawnWave, prefab: "ZapMouse", count: 3, x: 14, z: 5)); f.Update(); var world = f.Context.PendingObjects;
            Assert.Equal(3, world.Length); Assert.True(world.All(owner => owner.Master == Master.RightPlayer && owner.Prefab == "ZapMouse" && owner.Position.Z == 5)); Assert.Sequence(new[] { (FixMath.NET.Fix64)13.5m, (FixMath.NET.Fix64)14, (FixMath.NET.Fix64)14.5m }, world.Select(owner => owner.Position.X).OrderBy(x => x));
        }
        [PortedTest(Source)] public static void install_object_action_registers_under_its_installer_id_and_overrides_hp() {
            var f = new Fixture(new PveScenarioAction(PveActionType.InstallObject, "adds", "ZapMouse", x: 14, z: 7, maxHp: 250)); f.Update(); var owner = f.Installer.GetInstalledObject("adds"); Assert.Equal(250, PveScenarioInstaller.FindHealthMob(owner).MaxHp); Assert.Equal(new Vector3(14, 0, 7), owner.Position);
        }
        [PortedTest(Source)] public static void set_spawner_replaces_the_existing_spawner() {
            var f = new Fixture(new PveScenarioAction(PveActionType.SetSpawner, "boss", "ZapMouse", 5, interval: 3)); var boss = f.Install("boss", "PveNatureSlimeNest"); var old = boss.GetComponent<Spawner>(); f.Update(); boss.FlushComponents();
            Assert.Equal(1, boss.GetComponents<Spawner>().Length); var spawner = boss.GetComponent<Spawner>(); Assert.Equal(false, ReferenceEquals(old, spawner));
            int before = f.Context.PendingObjects.Length; f.Context.DeltaTime = 1; spawner.Update(); spawner.Update(); Assert.Equal(before, f.Context.PendingObjects.Length); spawner.Update(); Assert.Equal(before + 5, f.Context.PendingObjects.Length);
        }
        [PortedTest(Source)] public static void set_spawner_without_an_interval_uses_the_spawner_default() {
            var f = new Fixture(new PveScenarioAction(PveActionType.SetSpawner, "boss", "ZapMouse", 1)); var boss = f.Install("boss", "PveNatureSlimeNest"); f.Update(); boss.FlushComponents(); int before = f.Context.PendingObjects.Length; f.Context.DeltaTime = 1;
            boss.GetComponent<Spawner>().Update(); Assert.Equal(before, f.Context.PendingObjects.Length); boss.GetComponent<Spawner>().Update(); Assert.Equal(before + 1, f.Context.PendingObjects.Length);
        }
        [PortedTest(Source)] public static void set_spawner_with_zero_count_removes_the_spawner_without_replacing() {
            var f = new Fixture(new PveScenarioAction(PveActionType.SetSpawner, "boss", count: 0)); var boss = f.Install("boss", "PveNatureSlimeNest"); f.Update(); boss.FlushComponents(); Assert.Equal(0, boss.GetComponents<Spawner>().Length);
        }
        [PortedTest(Source)] public static void install_one_registers_the_object_under_its_installer_id() { var f = new Fixture(); var created = f.Install("boss"); Assert.Equal(created.Id, f.Installer.Runtime.GetInstalledObjectId("boss")); }
        [PortedTest(Source)] public static void install_leaves_hp_untouched_when_max_hp_is_none() { var f = new Fixture(); var mob = PveScenarioInstaller.FindHealthMob(f.Install("boss")); Assert.Equal(100, mob.MaxHp); Assert.Equal(100, mob.Hp); }
        [PortedTest(Source)] public static void install_overrides_max_hp_and_current_hp_when_given() { var f = new Fixture(); var mob = PveScenarioInstaller.FindHealthMob(f.Install("boss", hp: 500)); Assert.Equal(500, mob.MaxHp); Assert.Equal(500, mob.Hp); }
        [PortedTest(Source)] public static void install_installs_every_installer_in_the_list() {
            var f = new Fixture(); f.Installer.Install(new[] { new PveInstallObject("a", "ZapMouse", Master.RightPlayer, Vector3.Zero), new PveInstallObject("b", "ZapMouse", Master.RightPlayer, Vector3.Zero) }, f.Context);
            Assert.True(f.Installer.Runtime.GetInstalledObjectId("a") != -1); Assert.True(f.Installer.Runtime.GetInstalledObjectId("b") != -1); Assert.Equal(-1, f.Installer.Runtime.GetInstalledObjectId("unknown")); Assert.Equal(-1, f.Installer.Runtime.GetInstalledObjectId(null));
        }
        [PortedTest(Source)] public static void get_installed_object_is_none_when_never_installed_or_blank() { var f = new Fixture(); foreach (var id in new[] { "boss", "  ", null }) Assert.True(f.Installer.GetInstalledObject(id) == null); }
        [PortedTest(Source)] public static void a_scenario_structure_never_expires_on_its_own() { var f = new Fixture(); var owner = f.Install("lifetime", "FireSlime"); owner.FlushComponents(); Assert.Equal(0, owner.GetComponents<TimedSelfDestroyer>().Length); }
        [PortedTest(Source)] public static void hp_override_and_hp_trigger_use_the_boss_mob_not_the_spawner_added_before_it() {
            var f = new Fixture(); f.System.Scenario = new PveScenario(new[] { new PveScenarioEvent("e1", PveTriggerType.InstallerHpPercentLte, 50, target: "boss", lines: new[] { "half" }) });
            int sent = 0; f.System.SendScriptEvent = (id, message) => sent++; var boss = f.Install("boss", "PveNatureSlimeNest", 200);
            Assert.Equal(200, boss.GetComponent<TerminalBoss>().MaxHp); Assert.Equal(0, boss.GetComponent<Spawner>().MaxHp); f.Update(); Assert.Equal(0, sent);
            boss.GetComponent<TerminalBoss>().Damage(100); f.Context.FrameNumber = 1; f.Update(); Assert.Equal(2, sent);
        }
        [PortedTest(Source)] public static void installer_destroyed_fires_for_a_terminal_objective_target() {
            var f = new Fixture(); f.System.Scenario = new PveScenario(new[] { new PveScenarioEvent("e1", PveTriggerType.InstallerDestroyed, 0, target: "boss", lines: new[] { "dead" }) });
            int sent = 0; f.System.SendScriptEvent = (id, message) => sent++; var boss = f.Install("boss", "PveNatureSlimeNest"); f.Update(); Assert.Equal(0, sent);
            boss.GetComponent<TerminalBoss>().Damage(1000); Assert.Equal(false, boss.IsDestroyed); f.Context.FrameNumber = 1; f.Update(); Assert.Equal(2, sent);
        }
        private static Fixture ShieldFixture(params PveShield[] shields) { var f = new Fixture(); f.System.Scenario = new PveScenario(new PveScenarioEvent[0], shields: shields); return f; }
        [PortedTest(Source)] public static void a_prefab_without_an_initializer_does_not_stop_the_script() {
            var f = new Fixture(new PveScenarioAction(PveActionType.SpawnWave, prefab: "WaterSlime", count: 1, x: 1, z: 1));
            int failures = 0; f.Context.InitializationFailed += (owner, exception) => { Assert.Equal("WaterSlime", owner.Prefab); failures++; };
            f.Update(); f.Context.FrameNumber = 1; f.Update();
            Assert.Equal(1, f.Context.PendingObjects.Length); Assert.Equal(1, failures);
            Assert.True(f.Context.PendingObjects[0].IsDestroyed);
        }
        [PortedTest(Source)] public static void a_shielded_installer_gets_the_component_once_and_the_shield_follows_its_sources() {
            var f = ShieldFixture(new PveShield("boss", "pylon_a"), new PveShield("boss", "pylon_b")); var boss = f.Install("boss", "PveNatureSlimeNest"); f.Update(); f.Context.FrameNumber = 1; f.Update(); boss.FlushComponents();
            Assert.Equal(1, boss.GetComponents<PveShieldComponent>().Length); var shield = boss.GetComponent<PveShieldComponent>(); Assert.Equal(false, shield.IsShieldUp);
            var a = f.Install("pylon_a"); var b = f.Install("pylon_b"); Assert.True(shield.IsShieldUp); a.Destroy(); Assert.True(shield.IsShieldUp); b.Destroy(); Assert.Equal(false, shield.IsShieldUp);
        }
        [PortedTest(Source)] public static void a_shielded_installer_installed_late_is_picked_up_on_a_later_frame() {
            var f = ShieldFixture(new PveShield("boss", "pylon")); f.Update(); Assert.Equal(0, f.Context.PendingObjects.Length); var boss = f.Install("boss", "PveNatureSlimeNest"); Assert.Equal(0, boss.GetPendingComponents<PveShieldComponent>().Length);
            f.Context.FrameNumber = 1; f.Update(); boss.FlushComponents(); Assert.Equal(1, boss.GetComponents<PveShieldComponent>().Length);
        }
        [PortedTest(Source)] public static void a_destroyed_shielded_installer_gets_no_component() {
            var f = ShieldFixture(new PveShield("boss", "pylon")); var boss = f.Install("boss", "PveNatureSlimeNest"); boss.Destroy(); f.Update(); Assert.Equal(0, boss.GetComponents<PveShieldComponent>().Length); Assert.Equal(0, boss.GetPendingComponents<PveShieldComponent>().Length);
        }
        [PortedTest(Source)] public static void a_boss_that_reached_its_end_state_no_longer_counts_as_a_source() {
            var f = ShieldFixture(new PveShield("boss", "mini")); var boss = f.Install("boss"); var mini = f.Install("mini", "PveNatureSlimeNest"); f.Update(); boss.FlushComponents(); var shield = boss.GetComponent<PveShieldComponent>(); Assert.True(shield.IsShieldUp);
            mini.GetComponent<TerminalBoss>().Damage(1000); Assert.Equal(false, mini.IsDestroyed); Assert.Equal(false, shield.IsShieldUp);
        }
    }
}
