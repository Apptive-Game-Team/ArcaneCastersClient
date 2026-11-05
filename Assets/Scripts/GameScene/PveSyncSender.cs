using GameScene.Dto;
using Global;
using Global.Serialization;

namespace GameScene
{
    /// <summary>
    /// The server's PVE loop starts before this client subscribes to its frame topic, and topic
    /// messages are not replayed, so the objective and the opening script events can be lost.
    /// This asks the server for them once the subscription exists.
    /// </summary>
    public static class PveSyncSender
    {
        /// <summary>Only an adventure match is a PVE match here; the server ignores the request elsewhere.</summary>
        public static bool IsPveMatch => SceneContext.AdventureId.HasValue;

        public static bool Send(int lastEventSeq)
        {
            if (!IsPveMatch)
            {
                return false;
            }

            if (SceneContext.MatchInfo == null || StompConnector.Instance == null)
            {
                WDebug.LogWarning("[PveSync] MatchInfo 나 StompConnector 가 없어 pveSync 를 보내지 않는다.");
                return false;
            }

            string json = JsonCodec.Serialize(new PveSyncInput(lastEventSeq));
            string destination = $"/app/game/input/{SceneContext.MatchInfo.sessionId}/{SceneContext.UserID}";
            StompConnector.Instance.SendMessageToServer(destination, json);
            return true;
        }
    }
}
