using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum CardRole { Fighter, AntiAir, Defense, Pusher, Support, Economy }
    public static class CardRoles {
        private static readonly Dictionary<string, CardRole> roles = Build();
        private static readonly HashSet<string> warned = new HashSet<string>(StringComparer.Ordinal);
        public static event Action<string> UnknownSummon;
        private static Dictionary<string, CardRole> Build() {
            var result = new Dictionary<string, CardRole>(StringComparer.Ordinal);
            void Add(CardRole role, string names) { foreach (var name in names.Split(' ')) result.Add(name, role); }
            Add(CardRole.Fighter, "aqua_archer bean_shooter bomb_sprite cloud_dragon dimension_toad ember_spirit_swarm evil_ent fire_lord_spirit fire_spirit magma_spirit mini_rock_swarm rock_golem rock_mage sea_serpent seed_nest storm_rider storm_stag thunder_bird_swarm thunder_spirit towerback tree_golem vine_spirit wall_golem water_slime_swarm zap_mouse");
            Add(CardRole.AntiAir, "bubble_spirit tower wind_spirit");
            Add(CardRole.Defense, "cannon crater dragon_tower electric_tower firework_tower rock_turret shock_trap titan_remnant vine_colony");
            Add(CardRole.Pusher, "wind_totem"); Add(CardRole.Support, "bubble_generator grass_generator healing_totem life_tree repair_totem"); Add(CardRole.Economy, "mana_well");
            return result;
        }
        public static CardRole? OfName(string name) => roles.TryGetValue(name, out var role) ? role : (CardRole?)null;
        public static IReadOnlyDictionary<string, CardRole> Table => new System.Collections.ObjectModel.ReadOnlyDictionary<string, CardRole>(roles);
        public static CardRole? Of(BotMagic magic) {
            var role = OfName(magic.Name);
            if (!role.HasValue && magic.Summoning.HasValue) {
                bool first; lock (warned) first = warned.Add(magic.Name);
                if (first) UnknownSummon?.Invoke(magic.Name);
            }
            return role;
        }
    }
    public static class SummonRelevance {
        public static bool Relevant(BotMagic magic, ThinkContext c, IDictionary<string, Fix64> parameters) {
            bool Enemy(BotVisibleObject o) => o.Master == c.EnemySide && o.Mob && o.Targetable && o.Prefab != "Player";
            switch (CardRoles.Of(magic)) {
                case CardRole.AntiAir: return c.Objects.Any(o => Enemy(o) && parameters.TryGetValue(UnitCatalogue.GameObjectName(o.Prefab) + ".spawn_height", out var height) && height > Fix64.Zero);
                case CardRole.Defense:
                case CardRole.Pusher:
                    var enemyPosition = new Vector3(c.EnemySide == Master.LeftPlayer ? 1 : 17, 0, 5);
                    var half = (enemyPosition - c.PlayerPosition).Length() / 2;
                    return c.Objects.Any(o => Enemy(o) && (o.Position - c.PlayerPosition).Length() <= half);
                case CardRole.Support: return c.Objects.Count(o => o.Master == c.BotSide && o.Targetable && o.Prefab != "Player") >= 2;
                default: return true;
            }
        }
    }
    public readonly struct DeckProfile {
        public readonly int Cards, BigFighters;
        public bool Known => Cards > 0;
        public bool WavesPayOff => BigFighters >= 3;
        public DeckProfile(int cards, int bigFighters) { Cards = cards; BigFighters = bigFighters; }
        public static DeckProfile Of(IReadOnlyList<long> cards, IBotMagicCatalog catalog, BotSpellStats stats) {
            int big = 0;
            foreach (long id in cards) {
                var magic = catalog.GetMagic(id); if (magic == null || !magic.Summoning.HasValue || CardRoles.Of(magic) != CardRole.Fighter) continue;
                int cost = stats.ManaCost(magic); if (cost >= GamePlanner.WaveCardCost && cost < BotSpellStats.UnknownManaCost) big++;
            }
            return new DeckProfile(cards.Count, big);
        }
    }
    public enum BotPlanKind { None, Save, Wave }
    public readonly struct BotPlan {
        public readonly BotPlanKind Kind;
        public readonly ScoredPlay Play;
        public bool Saving => Kind == BotPlanKind.Save;
        public BotPlan(BotPlanKind kind, ScoredPlay play = null) { Kind = kind; Play = play; }
    }
    public static class GamePlanner {
        public const int WaveCardCost = 50, CapMargin = 10;
        public static BotPlan Plan(ThinkContext c, int maxMana, IDictionary<string, Fix64> parameters, DeckProfile deck, Func<BotMagic, Vector3> placement) {
            if (deck.Known && !deck.WavesPayOff) return default;
            var fighters = c.Hand.Where(m => m.Summoning.HasValue && CardRoles.Of(m) == CardRole.Fighter).Select(m => (magic: m, cost: c.SpellStats.ManaCost(m))).Where(f => f.cost > 0 && f.cost < BotSpellStats.UnknownManaCost).ToArray();
            if (!fighters.Any(f => f.cost >= WaveCardCost)) return default;
            int cap = maxMana > 0 ? maxMana - CapMargin : int.MaxValue;
            int target = Math.Max(fighters.Max(f => f.cost), Math.Min(fighters.Sum(f => f.cost), cap));
            BotVisibleObject? forming = null; Fix64 nearest = Fix64.MaxValue;
            foreach (var o in c.Objects) {
                if (o.Master != c.BotSide || !o.Mob || !o.Targetable || o.Prefab == "Player") continue;
                if (!parameters.TryGetValue(UnitCatalogue.GameObjectName(o.Prefab) + ".speed", out var speed) || speed <= Fix64.Zero) continue;
                var distance = (o.Position - c.PlayerPosition).Length(); if (distance > (Fix64)5) continue;
                if (distance < nearest) { forming = o; nearest = distance; }
            }
            BotMagic next = null; int nextCost = 0;
            foreach (var f in fighters) if (f.cost <= c.Mana && (next == null || f.cost > nextCost)) { next = f.magic; nextCost = f.cost; }
            if (next != null && (c.Mana >= target || forming.HasValue)) {
                var spot = forming.HasValue ? forming.Value.Position : placement(next);
                string reason = forming.HasValue ? "Joins the wave forming near our base: " : $"Releases a wave at mana {c.Mana}: ";
                return new BotPlan(BotPlanKind.Wave, c.Scored(next, spot, nextCost, Fix64.One, "plan.wave", reason + next.Name + "."));
            }
            return c.Mana >= cap ? default : new BotPlan(BotPlanKind.Save);
        }
    }
}
