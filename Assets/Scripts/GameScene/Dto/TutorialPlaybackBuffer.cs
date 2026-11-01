using System;
using System.Collections.Generic;

namespace GameScene.Dto
{
    /// <summary>HTTP 수신 순서와 실제 화면에 표시할 tick 시각을 분리한다.</summary>
    public sealed class TutorialPlaybackBuffer
    {
        private const int MaximumFrames = 100;
        private const int MaximumLagTicks = 10;
        private const double InitialDelaySeconds = 0.1;
        private readonly Queue<TutorialFrameDto> pending = new Queue<TutorialFrameDto>();
        private bool initialized;
        private bool clockStarted;
        private bool hasClockSample;
        private double lastClock;
        private double playTick;
        private int revision;
        private int playedTick;
        private int latestTick;
        private int groupTick;
        private bool groupPlayed;
        private double settledAt;

        public int ReceivedFrame { get; private set; }
        public int PlayedFrame { get; private set; }
        public int TickRate { get; private set; } = 20;
        public int Count => pending.Count;
        public bool NeedsSync { get; private set; }
        public double FrameDuration => 1d / TickRate;

        /// <summary>true이면 driver는 이번 응답의 snapshot만 적용하고 과거 프레임을 버린다.</summary>
        public bool Receive(TutorialStateDto state, double now)
        {
            int rate = state.tickRate > 0 ? state.tickRate : 20;
            if (!initialized || NeedsSync || revision != state.revision || TickRate != rate)
                return Reset(state, now, rate);
            if (state.lastFrame < ReceivedFrame) return false;

            int expected = ReceivedFrame;
            int tick = latestTick;
            foreach (var frame in state.frames ?? Array.Empty<TutorialFrameDto>())
            {
                if (frame.frame <= expected) continue;
                if (frame.frame != expected + 1 || frame.tick < tick || frame.tick > state.tick)
                    return Reset(state, now, rate);
                expected = frame.frame;
                tick = frame.tick;
            }
            if (expected != state.lastFrame || state.tick < latestTick
                || state.tick - playedTick > MaximumLagTicks)
                return Reset(state, now, rate);

            foreach (var frame in state.frames ?? Array.Empty<TutorialFrameDto>())
            {
                if (frame.frame <= ReceivedFrame) continue;
                pending.Enqueue(frame);
                ReceivedFrame = frame.frame;
            }
            latestTick = state.tick;
            if (pending.Count > MaximumFrames) return Reset(state, now, rate);
            if (!clockStarted && latestTick > playedTick)
            {
                clockStarted = true;
                playTick = playedTick + 1 - InitialDelaySeconds * TickRate;
                lastClock = now;
                hasClockSample = true;
            }
            return false;
        }

        private bool Reset(TutorialStateDto state, double now, int rate)
        {
            pending.Clear();
            initialized = true;
            NeedsSync = false;
            clockStarted = false;
            hasClockSample = true;
            lastClock = now;
            playTick = playedTick = latestTick = state.tick;
            revision = state.revision;
            TickRate = rate;
            ReceivedFrame = PlayedFrame = state.lastFrame;
            settledAt = now + FrameDuration;
            return true;
        }

        public void Invalidate()
        {
            pending.Clear();
            initialized = false;
            clockStarted = false;
        }

        /// <summary>렌더 프레임마다 한 번 호출한다. 지연 뒤 과거 tick을 한꺼번에 재생하지 않는다.</summary>
        public void BeginUpdate(double now)
        {
            groupPlayed = false;
            if (!hasClockSample) { lastClock = now; hasClockSample = true; }
            double elapsed = Math.Max(0, now - lastClock);
            lastClock = now;
            if (elapsed > 0.5)
            {
                NeedsSync = true;
                pending.Clear();
                clockStarted = false;
                return;
            }
            if (clockStarted) playTick += Math.Min(elapsed, FrameDuration) * TickRate;
        }

        public bool TryTake(double now, out TutorialFrameDto frame)
        {
            frame = null;
            if (NeedsSync || pending.Count == 0) return false;
            var next = pending.Peek();
            // 이미 표시한 tick에 나중에 도착한 CAST도 frame이 새로우면 한 번 표시한다.
            bool lateCapture = next.tick <= playedTick;
            if (!lateCapture && next.tick > playTick + 0.000001) return false;
            if (groupPlayed && next.tick != groupTick) return false;
            pending.Dequeue();
            frame = next;
            groupTick = playedTick = next.tick;
            groupPlayed = true;
            PlayedFrame = next.frame;
            settledAt = now + FrameDuration;
            if (pending.Count == 0)
            {
                clockStarted = false;
                playTick = playedTick;
            }
            return true;
        }

        public bool IsSettled(double now) => !NeedsSync && pending.Count == 0
            && PlayedFrame >= ReceivedFrame && now + 0.000001 >= settledAt;
    }
}
