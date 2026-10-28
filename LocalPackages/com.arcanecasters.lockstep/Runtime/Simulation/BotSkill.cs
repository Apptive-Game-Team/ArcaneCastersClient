using System;
using System.Collections.Generic;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum BotCapability { SeedSpiritCombo, MobClusterCombo, Lethal, StrongestLane, SummonStrength, ThreatStrength, AllyCover, ManaDiscipline, RolePlacement, LeadTargeting, SummonRelevance, GamePlan }
    public sealed class BotSkill {
        private readonly SortedSet<BotCapability> capabilities;
        public Fix64 NoiseAmplitude { get; }
        public BotCapability[] Capabilities => capabilities.ToArray();
        public BotSkill(Fix64 noiseAmplitude, IEnumerable<BotCapability> capabilities) {
            NoiseAmplitude = noiseAmplitude; this.capabilities = new SortedSet<BotCapability>(capabilities);
        }
        public bool Has(BotCapability capability) => capabilities.Contains(capability);
        public BotSkill With(params BotCapability[] added) => new BotSkill(NoiseAmplitude, capabilities.Concat(added));
        public BotSkill Without(params BotCapability[] removed) => new BotSkill(NoiseAmplitude, capabilities.Except(removed));
        private BotSkill WithNoise(Fix64 amplitude) => new BotSkill(amplitude, capabilities);
        public static BotSkill ForTier(BotTier tier) {
            var combos = new[] { BotCapability.SeedSpiritCombo, BotCapability.MobClusterCombo };
            switch (tier) {
                case BotTier.Hospitality: return new BotSkill(0, Array.Empty<BotCapability>());
                case BotTier.Intro: return new BotSkill((Fix64)0.50m, combos);
                case BotTier.Beginner: return new BotSkill((Fix64)0.30m, combos).With(BotCapability.StrongestLane);
                case BotTier.Intermediate: return ForTier(BotTier.Beginner).WithNoise((Fix64)0.18m).With(BotCapability.Lethal, BotCapability.SummonStrength, BotCapability.ThreatStrength);
                case BotTier.Advanced: return ForTier(BotTier.Intermediate).WithNoise((Fix64)0.08m).With(BotCapability.AllyCover, BotCapability.ManaDiscipline, BotCapability.RolePlacement, BotCapability.SummonRelevance, BotCapability.GamePlan);
                case BotTier.Elite: return ForTier(BotTier.Advanced).WithNoise(0).With(BotCapability.LeadTargeting);
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }
    }
}
