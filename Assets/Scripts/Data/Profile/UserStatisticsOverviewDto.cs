using System;

namespace Data.Profile
{
    [Serializable]
    public class UserStatisticsOverviewDto
    {
        public int totalGameNum;
        public int totalWinNum;
        public int totalLoseNum;
        public int totalDrawNum;

        public int TotalGameNum => totalGameNum;
        public int TotalWinNum => totalWinNum;
        public int TotalLoseNum => totalLoseNum;
        public int TotalDrawNum => totalDrawNum;
        public float WinRate => totalGameNum <= 0 ? 0f : (float)totalWinNum / totalGameNum;
    }
}
