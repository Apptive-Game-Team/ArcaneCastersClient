using System;
using System.Linq;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BotSkillParityTests {
        private const string Source = "game/domain/bot/bot_skill.rs";
        [PortedTest(Source)] public static void hospitality_knows_nothing_beyond_the_base_scorer() {
            var skill = BotSkill.ForTier(BotTier.Hospitality); Assert.Equal(0, skill.Capabilities.Length); Assert.Equal(Fix64.Zero, skill.NoiseAmplitude);
        }
        [PortedTest(Source)] public static void intro_plays_exactly_as_every_ordinary_tier_did_before() => Assert.Sequence(new[] { BotCapability.SeedSpiritCombo, BotCapability.MobClusterCombo }, BotSkill.ForTier(BotTier.Intro).Capabilities);
        [PortedTest(Source)] public static void each_tier_knows_everything_the_tier_below_knows_and_is_no_noisier() {
            var tiers = new[] { BotTier.Intro, BotTier.Beginner, BotTier.Intermediate, BotTier.Advanced, BotTier.Elite };
            for (int i = 1; i < tiers.Length; i++) {
                var lower = BotSkill.ForTier(tiers[i - 1]); var higher = BotSkill.ForTier(tiers[i]);
                Assert.True(lower.Capabilities.All(higher.Has)); Assert.True(higher.NoiseAmplitude <= lower.NoiseAmplitude);
            }
        }
        [PortedTest(Source)] public static void elite_knows_every_capability() => Assert.Sequence(Enum.GetValues(typeof(BotCapability)).Cast<BotCapability>(), BotSkill.ForTier(BotTier.Elite).Capabilities);
        [PortedTest(Source)] public static void keeps_the_noise_each_tier_had_before_skills() {
            var expected = new decimal[] { 0, 0.50m, 0.30m, 0.18m, 0.08m, 0 };
            foreach (BotTier tier in Enum.GetValues(typeof(BotTier))) Assert.Equal((Fix64)expected[(int)tier], BotSkill.ForTier(tier).NoiseAmplitude);
        }
        [PortedTest(Source)] public static void with_and_without_return_adjusted_copies() {
            var original = new BotSkill((Fix64)0.1m, new[] { BotCapability.Lethal }); var added = original.With(BotCapability.AllyCover); var removed = added.Without(BotCapability.Lethal);
            Assert.Sequence(new[] { BotCapability.Lethal }, original.Capabilities); Assert.Sequence(new[] { BotCapability.Lethal, BotCapability.AllyCover }, added.Capabilities);
            Assert.Sequence(new[] { BotCapability.AllyCover }, removed.Capabilities); Assert.Equal((Fix64)0.1m, removed.NoiseAmplitude);
        }
    }
}
