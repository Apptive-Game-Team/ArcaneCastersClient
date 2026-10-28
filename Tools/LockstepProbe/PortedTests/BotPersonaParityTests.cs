using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BotPersonaParityTests {
        private const string Source = "game/domain/bot/bot_persona.rs";
        [PortedTest(Source)] public static void brain_type_reads_blank_and_unknown_values_as_rule() {
            foreach (var value in new string[] { null, "  ", "GPT" }) Assert.Equal(BotBrainType.Rule, BotPersona.BrainTypeFromDatabase(value));
            Assert.Equal(BotBrainType.Jev, BotPersona.BrainTypeFromDatabase(" jev ")); Assert.Equal(BotBrainType.Openrouter, BotPersona.BrainTypeFromDatabase("OPENROUTER"));
            Assert.True(!BotPersona.UsesLlm(BotBrainType.Rule)); Assert.True(BotPersona.UsesLlm(BotBrainType.Jev));
        }
        [PortedTest(Source)] public static void llm_model_is_trimmed_and_blank_is_none() {
            Assert.Equal<string>(null, BotPersona.NormalizeLlmModel(null)); Assert.Equal<string>(null, BotPersona.NormalizeLlmModel("   "));
            Assert.Equal("openai/gpt-4o", BotPersona.NormalizeLlmModel(" openai/gpt-4o "));
        }
        public static void CheckNormalization() {
            var persona = new BotPersona(); Assert.Equal((Fix64)8 / 20, persona.ReactionIntervalSeconds); Assert.Equal(250, persona.NormalizedThinkingTimeMs);
            persona.ReactionIntervalFrames = -1; persona.ThinkingTimeMs = -1; persona.CounterAggression = (Fix64)(-0.5m);
            Assert.Equal((Fix64)1 / 20, persona.ReactionIntervalSeconds); Assert.Equal(0, persona.NormalizedThinkingTimeMs); Assert.Equal((Fix64)(-0.5m), persona.NormalizedCounterAggression);
            persona.CounterAggression = -2; Assert.Equal(-Fix64.One, persona.NormalizedCounterAggression); persona.CounterAggression = 2; Assert.Equal(Fix64.One, persona.NormalizedCounterAggression);
        }
    }
}
