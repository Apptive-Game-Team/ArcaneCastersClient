using Global;
using UnityEngine.Networking;

namespace Data.Profile
{
    public static class ManualRewardClaims
    {
        public const string Header = "X-Reward-Claims";
        public const string Capability = "manual-v1";
        public static void Apply(UnityWebRequest request) => request.SetRequestHeader(Header, Capability);
    }

    public readonly struct RewardRequestContext
    {
        public readonly long Owner;
        public readonly string Server;
        private RewardRequestContext(long owner, string server) { Owner = owner; Server = server; }
        public static RewardRequestContext Capture() => new RewardRequestContext(SceneContext.User == null ? 0 : SceneContext.UserID, ServerList.MatchingServer.url);
        public bool IsCurrent => Owner > 0 && SceneContext.User != null && SceneContext.UserID == Owner
            && ServerList.MatchingServer.url == Server;
    }
}
