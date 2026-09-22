namespace GameScene.Dto
{
    /// <summary>
    /// 서버가 세션 전원에게 보내는 emote 알림. <c>{"type":"emote","side":"LeftPlayer","emote":"Laugh"}</c>.
    /// </summary>
    /// <remarks>
    /// 두 필드 모두 string 이다. enum 으로 두면 서버가 나중에 늘린 emote 이름 하나에
    /// StringEnumConverter 가 throw 하고 메시지 전체가 버려진다.
    /// </remarks>
    [System.Serializable]
    public class EmoteInfo : ServerMessage
    {
        public string side;
        public string emote;
    }
}
