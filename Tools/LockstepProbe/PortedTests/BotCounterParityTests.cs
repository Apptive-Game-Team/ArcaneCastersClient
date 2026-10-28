using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    internal sealed class FakeBotTags : ITagCatalog {
        public IReadOnlyCollection<string> Tags = new[] { "CAT_Small" };
        public Fix64 Attacking = 3, Defending = 2;
        public bool Failing;
        public int Lookups, Invalidations;
        public readonly List<string> Asked = new List<string>();
        public IReadOnlyCollection<string> GetMagicTags(long id) { Lookups++; return Tags; }
        public IReadOnlyCollection<string> GetGameObjectTags(string prefab) => new[] { "CAT_AoE" };
        public Fix64 GetCounterWeight(IReadOnlyCollection<string> attacker, IReadOnlyCollection<string> target) {
            if (Failing) throw new TagLookupException("no such table");
            Asked.Add(string.Join(",", attacker) + ":" + string.Join(",", target));
            return attacker.Contains("CAT_Small") ? Attacking : Defending;
        }
        public void InvalidateCache() { Invalidations++; }
        public BotCounterEvaluator Evaluator() => new BotCounterEvaluator(new MagicMetadataService(this), this);
    }
    public static class BotCounterParityTests {
        public static void CheckSnapshot() {
            var snapshot = new BattleTagSnapshot {
                magicTags = new[] { new BattleMagicTags { magicId = 7, tags = new[] { "area", "fire", "area" } } },
                objectTags = new[] { new BattleObjectTags { gameObjectName = "fire_spirit", tags = new[] { "small" } } },
                counterRules = new[] { new BattleCounterRule { attackerTag = "area", targetTag = "small", weightRaw = (3L << 32) + 1 }, new BattleCounterRule { attackerTag = "fire", targetTag = "small", weightRaw = -(1L << 32) }, new BattleCounterRule { attackerTag = "small", targetTag = "area", weightRaw = 5L << 32 } }
            };
            var catalog = new BattleTagCatalog(snapshot);
            snapshot.magicTags[0].tags[0] = "changed"; snapshot.objectTags[0].tags[0] = "changed"; snapshot.counterRules[0].weightRaw = 0;
            Assert.Sequence(new[] { "area", "fire" }, catalog.GetMagicTags(7)); Assert.Equal(0, catalog.GetMagicTags(99).Count); Assert.Equal(0, catalog.GetGameObjectTags("Player").Count);
            var evaluator = catalog.CreateEvaluator(); Assert.Equal((2L << 32) + 1, evaluator.Evaluate(Magic, Enemies()).RawValue); Assert.Equal((Fix64)5, evaluator.EvaluateVulnerability(Magic, Enemies()));
            Assert.Equal((2L << 32) + 1, catalog.GetCounterWeight(new[] { "fire", "area", "area" }, new[] { "small", "small" }).RawValue);
            catalog.InvalidateCache(); Assert.Equal((2L << 32) + 1, evaluator.Evaluate(Magic, Enemies()).RawValue);
        }
        private const string Source = "game/service/bot/bot_counter_evaluator.rs";
        private static readonly BotMagic Magic = new BotMagic(7, "magic");
        private static BotVisibleObject[] Enemies() => new[] { new BotVisibleObject(1, Master.RightPlayer, "FireSpirit", new Vector3(), 10, true, true) };
        [PortedTest(Source)] public static void scores_the_magic_as_the_attacker_when_asked_how_much_it_beats_the_field() {
            var tags = new FakeBotTags { Defending = 0 }; Assert.Equal((Fix64)3, tags.Evaluator().Evaluate(Magic, Enemies())); Assert.Equal("CAT_Small:CAT_AoE", tags.Asked[0]);
        }
        [PortedTest(Source)] public static void scores_the_enemy_as_the_attacker_when_asked_how_much_the_field_beats_it() {
            var tags = new FakeBotTags { Attacking = 0 }; Assert.Equal((Fix64)2, tags.Evaluator().EvaluateVulnerability(Magic, Enemies())); Assert.Equal("CAT_AoE:CAT_Small", tags.Asked[0]);
        }
        [PortedTest(Source)] public static void both_directions_stay_neutral_when_the_magic_carries_no_tags() {
            var tags = new FakeBotTags { Tags = new string[0] }; var evaluator = tags.Evaluator(); Assert.Equal(Fix64.Zero, evaluator.Evaluate(Magic, Enemies())); Assert.Equal(Fix64.Zero, evaluator.EvaluateVulnerability(Magic, Enemies()));
        }
        [PortedTest(Source)] public static void both_directions_fall_back_to_neutral_when_the_lookup_fails() {
            var evaluator = new FakeBotTags { Failing = true }.Evaluator(); Assert.Equal(Fix64.Zero, evaluator.Evaluate(Magic, Enemies())); Assert.Equal(Fix64.Zero, evaluator.EvaluateVulnerability(Magic, Enemies()));
        }
        [PortedTest(Source)] public static void an_unknown_magic_id_or_an_empty_field_is_neutral() {
            var tags = new FakeBotTags(); var evaluator = tags.Evaluator(); Assert.Equal(Fix64.Zero, evaluator.Evaluate(new BotMagic(0, "none"), Enemies())); Assert.Equal(Fix64.Zero, evaluator.Evaluate(Magic, new BotVisibleObject[0])); Assert.Equal(0, tags.Lookups);
        }
    }
}
