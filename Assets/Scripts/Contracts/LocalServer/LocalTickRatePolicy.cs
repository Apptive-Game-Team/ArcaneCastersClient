using System;
using System.Collections.Generic;

namespace LocalServer
{
    /// <summary>
    /// <see cref="LocalTickRatePolicy"/> 가 다음 실행까지 들고 가는 값. 저장은 호출부가 PlayerPrefs 로 한다.
    /// </summary>
    public sealed class LocalTickRateState
    {
        /// <summary>자동 모드에서 다음 경기에 쓸 tick rate.</summary>
        public int Current = LocalTickRatePolicy.StartTickRate;

        /// <summary>한 번이라도 낮춘 적이 있는지. 이후로는 3경기 연속 여유가 있어야 올린다.</summary>
        public bool Lowered;

        /// <summary>낮춘 뒤 목표의 98% 이상을 연속으로 낸 경기 수.</summary>
        public int GoodStreak;

        /// <summary>사용자가 직접 고른 tick rate. 0 이면 자동이다.</summary>
        public int Manual;
    }

    /// <summary>
    /// 로컬 서버 경기의 tick rate 를 정하는 규칙. 상위 계획
    /// (<c>.plan/general/2026-10-10-local-game-server.md</c> "tick rate 정하기")를 그대로 옮겼다.
    ///
    /// <list type="bullet">
    /// <item>상한은 모니터 주사율을 반올림한 값이고 240 이하다. 주사율을 모르면 60 이다.</item>
    /// <item>단계는 20 → 30 → 60 → 120 → 144 → 240 이고, 상한을 넘는 단계 대신 상한 자체가 마지막 단계다.</item>
    /// <item>20 에서 시작한다. 낮춘 적이 없으면 하위 10% FPS 가 목표의 98% 이상인 경기마다 한 단계 올린다.</item>
    /// <item>90% 미만이면 한 단계 낮춘다(20 아래로는 안 간다). 그 뒤로는 3경기 연속 98% 이상이어야 올린다.</item>
    /// <item>사용자가 값을 고르면 자동으로 올리고 내리지 않는다.</item>
    /// </list>
    ///
    /// UnityEngine 에 의존하지 않는다. 주사율은 호출부가 <c>Screen.currentResolution</c> 에서 읽어 넘긴다.
    /// </summary>
    public sealed class LocalTickRatePolicy
    {
        public const int StartTickRate = 20;
        public const int MaximumCap = 240;
        public const int FallbackCap = 60;

        /// <summary>올리는 기준: 하위 10% FPS 가 목표의 이 비율 이상.</summary>
        public const double RaiseRatio = 0.98;

        /// <summary>낮추는 기준: 하위 10% FPS 가 목표의 이 비율 미만.</summary>
        public const double LowerRatio = 0.90;

        /// <summary>한 번 낮춘 뒤 다시 올리려면 연속으로 필요한 여유 있는 경기 수.</summary>
        public const int GoodStreakToRaiseAfterLowering = 3;

        private static readonly int[] Steps = { 20, 30, 60, 120, 144, 240 };

        /// <summary>설정 화면에서 직접 고를 수 있는 값. 0 은 자동이다.</summary>
        public static readonly IReadOnlyList<int> ManualChoices = new[] { 0, 20, 30, 60, 120, 144 };

        private readonly LocalTickRateState state;
        private int cap;

        public LocalTickRatePolicy(LocalTickRateState state, double refreshRate)
        {
            this.state = state ?? new LocalTickRateState();
            UpdateRefreshRate(refreshRate);
        }

        public LocalTickRateState State => state;

        /// <summary>지금 주사율에서의 상한.</summary>
        public int Cap => cap;

        public bool IsManual => state.Manual > 0;

        /// <summary>다음 경기에 쓸 tick rate.</summary>
        public int TickRate => IsManual ? state.Manual : state.Current;

        /// <summary>
        /// 주사율이 바뀌면 상한만 다시 계산한다. 저장된 값이 새 상한의 단계 밖이면 그 아래 단계로 내린다.
        /// </summary>
        public void UpdateRefreshRate(double refreshRate)
        {
            cap = CapFromRefreshRate(refreshRate);
            state.Current = SnapToLadder(state.Current, cap);
        }

        /// <summary>사용자가 tick rate 를 고른다. 0 이면 자동으로 돌아간다.</summary>
        public void SetManual(int tickRate)
        {
            state.Manual = tickRate > 0 ? tickRate : 0;
        }

        /// <summary>
        /// 경기가 끝나고 로컬 서버가 보낸 <c>matchStats</c> 를 반영한다. 다음 경기의 tick rate 가 바뀌면 true.
        ///
        /// 직접 고른 값이거나, 지금 자동 값과 다른 tick rate 로 치른 경기의 측정값은 쓰지 않는다.
        /// 다른 값으로 치른 경기의 FPS 로는 지금 단계가 버거운지 알 수 없다.
        /// </summary>
        public bool ApplyMatchStats(int targetTickRate, double p10Fps)
        {
            if (IsManual) return false;
            if (targetTickRate <= 0 || targetTickRate != state.Current) return false;
            if (double.IsNaN(p10Fps) || p10Fps < 0) return false;

            double ratio = p10Fps / targetTickRate;
            IReadOnlyList<int> ladder = Ladder(cap);
            int index = IndexOf(ladder, state.Current);

            if (ratio < LowerRatio)
            {
                state.Lowered = true;
                state.GoodStreak = 0;
                if (index <= 0) return false;

                state.Current = ladder[index - 1];
                return true;
            }

            if (ratio < RaiseRatio)
            {
                state.GoodStreak = 0;
                return false;
            }

            if (index >= ladder.Count - 1)
            {
                state.GoodStreak = 0;
                return false;
            }

            if (state.Lowered)
            {
                state.GoodStreak++;
                if (state.GoodStreak < GoodStreakToRaiseAfterLowering) return false;
            }

            state.GoodStreak = 0;
            state.Current = ladder[index + 1];
            return true;
        }

        /// <summary>주사율을 상한으로 바꾼다. 반올림하고 240 으로 자르며, 읽지 못한 값(0 이하, NaN)은 60 이다.</summary>
        public static int CapFromRefreshRate(double refreshRate)
        {
            if (double.IsNaN(refreshRate) || double.IsInfinity(refreshRate) || refreshRate <= 0) return FallbackCap;

            int rounded = (int)Math.Round(refreshRate, MidpointRounding.AwayFromZero);
            if (rounded > MaximumCap) return MaximumCap;
            return rounded < StartTickRate ? StartTickRate : rounded;
        }

        /// <summary>상한까지의 단계. 상한보다 작은 단계 뒤에 상한 자체가 온다(75 이면 20, 30, 60, 75).</summary>
        public static IReadOnlyList<int> Ladder(int cap)
        {
            var ladder = new List<int>(Steps.Length);
            foreach (int step in Steps)
            {
                if (step < cap) ladder.Add(step);
            }

            ladder.Add(cap);
            return ladder;
        }

        private static int SnapToLadder(int value, int cap)
        {
            IReadOnlyList<int> ladder = Ladder(cap);
            int snapped = ladder[0];
            foreach (int step in ladder)
            {
                if (step <= value) snapped = step;
            }

            return snapped;
        }

        private static int IndexOf(IReadOnlyList<int> ladder, int value)
        {
            for (int i = 0; i < ladder.Count; i++)
            {
                if (ladder[i] == value) return i;
            }

            return 0;
        }
    }
}
