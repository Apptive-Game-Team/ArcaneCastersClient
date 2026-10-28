using System;
using System.Collections.Generic;
using System.Text;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class UnitCatalogue {
        private readonly IDictionary<string, Fix64> parameters;
        private readonly Fix64 averagePower, averagePowerPerMana;
        public Fix64 AverageSummonCost { get; }
        public Exception InitializationFailure { get; }
        public UnitCatalogue(IBotMagicCatalog catalog, BotSpellStats stats, IDictionary<string, Fix64> parameters) {
            this.parameters = parameters;
            Fix64 powerSum = 0, perManaSum = 0, costSum = 0; int powerCount = 0, perManaCount = 0, costCount = 0;
            try {
                foreach (var magic in catalog.GetAllMagics()) {
                    if (!magic.Summoning.HasValue) continue;
                    int cost = stats.ManaCost(magic); if (cost <= 0 || cost >= BotSpellStats.UnknownManaCost) continue;
                    costSum += cost; costCount++;
                    var summon = magic.Summoning.Value; var power = Power(summon.Prefab, Value(summon.Prefab, "hp"));
                    if (power > Fix64.Zero) { powerSum += power; powerCount++; perManaSum += power * Math.Max(1, summon.Quantity) / cost; perManaCount++; }
                }
            } catch (Exception exception) { InitializationFailure = exception; }
            averagePower = powerCount == 0 ? Fix64.Zero : powerSum / powerCount;
            averagePowerPerMana = perManaCount == 0 ? Fix64.Zero : perManaSum / perManaCount;
            AverageSummonCost = costCount == 0 ? Fix64.Zero : costSum / costCount;
        }
        public static string GameObjectName(string prefab) {
            var name = new StringBuilder();
            for (int i = 0; i < prefab.Length; i++) { if (i > 0 && char.IsUpper(prefab[i])) name.Append('_'); name.Append(char.ToLowerInvariant(prefab[i])); }
            return name.ToString();
        }
        private Fix64 Value(string prefab, string key) => parameters.TryGetValue(GameObjectName(prefab) + "." + key, out var value) ? value : Fix64.Zero;
        private Fix64 Power(string prefab, Fix64 hp) {
            var damage = Value(prefab, "damage"); if (hp <= Fix64.Zero || damage <= Fix64.Zero) return Fix64.Zero;
            var interval = Value(prefab, "attack_interval");
            return Fix64.Sqrt(hp * (damage / (interval > Fix64.Zero ? interval : Fix64.One)));
        }
        public Fix64 RelativeEfficiency(BotMagic magic, int cost) {
            if (!magic.Summoning.HasValue || cost <= 0 || averagePowerPerMana <= Fix64.Zero) return Fix64.One;
            var summon = magic.Summoning.Value; var power = Power(summon.Prefab, Value(summon.Prefab, "hp"));
            if (power <= Fix64.Zero) return Fix64.One;
            return MathHelper.Clamp((power * Math.Max(1, summon.Quantity) / cost) / averagePowerPerMana, (Fix64)0.5m, 2);
        }
        public Fix64 RelativeThreat(BotVisibleObject item) {
            if (item.Prefab == "Player") return Fix64.Zero;
            if (averagePower <= Fix64.Zero) return Fix64.One;
            var power = Power(item.Prefab, item.Hp); if (power <= Fix64.Zero) return Fix64.One;
            return MathHelper.Clamp(power / averagePower, (Fix64)0.25m, 3);
        }
        public bool SummonsRanged(BotMagic magic) => magic.Summoning.HasValue && Value(magic.Summoning.Value.Prefab, "attack_range") >= (Fix64)3;
    }
}
