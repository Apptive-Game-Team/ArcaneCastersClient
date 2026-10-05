using UnityEngine;

namespace RewardChest.Renderers
{
    /// <summary>
    /// <c>CHEST</c>: a chest the player has to open on the chest screen. The icon is the closed chest from
    /// <see cref="RewardSpriteResolver.ChestIconRoot"/>; the placeholder shows only if even the default art is missing.
    /// </summary>
    public sealed class ChestRewardTileRenderer : IRewardTileRenderer
    {
        public bool Handles(string rewardType)
        {
            return rewardType == RewardTypes.Chest;
        }

        public RewardTileContent Build(RewardView reward)
        {
            RewardSpriteResolver.TryResolveChestIcon(reward.Key, out Sprite sprite);
            return new RewardTileContent(sprite, RewardNameKeys.Table, RewardNameKeys.Chest, "Chest");
        }
    }
}
