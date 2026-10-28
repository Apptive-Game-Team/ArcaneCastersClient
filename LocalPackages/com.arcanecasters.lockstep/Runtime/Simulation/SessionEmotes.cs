using System;
using System.Threading;

namespace ArcaneCasters.Simulation {
    public sealed class EmoteCooldown {
        public const long DurationMillis = 3000;
        private readonly IMillisClock clock;
        private readonly long[] until = new long[2];
        public EmoteCooldown(IMillisClock clock) { this.clock = clock ?? throw new ArgumentNullException(nameof(clock)); }
        public long LeftUntilMillis => Interlocked.Read(ref until[0]);
        public long RightUntilMillis => Interlocked.Read(ref until[1]);
        public bool TryConsume(Master? side) {
            int index = side == Master.LeftPlayer ? 0 : side == Master.RightPlayer ? 1 : -1;
            if (index < 0) return false;
            long now = clock.NowMillis, previous = Interlocked.Read(ref until[index]);
            if (now < previous) return false;
            return Interlocked.CompareExchange(ref until[index], now + DurationMillis, previous) == previous;
        }
    }
    public sealed class SessionEmotes {
        private readonly SimulationContext context;
        public EmoteCooldown Cooldown { get; }
        public event Action<Master, Emote> Sent;
        public SessionEmotes(SimulationContext context) {
            this.context = context ?? throw new ArgumentNullException(nameof(context)); Cooldown = new EmoteCooldown(new FrameMillisClock(context));
        }
        public static Emote? Parse(string name) {
            switch (name) { case "Laugh": return Emote.Laugh; case "Greet": return Emote.Greet; case "Taunt": return Emote.Taunt; case "Cry": return Emote.Cry; case "Surprised": return Emote.Surprised; default: return null; }
        }
        // 확정된 입력을 소비하는 프레임에서만 호출한다.
        public bool ReceivePlayer(long userId, string name) {
            var emote = Parse(name); if (!emote.HasValue) return false;
            Master? side = null;
            foreach (var candidate in new[] { Master.LeftPlayer, Master.RightPlayer }) if (context.UserIds.TryGetValue(candidate, out var id) && id == userId) { side = candidate; break; }
            if (!side.HasValue || !Cooldown.TryConsume(side)) return false;
            Sent?.Invoke(side.Value, emote.Value);
            var opponent = side == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer;
            if (context.Service<SessionBotControl>()?.HasBotAgent(opponent) == true) context.SubmitAction("botEmoteReply", c => c.Service<SessionBotControl>()?.OnOpponentEmote(opponent, emote.Value));
            return true;
        }
        // BotAgent가 같은 Cooldown을 소비한 뒤 호출한다.
        public void PublishBot(Master side, Emote emote) => Sent?.Invoke(side, emote);
    }
}
