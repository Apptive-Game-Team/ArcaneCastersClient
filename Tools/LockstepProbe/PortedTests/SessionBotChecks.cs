using System;
using System.Collections.Generic;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SessionBotChecks {
        private sealed class Neutral : IBotCounterScorer {
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
        }
        private static SessionBotControl Control(SimulationContext context, BotPersona persona, long[] deck, BotDecisionSourceFactory factory = null) {
            context.GameParameters["evil_ent.mana_cost"] = 80; context.GameParameters["evil_ent.range"] = 10;
            context.GameParameters["ember_spirit_swarm.mana_cost"] = 15; context.GameParameters["ember_spirit_swarm.range"] = 10; context.GameParameters["player.max_mana"] = 120;
            return new SessionBotControl(context, new TestBotCatalog(new BotMagic(1, "evil_ent", summoning: new SummonedBodies("EvilEnt")), new BotMagic(2, "ember_spirit_swarm", summoning: new SummonedBodies("FireSlime"))), new Neutral(), _ => persona, deck, deck, 77, _ => true, factory);
        }
        public static void Run() {
            var c = new SimulationContext(1); c.UserIds[Master.LeftPlayer] = -1; c.UserIds[Master.RightPlayer] = 2;
            var persona = new BotPersona { Tier = BotTier.Elite, CounterAggression = 0, ThinkingTimeMs = 0 };
            var deck = new long[] { 1, 1, 1, 2 }; var control = Control(c, persona, deck); deck[1] = 2; deck[2] = 2;
            var lifecycle = new BotLifecycle(c, control, _ => (Fix64)0.5m); lifecycle.InitializePractice(); Assert.True(control.HasBotAgent(Master.LeftPlayer)); Assert.True(!control.HasBotAgent(Master.RightPlayer));
            persona.Tier = BotTier.Hospitality; Assert.Equal(BotTier.Elite, control.Left.Persona.Tier);
            var eye = new BotEye(new BotVisibleObject[0], new long[] { 1, 2 }, 20, 1000);
            Assert.True(control.Left.DecisionSource.Think(eye, c.GameParameters, Master.LeftPlayer, false) == null);
            lifecycle.DeactivateForUser(-1); Assert.True(control.HasBotAgent(Master.LeftPlayer)); lifecycle.ActivateForUser(2); Assert.True(control.HasBotAgent(Master.RightPlayer)); lifecycle.DeactivateForUser(2); Assert.True(!control.HasBotAgent(Master.RightPlayer));
            var traces = new List<string>[] { new List<string>(), new List<string>() };
            for (int run = 0; run < 2; run++) {
                var context = new SimulationContext(1); var copy = Control(context, new BotPersona { Tier = BotTier.Intro, CounterAggression = 0, ThinkingTimeMs = 0 }, new long[] { 1, 2 });
                foreach (var side in new[] { Master.RightPlayer, Master.LeftPlayer, Master.RightPlayer }) {
                    copy.NewBotAgent(context, side, 1, (Fix64)0.5m); var agent = side == Master.LeftPlayer ? copy.Left : copy.Right;
                    var decision = agent.DecisionSource.Think(new BotEye(new BotVisibleObject[0], new long[] { 1, 2 }, 100, 1000), context.GameParameters, side, false);
                    traces[run].Add(decision.MagicId + ":" + decision.Target.X.RawValue + ":" + decision.Target.Z.RawValue);
                    copy.RemoveBotAgent(side);
                }
            }
            Assert.Sequence(traces[0], traces[1]);
            var modelPersona = new BotPersona { BrainType = BotBrainType.Jev }; var rejected = Control(c, modelPersona, new long[0]); bool failed = false;
            try { rejected.NewBotAgent(c, Master.LeftPlayer, 1, 0); } catch (InvalidOperationException) { failed = true; } Assert.True(failed); Assert.True(rejected.Left == null);
            bool called = false; var custom = Control(c, modelPersona, new long[0], (side, generation, p, fallback) => { called = true; Assert.Equal(BotBrainType.Jev, p.BrainType); return fallback; });
            custom.NewBotAgent(c, Master.LeftPlayer, 1, 0); Assert.True(called); Assert.True(custom.Left != null);
            var identities = new List<string>();
            bool rejectNext = false;
            var identified = Control(c, modelPersona, new long[0], (side, generation, p, fallback) => {
                identities.Add(side + ":" + generation);
                if (rejectNext) { rejectNext = false; throw new InvalidOperationException("factory fixture"); }
                return fallback;
            });
            identified.NewBotAgent(c, Master.RightPlayer, -3, 0);
            identified.RemoveBotAgent(Master.RightPlayer);
            identified.NewBotAgent(c, Master.LeftPlayer, -4, 0);
            rejectNext = true;
            failed = false;
            try { identified.NewBotAgent(c, Master.RightPlayer, -3, 0); } catch (InvalidOperationException) { failed = true; }
            Assert.True(failed); Assert.True(identified.Right == null);
            identified.NewBotAgent(c, Master.RightPlayer, -3, 0);
            identified.RemoveBotAgent(Master.LeftPlayer);
            identified.NewBotAgent(c, Master.LeftPlayer, -4, 0);
            Assert.Sequence(new[] { "RightPlayer:0", "LeftPlayer:0", "RightPlayer:1", "RightPlayer:1", "LeftPlayer:1" }, identities);
        }
    }
}
