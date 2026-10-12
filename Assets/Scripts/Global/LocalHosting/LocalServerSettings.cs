using System;
using LocalServer;
using UnityEngine;

namespace Global.LocalHosting
{
    /// <summary>
    /// 설정 화면의 "내 PC 에서 경기 실행" 과 tick rate 를 PlayerPrefs 에 둔다.
    /// </summary>
    public static class LocalServerSettings
    {
        private const string EnabledKey = "LocalServer.Enabled";
        private const string TickCurrentKey = "LocalServer.TickRate.Current";
        private const string TickLoweredKey = "LocalServer.TickRate.Lowered";
        private const string TickGoodStreakKey = "LocalServer.TickRate.GoodStreak";
        private const string TickManualKey = "LocalServer.TickRate.Manual";

        private static LocalTickRatePolicy tickRatePolicy;

        /// <summary>설정이 바뀌었다. 설정 화면과 <see cref="LocalServerService"/> 가 듣는다.</summary>
        public static event Action Changed;

        /// <summary>
        /// 기본은 켜짐이다. 서버 묶음은 설정 화면에서 직접 설치해야 하므로, 설치하지 않은 사용자에게는 이 값이
        /// 켜져 있어도 아무 일도 일어나지 않는다.
        /// </summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set
            {
                if (value == Enabled) return;
                PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>읽을 때마다 지금 모니터 주사율로 상한을 다시 계산한다.</summary>
        public static LocalTickRatePolicy TickRatePolicy
        {
            get
            {
                if (tickRatePolicy == null)
                {
                    var state = new LocalTickRateState
                    {
                        Current = PlayerPrefs.GetInt(TickCurrentKey, LocalTickRatePolicy.StartTickRate),
                        Lowered = PlayerPrefs.GetInt(TickLoweredKey, 0) == 1,
                        GoodStreak = PlayerPrefs.GetInt(TickGoodStreakKey, 0),
                        Manual = PlayerPrefs.GetInt(TickManualKey, 0)
                    };
                    tickRatePolicy = new LocalTickRatePolicy(state, ReadRefreshRate());
                }
                else
                {
                    tickRatePolicy.UpdateRefreshRate(ReadRefreshRate());
                }

                return tickRatePolicy;
            }
        }

        /// <summary>설정 화면에서 tick rate 를 고른다. 0 이면 자동.</summary>
        public static void SetManualTickRate(int tickRate)
        {
            TickRatePolicy.SetManual(tickRate);
            SaveTickRate();
            Changed?.Invoke();
        }

        /// <summary>로컬 서버가 보낸 <c>matchStats</c> 로 다음 경기의 tick rate 를 정한다.</summary>
        public static void ApplyMatchStats(int targetTickRate, double p10Fps)
        {
            LocalTickRatePolicy policy = TickRatePolicy;
            int before = policy.TickRate;
            policy.ApplyMatchStats(targetTickRate, p10Fps);
            SaveTickRate();

            if (before != policy.TickRate)
            {
                WDebug.Log($"[LocalServer] tick rate {before} → {policy.TickRate} (p10Fps={p10Fps:F1}, target={targetTickRate})");
                Changed?.Invoke();
            }
        }

        private static void SaveTickRate()
        {
            LocalTickRateState state = TickRatePolicy.State;
            PlayerPrefs.SetInt(TickCurrentKey, state.Current);
            PlayerPrefs.SetInt(TickLoweredKey, state.Lowered ? 1 : 0);
            PlayerPrefs.SetInt(TickGoodStreakKey, state.GoodStreak);
            PlayerPrefs.SetInt(TickManualKey, state.Manual);
            PlayerPrefs.Save();
        }

        /// <summary>모니터 주사율. 읽지 못하면 0 을 돌려주고, 정책이 상한을 60 으로 둔다.</summary>
        private static double ReadRefreshRate()
        {
            try
            {
                return Screen.currentResolution.refreshRateRatio.value;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
