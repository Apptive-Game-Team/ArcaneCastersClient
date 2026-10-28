using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    [Serializable] public sealed class BattleMagicTags { public long magicId; public string[] tags; }
    [Serializable] public sealed class BattleObjectTags { public string gameObjectName; public string[] tags; }
    [Serializable] public sealed class BattleCounterRule { public string attackerTag; public string targetTag; public long weightRaw; }
    [Serializable] public sealed class BattleTagSnapshot {
        public BattleMagicTags[] magicTags;
        public BattleObjectTags[] objectTags;
        public BattleCounterRule[] counterRules;
    }
    public sealed class BattleTagCatalog : ITagCatalog {
        private readonly Dictionary<long, IReadOnlyCollection<string>> magicTags = new Dictionary<long, IReadOnlyCollection<string>>();
        private readonly Dictionary<string, IReadOnlyCollection<string>> objectTags = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, Fix64>> weights = new Dictionary<string, Dictionary<string, Fix64>>(StringComparer.Ordinal);
        public BattleTagCatalog(BattleTagSnapshot snapshot) {
            if (snapshot?.magicTags == null || snapshot.objectTags == null || snapshot.counterRules == null) throw new InvalidDataException("Missing battle tag snapshot.");
            foreach (var row in snapshot.magicTags) {
                if (row == null || row.magicId <= 0 || magicTags.ContainsKey(row.magicId)) throw new InvalidDataException("Invalid or duplicate magic tags.");
                magicTags.Add(row.magicId, CopyTags(row.tags));
            }
            foreach (var row in snapshot.objectTags) {
                if (row == null || string.IsNullOrWhiteSpace(row.gameObjectName) || objectTags.ContainsKey(row.gameObjectName)) throw new InvalidDataException("Invalid or duplicate object tags.");
                objectTags.Add(row.gameObjectName, CopyTags(row.tags));
            }
            foreach (var row in snapshot.counterRules) {
                if (row == null || string.IsNullOrWhiteSpace(row.attackerTag) || string.IsNullOrWhiteSpace(row.targetTag)) throw new InvalidDataException("Invalid counter rule.");
                if (!weights.TryGetValue(row.attackerTag, out var targets)) { targets = new Dictionary<string, Fix64>(StringComparer.Ordinal); weights.Add(row.attackerTag, targets); }
                if (targets.ContainsKey(row.targetTag)) throw new InvalidDataException("Duplicate counter rule.");
                targets.Add(row.targetTag, Fix64.FromRaw(row.weightRaw));
            }
        }
        private static IReadOnlyCollection<string> CopyTags(string[] tags) {
            if (tags == null || tags.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Invalid battle tags.");
            return Array.AsReadOnly(new SortedSet<string>(tags, StringComparer.Ordinal).ToArray());
        }
        public IReadOnlyCollection<string> GetMagicTags(long magicId) => magicTags.TryGetValue(magicId, out var tags) ? tags : Array.Empty<string>();
        public IReadOnlyCollection<string> GetGameObjectTags(string prefab) => objectTags.TryGetValue(UnitCatalogue.GameObjectName(prefab), out var tags) ? tags : Array.Empty<string>();
        public Fix64 GetCounterWeight(IReadOnlyCollection<string> attacker, IReadOnlyCollection<string> target) {
            var sum = Fix64.Zero;
            var targets = new SortedSet<string>(target, StringComparer.Ordinal);
            foreach (var tag in new SortedSet<string>(attacker, StringComparer.Ordinal)) {
                if (!weights.TryGetValue(tag, out var rules)) continue;
                foreach (var other in targets) if (rules.TryGetValue(other, out var weight)) sum += weight;
            }
            return sum;
        }
        // 스냅샷은 매치 동안 불변이며 외부 데이터베이스 캐시를 참조하지 않는다.
        public void InvalidateCache() { }
        public BotCounterEvaluator CreateEvaluator() => new BotCounterEvaluator(new MagicMetadataService(this), this);
    }
}
