using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    [Serializable] public sealed class BattleParameterRow { public string key; public long rawValue; }
    [Serializable] public sealed class BattleMagicRow { public long id; public string name; public string element; }
    [Serializable] public sealed class BattleContentSnapshot {
        public int schemaVersion;
        public BattleParameterRow[] parameters;
        public BattleMagicRow[] magics;
        public long[] leftOwnedMagicIds;
        public long[] rightOwnedMagicIds;
    }
    // 매치 시작 시 확정한 콘텐츠만 읽는다. 원본 DTO와 캐시 변경은 진행 중인 전투에 반영하지 않는다.
    public sealed class BattleContent : IMagicCatalog {
        private readonly SortedDictionary<string, Fix64> parameters = new SortedDictionary<string, Fix64>(StringComparer.Ordinal);
        private readonly MagicInfo[] magics;
        private readonly HashSet<long> leftOwned, rightOwned;
        private readonly long leftUserId, rightUserId;
        public BattleContent(BattleContentSnapshot snapshot, long leftUserId, long rightUserId) {
            if (snapshot == null || snapshot.schemaVersion != 1 || snapshot.parameters == null || snapshot.magics == null
                || snapshot.leftOwnedMagicIds == null || snapshot.rightOwnedMagicIds == null || leftUserId == rightUserId)
                throw new InvalidDataException("Invalid battle content snapshot.");
            this.leftUserId = leftUserId; this.rightUserId = rightUserId;
            foreach (var row in snapshot.parameters) {
                if (row == null || string.IsNullOrWhiteSpace(row.key) || parameters.ContainsKey(row.key))
                    throw new InvalidDataException("Invalid or duplicate battle parameter.");
                parameters.Add(row.key, Fix64.FromRaw(row.rawValue));
            }
            var loaded = new SortedDictionary<long, MagicInfo>();
            foreach (var row in snapshot.magics) {
                if (row == null || row.id <= 0 || string.IsNullOrWhiteSpace(row.name) || loaded.ContainsKey(row.id))
                    throw new InvalidDataException("Invalid or duplicate battle magic.");
                Element? element = null;
                if (row.element != null) {
                    if (!Enum.GetNames(typeof(Element)).Contains(row.element)) throw new InvalidDataException("Unknown battle magic element.");
                    element = (Element)Enum.Parse(typeof(Element), row.element);
                }
                loaded.Add(row.id, new MagicInfo(row.id, row.name, element));
            }
            magics = loaded.Values.ToArray();
            leftOwned = LoadOwnership(snapshot.leftOwnedMagicIds, loaded); rightOwned = LoadOwnership(snapshot.rightOwnedMagicIds, loaded);
        }
        private static HashSet<long> LoadOwnership(long[] ids, IDictionary<long, MagicInfo> known) {
            var owned = new HashSet<long>();
            foreach (long id in ids) {
                if (!known.ContainsKey(id)) throw new InvalidDataException("Owned magic is missing from battle content.");
                owned.Add(id);
            }
            return owned;
        }
        public IEnumerable<MagicInfo> GetAllMagic() => Array.AsReadOnly(magics);
        public bool MatchesParticipants(long left, long right) => leftUserId == left && rightUserId == right;
        public bool ExistUserMagic(long userId, long magicId) => userId == leftUserId ? leftOwned.Contains(magicId) : userId == rightUserId && rightOwned.Contains(magicId);
        public DatabaseMagicParser Configure(SimulationContext context) {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.GameParameters.Count != 0 || context.BossMagicParser != null) throw new InvalidOperationException("Battle content must configure an empty context.");
            var parser = new DatabaseMagicParser(this, MagicRegistry.Standard());
            foreach (var pair in parameters) context.GameParameters.Add(pair.Key, pair.Value);
            context.BossMagicParser = new DatabaseBossMagicParser(parser);
            return parser;
        }
    }
}
