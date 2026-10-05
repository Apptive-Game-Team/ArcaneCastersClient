namespace RewardChest.Renderers
{
    /// <summary>
    /// Any reward type no other renderer handles: placeholder icon and the raw type name, so a type the
    /// server added before this client shipped is still visible instead of dropped.
    /// </summary>
    public sealed class FallbackRewardTileRenderer : IRewardTileRenderer
    {
        public bool Handles(string rewardType)
        {
            return true;
        }

        public RewardTileContent Build(RewardView reward)
        {
            if (reward.Type == RewardTypes.Unknown)
            {
                return new RewardTileContent(null, RewardNameKeys.Table, RewardNameKeys.Unknown, "Reward");
            }

            return new RewardTileContent(null, null, null, reward.Type);
        }
    }
}
