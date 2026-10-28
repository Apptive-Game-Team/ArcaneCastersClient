using System;
using System.Collections.Generic;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class TagLookupException : Exception {
        public TagLookupException(string message) : base(message) { }
    }
    public interface ITagCatalog {
        IReadOnlyCollection<string> GetMagicTags(long magicId);
        IReadOnlyCollection<string> GetGameObjectTags(string prefab);
        Fix64 GetCounterWeight(IReadOnlyCollection<string> attacker, IReadOnlyCollection<string> target);
        void InvalidateCache();
    }
    public sealed class MagicMetadataService {
        private readonly ITagCatalog catalog;
        private readonly Dictionary<long, IReadOnlyCollection<string>> cache = new Dictionary<long, IReadOnlyCollection<string>>();
        public MagicMetadataService(ITagCatalog catalog) { this.catalog = catalog; }
        public IReadOnlyCollection<string> GetMagicTags(long magicId) {
            lock (cache) {
                if (cache.TryGetValue(magicId, out var tags)) return tags;
                var ordered = new SortedSet<string>(catalog.GetMagicTags(magicId), StringComparer.Ordinal);
                var values = new string[ordered.Count]; ordered.CopyTo(values);
                tags = Array.AsReadOnly(values); cache.Add(magicId, tags); return tags;
            }
        }
        public void InvalidateCache() { lock (cache) cache.Clear(); catalog.InvalidateCache(); }
    }
    public interface IBotCounterScorer {
        Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies);
        Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies);
    }
    public sealed class BotCounterEvaluator : IBotCounterScorer {
        private readonly MagicMetadataService metadata;
        private readonly ITagCatalog catalog;
        public event Action<TagLookupException> LookupFailed;
        public BotCounterEvaluator(MagicMetadataService metadata, ITagCatalog catalog) { this.metadata = metadata; this.catalog = catalog; }
        public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => Score(magic, enemies, false);
        public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => Score(magic, enemies, true);
        private Fix64 Score(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies, bool defending) {
            if (magic.Id <= 0 || enemies.Count == 0) return Fix64.Zero;
            try {
                var tags = metadata.GetMagicTags(magic.Id);
                if (tags.Count == 0) return Fix64.Zero;
                var score = Fix64.Zero;
                foreach (var enemy in enemies) {
                    var target = catalog.GetGameObjectTags(enemy.Prefab);
                    score += defending ? catalog.GetCounterWeight(target, tags) : catalog.GetCounterWeight(tags, target);
                }
                return score;
            } catch (TagLookupException error) { LookupFailed?.Invoke(error); return Fix64.Zero; }
        }
    }
}
