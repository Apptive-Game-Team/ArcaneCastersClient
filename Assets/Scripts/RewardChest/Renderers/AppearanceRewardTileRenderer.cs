using UnityEngine;

namespace RewardChest.Renderers
{
    /// <summary>
    /// <c>APPEARANCE</c>: the idle pose under <c>Resources/PlayerAppearances/&lt;rewardKey&gt;</c>, the same sprite
    /// the lobby avatar shows once the player wears it.
    /// </summary>
    public sealed class AppearanceRewardTileRenderer : IRewardTileRenderer
    {
        public bool Handles(string rewardType)
        {
            return rewardType == RewardTypes.Appearance;
        }

        public RewardTileContent Build(RewardView reward)
        {
            RewardSpriteResolver.TryResolveAppearanceIcon(reward.Key, out Sprite sprite);
            return new RewardTileContent(sprite, RewardNameKeys.Table, RewardNameKeys.Appearance, "New Look");
        }
    }
}
