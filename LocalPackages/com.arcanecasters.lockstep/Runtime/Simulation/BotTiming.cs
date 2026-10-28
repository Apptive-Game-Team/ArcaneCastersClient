using System;
using System.Threading;

namespace ArcaneCasters.Simulation {
    public enum BotTier { Hospitality, Intro, Beginner, Intermediate, Advanced, Elite }
    public interface IMillisClock { long NowMillis { get; } }
    public sealed class FrameMillisClock : IMillisClock {
        private readonly SimulationContext context;
        public FrameMillisClock(SimulationContext context) { this.context = context ?? throw new ArgumentNullException(nameof(context)); }
        public long NowMillis => (long)context.FrameNumber * 1000 / context.TickRate;
    }
    public sealed class CastDeadline {
        public const long HospitalityMaxSilenceMillis = 60000;
        private readonly long maxSilenceMillis;
        private long lastCastAtMillis;
        public CastDeadline(BotTier tier, long startedAtMillis) {
            maxSilenceMillis = tier == BotTier.Hospitality ? HospitalityMaxSilenceMillis : 0;
            lastCastAtMillis = startedAtMillis;
        }
        public bool Overdue(long nowMillis) => maxSilenceMillis != 0 && nowMillis - Interlocked.Read(ref lastCastAtMillis) >= maxSilenceMillis;
        public void RecordCast(long nowMillis) => Interlocked.Exchange(ref lastCastAtMillis, nowMillis);
    }
}
