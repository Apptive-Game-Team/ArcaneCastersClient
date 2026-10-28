using System.Collections.Generic;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class BotActionParityTests {
        private const string Source = "game/domain/bot/bot_action.rs";
        private sealed class Recorder : IBotInputHandler {
            public readonly List<(Master, long)> Seen = new List<(Master, long)>();
            public void HandleBotPlayerInput(SimulationContext context, Master side, BotInputRequest request) => Seen.Add((side, request.MagicId));
        }
        [PortedTest(Source)] public static void queues_the_cast_for_the_next_frame() {
            var context = new SimulationContext(1); var recorder = new Recorder(); context.InsertService(new BotInputHandlerService(recorder));
            Assert.True(new BotAction().UseCard(context.ActionSender(), new BotInputRequest(0, 34, null), Master.RightPlayer));
            Assert.Equal(0, recorder.Seen.Count); context.DrainActions(); Assert.Sequence(new[] { (Master.RightPlayer, 34L) }, recorder.Seen);
        }
        public static void CheckMissingHandlerAndOverflow() {
            var context = new SimulationContext(1); var action = new BotAction(); int missing = 0; action.MissingHandler += side => { Assert.Equal(Master.LeftPlayer, side); missing++; };
            action.UseCard(context.ActionSender(), new BotInputRequest(7, 34, null), Master.LeftPlayer); Assert.Equal(0, missing); context.DrainActions(); Assert.Equal(1, missing);
            for (int i = 0; i < GameActionQueue<SimulationContext>.Capacity; i++) Assert.True(context.SubmitAction("fill", _ => { }));
            Assert.True(!action.UseCard(context.ActionSender(), new BotInputRequest(8, 34, null), Master.LeftPlayer)); context.DrainActions(); Assert.Equal(1, missing);
        }
    }
}
