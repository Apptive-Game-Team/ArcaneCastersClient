using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;

namespace ArcaneCasters.Parity {
    public static class PveScriptParityTests {
        private const string Source = "tests/pve_engine.rs";
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0);
            public readonly PveScenarioInstaller Installer = new PveScenarioInstaller();
            public readonly PveScriptSystem System = new PveScriptSystem();
            public readonly List<PveScriptEvent> Sent = new List<PveScriptEvent>();
            public Fixture(params PveScenarioEvent[] events) {
                Context.RegisterPrefab("Unit", owner => owner.AddComponent(new DummyMob(owner, 100)));
                Installer.Install(new PveInstallObject[0], Context); System.Scenario = new PveScenario(events); System.Runtime = Installer.Runtime;
                System.Runner.Installer = Installer; System.Runner.Components = new PveScenarioComponents(); System.Runner.LeftUserId = 1; System.Runner.RightUserId = 2;
                System.SendScriptEvent = (id, message) => Sent.Add(message);
            }
            public SimObject Boss() => Installer.InstallOne(new PveInstallObject("boss", "Unit", Master.RightPlayer, Vector3.Zero), Context);
            public void Update(int frame) { Context.FrameNumber = frame; System.Update(Context); }
        }
        private static PveScenarioEvent Event(PveTriggerType type, int value = 0, string target = null, string[] lines = null) => new PveScenarioEvent("e1", type, value, target, key: "key.e1", lines: lines ?? new[] { "hi" });
        [PortedTest(Source)] public static void frame_num_gte_fires_once_the_frame_is_reached() {
            var f = new Fixture(Event(PveTriggerType.FrameNumGte, 100)); f.Update(50); Assert.Equal(0, f.Sent.Count); f.Update(100); Assert.Equal(2, f.Sent.Count);
        }
        [PortedTest(Source)] public static void seconds_gte_fires_once_elapsed_frames_cover_the_seconds() {
            var f = new Fixture(Event(PveTriggerType.SecondsGte, 5)); f.Update(99); Assert.Equal(0, f.Sent.Count); f.Update(100); Assert.Equal(2, f.Sent.Count);
        }
        [PortedTest(Source)] public static void frame_num_gte_counts_twenty_fps_frames_at_any_tick_rate() {
            foreach (int rate in new[] { 20, 60 }) { var f = new Fixture(Event(PveTriggerType.FrameNumGte, 20)); f.Context.SetTickRate(rate); int at = -1; for (int i = 1; i <= rate * 2; i++) { f.Update(i); if (f.Sent.Count > 0) { at = i; break; } } Assert.Equal(rate, at); }
        }
        [PortedTest(Source)] public static void installer_hp_percent_lte_fires_only_once_hp_drops_to_threshold() {
            var f = new Fixture(Event(PveTriggerType.InstallerHpPercentLte, 20, "boss")); var boss = f.Boss().GetComponent<DummyMob>(); f.Update(0); Assert.Equal(0, f.Sent.Count); boss.ApplyDamage(new Hit(50, 0)); f.Update(1); Assert.Equal(0, f.Sent.Count); boss.ApplyDamage(new Hit(30, 0)); f.Update(2); Assert.Equal(2, f.Sent.Count);
        }
        [PortedTest(Source)] public static void installer_hp_percent_lte_does_not_fire_at_zero_hp_or_before_the_installer_exists() { var f = new Fixture(Event(PveTriggerType.InstallerHpPercentLte, 100, "boss")); f.Update(0); Assert.Equal(0, f.Sent.Count); }
        [PortedTest(Source)] public static void installer_hp_percent_lte_ignores_a_dead_installer() { var f = new Fixture(Event(PveTriggerType.InstallerHpPercentLte, 100, "boss")); f.Boss().GetComponent<DummyMob>().ApplyDamage(new Hit(100, 0)); f.Update(0); Assert.Equal(0, f.Sent.Count); }
        [PortedTest(Source)] public static void installer_destroyed_fires_once_the_object_is_destroyed() { var f = new Fixture(Event(PveTriggerType.InstallerDestroyed, target: "boss")); var boss = f.Boss(); f.Update(0); Assert.Equal(0, f.Sent.Count); boss.Destroy(); f.Update(1); Assert.Equal(2, f.Sent.Count); }
        [PortedTest(Source)] public static void installer_destroyed_does_not_fire_for_an_object_not_yet_in_the_world_list() { var f = new Fixture(Event(PveTriggerType.InstallerDestroyed, target: "boss")); f.Boss(); Assert.Equal(0, f.Context.Objects.Length); f.Update(0); Assert.Equal(0, f.Sent.Count); }
        [PortedTest(Source)] public static void installer_destroyed_does_not_fire_before_the_installer_exists() { var f = new Fixture(Event(PveTriggerType.InstallerDestroyed, target: "boss")); f.Update(0); Assert.Equal(0, f.Sent.Count); }
        [PortedTest(Source)] public static void does_not_send_dialogue_when_the_event_has_no_lines() { var f = new Fixture(Event(PveTriggerType.FrameNumGte, lines: new string[0])); f.Update(0); Assert.Equal(0, f.Sent.Count); }
        [PortedTest(Source)] public static void every_event_fires_at_most_once() { var f = new Fixture(Event(PveTriggerType.FrameNumGte)); f.Update(0); f.Update(0); f.Update(0); Assert.Equal(2, f.Sent.Count); }
        [PortedTest(Source)] public static void the_speaker_object_id_comes_from_the_runtime() {
            var f = new Fixture(new PveScenarioEvent("e1", PveTriggerType.FrameNumGte, 0, speaker: "boss", lines: new[] { "hi" })); var boss = f.Boss(); f.Update(0); Assert.Equal(boss.Id, f.Sent[0].SpeakerObjectId);
            var g = new Fixture(new PveScenarioEvent("e1", PveTriggerType.FrameNumGte, 0, speaker: "missing", lines: new[] { "hi" })); g.Update(0); Assert.Equal(-1, g.Sent[0].SpeakerObjectId);
        }
        public static void CheckActionsReplayAndShields() {
            var f = new Fixture(new PveScenarioEvent("e1", PveTriggerType.FrameNumGte, 0, lines: new[] { "hi" }, actions: new[] {
                new PveScenarioAction((PveActionType)999), new PveScenarioAction(PveActionType.InstallObject, "pillar", "Unit", maxHp: 200), new PveScenarioAction(PveActionType.SetBgm, bgmKey: "boss") }));
            var boss = f.Boss(); var stateMessages = new List<PveChannelState>(); f.System.Runner.SendState = (id, state) => stateMessages.Add(state);
            f.System.Scenario = new PveScenario(f.System.Scenario.Events, shields: new[] { new PveShield("boss", "pillar") });
            int errors = 0; f.System.Failed += (id, error) => errors++;
            f.Update(0); boss.FlushComponents(); Assert.Equal(1, errors); Assert.Equal(2, stateMessages.Count); Assert.Equal(200, f.Installer.GetInstalledObject("pillar").GetComponent<DummyMob>().Hp);
            boss.GetComponent<DummyMob>().ApplyDamage(new Hit(20, 0)); Assert.Equal(100, boss.GetComponent<DummyMob>().Hp);
            f.Update(1); boss.FlushComponents(); Assert.Equal(1, boss.GetComponents<PveShieldComponent>().Length); Assert.Equal(1, errors);
            f.Installer.GetInstalledObject("pillar").Destroy(); boss.GetComponent<DummyMob>().ApplyDamage(new Hit(20, 0)); Assert.Equal(80, boss.GetComponent<DummyMob>().Hp);
            f.Sent.Clear(); f.Context.FrameNumber = 200; f.System.SendRecentEventsTo(f.Context, 7, 0); Assert.Equal(1, f.Sent.Count); Assert.Equal(1, f.Sent[0].Seq);
            f.System.SendRecentEventsTo(f.Context, 7, 1); Assert.Equal(1, f.Sent.Count); f.Context.FrameNumber = 201; f.System.SendRecentEventsTo(f.Context, 7, 0); Assert.Equal(1, f.Sent.Count);
            f.System.SendStatesTo(7); Assert.Equal(3, stateMessages.Count);
        }
    }
}
