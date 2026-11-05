using System;
using System.Collections.Generic;
using System.Linq;
using RewardChest;

namespace Data
{
    [Serializable]
    public class ProgressionPendingRewardsDto
    {
        public bool enabled;
        public int baselineLevel;
        public int[] claimedLevels;
        public ProgressionPendingRewardDto[] rewards;
    }

    [Serializable]
    public class ProgressionPendingRewardDto
    {
        public long id;
        public string source;
        public int? level;
        public string createdAt;
        public ProgressionClaimRewardDto[] rewards;
    }

    [Serializable]
    public class ProgressionClaimReceiptDto
    {
        public long id;
        public bool alreadyClaimed;
        public ProgressionClaimRewardDto[] rewards;
    }

    [Serializable]
    public class ProgressionClaimRewardDto
    {
        public string rewardType;
        public long rewardId;
        public string rewardKey;
        public int amount;
        public int rewardSlots;
    }

    public static class ProgressionClaimPresentation
    {
        public static bool IsAmbiguousFailure(long status, bool success) => !success
            && (status == 0 || status >= 500 || (status >= 200 && status < 300));

        public static bool ShouldRetry(int attempt, long status, bool success, bool sameContext) =>
            attempt == 0 && sameContext && IsAmbiguousFailure(status, success);

        public static bool IsClaimed(int level, int currentLevel, ProgressionPendingRewardsDto pending)
        {
            if (level > currentLevel || pending == null) return false;
            return !pending.enabled || level <= Math.Max(1, pending.baselineLevel)
                || (pending.claimedLevels?.Contains(level) ?? false);
        }

        public static List<RewardView> PositiveRewards(ProgressionClaimRewardDto[] rewards) =>
            (rewards ?? Array.Empty<ProgressionClaimRewardDto>()).Where(value => value != null && value.amount > 0)
                .Select(value => new RewardView(value.rewardType, value.rewardKey, value.rewardId, value.amount)).ToList();
    }
}
