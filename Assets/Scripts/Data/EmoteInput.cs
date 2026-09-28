namespace Data
{
    /// <summary>
    /// emote 입력. 카드 입력과 같은 destination 으로 보낸다.
    /// <c>{"type":"emote","emote":"Laugh"}</c> 말고 다른 필드는 서버가 읽지 않는다.
    /// </summary>
    [System.Serializable]
    public class EmoteInput
    {
        public string type = "emote";
        public string emote;

        public EmoteInput(string emote)
        {
            this.emote = emote;
        }
    }
}
