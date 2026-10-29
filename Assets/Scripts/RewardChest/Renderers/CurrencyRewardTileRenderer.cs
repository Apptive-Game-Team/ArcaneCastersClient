namespace RewardChest.Renderers
{
    public sealed class CurrencyRewardTileRenderer : IRewardTileRenderer
    {
        public bool Handles(string rewardType) => rewardType == "CURRENCY";

        public RewardTileContent Build(RewardView reward) =>
            new RewardTileContent(null, "LobbyUI", "ProgressionGold", "Gold");
    }
}
