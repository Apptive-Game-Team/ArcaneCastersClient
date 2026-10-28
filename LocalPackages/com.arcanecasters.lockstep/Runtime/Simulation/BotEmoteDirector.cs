using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum Emote { Laugh, Greet, Taunt, Cry, Surprised }
    public sealed class BotEmoteDirector {
        private sealed class Profile {
            public readonly Fix64 Rate, ReplyRate;
            public readonly int[][] Situations, Replies;
            public Profile(decimal rate, decimal replyRate, int[][] situations, int[][] replies) { Rate = (Fix64)rate; ReplyRate = (Fix64)replyRate; Situations = situations; Replies = replies; }
        }
        private static readonly Profile[] profiles = {
            new Profile(.8m, .95m, new[] { new[] {0,1,0,0,0}, new[] {3,0,0,0,1}, new[] {0,0,0,1,2} }, new[] { new[] {4,0,0,0,1}, new[] {0,5,0,0,0}, new[] {3,0,0,0,1}, new[] {0,1,0,0,2}, new[] {1,0,0,0,3} }),
            new Profile(.9m, .85m, new[] { new[] {1,2,0,0,0}, new[] {1,0,4,0,0}, new[] {0,0,0,1,2} }, new[] { new[] {4,0,0,0,0}, new[] {2,3,0,0,0}, new[] {1,0,4,0,0}, new[] {1,0,3,0,0}, new[] {3,0,1,0,0} }),
            new Profile(.7m, .8m, new[] { new[] {0,1,0,0,0}, new[] {1,0,0,0,2}, new[] {0,0,0,3,2} }, new[] { new[] {3,0,0,0,1}, new[] {0,4,0,0,1}, new[] {0,0,0,3,1}, new[] {0,0,0,4,0}, new[] {0,0,0,1,4} }),
            new Profile(.15m, .2m, new[] { new[] {0,1,0,0,0}, new[] {0,0,0,0,0}, new[] {0,0,0,0,0} }, new[] { new[] {1,0,0,0,0}, new[] {0,1,0,0,0}, new[] {0,0,0,0,0}, new[] {0,0,0,0,0}, new[] {0,0,0,0,0} })
        };
        private readonly Profile profile;
        private readonly JavaRandom random;
        private bool started, greetingSettled;
        private long greetingAt, nextAllowed, nextConsider, nextReplyAllowed, replyAt, lastSent = long.MinValue / 2;
        private Emote? pending;
        private int sent, situationalSent, repliesSent;
        private readonly int[] situationSent = new int[3];
        public BotEmoteDirector(BotTemperament temperament, JavaRandom random) { profile = profiles[(int)temperament]; this.random = random ?? throw new ArgumentNullException(nameof(random)); }
        public Emote? NextEmote(long now, int ownHp, int enemyHp) => NextEmote(now, ownHp, enemyHp, random);
        public void OnOpponentEmote(Emote? emote, long now) => OnOpponentEmote(emote, now, random);
        public void OnOpponentEmote(Emote? emote, long now, JavaRandom generator) {
            Start(now, generator);
            if (!emote.HasValue || pending.HasValue || repliesSent >= 2 || sent >= 6 || now < nextReplyAllowed) return;
            pending = emote; replyAt = now + 800 + Jitter(generator, 1700);
        }
        public Emote? NextEmote(long now, int ownHp, int enemyHp, JavaRandom generator) {
            Start(now, generator);
            if (sent >= 6 || now < lastSent + 3000) return null;
            if (pending.HasValue && now >= replyAt) {
                var incoming = pending.Value; pending = null; nextReplyAllowed = now + 8000;
                if (repliesSent < 2 && generator.NextFraction() < profile.ReplyRate) {
                    var reply = Pick(profile.Replies[(int)incoming], generator);
                    if (reply.HasValue) { repliesSent++; Record(now); return reply; }
                }
            }
            if (!greetingSettled && now >= greetingAt && situationalSent < 4) {
                greetingSettled = true;
                if (generator.NextFraction() < profile.Rate) { var greeting = Take(0, now, generator); if (greeting.HasValue) return greeting; }
            }
            if (situationalSent >= 4 || now < nextAllowed || now < nextConsider) return null;
            nextConsider = now + 2000;
            if (ownHp <= 0 || enemyHp <= 0) return null;
            int situation = (long)enemyHp * 10 < (long)ownHp * 7 ? 1 : (long)ownHp * 10 < (long)enemyHp * 7 ? 2 : -1;
            if (situation < 0 || situationSent[situation] >= 2 || generator.NextFraction() >= profile.Rate) return null;
            return Take(situation, now, generator);
        }
        private void Start(long now, JavaRandom generator) { if (started) return; started = true; greetingAt = now + 3000 + Jitter(generator, 5000); nextAllowed = now + 20000; }
        private Emote? Take(int situation, long now, JavaRandom generator) {
            var emote = Pick(profile.Situations[situation], generator); if (!emote.HasValue) return null;
            situationalSent++; situationSent[situation]++; nextAllowed = now + 20000 + Jitter(generator, 10000); Record(now); return emote;
        }
        private void Record(long now) { sent++; lastSent = now; }
        private static long Jitter(JavaRandom generator, int spread) => (generator.NextFraction() * spread).RawValue / (1L << 32);
        private static Emote? Pick(int[] weights, JavaRandom generator) {
            int total = 0; foreach (int weight in weights) total += weight;
            if (total <= 0) return null;
            var roll = generator.NextFraction() * total;
            for (int i = 0; i < weights.Length; i++) { roll -= weights[i]; if (roll < Fix64.Zero) return (Emote)i; }
            return Emote.Surprised;
        }
    }
}
