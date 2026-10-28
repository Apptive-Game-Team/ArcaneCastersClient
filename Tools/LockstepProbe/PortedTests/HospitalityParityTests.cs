using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class HospitalityParityTests {
        private const string Source = "game/domain/bot/hospitality_director.rs";
        private static HospitalityDirector Director() => new HospitalityDirector((Fix64)0.7m);
        [PortedTest(Source)] public static void leaves_the_bot_well_under_the_players_board() => Assert.Equal(70, Director().SummonAllowance(100, 0));
        [PortedTest(Source)] public static void holds_back_further_against_a_player_who_has_just_started() => Assert.Equal(50, new HospitalityDirector((Fix64)0.5m).SummonAllowance(100, 0));
        [PortedTest(Source)] public static void holds_nothing_back_at_full_progress() => Assert.Equal(100, new HospitalityDirector(1).SummonAllowance(100, 0));
        [PortedTest(Source)] public static void clamps_progress_to_the_tutorial_range() {
            Assert.Equal(50, new HospitalityDirector(0).SummonAllowance(100, 0)); Assert.Equal(100, new HospitalityDirector(5).SummonAllowance(100, 0));
        }
        [PortedTest(Source)] public static void counts_what_the_bot_already_has_out_against_its_allowance() => Assert.Equal(20, Director().SummonAllowance(100, 50));
        [PortedTest(Source)] public static void allows_nothing_once_the_bot_has_spent_its_share() => Assert.True(Director().SummonAllowance(100, 70) <= 0);
        [PortedTest(Source)] public static void allows_nothing_against_an_empty_board() => Assert.True(Director().SummonAllowance(0, 0) <= 0);
        [PortedTest(Source)] public static void hands_the_fight_over_when_the_player_is_not_ahead_on_board() {
            Assert.Equal(HospitalityDirector.Full, Director().Aggression(0, 0, 0)); Assert.Equal(HospitalityDirector.Full, Director().Aggression(1, 3, 0));
        }
        [PortedTest(Source)] public static void eases_off_once_the_player_is_clearly_ahead() => Assert.Equal(HospitalityDirector.Eased, Director().Aggression(3, 1, 0));
        [PortedTest(Source)] public static void eases_off_when_the_player_is_pushing_hard_regardless_of_counts() => Assert.Equal(HospitalityDirector.Eased, Director().Aggression(1, 0, (Fix64)0.9m));
        [PortedTest(Source)] public static void stays_between_the_two_on_a_close_board() => Assert.Equal(HospitalityDirector.Partial, Director().Aggression(1, 0, 0));
        [PortedTest(Source)] public static void never_asks_for_a_play_that_beats_the_player() {
            for (int player = 0; player <= 5; player++) for (int bot = 0; bot <= 5; bot++) for (int step = 0; step <= 4; step++) {
                var aggression = Director().Aggression(player, bot, (Fix64)step / 4);
                Assert.True(aggression < Fix64.Zero && aggression >= HospitalityDirector.Full);
            }
        }
        public static void CheckFractionalAllowance() {
            Assert.Equal(6, Director().SummonAllowance(9, 0)); Assert.Equal(7, Director().SummonAllowance(10, 0));
            Assert.Equal(69, new HospitalityDirector((Fix64)0.699m).SummonAllowance(100, 0));
            Assert.Equal(-1, Director().SummonAllowance(100, 71));
        }
    }
}
