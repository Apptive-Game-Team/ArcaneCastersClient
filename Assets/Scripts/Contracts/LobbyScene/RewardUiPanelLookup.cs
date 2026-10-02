using UnityEngine;

namespace LobbyScene
{
    public static class RewardUiPanelLookup
    {
        public static Transform FindRewardPanel(Transform rewardTransform)
        {
            var panel = rewardTransform.Find("Panel");
            if (panel != null)
            {
                return panel;
            }

            panel = rewardTransform.Find("Panal");
            if (panel != null)
            {
                return panel;
            }

            return rewardTransform;
        }
    }
}
