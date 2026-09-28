using Data;
using Global;
using Global.Serialization;

namespace GameScene.Emote
{
    /// <summary>
    /// 고른 emote 를 카드 입력과 같은 destination 으로 보낸다.
    /// </summary>
    public static class EmoteInputSender
    {
        public static bool Send(EmoteType emote)
        {
            string wireName = EmoteTypes.WireName(emote);
            if (wireName == null)
            {
                WDebug.LogWarning($"[Emote] 보낼 수 없는 emote. value: {emote}");
                return false;
            }

            if (SceneContext.MatchInfo == null)
            {
                WDebug.LogWarning("[Emote] MatchInfo 가 없어 emote 를 보내지 않는다.");
                return false;
            }

            if (StompConnector.Instance == null)
            {
                WDebug.LogWarning("[Emote] StompConnector 가 없어 emote 를 보내지 않는다.");
                return false;
            }

            string json = JsonCodec.Serialize(new EmoteInput(wireName));
            string destination = $"/app/game/input/{SceneContext.MatchInfo.sessionId}/{SceneContext.UserID}";
            StompConnector.Instance.SendMessageToServer(destination, json);
            return true;
        }
    }
}
