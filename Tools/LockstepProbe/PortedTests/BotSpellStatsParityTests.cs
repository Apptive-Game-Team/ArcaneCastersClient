using System.Collections.Generic;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BotSpellStatsParityTests {
        private const string Source = "game/domain/bot/bot_spell_stats.rs";
        [PortedTest(Source)] public static void charges_one_cast_read_under_the_magic_name() {
            var stats = new BotSpellStats(new Dictionary<string, Fix64> { ["leafair.mana_cost"] = 20 }); Assert.Equal(20, stats.ManaCost(new BotMagic(1, "leafair")));
        }
        [PortedTest(Source)] public static void treats_an_unpriced_magic_as_unaffordable() {
            var stats = new BotSpellStats(new Dictionary<string, Fix64>()); Assert.Equal(BotSpellStats.UnknownManaCost, stats.ManaCost(new BotMagic(1, "leafair")));
        }
        [PortedTest(Source)] public static void reads_cast_range_from_the_magic() {
            var stats = new BotSpellStats(new Dictionary<string, Fix64> { ["razor_gale.range"] = 9 }); Assert.Equal((Fix64)9, stats.CastRange(new BotMagic(1, "razor_gale")));
        }
        [PortedTest(Source)] public static void falls_back_to_neutral_damage_when_the_magic_has_no_damage_row() {
            var stats = new BotSpellStats(new Dictionary<string, Fix64>()); Assert.Equal(BotSpellStats.UnknownDamage, stats.DamagePerTarget(new BotMagic(1, "zap_mouse")));
        }
    }
}
