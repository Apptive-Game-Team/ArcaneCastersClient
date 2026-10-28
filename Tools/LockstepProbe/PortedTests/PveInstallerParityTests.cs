using ArcaneCasters.Simulation;
using BEPUutilities;

namespace ArcaneCasters.Parity {
    public static class PveInstallerParityTests {
        [PortedTest("game/service/pve/pve_scenario_installer.rs")] public static void max_hp_override_waits_for_the_boss_mob_to_appear() {
            var context = new SimulationContext(0); var installer = new PveScenarioInstaller();
            var boss = new SimObject(context, Master.RightPlayer, "PveNatureSlimeNest", Vector3.Zero);
            installer.ApplyMaxHpOverride(boss, 600);
            var mob = new DummyMob(boss, 1000); boss.AddComponent(mob); boss.FlushComponents();
            installer.FinishPendingSetup(); Assert.Equal(1000, mob.MaxHp);
            boss.SetStatus(ObjectStatus.Idle); installer.FinishPendingSetup(); Assert.Equal(600, mob.MaxHp); Assert.Equal(600, mob.Hp);
            mob.ApplyDamage(new Hit(590, 0)); installer.FinishPendingSetup(); Assert.Equal(10, mob.Hp);
        }
        public static void CheckInstallLifecycle() {
            var context = new SimulationContext(0); context.RegisterPrefab("Structure", owner => {
                owner.AddComponent(new Spawner(owner, 0, "Structure", 2, false, 1));
                owner.AddComponent(new DummyMob(owner, 100));
                owner.AddComponent(new PveGateKeeperMob(owner, 200));
                owner.AddComponent(new TimedSelfDestroyer(owner, 1));
            });
            var installer = new PveScenarioInstaller { Components = new PveScenarioComponents() };
            installer.Install(new PveInstallObject[0], context);
            Assert.Equal(-1, installer.Runtime.GetInstalledObjectId(null)); Assert.Equal(-1, installer.Runtime.GetInstalledObjectId("boss"));
            var boss = installer.InstallOne(new PveInstallObject("boss", "Structure", Master.RightPlayer, new Vector3(10, 0, 5), 600), context);
            Assert.True(ReferenceEquals(boss, installer.GetInstalledObject("boss"))); Assert.Equal(boss.Id, installer.Runtime.GetInstalledObjectId("boss"));
            Assert.Equal(0, context.Objects.Length); Assert.Equal(100, boss.GetComponent<DummyMob>().Hp); Assert.Equal(600, boss.GetComponent<PveGateKeeperMob>().Hp);
            boss.FlushComponents(); Assert.Equal(0, boss.GetComponents<TimedSelfDestroyer>().Length);
            boss.Destroy(); Assert.True(ReferenceEquals(boss, installer.GetInstalledObject("boss")));
            var replacement = installer.InstallOne(new PveInstallObject("boss", "Structure", Master.RightPlayer, Vector3.Zero), context);
            Assert.True(ReferenceEquals(replacement, installer.GetInstalledObject("boss"))); Assert.Equal(200, replacement.GetComponent<PveGateKeeperMob>().Hp);
            var oldRuntime = installer.Runtime; installer.Install(new PveInstallObject[0], context);
            Assert.True(installer.GetInstalledObject("boss") == null); Assert.True(ReferenceEquals(replacement, oldRuntime.GetInstalledGameObject("boss")));
        }
    }
}
