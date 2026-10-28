using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class BotEmoteParityTests {
        private const string Source = "game/domain/bot/bot_emote_director.rs";
        private static List<(long time, Emote emote)> Run(BotTemperament temperament, JavaRandom random, int health, params (long time, Emote emote)[] incoming) {
            var director = new BotEmoteDirector(temperament, new JavaRandom(0)); var sent = new List<(long, Emote)>();
            for (long now = 0; now <= 300000; now += 100) {
                foreach (var one in incoming) if (one.time == now) director.OnOpponentEmote(one.emote, now, random);
                int state = health == 3 ? (now < 150000 ? 1 : 2) : health;
                var emote = director.NextEmote(now, state == 2 ? 400 : 1000, state == 1 ? 400 : 1000, random);
                if (emote.HasValue) sent.Add((now, emote.Value));
            }
            return sent;
        }
        private static int Total(BotTemperament temperament) { var random = new JavaRandom(20260922); int total = 0; for (int i = 0; i < 200; i++) total += Run(temperament, random, 3).Count; return total; }
        private static (long time, Emote emote)[] Replies(List<(long time, Emote emote)> sent) => sent.Where(s => s.time > 8100).ToArray();
        [PortedTest(Source)] public static void stoic_stays_far_quieter_than_warm() { int warm = Total(BotTemperament.Warm); Assert.True(Total(BotTemperament.Stoic) < warm / 5); Assert.True(warm > 200); }
        [PortedTest(Source)] public static void sends_at_most_four_situational_emotes_in_one_match() {
            var random = new JavaRandom(4242); int busiest = 0; for (int i = 0; i < 200; i++) { int count = Run(BotTemperament.Smug, random, 3).Count; Assert.True(count <= 4); busiest = Math.Max(busiest, count); } Assert.Equal(4, busiest);
        }
        [PortedTest(Source)] public static void sends_at_most_six_emotes_in_one_match_with_replies_included() {
            var random = new JavaRandom(606); for (int i = 0; i < 200; i++) Assert.True(Run(BotTemperament.Smug, random, 3, (10000, Emote.Greet), (30000, Emote.Laugh), (60000, Emote.Taunt), (120000, Emote.Cry), (200000, Emote.Surprised)).Count <= 6);
        }
        [PortedTest(Source)] public static void keeps_at_least_twenty_seconds_between_emotes() {
            var random = new JavaRandom(7); foreach (BotTemperament temperament in Enum.GetValues(typeof(BotTemperament))) for (int i = 0; i < 200; i++) { var sent = Run(temperament, random, 3); for (int j = 1; j < sent.Count; j++) Assert.True(sent[j].time - sent[j - 1].time >= 20000); }
        }
        [PortedTest(Source)] public static void warm_never_taunts() {
            var random = new JavaRandom(11); for (int health = 0; health < 4; health++) for (int i = 0; i < 200; i++) Assert.True(Run(BotTemperament.Warm, random, health).All(s => s.emote != Emote.Taunt));
        }
        [PortedTest(Source)] public static void ahead_and_behind_draw_from_their_own_sets() {
            var random = new JavaRandom(99); for (int i = 0; i < 200; i++) {
                Assert.True(Run(BotTemperament.Smug, random, 1).Where(s => s.time >= 20000).All(s => s.emote == Emote.Taunt || s.emote == Emote.Laugh));
                Assert.True(Run(BotTemperament.Timid, random, 2).Where(s => s.time >= 20000).All(s => s.emote == Emote.Cry || s.emote == Emote.Surprised));
            }
        }
        [PortedTest(Source)] public static void spends_a_separate_budget_per_situation() { var random = new JavaRandom(1234); for (int i = 0; i < 200; i++) Assert.True(Run(BotTemperament.Smug, random, 1).Count(s => s.time >= 20000) <= 2); }
        [PortedTest(Source)] public static void stays_quiet_while_the_score_is_close() { var random = new JavaRandom(55); for (int i = 0; i < 200; i++) Assert.Equal(0, Run(BotTemperament.Smug, random, 0).Count(s => s.time >= 20000)); }
        [PortedTest(Source)] public static void varies_the_greeting_time() {
            var random = new JavaRandom(2026); var times = new List<long>(); for (int i = 0; i < 200; i++) { var sent = Run(BotTemperament.Warm, random, 0); if (sent.Count > 0) times.Add(sent[0].time); }
            Assert.True(times.Count > 0); Assert.True(times.Distinct().Count() > 10); Assert.True(times.All(t => t >= 3000 && t <= 8100));
        }
        private static int GreetBacks(BotTemperament temperament) {
            var random = new JavaRandom(20260923); int count = 0; for (int i = 0; i < 200; i++) { var replies = Replies(Run(temperament, random, 0, (12000, Emote.Greet))); if (replies.Length > 0 && replies[0].emote == Emote.Greet) count++; } return count;
        }
        [PortedTest(Source)] public static void warm_greets_back_far_more_often_than_stoic() { int warm = GreetBacks(BotTemperament.Warm); Assert.True(GreetBacks(BotTemperament.Stoic) < warm / 3); Assert.True(warm > 100); }
        [PortedTest(Source)] public static void warm_never_taunts_in_reply() {
            var random = new JavaRandom(808); foreach (Emote emote in Enum.GetValues(typeof(Emote))) for (int i = 0; i < 200; i++) Assert.True(Run(BotTemperament.Warm, random, 0, (12000, emote)).All(s => s.emote != Emote.Taunt));
        }
        [PortedTest(Source)] public static void sends_at_most_two_replies_in_one_match() {
            var random = new JavaRandom(212); int busiest = 0; for (int i = 0; i < 200; i++) { int count = Replies(Run(BotTemperament.Warm, random, 0, (10000, Emote.Greet), (30000, Emote.Laugh), (50000, Emote.Taunt), (70000, Emote.Cry), (90000, Emote.Surprised))).Length; Assert.True(count <= 2); busiest = Math.Max(busiest, count); } Assert.Equal(2, busiest);
        }
        [PortedTest(Source)] public static void answers_a_burst_only_once() {
            var random = new JavaRandom(313); for (int i = 0; i < 200; i++) Assert.True(Replies(Run(BotTemperament.Warm, random, 0, (10000, Emote.Greet), (10300, Emote.Laugh), (10600, Emote.Taunt), (10900, Emote.Cry), (11200, Emote.Surprised))).Count(s => s.time < 18000) <= 1);
        }
        [PortedTest(Source)] public static void answers_while_the_situational_gap_is_closed() {
            var random = new JavaRandom(414); int answered = 0; for (int i = 0; i < 200; i++) { var replies = Replies(Run(BotTemperament.Warm, random, 0, (12000, Emote.Greet))); if (replies.Length > 0) { answered++; Assert.True(replies[0].time < 20000); } } Assert.True(answered > 150);
        }
        [PortedTest(Source)] public static void varies_the_reply_delay() {
            var random = new JavaRandom(515); var delays = new List<long>(); for (int i = 0; i < 200; i++) { var replies = Replies(Run(BotTemperament.Warm, random, 0, (12000, Emote.Greet))); if (replies.Length > 0) delays.Add(replies[0].time - 12000); }
            Assert.True(delays.Count > 0); Assert.True(delays.Distinct().Count() > 10); Assert.True(delays.All(t => t >= 800 && t <= 2600));
        }
        [PortedTest(Source)] public static void the_owned_generator_drives_the_public_entry_points() {
            var owned = new BotEmoteDirector(BotTemperament.Warm, new JavaRandom(3)); var explicitDirector = new BotEmoteDirector(BotTemperament.Warm, new JavaRandom(9)); var random = new JavaRandom(3);
            var actual = new List<Emote?>(); var expected = new List<Emote?>(); for (int i = 0; i < 2000; i++) { actual.Add(owned.NextEmote(i * 100, 1000, 1000)); expected.Add(explicitDirector.NextEmote(i * 100, 1000, 1000, random)); }
            Assert.Equal(expected.FindIndex(e => e.HasValue), actual.FindIndex(e => e.HasValue)); Assert.Sequence(expected, actual);
        }
    }
}
