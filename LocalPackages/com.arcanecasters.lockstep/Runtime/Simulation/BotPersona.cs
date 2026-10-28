using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum BotTemperament { Warm, Smug, Timid, Stoic }
    public enum BotBrainType { Rule, Jev, Openrouter }
    public sealed class BotPersona {
        public const int LegacyTickRate = 20;
        public const int LlmModelMaxLength = 100;
        public long UserId { get; set; }
        public string Name { get; set; } = "Default Bot";
        public BotTier Tier { get; set; } = BotTier.Beginner;
        public int ThinkingTimeMs { get; set; } = 250;
        public int ReactionIntervalFrames { get; set; } = 8;
        public Fix64 CounterAggression { get; set; } = (Fix64)0.25m;
        public bool Enabled { get; set; } = true;
        public bool Hospitality { get; set; }
        public BotTemperament Temperament { get; set; } = BotTemperament.Warm;
        public BotBrainType BrainType { get; set; } = BotBrainType.Rule;
        public string LlmModel { get; set; }
        public int NormalizedReactionIntervalFrames => Math.Max(1, ReactionIntervalFrames);
        public Fix64 ReactionIntervalSeconds => (Fix64)NormalizedReactionIntervalFrames / LegacyTickRate;
        public int NormalizedThinkingTimeMs => Math.Max(0, ThinkingTimeMs);
        public Fix64 NormalizedCounterAggression => BEPUutilities.MathHelper.Clamp(CounterAggression, -Fix64.One, Fix64.One);
        public string NormalizedLlmModel => NormalizeLlmModel(LlmModel);
        public static string NormalizeLlmModel(string model) => string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        public static BotBrainType BrainTypeFromDatabase(string value) {
            if (string.Equals(value?.Trim(), "JEV", StringComparison.OrdinalIgnoreCase)) return BotBrainType.Jev;
            if (string.Equals(value?.Trim(), "OPENROUTER", StringComparison.OrdinalIgnoreCase)) return BotBrainType.Openrouter;
            return BotBrainType.Rule;
        }
        public static string BrainTypeName(BotBrainType type) {
            switch (type) { case BotBrainType.Rule: return "RULE"; case BotBrainType.Jev: return "JEV"; case BotBrainType.Openrouter: return "OPENROUTER"; default: throw new ArgumentOutOfRangeException(nameof(type)); }
        }
        public static bool UsesLlm(BotBrainType type) => type != BotBrainType.Rule;
    }
}
