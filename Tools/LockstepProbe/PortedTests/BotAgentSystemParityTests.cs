using System;
using System.Collections.Generic;
using System.Threading;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BotAgentSystemParityTests {
        private const string Source = "game/service/system/bot_agent_system.rs";
        private sealed class Counters : IBotCounterScorer {
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
        }
        private static SimulationContext Context() {
            var c = new SimulationContext(1); c.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100)) { Mana = 10 }; c.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100)); return c;
        }
        private static BotAgent Agent(SimulationContext c, Master side = Master.LeftPlayer) {
            var persona = new BotPersona { Tier = BotTier.Elite, ThinkingTimeMs = 0, ReactionIntervalFrames = 1, CounterAggression = 0 };
            var values = new Dictionary<string, Fix64> { ["spirit.mana_cost"] = 3, ["spirit.range"] = 6 };
            var brain = new BotBrain(new TestBotCatalog(new BotMagic(5, "spirit", summoning: new SummonedBodies("FireSpirit"))), new Counters(), persona, new JavaRandom(1));
            int id = 0; return new BotAgent(new RuleDecisionSource(brain), side, persona, values, new FrameMillisClock(c), () => id++);
        }
        private sealed class Parked : IBotThinkExecutor { public readonly Queue<Action> Jobs = new Queue<Action>(); public void Submit(Action job) => Jobs.Enqueue(job); }
        [PortedTest(Source)] public static void a_due_bot_thinks_on_the_executor_and_queues_its_cast() {
            var c = Context(); c.Players[Master.LeftPlayer].AddCard(5); int thoughts = 0; var agent = Agent(c); agent.Thought += _ => thoughts++;
            var system = new BotAgentSystem(new BotAgentSlots { Left = agent }, _ => null, new FrameMillisClock(c)); system.Update(c);
            Assert.Equal(1, c.PendingActionCount); Assert.Equal(1, thoughts);
        }
        [PortedTest(Source)] public static void a_bot_still_thinking_is_not_submitted_again() {
            var c = Context(); var parked = new Parked(); var system = new BotAgentSystem(new BotAgentSlots { Left = Agent(c) }, _ => null, new FrameMillisClock(c), parked);
            system.Update(c); system.Update(c); Assert.Equal(1, parked.Jobs.Count);
            var job = parked.Jobs.Dequeue();
#if UNITY_WEBGL && !UNITY_EDITOR
            job();
#else
            var thread = new Thread(() => job()); thread.Start(); thread.Join();
#endif
            system.Update(c); Assert.Equal(1, parked.Jobs.Count); parked.Jobs.Dequeue()(); Assert.True(!system.LeftProcessing);
        }
        [PortedTest(Source)] public static void a_failing_think_job_frees_the_bot() {
            var c = Context(); c.Players[Master.LeftPlayer].AddCard(5); var agent = Agent(c); agent.Thought += _ => throw new InvalidOperationException("sink is down");
            var system = new BotAgentSystem(new BotAgentSlots { Left = agent }, _ => null, new FrameMillisClock(c)); int failures = 0; system.ThinkFailed += (side, error) => { Assert.Equal(Master.LeftPlayer, side); failures++; };
            system.Update(c); Assert.True(!system.LeftProcessing); Assert.Equal(1, failures);
        }
        public static void CheckOrderAndReplacement() {
            var c = Context(); c.Players[Master.LeftPlayer].AddCard(5); c.Players[Master.RightPlayer].AddCard(5); c.Players[Master.RightPlayer].Mana = 10;
            var order = new List<Master>(); var left = Agent(c); var right = Agent(c, Master.RightPlayer);
            left.Thought += _ => order.Add(Master.LeftPlayer); right.Thought += _ => order.Add(Master.RightPlayer);
            var slots = new BotAgentSlots { Left = left, Right = right }; var system = new BotAgentSystem(slots, _ => null, new FrameMillisClock(c));
            system.Update(c); Assert.Sequence(new[] { Master.LeftPlayer, Master.RightPlayer }, order); Assert.Equal(2, c.PendingActionCount);
            slots.RemoveBotAgent(Master.LeftPlayer); order.Clear(); system.Update(c); Assert.Sequence(new[] { Master.RightPlayer }, order); Assert.True(!slots.HasBotAgent(Master.LeftPlayer)); Assert.True(slots.HasBotAgent(Master.RightPlayer));
        }
    }
}
