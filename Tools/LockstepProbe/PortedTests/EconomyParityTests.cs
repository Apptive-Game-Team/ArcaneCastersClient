using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class EconomyParityTests {
        [PortedTest("game/service/system/frame_data_system.rs")]
        public static void charges_mana_and_draws_into_the_frame() {
            var context = new SimulationContext(0) { DeltaTime = Fix64.One };
            context.Players.Add(Master.LeftPlayer, Player(10)); context.Players.Add(Master.RightPlayer, Player(10));
            context.CardDecks.Add(Master.LeftPlayer, new CardDeck(new long[] { 34 }, 1));
            var system = new BattleEconomySystem(); system.Update(context);
            Assert.Equal(4, system.Left.Mana); Assert.Sequence(new long[] { 34 }, system.Left.DrawnCards);
            Assert.Equal(4, context.Players[Master.LeftPlayer].Mana);
        }
        private const string Mana = "game/service/mana_charger.rs";
        private const string Deck = "game/service/card_deck.rs";
        private const string Collections = "game/util/collections.rs";
        private const string Timer = "game/util/interval_timer.rs";
        private const string Elements = "game/domain/magic/elemental_chart.rs";
        private static PlayerData Player(int max = 20) => new PlayerData(new ManaCharger(max));
        private static void Charge(PlayerData player, int count) {
            for (int i = 0; i < count; i++) player.ManaCharger.ChargeMana(player, (Fix64)0.25m);
        }
        private static long[] LargeDeck() => Enumerable.Range(1, 1000).Select(i => (long)i).ToArray();

        [PortedTest(Mana)]
        public static void charges_one_mana_per_quarter_second_up_to_the_max() {
            var player = Player(3);
            for (int i = 0; i < 20; i++) player.ManaCharger.ChargeMana(player, (Fix64)0.05m);
            Assert.Equal(3, player.Mana);
        }
        [PortedTest(Mana)]
        public static void fever_doubles_the_charge() {
            var player = Player(100); player.ManaCharger.UpdateManaCharge(Fix64.One);
            for (int i = 0; i < 20; i++) player.ManaCharger.ChargeMana(player, (Fix64)0.05m);
            Assert.Equal(8, player.Mana);
        }
        [PortedTest(Mana)]
        public static void charges_the_default_one_mana_each_interval() {
            var player = Player(); Charge(player, 5); Assert.Equal(5, player.Mana);
        }
        [PortedTest(Mana)]
        public static void accumulates_mana_well_fractional_bonus_across_intervals() {
            var player = Player(); player.ManaCharger.UpdateManaCharge((Fix64)0.8m);
            Charge(player, 5); Assert.Equal(9, player.Mana);
        }
        [PortedTest(Mana)]
        public static void discards_fractional_mana_while_at_the_maximum() {
            var player = Player(); player.ManaCharger.UpdateManaCharge((Fix64)0.8m);
            Charge(player, 1); player.Mana = 20; Charge(player, 1); player.Mana = 0; Charge(player, 1);
            Assert.Equal(1, player.Mana);
        }
        [PortedTest(Mana)]
        public static void charges_the_same_mana_in_a_minute_at_any_tick_rate() {
            foreach (int rate in new[] { 20, 60 }) {
                var player = Player(10000);
                for (int i = 0; i < rate * 60; i++) player.ManaCharger.ChargeMana(player, Fix64.One / rate);
                Assert.Equal(240, player.Mana);
            }
        }
        [PortedTest(Mana)]
        public static void charges_the_first_mana_after_a_quarter_second() {
            foreach (int rate in new[] { 20, 60 }) {
                var player = Player();
                for (int i = 1; i < rate / 4; i++) player.ManaCharger.ChargeMana(player, Fix64.One / rate);
                Assert.Equal(0, player.Mana);
                player.ManaCharger.ChargeMana(player, Fix64.One / rate); Assert.Equal(1, player.Mana);
            }
        }
        [PortedTest(Deck)]
        public static void draws_the_same_cards_in_a_minute_with_fever_halfway_at_any_tick_rate() {
            foreach (int rate in new[] { 20, 60 }) {
                var deck = new CardDeck(LargeDeck(), 1); var player = Player(10); int drawn = 0;
                for (int frame = 1; frame <= 60 * rate; frame++) {
                    if (frame == 30 * rate + 1) deck.Fever();
                    deck.DrawCard(player, null, Fix64.One / rate);
                    drawn += player.Cards.Count; player.Cards.Clear();
                }
                Assert.Equal(90, drawn);
            }
        }
        [PortedTest(Deck)]
        public static void draws_the_first_card_after_one_second() {
            foreach (int rate in new[] { 20, 60 }) {
                var deck = new CardDeck(LargeDeck(), 1); var player = Player(10);
                for (int frame = 1; frame < rate; frame++) deck.DrawCard(player, null, Fix64.One / rate);
                Assert.Equal(0, player.Cards.Count);
                deck.DrawCard(player, null, Fix64.One / rate); Assert.Equal(1, player.Cards.Count);
            }
        }
        [PortedTest(Deck)]
        public static void keeps_the_time_already_waited_when_fever_starts() {
            foreach (int rate in new[] { 20, 60 }) {
                var deck = new CardDeck(LargeDeck(), 1); var player = Player(10);
                for (int frame = 0; frame < rate * 3 / 10; frame++) deck.DrawCard(player, null, Fix64.One / rate);
                deck.Fever(); int untilDraw = 0;
                while (player.Cards.Count == 0 && untilDraw <= rate) { deck.DrawCard(player, null, Fix64.One / rate); untilDraw++; }
                Assert.Equal(rate / 5, untilDraw);
            }
        }
        [PortedTest(Deck)]
        public static void shuffles_like_java_and_draws_in_order() {
            var deck = new CardDeck(Enumerable.Range(1, 10).Select(i => (long)i).ToArray(), 1);
            Assert.Sequence(new long[] { 7, 10, 8, 9, 5, 3, 1, 4, 2, 6 }, deck.RemainingCards);
            var player = Player(10); var drawn = new List<long>(); deck.DrawCard(player, drawn, (Fix64)2);
            Assert.Sequence(new long[] { 7, 10 }, drawn); Assert.Sequence(drawn, player.Cards);
        }
        [PortedTest(Collections)]
        public static void shuffles_ten_cards_like_java() {
            var cards = Enumerable.Range(1, 10).Select(i => (long)i).ToArray(); new JavaRandom(1).Shuffle(cards);
            Assert.Sequence(new long[] { 7, 10, 8, 9, 5, 3, 1, 4, 2, 6 }, cards);
        }
        [PortedTest(Collections)]
        public static void shuffles_a_thousand_cards_like_java() {
            var cards = LargeDeck(); new JavaRandom(1).Shuffle(cards);
            Assert.Sequence(new long[] { 447, 991, 801, 544, 931, 737, 414, 66 }, cards.Take(8));
        }
        [PortedTest(Collections)]
        public static void leaves_short_lists_alone() {
            var empty = new long[0]; new JavaRandom(1).Shuffle(empty); Assert.Equal(0, empty.Length);
            var one = new long[] { 5 }; new JavaRandom(1).Shuffle(one); Assert.Sequence(new long[] { 5 }, one);
        }
        [PortedTest(Timer)]
        public static void fires_once_per_interval_at_any_tick_rate() {
            foreach (int rate in new[] { 20, 60 }) {
                var timer = new IntervalTimer(); int fired = 0;
                for (int i = 0; i < rate * 10; i++) fired += timer.Advance(Fix64.One / rate, (Fix64)0.25m);
                Assert.Equal(40, fired);
            }
        }
        [PortedTest(Timer)]
        public static void a_long_frame_catches_up() { Assert.Equal(4, new IntervalTimer().Advance(Fix64.One, (Fix64)0.25m)); }
        [PortedTest(Timer)]
        public static void carry_over_keeps_only_an_overshoot_shorter_than_a_frame() {
            Assert.Near((Fix64)0.02m, IntervalTimer.CarryOver((Fix64)1.02m, Fix64.One, (Fix64)0.05m), Fix64.FromRaw(1));
            Assert.Equal(Fix64.Zero, IntervalTimer.CarryOver((Fix64)1.2m, Fix64.One, (Fix64)0.05m));
            Assert.Equal(Fix64.Zero, IntervalTimer.CarryOver((Fix64)0.9m, Fix64.One, (Fix64)0.05m));
        }
        [PortedTest(Elements)]
        public static void reads_rows_as_attack_and_columns_as_defense() {
            Assert.Equal((Fix64)2, ElementalChart.Multiplier(Element.Fire, Element.Nature));
            Assert.Equal((Fix64)0.5m, ElementalChart.Multiplier(Element.Nature, Element.Fire));
            Assert.Equal((Fix64)0.5m, ElementalChart.Multiplier(Element.Rock, Element.Wind));
        }
        [PortedTest(Elements)]
        public static void averages_every_pair_and_treats_empty_as_none() {
            Assert.Equal(Fix64.One, ElementalChart.Average(new Element[0], new Element[0]));
            Assert.Equal((Fix64)1.25m, ElementalChart.Average(new[] { Element.Fire, Element.Water }, new[] { Element.Nature }));
        }
    }
}
