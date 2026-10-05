namespace RewardChest.Renderers
{
    /// <summary>
    /// The one list of reward tile renderers. To draw a new reward type, write an
    /// <see cref="IRewardTileRenderer"/> and add it here; nothing else changes.
    /// </summary>
    public static class RewardTileRenderers
    {
        public static RewardTileRendererSelector CreateDefaultSelector()
        {
            return new RewardTileRendererSelector(
                new IRewardTileRenderer[]
                {
                    new MagicRewardTileRenderer(),
                    new DecorationRewardTileRenderer(),
                    new AppearanceRewardTileRenderer(),
                    new ChestRewardTileRenderer(),
                },
                new FallbackRewardTileRenderer());
        }
    }
}
