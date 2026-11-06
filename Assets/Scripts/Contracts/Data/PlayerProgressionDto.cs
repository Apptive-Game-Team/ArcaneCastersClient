using System;

namespace Data
{
    [Serializable]
    public class PlayerProgressionDto
    {
        public string faction;
        public int level;
        public long xp;
        public long levelXp;
        public int nextLevelXp;
        public long currency;
        public long gainedXp;
        public int grantedChests;
        public ProgressionMatchXpDto matchXp;
    }

    [Serializable]
    public class ProgressionMatchXpDto
    {
        public int baseXp;
        public int firstWinBonusXp;
    }
}
