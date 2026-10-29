using System;

namespace Data {
    [Serializable]
    public class ProgressionRewardsDto {
        public string faction;
        public ProgressionLevelRewardDto[] levels;
    }

    [Serializable]
    public class ProgressionLevelRewardDto {
        public int level;
        public long requiredXp;
        public int chestCount;
        public int rewardSlots;
        public ProgressionFixedRewardDto[] cards;
    }

    [Serializable]
    public class ProgressionFixedRewardDto {
        public long magicId;
        public string magicName;
        public int amount;
    }
}
