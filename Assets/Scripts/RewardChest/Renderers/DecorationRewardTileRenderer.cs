namespace RewardChest.Renderers
{
    /// <summary>
    /// <c>DECORATION</c>. The client has no decoration art or catalog yet, so the tile keeps its
    /// placeholder icon and shows the type's localized name.
    /// </summary>
    public sealed class DecorationRewardTileRenderer : IRewardTileRenderer
    {
        public bool Handles(string rewardType)
        {
            return rewardType == RewardTypes.Decoration;
        }

        public RewardTileContent Build(RewardView reward)
        {
            return new RewardTileContent(null, RewardNameKeys.Table, RewardNameKeys.Decoration, "Decoration");
        }
    }
}
