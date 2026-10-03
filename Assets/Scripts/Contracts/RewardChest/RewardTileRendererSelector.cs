using System;
using System.Collections.Generic;

namespace RewardChest
{
    /// <summary>
    /// Picks the renderer for a reward: the first registered renderer that handles its type, otherwise
    /// the fallback. A reward type the client does not know therefore always gets a tile.
    /// </summary>
    public sealed class RewardTileRendererSelector
    {
        private readonly List<IRewardTileRenderer> renderers;
        private readonly IRewardTileRenderer fallback;

        public RewardTileRendererSelector(IEnumerable<IRewardTileRenderer> renderers, IRewardTileRenderer fallback)
        {
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
            this.renderers = new List<IRewardTileRenderer>();
            if (renderers == null)
            {
                return;
            }

            foreach (IRewardTileRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    this.renderers.Add(renderer);
                }
            }
        }

        public IRewardTileRenderer Select(string rewardType)
        {
            string normalizedType = RewardTypes.Normalize(rewardType);
            foreach (IRewardTileRenderer renderer in renderers)
            {
                if (renderer.Handles(normalizedType))
                {
                    return renderer;
                }
            }

            return fallback;
        }

        public RewardTileContent Build(RewardView reward)
        {
            if (reward == null)
            {
                return null;
            }

            return Select(reward.Type).Build(reward);
        }
    }
}
