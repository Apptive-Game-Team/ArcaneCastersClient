using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class PveReplayParityTests {
        private const string Source = "tests/pve_engine.rs";
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0);
            public readonly PveScriptSystem System = new PveScriptSystem();
            public readonly List<KeyValuePair<long, object>> Sent = new List<KeyValuePair<long, object>>();
            public Fixture(params PveScenarioEvent[] events) {
                System.Scenario = new PveScenario(events); System.Runner.LeftUserId = 1; System.Runner.RightUserId = 2;
                System.SendScriptEvent = (id, value) => Sent.Add(new KeyValuePair<long, object>(id, value)); System.Runner.SendState = (id, value) => Sent.Add(new KeyValuePair<long, object>(id, value));
            }
            public void Update(int frame) { Context.FrameNumber = frame; System.Update(Context); }
            public PveScriptEvent[] Events(long id) => Sent.Where(item => item.Key == id).Select(item => item.Value).OfType<PveScriptEvent>().ToArray();
            public PveChannelState[] States(long id) => Sent.Where(item => item.Key == id).Select(item => item.Value).OfType<PveChannelState>().ToArray();
        }
        private static PveScenarioEvent Event(string id, int frame, string[] lines, params PveScenarioAction[] actions) => new PveScenarioEvent(id, PveTriggerType.FrameNumGte, frame, key: "key." + id, lines: lines, actions: actions);
        private static Fixture Events() => new Fixture(Event("e1", 10, new[] { "one" }), Event("silent", 10, new string[0]), Event("e2", 20, new[] { "two" }), Event("e3", 300, new[] { "three" }));
        private static Fixture Bgm() => new Fixture(
            Event("boss", 10, new[] { "here I come" }, new PveScenarioAction(PveActionType.SetBgm, bgmKey: "boss_battle")),
            Event("again", 20, new string[0], new PveScenarioAction(PveActionType.SetBgm, bgmKey: "boss_battle")),
            Event("calm", 30, new string[0], new PveScenarioAction(PveActionType.SetBgm)));
        [PortedTest(Source)] public static void numbers_events_with_lines_from_one_and_skips_events_without_lines() {
            var f = Events(); foreach (int frame in new[] { 10, 20, 300 }) f.Update(frame);
            Assert.Sequence(new[] { 1, 2, 3 }, f.Events(1).Select(e => e.Seq)); Assert.Sequence(new[] { "key.e1", "key.e2", "key.e3" }, f.Events(1).Select(e => e.Key)); Assert.Sequence(new[] { 1, 2, 3 }, f.Events(2).Select(e => e.Seq));
        }
        [PortedTest(Source)] public static void replays_only_newer_and_recent_events_in_order_to_the_asking_user() {
            var f = Events(); f.Update(10); f.Update(20); f.Sent.Clear(); f.Context.FrameNumber = 30; f.System.SendRecentEventsTo(f.Context, 7, 0);
            Assert.Sequence(new[] { 1, 2 }, f.Events(7).Select(e => e.Seq)); Assert.Equal(0, f.Events(1).Length); Assert.Equal(0, f.Events(2).Length);
            f.Sent.Clear(); f.System.SendRecentEventsTo(f.Context, 7, 1); Assert.Sequence(new[] { 2 }, f.Events(7).Select(e => e.Seq));
            f.Sent.Clear(); f.System.SendRecentEventsTo(f.Context, 7, 2); Assert.Equal(0, f.Sent.Count);
        }
        [PortedTest(Source)] public static void does_not_replay_events_older_than_the_replay_window() {
            var f = Events(); f.Update(10); f.Update(20); f.Sent.Clear(); f.Context.FrameNumber = 10 + 10 * f.Context.TickRate + 1; f.System.SendRecentEventsTo(f.Context, 7, 0);
            Assert.Sequence(new[] { 2 }, f.Events(7).Select(e => e.Seq)); f.Sent.Clear(); f.Update(300); Assert.Sequence(new[] { 3 }, f.Events(1).Select(e => e.Seq));
        }
        [PortedTest(Source)] public static void play_bgm_action_pushes_the_bgm_state_to_both_users_only_when_the_value_changes() {
            var f = Bgm(); foreach (int frame in new[] { 10, 20, 30 }) f.Update(frame);
            foreach (long id in new long[] { 1, 2 }) { Assert.Sequence(new[] { new PveChannelState("bgm", "boss_battle", 1), new PveChannelState("bgm", null, 2) }, f.States(id)); Assert.Equal("pveState", f.States(id)[0].Type); }
            Assert.Sequence(new[] { 1 }, f.Events(1).Select(e => e.Seq));
        }
        [PortedTest(Source)] public static void pve_sync_carries_the_current_bgm_long_after_the_event_left_the_replay_window() {
            var f = Bgm(); f.Update(10); f.Sent.Clear(); f.Context.FrameNumber = 10 + 10 * f.Context.TickRate + 1; f.System.SendRecentEventsTo(f.Context, 7, 0); f.System.SendStatesTo(7);
            Assert.Equal(1, f.Sent.Count); Assert.Equal(7L, f.Sent[0].Key); Assert.Equal(new PveChannelState("bgm", "boss_battle", 1), (PveChannelState)f.Sent[0].Value);
        }
        [PortedTest(Source)] public static void pve_sync_sends_no_state_before_any_channel_is_set() { var f = Bgm(); f.System.SendStatesTo(7); Assert.Equal(0, f.Sent.Count); }
    }
}
