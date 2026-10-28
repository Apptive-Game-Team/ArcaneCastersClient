using System;
using System.Collections.Generic;
using System.Threading;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class GameActionParityTests {
        private const string Source = "game/service/game_action_queue.rs";
        [PortedTest(Source)] public static void runs_queued_actions_in_submission_order() {
            var queue = new GameActionQueue<List<string>>(); var log = new List<string>();
            queue.Submit("first", c => c.Add("first")); queue.Submit("second", c => c.Add("second"));
            Assert.Equal(0, log.Count); queue.Drain(log); Assert.Sequence(new[] { "first", "second" }, log); Assert.Equal(0, queue.Count);
        }
        [PortedTest(Source)] public static void leaves_actions_queued_during_a_drain_for_the_next_drain() {
            var queue = new GameActionQueue<List<string>>(); var log = new List<string>();
            queue.Submit("outer", c => { c.Add("outer"); queue.Submit("inner", x => x.Add("inner")); });
            queue.Drain(log); Assert.Sequence(new[] { "outer" }, log); Assert.Equal(1, queue.Count);
            queue.Drain(log); Assert.Sequence(new[] { "outer", "inner" }, log);
        }
        [PortedTest(Source)] public static void keeps_draining_after_an_action_panics() {
            var queue = new GameActionQueue<List<string>>(); var log = new List<string>();
            string failed = null; queue.Failed += (name, error) => failed = name + ":" + error.Message;
            queue.Submit("boom", _ => throw new InvalidOperationException("boom")); queue.Submit("after", c => c.Add("after"));
            queue.Drain(log); Assert.Sequence(new[] { "after" }, log); Assert.Equal("boom:boom", failed);
        }
        [PortedTest(Source)] public static void rejects_submissions_once_full_so_a_flood_cannot_grow_the_session() {
            var queue = new GameActionQueue<object>(); int runs = 0;
            for (int i = 0; i < GameActionQueue<object>.Capacity; i++) Assert.True(queue.Submit("fill", _ => runs++));
            Assert.True(!queue.Submit("overflow", _ => runs++)); queue.Drain(null); Assert.Equal(256, runs);
            Assert.True(queue.Submit("reused", _ => runs++)); queue.Drain(null); Assert.Equal(257, runs);
        }
        [RequiresThreads]
        [PortedTest(Source)] public static void accepts_concurrent_submissions_without_losing_any() {
            var queue = new GameActionQueue<object>(); int runs = 0, accepted = 0;
            using (var start = new Barrier(8)) {
                var threads = new Thread[8];
                for (int i = 0; i < threads.Length; i++) {
                    var sender = new GameActionSender<object>(queue);
                    threads[i] = new Thread(() => { start.SignalAndWait(); for (int n = 0; n < 20; n++) if (sender.Submit("concurrent", _ => runs++)) Interlocked.Increment(ref accepted); });
                    threads[i].Start();
                }
                foreach (var thread in threads) thread.Join();
            }
            Assert.Equal(160, accepted); Assert.Equal(0, runs); queue.Drain(null); Assert.Equal(160, runs);
        }
        [PortedTest("game/service/system/game_action_system.rs")] public static void drains_queued_actions_during_its_update() {
            var context = new SimulationContext(); context.SubmitAction("frame", c => c.FrameNumber++);
            new GameActionSystem().Update(context); Assert.Equal(1, context.FrameNumber); Assert.Equal(0, context.PendingActionCount);
        }
    }
}
