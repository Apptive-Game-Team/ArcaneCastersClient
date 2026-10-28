using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BotLifecycleParityTests {
        private const string Source = "game/service/word_online_loop.rs";
        private sealed class Agents : IBotControl {
            public readonly List<(Master side, long id, Fix64 progress)> Sides = new List<(Master, long, Fix64)>();
            public void NewBotAgent(SimulationContext context, Master side, long id, Fix64 progress) => Sides.Add((side, id, progress));
            public void RemoveBotAgent(Master side) => Sides.RemoveAll(item => item.side == side);
            public bool HasBotAgent(Master side) => Sides.Any(item => item.side == side);
        }
        private static (SimulationContext context, Agents agents, BotLifecycle lifecycle) Practice() {
            var context = new SimulationContext(); context.UserIds[Master.LeftPlayer] = 5; context.UserIds[Master.RightPlayer] = -9;
            var agents = new Agents(); var lifecycle = new BotLifecycle(context, agents, id => { Assert.Equal(5L, id); return (Fix64)0.5m; });
            lifecycle.InitializePractice();
            return (context, agents, new BotLifecycle(context, agents, _ => (Fix64)0.5m));
        }
        [PortedTest(Source)] public static void a_practice_match_gives_the_bot_side_its_agent_with_the_opponents_progress() {
            var f = Practice(); Assert.Sequence(new[] { (Master.RightPlayer, -9L, (Fix64)0.5m) }, f.agents.Sides);
        }
        [PortedTest(Source)] public static void a_disconnected_human_gets_a_bot_until_they_return() {
            var f = Practice(); f.lifecycle.ActivateForUser(5); f.lifecycle.ActivateForUser(5);
            Assert.True(f.lifecycle.HasBotAgent(Master.LeftPlayer)); Assert.Equal(2, f.agents.Sides.Count);
            f.lifecycle.DeactivateForUser(5); Assert.True(!f.lifecycle.HasBotAgent(Master.LeftPlayer));
            f.lifecycle.DeactivateForUser(-9); Assert.True(f.lifecycle.HasBotAgent(Master.RightPlayer));
        }
        public static void CheckQueuedLifecycleAndFever() {
            var f = Practice(); f.context.SubmitAction("disconnected", _ => f.lifecycle.ActivateForUser(5));
            Assert.True(!f.lifecycle.HasBotAgent(Master.LeftPlayer)); new GameActionSystem().Update(f.context); Assert.True(f.lifecycle.HasBotAgent(Master.LeftPlayer));
            f.lifecycle.ActivateForUser(100); f.lifecycle.DeactivateForUser(100); Assert.Equal(2, f.agents.Sides.Count);
            var noBots = new BotLifecycle(f.context, null, _ => Fix64.Zero); noBots.InitializePractice(); noBots.ActivateForUser(5); noBots.DeactivateForUser(5);
            bool fever = false; int clockReads = 0;
            var system = new FeverTimeSystem(() => { clockReads++; return fever; });
            foreach (var side in new[] { Master.LeftPlayer, Master.RightPlayer }) {
                f.context.Players[side] = new PlayerData(new ManaCharger(100));
                f.context.CardDecks[side] = new CardDeck(new long[] { 1, 2, 3 }, 1);
            }
            system.Update(f.context); Assert.Equal(Fix64.One, f.context.Players[Master.LeftPlayer].ManaCharger.ChargeValue.Total);
            fever = true; system.Update(f.context); system.Update(f.context); Assert.Equal(2, clockReads);
            foreach (var side in new[] { Master.LeftPlayer, Master.RightPlayer }) {
                var player = f.context.Players[side]; Assert.Equal((Fix64)2, player.ManaCharger.ChargeValue.Total);
                f.context.CardDecks[side].DrawCard(player, null, (Fix64)0.5m); Assert.Equal(1, player.Cards.Count);
                player.ManaCharger.ChargeMana(player, (Fix64)0.25m); Assert.Equal(2, player.Mana);
            }
        }
    }
}
