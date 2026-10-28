using System.Collections.Generic;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class SessionEmoteParityTests {
        private const string Source = "game/domain/session_object.rs";
        [PortedTest(Source)] public static void emote_cooldown_drops_the_second_emote() {
            var c = new SimulationContext(1); var cooldown = new EmoteCooldown(new FrameMillisClock(c));
            Assert.True(cooldown.TryConsume(Master.LeftPlayer)); Assert.True(!cooldown.TryConsume(Master.LeftPlayer)); Assert.True(cooldown.TryConsume(Master.RightPlayer)); Assert.True(!cooldown.TryConsume(null));
        }
        public static void CheckFrameBoundariesAndParsing() {
            foreach (int rate in new[] { 20, 60 }) {
                var c = new SimulationContext(1); c.SetTickRate(rate); c.UserIds[Master.LeftPlayer] = 11; c.UserIds[Master.RightPlayer] = 22;
                var session = new SessionEmotes(c); var sent = new List<(Master, Emote)>(); session.Sent += (side, emote) => sent.Add((side, emote));
                foreach (var invalid in new[] { null, "greet", " Greet", "1", "Unknown" }) Assert.True(!session.ReceivePlayer(11, invalid));
                Assert.True(!session.ReceivePlayer(99, "Greet")); Assert.Equal(0L, session.Cooldown.LeftUntilMillis);
                Assert.True(session.ReceivePlayer(11, "Greet")); Assert.True(!session.Cooldown.TryConsume(Master.LeftPlayer)); Assert.True(session.Cooldown.TryConsume(Master.RightPlayer));
                Assert.True(!session.ReceivePlayer(22, "Laugh")); Assert.Equal(3000L, session.Cooldown.LeftUntilMillis);
                c.FrameNumber = 3 * rate - 1; Assert.True(!session.ReceivePlayer(11, "Cry")); c.FrameNumber++; Assert.True(session.ReceivePlayer(11, "Surprised"));
                Assert.Sequence(new[] { (Master.LeftPlayer, Emote.Greet), (Master.LeftPlayer, Emote.Surprised) }, sent); Assert.Equal(6000L, session.Cooldown.LeftUntilMillis); Assert.True(!session.Cooldown.TryConsume(Master.None));
            }
        }
    }
}
