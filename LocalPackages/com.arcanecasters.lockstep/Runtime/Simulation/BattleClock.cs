using System;
using System.Collections.Generic;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class GameTimer {
        private readonly IMillisClock clock;
        private readonly long feverDuration;
        public long StartTime { get; private set; }
        public long Duration { get; private set; }
        public long RemainingMilliseconds => StartTime + Duration - clock.NowMillis;
        public long RemainingSeconds => RemainingMilliseconds / 1000;
        public bool Ended => RemainingMilliseconds <= 0;
        public bool Fever => RemainingMilliseconds < feverDuration;
        public GameTimer(long durationMilliseconds, long feverMilliseconds, IMillisClock clock) {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (feverMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(feverMilliseconds));
            feverDuration = feverMilliseconds;
            OverrideDuration(durationMilliseconds);
        }
        // 시작 시간을 다시 잡는 원본 규칙을 유지한다. 실제 전투에서는 프레임 시계만 주입한다.
        public void OverrideDuration(long durationMilliseconds) {
            if (durationMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
            Duration = durationMilliseconds; StartTime = clock.NowMillis;
        }
    }

    public sealed class BattleClock {
        public const int TickMilliseconds = 50;
        public int Tick { get; private set; }
        public int DurationMilliseconds { get; }
        public int FeverMilliseconds { get; }
        public int RemainingMilliseconds => Math.Max(0, DurationMilliseconds - Tick * TickMilliseconds);
        public bool Ended => RemainingMilliseconds == 0;
        public bool Fever => RemainingMilliseconds < FeverMilliseconds;
        public BattleClock(int durationMilliseconds, int feverMilliseconds) {
            if (durationMilliseconds <= 0 || durationMilliseconds > 600000 || feverMilliseconds < 0 || feverMilliseconds > durationMilliseconds)
                throw new ArgumentOutOfRangeException();
            DurationMilliseconds = durationMilliseconds; FeverMilliseconds = feverMilliseconds;
        }
        public void Advance() { if (!Ended) Tick++; }
    }

    public sealed class IntervalTimer {
        // 원본 IntervalTimer의 오차 경계를 유지하되 모든 누적과 비교는 고정 소수점으로 수행한다.
        public static readonly Fix64 EpsilonSeconds = (Fix64)0.000001m;
        public Fix64 ElapsedSeconds { get; private set; }
        public Fix64 ElapsedTicks => ElapsedSeconds * PlayerEconomy.TickRate;
        public int Advance(Fix64 intervalSeconds) => Advance(PhysicsWorld.StepDuration, intervalSeconds);
        public int Advance(Fix64 deltaSeconds, Fix64 intervalSeconds) {
            if (intervalSeconds <= Fix64.Zero || deltaSeconds < Fix64.Zero) throw new ArgumentOutOfRangeException();
            ElapsedSeconds += deltaSeconds;
            int completed = 0;
            while (ElapsedSeconds + EpsilonSeconds >= intervalSeconds) {
                ElapsedSeconds -= intervalSeconds;
                if (++completed > 4096) throw new InvalidOperationException("Too many interval events in one tick");
            }
            return completed;
        }
        public static Fix64 CarryOver(Fix64 timer, Fix64 interval, Fix64 delta) {
            Fix64 overshoot = timer - interval;
            return overshoot <= Fix64.Zero || overshoot >= delta ? Fix64.Zero : overshoot;
        }
    }

    public readonly struct ScheduledAction {
        public readonly int DueTick, OwnerId, Kind;
        public readonly long Sequence, Payload;
        internal ScheduledAction(int dueTick, long sequence, int ownerId, int kind, long payload) {
            DueTick = dueTick; Sequence = sequence; OwnerId = ownerId; Kind = kind; Payload = payload;
        }
    }

    public sealed class TickScheduler {
        private sealed class Ordering : IComparer<ScheduledAction> {
            public int Compare(ScheduledAction a, ScheduledAction b) {
                int tick = a.DueTick.CompareTo(b.DueTick);
                return tick == 0 ? a.Sequence.CompareTo(b.Sequence) : tick;
            }
        }
        private readonly SortedSet<ScheduledAction> pending = new SortedSet<ScheduledAction>(new Ordering());
        public int Tick { get; private set; } = -1;
        public long NextSequence { get; private set; }
        public ScheduledAction[] Pending { get { var copy = new ScheduledAction[pending.Count]; pending.CopyTo(copy); return copy; } }

        public void Schedule(int dueTick, int ownerId, int kind, long payload = 0) {
            if (dueTick <= Tick || dueTick >= 12000 || ownerId < 0 || kind < 0) throw new ArgumentOutOfRangeException();
            if (pending.Count >= 65536) throw new InvalidOperationException("Too many scheduled actions");
            pending.Add(new ScheduledAction(dueTick, checked(NextSequence++), ownerId, kind, payload));
        }
        public void CancelOwner(int ownerId) { pending.RemoveWhere(action => action.OwnerId == ownerId); }
        public ScheduledAction[] Advance(int tick) {
            if (tick != Tick + 1) throw new InvalidOperationException("Scheduler requires contiguous confirmed ticks");
            Tick = tick;
            var due = new List<ScheduledAction>();
            while (pending.Count > 0 && pending.Min.DueTick <= tick) {
                var action = pending.Min; pending.Remove(action); due.Add(action);
            }
            return due.ToArray();
        }
    }
}
