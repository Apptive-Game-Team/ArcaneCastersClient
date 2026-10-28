using System.Collections.Generic;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BotAgentParityTests {
        private const string Source = "game/domain/bot/bot_agent.rs";
        private sealed class Clock : IMillisClock { public long Value = 1000000; public long NowMillis => Value; }
        private sealed class Counters : IBotCounterScorer {
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
        }
        private static BotAgent Agent(IMillisClock clock, int delay = 250, bool allowEmote = true) {
            var persona = new BotPersona { ThinkingTimeMs = delay, ReactionIntervalFrames = 8 };
            var parameters = new Dictionary<string, Fix64> { ["spirit.mana_cost"] = 3, ["spirit.range"] = 6 };
            var brain = new BotBrain(new TestBotCatalog(new BotMagic(5, "spirit", summoning: new SummonedBodies("FireSpirit"))), new Counters(), persona, new JavaRandom(1));
            brain.ThinkFailed += error => throw error; int sequence = 0;
            return new BotAgent(new RuleDecisionSource(brain), Master.RightPlayer, persona, parameters, clock, () => sequence++, new BotEmoteDirector(BotTemperament.Warm, new JavaRandom(1)), _ => allowEmote);
        }
        private static BotEye Eye() => new BotEye(new BotVisibleObject[0], new long[] { 5 }, 10, 100);
        [RequiresThreads]
        [PortedTest(Source)] public static void the_agent_can_be_shared_with_the_executor_thread() {
            var agent = Agent(new Clock(), 0); var context = new SimulationContext(1);
            var requests = new List<BotInputRequest>(); context.InsertService(new BotInputHandlerService(new RecordingInput(requests)));
            var errors = new System.Exception[2]; var workers = new System.Threading.Thread[2];
            using (var ready = new System.Threading.Barrier(2)) {
                for (int i = 0; i < workers.Length; i++) {
                    int slot = i;
                    workers[i] = new System.Threading.Thread(() => {
                        try {
                            if (!ready.SignalAndWait(System.TimeSpan.FromSeconds(5))) throw new System.TimeoutException("Executor did not start");
                            for (int job = 0; job < 4; job++) agent.OnTick(Eye(), context.ActionSender());
                        } catch (System.Exception error) { errors[slot] = error; }
                    });
                    workers[i].Start();
                }
                foreach (var worker in workers) Assert.True(worker.Join(10000));
            }
            foreach (var error in errors) if (error != null) throw error;
            Assert.Equal(8, context.PendingActionCount); Assert.Equal(5L, agent.LastDecision.MagicId);
            context.DrainActions(); Assert.Equal(8, requests.Count);
            for (int i = 0; i < requests.Count; i++) { Assert.Equal(i, requests[i].Id); Assert.Equal(5L, requests[i].MagicId); }
        }
        [PortedTest(Source)] public static void reacts_as_often_per_second_at_any_tick_rate() {
            foreach (int rate in new[] { 20, 60 }) {
                var agent = Agent(new Clock()); int reactions = 0, first = -1;
                for (int frame = 1; frame <= 8 * rate; frame++) if (agent.ShouldProcess(Fix64.One / rate)) { reactions++; if (first < 0) first = frame; }
                Assert.Equal(20, reactions); Assert.Equal(rate * 2 / 5, first);
            }
        }
        [PortedTest(Source)] public static void a_decision_waits_out_the_thinking_time_before_it_is_queued() {
            var clock = new Clock(); var agent = Agent(clock); var context = new SimulationContext(1); var thoughts = new List<BotThought>(); agent.Thought += thoughts.Add;
            agent.OnTick(Eye(), context.ActionSender()); Assert.True(agent.LastDecision == null); Assert.Equal(0, context.PendingActionCount); Assert.True(!agent.ShouldProcess(0));
            clock.Value += 249; Assert.True(!agent.HasReadyPendingDecision); clock.Value++; Assert.True(agent.HasReadyPendingDecision); Assert.True(agent.ShouldProcess(0));
            agent.OnTick(Eye(), context.ActionSender()); Assert.Equal(5L, agent.LastDecision.MagicId); Assert.Equal(1, context.PendingActionCount); Assert.Equal(1, thoughts.Count); Assert.Sequence(new long[] { 5 }, thoughts[0].Cards); Assert.Equal(Master.RightPlayer, thoughts[0].Side);
        }
        [PortedTest(Source)] public static void a_zero_thinking_time_dispatches_in_the_same_tick() {
            var agent = Agent(new Clock { Value = 0 }, 0); var context = new SimulationContext(1); agent.OnTick(Eye(), context.ActionSender()); Assert.Equal(1, context.PendingActionCount); Assert.Equal("score.best-value", agent.LastDecision.RuleId);
        }
        [PortedTest(Source)] public static void publishes_an_idle_thought_every_ten_seconds_until_it_has_decided_anything() {
            var clock = new Clock(); var agent = Agent(clock); var context = new SimulationContext(1); var thoughts = new List<BotThought>(); agent.Thought += thoughts.Add;
            clock.Value += BotAgent.ThoughtIntervalMillis; agent.OnTick(new BotEye(new BotVisibleObject[0], new long[0], 10, 100), context.ActionSender());
            Assert.Equal(1, thoughts.Count); Assert.Equal("idle.observing", thoughts[0].RuleId); Assert.Equal(0, thoughts[0].Cards.Count); Assert.Equal(new BEPUutilities.Vector3(17, 0, 5), thoughts[0].Target);
        }
        public static void CheckFrameClockDispatch() {
            foreach (int rate in new[] { 20, 60 }) {
                var context = new SimulationContext(1); context.SetTickRate(rate); var clock = new FrameMillisClock(context); var agent = Agent(clock);
                var requests = new List<BotInputRequest>(); context.InsertService(new BotInputHandlerService(new RecordingInput(requests)));
                agent.OnTick(Eye(), context.ActionSender()); int ready = rate / 4;
                context.FrameNumber = ready - 1; agent.OnTick(Eye(), context.ActionSender()); Assert.Equal(0, context.PendingActionCount);
                context.FrameNumber = ready; agent.OnTick(Eye(), context.ActionSender()); Assert.Equal(1, context.PendingActionCount); context.DrainActions(); Assert.Equal(1, requests.Count); Assert.Equal(0, requests[0].Id);
                agent.OnTick(Eye(), context.ActionSender()); Assert.Equal(0, context.PendingActionCount);
                context.FrameNumber = ready * 2; agent.OnTick(Eye(), context.ActionSender()); context.DrainActions(); Assert.Equal(2, requests.Count); Assert.Equal(1, requests[1].Id);
            }
        }
        private sealed class RecordingInput : IBotInputHandler {
            private readonly List<BotInputRequest> requests;
            public RecordingInput(List<BotInputRequest> requests) { this.requests = requests; }
            public void HandleBotPlayerInput(SimulationContext context, Master side, BotInputRequest request) => requests.Add(request);
        }
        private static List<(Master, Emote)> Emotes(bool allowed) {
            var agent = Agent(new Clock { Value = 0 }, 0, allowed); var context = new SimulationContext(1);
            context.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100)) { Hp = 1000 };
            context.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100)) { Hp = 1000 };
            var sent = new List<(Master, Emote)>(); agent.EmoteSent += (side, emote) => sent.Add((side, emote));
            for (int tick = 0; tick < 100; tick++) agent.UpdateEmote(context, tick * 100);
            return sent;
        }
        [PortedTest(Source)] public static void a_refused_emote_cooldown_drops_the_emote() => Assert.Equal(0, Emotes(false).Count);
        [PortedTest(Source)] public static void sends_the_emote_the_director_asks_for() => Assert.Sequence(new[] { (Master.RightPlayer, Emote.Greet) }, Emotes(true));
    }
}
