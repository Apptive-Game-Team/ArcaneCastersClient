using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcaneCasters.Simulation {
    public sealed class ParsedBotMagicCatalog : IBotMagicCatalog {
        private readonly DatabaseMagicParser parser;
        private readonly SimulationContext context;
        private BotMagic[] magics;
        public ParsedBotMagicCatalog(DatabaseMagicParser parser, SimulationContext context) { this.parser = parser; this.context = context; }
        public static BotMagic FromBound(BoundMagic magic, SimulationContext context) {
            var kind = magic.Name == "vine_toss" ? BotMagicKind.VineToss : magic.Name == "overgrowth" ? BotMagicKind.Overgrowth : magic.IsExplosionMagic ? BotMagicKind.Explosion : BotMagicKind.Other;
            var className = string.Concat(magic.Name.Split('_').Where(part => part.Length > 0).Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1))) + "Magic";
            SummonedBodies? summoning = magic.SummonedPrefab == null ? (SummonedBodies?)null : new SummonedBodies(magic.SummonedPrefab, magic.SummonedQuantity(context), magic.Airborne);
            return new BotMagic(magic.Id, magic.Name, magic.Element, kind, className, summoning, magic.IsExplosionMagic);
        }
        private BotMagic[] Load() {
            if (magics != null) return magics;
            var built = parser.GetAllMagics().Select(magic => FromBound(magic, context)).ToArray();
            if (built.Length > 0) magics = built;
            return built;
        }
        public BotMagic GetMagic(long magicId) => magicId <= 0 ? null : Load().FirstOrDefault(m => m.Id == magicId);
        public IEnumerable<BotMagic> GetAllMagics() => Array.AsReadOnly(Load());
        public void InvalidateCache() { magics = null; }
    }
}
