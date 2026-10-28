using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PveActionChecks {
        public static void Run() {
            var c = new SimulationContext(0) { DeltaTime = (Fix64)0.5m };
            c.RegisterPrefab("Unit", owner => owner.AddComponent(new MeleeAttackMob(owner, 100, 2, 1, 5, 1)));
            var installer = new PveScenarioInstaller(); installer.Install(new PveInstallObject[0], c);
            var runner = new PveActionRunner { Installer = installer, Components = new PveScenarioComponents(), LeftUserId = 1, RightUserId = 2 };
            runner.SpawnWave(c, "Unit", 3, 14, 5);
            Assert.Sequence(new[] { (Fix64)13.5m, (Fix64)14, (Fix64)14.5m }, c.PendingObjects.Select(owner => owner.Position.X)); Assert.True(c.PendingObjects.All(owner => owner.Master == Master.RightPlayer));
            runner.InstallObject(c, "gate", "Unit", 14, 7, 250); var gate = installer.GetInstalledObject("gate"); Assert.Equal(250, gate.GetComponent<MobComponent>().Hp);
            runner.SetSpawner("gate", "Unit", 2); gate.FlushComponents(); var original = gate.GetComponent<Spawner>(); Assert.True(original != null);
            runner.SetSpawner("gate", "Unit", 1, 3); gate.FlushComponents(); Assert.Equal(1, gate.GetComponents<Spawner>().Length); Assert.Equal(false, ReferenceEquals(original, gate.GetComponent<Spawner>()));
            runner.SetSpawner("gate", null, 0); gate.FlushComponents(); Assert.Equal(0, gate.GetComponents<Spawner>().Length);
            var summon = c.Spawn(Master.LeftPlayer, "Unit", new Vector3(2, 3, 3)); c.FlushObjects(); runner.PullPlayerUnits(c, "gate"); summon.FlushComponents(); Assert.Equal(0, summon.GetComponents<MobComponent>().Length);
            var pull = summon.GetComponent<PulledToPoint>(); pull.Update(); Assert.Equal((Fix64)3, summon.Position.Y); Assert.Near((Fix64)6, summon.Position.X, (Fix64)0.000001m); pull.Update(); Assert.Equal(false, summon.IsDestroyed); pull.Update(); Assert.True(summon.IsDestroyed); Assert.Equal(new Vector3(14, 3, 7), summon.Position); Assert.Equal(false, gate.IsDestroyed);
            var gone = c.Spawn(Master.LeftPlayer, "Unit", Vector3.Zero); c.FlushObjects(); runner.PullPlayerUnits(c, "missing"); Assert.True(gone.IsDestroyed);
            runner.SetBgm("ignored"); Assert.Equal(0, runner.States.Snapshot().Length);
            var sent = new List<KeyValuePair<long, PveChannelState>>(); runner.SendState = (id, state) => sent.Add(new KeyValuePair<long, PveChannelState>(id, state));
            runner.SetBgm("boss"); runner.SetBgm("boss"); runner.SetBgm(null); Assert.Equal(4, sent.Count); Assert.Sequence(new long[] { 1, 2, 1, 2 }, sent.Select(item => item.Key));
            runner.SendStatesTo(7); Assert.Equal(5, sent.Count); Assert.Equal(7L, sent[4].Key); Assert.Equal(2, sent[4].Value.Seq); Assert.True(sent[4].Value.Value == null);
        }
    }
}
