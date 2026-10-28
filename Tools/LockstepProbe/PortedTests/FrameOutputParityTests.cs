using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class FrameOutputParityTests {
        private const string FrameSource = "game/service/system/frame_data_system.rs";
        private const string SyncSource = "game/service/system/sync_frame_data_system.rs";
        private sealed class Fixture : IDisposable {
            public readonly SimulationContext Context = new SimulationContext();
            public readonly BattleSessionOutput Output;
            public readonly List<(long id, BattlePresentationFrame frame)> Sent = new List<(long, BattlePresentationFrame)>();
            public bool Subscribed;
            public Fixture(bool registry = true) {
                Context.SetTickRate(20); Context.FrameNumber = 3;
                Context.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100)); Context.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100));
                var economy = new BattleEconomySystem(); economy.Update(Context);
                Output = new BattleSessionOutput(11,22,(id, frame) => Sent.Add((id,frame)), registry ? () => Subscribed : (Func<bool>)null); Output.Initialize(Context, economy, 100000);
            }
            public void Frame(bool sync = false) { Output.BeginFrame(Context); if (sync) Output.BuildSnapshot(Context); Output.EndFrame(Context); }
            public BattlePresentationFrame[] To(long id) => Sent.Where(s => s.id == id).Select(s => s.frame).ToArray();
            public void Dispose() => Output.Dispose();
        }
        [PortedTest(FrameSource)] public static void builds_no_spectator_payload_when_nobody_is_subscribed() {
            using (var f = new Fixture()) { f.Frame(); Assert.True(f.Output.SpectatorFrame == null); Assert.Equal(0, f.To(0).Length); }
        }
        [PortedTest(FrameSource)] public static void still_sends_both_players_their_frames_without_spectators() {
            using (var f = new Fixture()) { f.Frame(); Assert.Equal(1, f.To(11).Length); Assert.Equal(1, f.To(22).Length); Assert.Equal(f.Output.LeftFrame, f.To(11)[0]); Assert.Equal(f.Output.RightFrame, f.To(22)[0]); }
        }
        [PortedTest(FrameSource)] public static void builds_and_sends_the_spectator_payload_while_someone_is_subscribed() {
            using (var f = new Fixture { Subscribed = true }) { f.Frame(); Assert.Equal(0, f.Output.SpectatorFrame.Mana); Assert.Sequence(new[] { f.Output.SpectatorFrame }, f.To(0)); }
            using (var f = new Fixture { Subscribed = true }) {
                var effect = new BattleEvent("effect", 3, 7); f.Context.AddEvent(effect); f.Frame();
                foreach (long id in new long[] { 11,22,0 }) Assert.Sequence(new[] { effect }, f.To(id)[0].Events);
                f.To(11)[0].Events[0] = default; Assert.Equal(effect, f.To(22)[0].Events[0]); Assert.Equal(effect, f.To(0)[0].Events[0]);
                f.Frame(); foreach (long id in new long[] { 11,22,0 }) Assert.Equal(0, f.To(id)[1].Events.Length);
            }
        }
        [PortedTest(FrameSource)] public static void picks_up_a_spectator_on_the_next_frame_and_drops_it_when_they_leave() {
            using (var f = new Fixture()) {
                f.Output.BeginFrame(f.Context); Assert.True(f.Output.SpectatorFrame == null);
                f.Subscribed = true; f.Output.BeginFrame(f.Context); Assert.True(f.Output.SpectatorFrame != null);
                f.Subscribed = false; f.Output.BeginFrame(f.Context); Assert.True(f.Output.SpectatorFrame == null);
            }
        }
        [PortedTest(FrameSource)] public static void sends_nothing_to_the_broadcast_destination_when_the_session_has_no_registry_wired() {
            using (var f = new Fixture(false)) { f.Frame(); Assert.True(f.Output.SpectatorFrame == null); Assert.Equal(0, f.To(0).Length); }
        }
        private static Fixture Sync(bool subscribed) {
            var f = new Fixture { Subscribed = subscribed }; f.Context.FrameNumber = 10;
            f.Context.Players[Master.LeftPlayer].AddCard(2); f.Context.Players[Master.LeftPlayer].AddCard(1); f.Context.Players[Master.RightPlayer].AddCard(4); f.Frame(true); return f;
        }
        [PortedTest(SyncSource)] public static void sends_spectators_a_sync_frame_without_any_player_hand() {
            using (var f = Sync(true)) { var frames = f.To(0); Assert.Equal(1, frames.Length); var frame = frames[0]; Assert.True(frame.IsSync); Assert.Equal(0, frame.Cards.Length); Assert.Equal(0, frame.Mana); Assert.Equal(10, frame.Frame); Assert.Equal(20, frame.TickRate); }
        }
        [PortedTest(SyncSource)] public static void keeps_sending_each_player_their_own_hand() {
            using (var f = Sync(true)) { Assert.Sequence(new long[] { 2,1 }, f.To(11)[0].Cards); Assert.Sequence(new long[] { 4 }, f.To(22)[0].Cards); }
        }
        [PortedTest(SyncSource)] public static void sends_no_spectator_sync_frame_when_nobody_is_subscribed() {
            using (var f = Sync(false)) { Assert.Equal(0, f.To(0).Length); Assert.Equal(2, f.Sent.Count(s => s.frame.IsSync)); }
        }
    }
}
