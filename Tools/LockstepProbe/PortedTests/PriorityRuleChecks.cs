using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PriorityRuleChecks {
        public static void Run() {
            var parameters = new Dictionary<string, Fix64> {
                ["finish.mana_cost"] = 5, ["finish.range"] = 18, ["finish.damage"] = 10,
                ["vine_toss.mana_cost"] = 2, ["vine_toss.range"] = 18,
                ["blast.mana_cost"] = 3, ["blast.range"] = 18,
                ["cheap.mana_cost"] = 1, ["cheap.range"] = 18, ["cheap.damage"] = 10
            };
            var finish = new BotMagic(1, "finish"); var vine = new BotMagic(2, "vine_toss", kind: BotMagicKind.VineToss, className: "VineTossMagic");
            var blast = new BotMagic(3, "blast", kind: BotMagicKind.Explosion, className: "BlastMagic"); var cheap = new BotMagic(4, "cheap");
            var origin = new Vector3(1, 0, 5);
            var objects = new[] {
                new BotVisibleObject(1, Master.RightPlayer, "Player", new Vector3(17, 0, 5), 10, false, true),
                new BotVisibleObject(2, Master.LeftPlayer, "SeedSpirit", new Vector3(4, 0, 5), 20, true, true),
                new BotVisibleObject(3, Master.RightPlayer, "FireSpirit", new Vector3(8, 0, 4), 20, true, true),
                new BotVisibleObject(4, Master.RightPlayer, "FireSpirit", new Vector3(8, 0, 5), 20, true, true),
                new BotVisibleObject(5, Master.RightPlayer, "FireSpirit", new Vector3(8, 0, 6), 20, true, true)
            };
            ThinkContext Context(BotTier tier, int mana, params BotMagic[] hand) => new ThinkContext(hand, mana, new BotSpellStats(parameters), objects, origin, Master.LeftPlayer, Master.RightPlayer, ThreatAssessment.Observe(objects, Master.RightPlayer, origin, 10), BotSkill.ForTier(tier), new JavaRandom(17));
            var elite = Context(BotTier.Elite, 10, vine, blast, finish, cheap); var choice = BotPriorityRules.Find(elite);
            Assert.Equal(4L, choice.MagicId); Assert.Equal("finish.lethal", choice.RuleId); Assert.Equal(new Vector3(17, 0, 5), choice.Target);
            var expected = new JavaRandom(17); expected.NextFraction(); expected.NextFraction(); Assert.Equal(expected.State, elite.Random.State);
            choice = BotPriorityRules.Find(Context(BotTier.Intro, 10, blast, finish, vine)); Assert.Equal(2L, choice.MagicId); Assert.Equal("combo.seed-spirit", choice.RuleId);
            Assert.Equal(objects[1].Position, choice.Target);
            Assert.True(BotPriorityRules.Find(Context(BotTier.Hospitality, 10, vine, blast, finish)) == null);
            choice = BotPriorityRules.Find(Context(BotTier.Intro, 10, blast)); Assert.Equal("combo.mob-cluster", choice.RuleId); Assert.Equal(new Vector3(8, 0, 5), choice.Target); Assert.Equal(Fix64.One, choice.Score);
            Assert.True(BotPriorityRules.Find(Context(BotTier.Intro, 1, blast, vine)) == null);
            parameters.Remove("finish.damage"); Assert.True(new LethalRule().Find(Context(BotTier.Elite, 10, finish)) == null);
            parameters["cheap.range"] = 1; Assert.True(new LethalRule().Find(Context(BotTier.Elite, 10, cheap)) == null);
            parameters["vine_toss.range"] = 1; Assert.True(new SeedSpiritComboRule().Find(Context(BotTier.Intro, 10, vine)) == null);
            parameters["blast.range"] = 1; Assert.True(new MobClusterComboRule().Find(Context(BotTier.Intro, 10, blast)) == null);
            parameters["cheap.range"] = 18; parameters["cheap.mana_cost"] = 0;
            choice = new LethalRule().Find(Context(BotTier.Elite, 0, cheap)); Assert.Equal(Fix64.MaxValue, choice.Score); Assert.Equal(Fix64.MaxValue, choice.NoisyScore);
            var first = new ScoredPlay(1, origin, 1, 1, 2, "a", ""); var second = new ScoredPlay(2, origin, 1, 1, 2, "b", "");
            Assert.True(ReferenceEquals(first, ScoredPlay.Best(new[] { first, second }, true))); Assert.True(ReferenceEquals(first, ScoredPlay.Best(new[] { first, second }, false)));
        }
    }
}
