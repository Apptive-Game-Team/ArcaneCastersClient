using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;

namespace ArcaneCasters.Simulation {
    public sealed class MagicInfo {
        public long? Id { get; }
        public string Name { get; }
        public Element? Element { get; }
        public MagicInfo(long? id, string name, Element? element = null) { Id = id; Name = name; Element = element; }
    }
    public interface IMagicCatalog {
        IEnumerable<MagicInfo> GetAllMagic();
        bool ExistUserMagic(long userId, long magicId);
    }
    public sealed class MagicImplementation {
        private readonly Action<SimulationContext, Master, Vector3?, Vector3> run;
        private readonly string summonedParameterKey;
        private readonly bool airborne;
        public string SummonedPrefab { get; }
        public bool IsSpawnMagic { get; }
        public bool IsVineFan { get; } public bool IsExplosionMagic { get; }
        public MagicImplementation(Action<SimulationContext, Master, Vector3?, Vector3> run, string summonedParameterKey = null, bool airborne = false, string summonedPrefab = null, bool isSpawnMagic = false, bool isVineFan = false, bool isExplosionMagic = false) {
            this.run = run ?? throw new ArgumentNullException(nameof(run)); this.summonedParameterKey = summonedParameterKey; this.airborne = airborne; SummonedPrefab = summonedPrefab; IsSpawnMagic = isSpawnMagic; IsVineFan = isVineFan; IsExplosionMagic = isExplosionMagic;
        }
        public BoundMagic Bind(long id, string name, Element element) => new BoundMagic(id, name, run, summonedParameterKey, airborne, element, SummonedPrefab, IsSpawnMagic, IsVineFan, IsExplosionMagic);
    }
    public sealed class MagicRegistry {
        public static MagicRegistry Standard() { var registry = new MagicRegistry(); StandardMagics.RegisterAll(registry); return registry; }
        private readonly SortedDictionary<string, MagicImplementation> implementations = new SortedDictionary<string, MagicImplementation>(StringComparer.Ordinal);
        public void Register(string name, MagicImplementation implementation) { implementations[name] = implementation ?? throw new ArgumentNullException(nameof(implementation)); }
        public bool ContainsBean(string name) => implementations.ContainsKey(name);
        public MagicImplementation GetBean(string name) => implementations.TryGetValue(name, out var implementation) ? implementation : null;
        public string[] BeanNames() => implementations.Keys.ToArray();
    }
    public sealed class DatabaseMagicParser : IMagicLookup {
        public const long InvalidMagicId = 0;
        private readonly IMagicCatalog catalog;
        public MagicRegistry Registry { get; }
        private SortedDictionary<long, BoundMagic> magics = new SortedDictionary<long, BoundMagic>();
        public DatabaseMagicParser(IMagicCatalog catalog, MagicRegistry registry, bool loadImmediately = true) {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog)); Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            if (loadImmediately) Init();
        }
        private void Init() {
            var loaded = new SortedDictionary<long, BoundMagic>();
            foreach (var row in catalog.GetAllMagic()) {
                string name = row.Name ?? ""; var implementation = Registry.GetBean(name);
                if (implementation == null) continue;
                long id = row.Id ?? InvalidMagicId; loaded[id] = implementation.Bind(id, name, row.Element ?? Element.None);
            }
            magics = loaded;
        }
        private void EnsureLoaded() { if (magics.Count == 0) Init(); }
        public void InvalidateCache() { magics.Clear(); }
        public BoundMagic GetMagic(long id) { EnsureLoaded(); return id > InvalidMagicId && magics.TryGetValue(id, out var magic) ? magic : null; }
        public BoundMagic ParseMagic(long userId, long magicId) { var magic = GetMagic(magicId); return magic != null && catalog.ExistUserMagic(userId, magic.Id) ? magic : null; }
        public BoundMagic ParseMagicForBot(long magicId) => GetMagic(magicId);
        public BoundMagic[] GetAllMagics() { EnsureLoaded(); return magics.Values.ToArray(); }
        public BoundMagic ParseMagicForBotByName(string name) {
            EnsureLoaded(); if (string.IsNullOrWhiteSpace(name)) return null;
            var implementation = Registry.GetBean(name); if (implementation == null) return null;
            // 같은 이름의 행이 여러 개면 모든 클라이언트에서 가장 작은 ID를 선택한다.
            return magics.Values.FirstOrDefault(magic => magic.Name == name) ?? implementation.Bind(InvalidMagicId, name, Element.None);
        }
    }
}

