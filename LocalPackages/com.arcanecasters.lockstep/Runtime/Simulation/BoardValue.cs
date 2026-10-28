using System;
using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class BoardValue {
        private readonly Dictionary<string, int> costByPrefab = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly IDictionary<string, Fix64> parameters;
        public Exception InitializationFailure { get; }
        public BoardValue(IBotMagicCatalog catalog, BotSpellStats stats, IDictionary<string, Fix64> parameters) {
            this.parameters = parameters;
            try {
                foreach (var magic in catalog.GetAllMagics()) {
                    if (!magic.Summoning.HasValue) continue;
                    int cost = stats.ManaCost(magic); if (cost >= BotSpellStats.UnknownManaCost) continue;
                    var summon = magic.Summoning.Value; int share = cost / Math.Max(1, summon.Quantity);
                    costByPrefab[summon.Prefab] = costByPrefab.TryGetValue(summon.Prefab, out var previous) ? Math.Min(previous, share) : share;
                }
            } catch (Exception exception) { InitializationFailure = exception; }
        }
        public int ManaCostOf(string prefab) => costByPrefab.TryGetValue(prefab, out var cost) ? cost : 0;
        public int ManaOnField(IEnumerable<BotVisibleObject> objects, Master side) {
            int total = 0;
            foreach (var item in objects) {
                if (item.Master != side || item.Prefab == "Player" || !item.Targetable) continue;
                Fix64 fraction = Fix64.One;
                if (parameters.TryGetValue(UnitCatalogue.GameObjectName(item.Prefab) + ".hp", out var hp) && hp > Fix64.Zero)
                    fraction = MathHelper.Clamp((Fix64)item.Hp / hp, Fix64.Zero, Fix64.One);
                total += (int)(Fix64.Floor(ManaCostOf(item.Prefab) * fraction + (Fix64)0.5m).RawValue / (1L << 32));
            }
            return total;
        }
    }
}
