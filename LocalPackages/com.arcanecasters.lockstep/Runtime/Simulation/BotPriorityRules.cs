using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class ScoredPlay {
        public long MagicId { get; }
        public Vector3 Target { get; }
        public int Cost { get; }
        public Fix64 Score { get; }
        public Fix64 NoisyScore { get; }
        public string RuleId { get; }
        public string Reason { get; }
        public ScoredPlay(long magicId, Vector3 target, int cost, Fix64 score, Fix64 noisyScore, string ruleId, string reason) {
            MagicId = magicId; Target = target; Cost = cost; Score = score; NoisyScore = noisyScore; RuleId = ruleId; Reason = reason;
        }
        public static ScoredPlay Best(IEnumerable<ScoredPlay> plays, bool noisy) {
            ScoredPlay best = null;
            foreach (var play in plays) if (best == null || (noisy ? play.NoisyScore > best.NoisyScore : play.Score > best.Score)) best = play;
            return best;
        }
    }
    public sealed class ThinkContext {
        public readonly IReadOnlyList<BotMagic> Hand;
        public readonly int Mana;
        public readonly BotSpellStats SpellStats;
        public readonly IReadOnlyList<BotVisibleObject> Objects;
        public readonly Vector3 PlayerPosition;
        public readonly Master BotSide, EnemySide;
        public readonly ThreatAssessment Threats;
        public readonly BotSkill Skill;
        public readonly JavaRandom Random;
        public ThinkContext(IReadOnlyList<BotMagic> hand, int mana, BotSpellStats stats, IReadOnlyList<BotVisibleObject> objects, Vector3 position, Master side, Master enemySide, ThreatAssessment threats, BotSkill skill, JavaRandom random) {
            Hand = hand; Mana = mana; SpellStats = stats; Objects = objects; PlayerPosition = position; BotSide = side; EnemySide = enemySide; Threats = threats; Skill = skill; Random = random;
        }
        // 무료 시전의 양의 무한대 점수는 고정 소수점 최댓값으로 표현한다.
        public static Fix64 PerCost(int value, int cost) => cost == 0 ? Fix64.MaxValue : (Fix64)value / cost;
        public ScoredPlay Scored(BotMagic magic, Vector3 target, int cost, Fix64 score, string ruleId, string reason) {
            var factor = Fix64.One + (Random.NextFraction() * 2 - Fix64.One) * Skill.NoiseAmplitude;
            var noisy = score == Fix64.MaxValue ? score : score * factor;
            return new ScoredPlay(magic.Id, target, cost, score, noisy, ruleId, reason);
        }
    }
    public interface IPriorityRule {
        BotCapability Capability { get; }
        ScoredPlay Find(ThinkContext context);
    }
    public static class BotPriorityRules {
        private static readonly IPriorityRule[] rules = { new LethalRule(), new SeedSpiritComboRule(), new MobClusterComboRule() };
        public static ScoredPlay Find(ThinkContext context) {
            foreach (var rule in rules) {
                if (!context.Skill.Has(rule.Capability)) continue;
                var play = rule.Find(context); if (play != null) return play;
            }
            return null;
        }
    }
    public sealed class LethalRule : IPriorityRule {
        public BotCapability Capability => BotCapability.Lethal;
        public ScoredPlay Find(ThinkContext c) {
            var cores = c.Threats.Threats.Where(t => t.PlayerCore).ToArray(); if (cores.Length == 0) return null;
            var core = cores[0]; var candidates = new List<ScoredPlay>();
            foreach (var magic in c.Hand) {
                if (magic.Summoning.HasValue) continue;
                int cost = c.SpellStats.ManaCost(magic); if (cost > c.Mana || (core.Position - c.PlayerPosition).Length() > c.SpellStats.CastRange(magic)) continue;
                var damage = c.SpellStats.StatedDamage(magic); if (damage <= Fix64.Zero || damage < (Fix64)core.Hp) continue;
                candidates.Add(c.Scored(magic, core.Position, cost, ThinkContext.PerCost(1, cost), "finish.lethal", $"{magic.Name} finishes the enemy player at {core.Hp} hp."));
            }
            return ScoredPlay.Best(candidates, false);
        }
    }
    public sealed class SeedSpiritComboRule : IPriorityRule {
        public BotCapability Capability => BotCapability.SeedSpiritCombo;
        public ScoredPlay Find(ThinkContext c) {
            var seeds = c.Objects.Where(o => o.Master == c.BotSide && o.Targetable && o.Prefab == "SeedSpirit").ToArray();
            if (seeds.Length == 0) return null;
            var candidates = new List<ScoredPlay>();
            foreach (var magic in c.Hand) {
                if (magic.Kind != BotMagicKind.VineToss && magic.Kind != BotMagicKind.Overgrowth) continue;
                int cost = c.SpellStats.ManaCost(magic); if (cost > c.Mana) continue;
                var range = c.SpellStats.CastRange(magic);
                foreach (var seed in seeds) if ((seed.Position - c.PlayerPosition).Length() <= range)
                    candidates.Add(c.Scored(magic, seed.Position, cost, ThinkContext.PerCost(1, cost), "combo.seed-spirit", $"{magic.ClassName} targets allied SeedSpirit {seed.Id}."));
            }
            return ScoredPlay.Best(candidates, true);
        }
    }
    public sealed class MobClusterComboRule : IPriorityRule {
        public BotCapability Capability => BotCapability.MobClusterCombo;
        private static readonly Fix64 Radius = (Fix64)2.5m;
        private static Vector3 Average(BotVisibleObject[] objects) {
            Vector3 sum = Vector3.Zero; foreach (var item in objects) sum += item.Position;
            return new Vector3(sum.X / objects.Length, sum.Y / objects.Length, sum.Z / objects.Length);
        }
        public ScoredPlay Find(ThinkContext c) {
            var mobs = c.Objects.Where(o => o.Master == c.EnemySide && o.Mob && o.Targetable).ToArray(); if (mobs.Length < 3) return null;
            var candidates = new List<ScoredPlay>();
            foreach (var magic in c.Hand) {
                if (!magic.IsExplosion) continue;
                int cost = c.SpellStats.ManaCost(magic); if (cost > c.Mana) continue;
                Vector3? best = null; int bestCount = 0; var range = c.SpellStats.CastRange(magic);
                foreach (var anchor in mobs) {
                    var cluster = mobs.Where(o => (o.Position - anchor.Position).Length() <= Radius).ToArray(); if (cluster.Length < 3) continue;
                    var initial = Average(cluster); cluster = mobs.Where(o => (o.Position - initial).Length() <= Radius).ToArray(); if (cluster.Length < 3) continue;
                    var center = Average(cluster); if ((center - c.PlayerPosition).Length() > range) continue;
                    if (!best.HasValue || cluster.Length > bestCount || (cluster.Length == bestCount && (center - c.PlayerPosition).Length() < (best.Value - c.PlayerPosition).Length())) { best = center; bestCount = cluster.Length; }
                }
                if (best.HasValue) candidates.Add(c.Scored(magic, best.Value, cost, ThinkContext.PerCost(bestCount, cost), "combo.mob-cluster", $"{magic.ClassName} targets a cluster of {bestCount} enemy mobs."));
            }
            return ScoredPlay.Best(candidates, true);
        }
    }
}
