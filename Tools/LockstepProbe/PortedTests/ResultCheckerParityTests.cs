using System.Collections.Generic;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class ResultCheckerParityTests {
        private const string Source = "game/service/result_checker.rs";
        [PortedTest(Source)] public static void keeps_the_first_loser_and_tells_both_players() {
            var result = new BattleResult(); Assert.True(!result.Ended);
            result.SetLoser(Master.LeftPlayer); result.SetLoser(Master.RightPlayer);
            Assert.True(result.Ended); Assert.Equal<Master?>(Master.LeftPlayer, result.Loser); Assert.Equal(2L, result.WinnerId(1,2));
            var sent = new List<(long id, BattleResultMessage message)>(); var mmr = new BattleResultMmr(1,2,3,4);
            result.Broadcast(1,2,mmr,(id, message) => sent.Add((id,message)));
            Assert.Equal(2, sent.Count); Assert.Equal(2L, sent[0].id); Assert.Equal(1L, sent[1].id);
            Assert.Equal("Lose", sent[0].message.LeftPlayer); Assert.Equal("Win", sent[0].message.RightPlayer);
            Assert.Equal(mmr, sent[0].message.Mmr); Assert.Equal(sent[0].message, sent[1].message);
        }
        [PortedTest(Source)] public static void no_loser_is_a_draw() {
            var result = new BattleResult(); result.SetEnd(); var sent = new List<BattleResultMessage>();
            result.Broadcast(1,2,new BattleResultMmr(),(id, message) => sent.Add(message)); Assert.Equal("Draw", sent[0].LeftPlayer);
        }
    }
}
