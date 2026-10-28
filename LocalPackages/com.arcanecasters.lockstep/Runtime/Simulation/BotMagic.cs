using System.Collections.Generic;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum BotMagicKind { Other, VineToss, Overgrowth, Explosion }
    public readonly struct SummonedBodies {
        public readonly string Prefab;
        public readonly int Quantity;
        public readonly bool Airborne;
        public SummonedBodies(string prefab, int quantity = 1, bool airborne = false) { Prefab = prefab; Quantity = quantity; Airborne = airborne; }
    }
    public sealed class BotMagic {
        public long Id { get; }
        public string Name { get; }
        public Element Element { get; }
        public BotMagicKind Kind { get; }
        public bool IsExplosion { get; }
        public string ClassName { get; }
        public SummonedBodies? Summoning { get; }
        public BotMagic(long id, string name, Element element = Element.None, BotMagicKind kind = BotMagicKind.Other, string className = "", SummonedBodies? summoning = null, bool? isExplosion = null) {
            Id = id; Name = name; Element = element; Kind = kind; ClassName = className; Summoning = summoning; IsExplosion = isExplosion ?? kind == BotMagicKind.Explosion;
        }
    }
    public interface IBotMagicCatalog {
        BotMagic GetMagic(long magicId);
        IEnumerable<BotMagic> GetAllMagics();
    }
    public sealed class BotSpellStats {
        public const int UnknownManaCost = 9999;
        public static readonly Fix64 UnknownDamage = 8;
        public static readonly Fix64 UnknownBlastRadius = (Fix64)0.5m;
        private readonly IDictionary<string, Fix64> parameters;
        public BotSpellStats(IDictionary<string, Fix64> parameters) { this.parameters = parameters; }
        private Fix64 Value(BotMagic magic, string key, Fix64 fallback) => parameters.TryGetValue(magic.Name + "." + key, out var value) ? value : fallback;
        public int ManaCost(BotMagic magic) => (int)(Value(magic, "mana_cost", UnknownManaCost).RawValue / (1L << 32));
        public Fix64 CastRange(BotMagic magic) => Value(magic, "range", 0);
        public Fix64 BlastRadius(BotMagic magic) => Value(magic, "radius", UnknownBlastRadius);
        public Fix64 DamagePerTarget(BotMagic magic) { var value = Value(magic, "damage", UnknownDamage); return value > Fix64.Zero ? value : UnknownDamage; }
        public Fix64 StatedDamage(BotMagic magic) { var value = Value(magic, "damage", 0); return value > Fix64.Zero ? value : Fix64.Zero; }
    }
}
