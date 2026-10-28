using System;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class CastDeadlineParityTests {
        private const string Source = "game/domain/bot/cast_deadline.rs";
        private const long Start = 1000000;
        [PortedTest(Source)] public static void never_stands_between_two_casts() {
            var deadline = new CastDeadline(BotTier.Hospitality, Start); deadline.RecordCast(Start); Assert.True(!deadline.Overdue(Start + 1));
        }
        [PortedTest(Source)] public static void forces_the_hospitality_bot_to_act_once_it_has_been_quiet_too_long() {
            var deadline = new CastDeadline(BotTier.Hospitality, Start);
            Assert.True(!deadline.Overdue(Start + CastDeadline.HospitalityMaxSilenceMillis - 1)); Assert.True(deadline.Overdue(Start + CastDeadline.HospitalityMaxSilenceMillis));
        }
        [PortedTest(Source)] public static void counts_the_silence_before_the_first_cast_too() {
            var deadline = new CastDeadline(BotTier.Hospitality, Start); Assert.True(deadline.Overdue(Start + CastDeadline.HospitalityMaxSilenceMillis));
        }
        [PortedTest(Source)] public static void casting_resets_the_deadline() {
            var deadline = new CastDeadline(BotTier.Hospitality, Start); long now = Start + CastDeadline.HospitalityMaxSilenceMillis; deadline.RecordCast(now); Assert.True(!deadline.Overdue(now));
        }
        [PortedTest(Source)] public static void leaves_every_other_tier_without_a_deadline() {
            foreach (BotTier tier in Enum.GetValues(typeof(BotTier))) if (tier != BotTier.Hospitality) Assert.True(!new CastDeadline(tier, Start).Overdue(Start + 10 * CastDeadline.HospitalityMaxSilenceMillis));
        }
        public static void CheckFrameClock() {
            foreach (int rate in new[] { 20, 60 }) {
                var context = new SimulationContext(); context.SetTickRate(rate); var clock = new FrameMillisClock(context); var deadline = new CastDeadline(BotTier.Hospitality, clock.NowMillis);
                context.FrameNumber = rate * 60 - 1; Assert.True(!deadline.Overdue(clock.NowMillis));
                context.FrameNumber++; Assert.Equal(60000L, clock.NowMillis); Assert.True(deadline.Overdue(clock.NowMillis));
                deadline.RecordCast(clock.NowMillis); Assert.True(!deadline.Overdue(clock.NowMillis));
                context.FrameNumber = rate * 120; Assert.True(deadline.Overdue(clock.NowMillis));
            }
        }
    }
}
