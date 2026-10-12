namespace GameScene.Dto
{
    /// <summary>
    /// One PVE state channel value: <c>{"type":"pveState","channel":"bgm","value":"boss","seq":3}</c>.
    /// Sent live when the server changes a channel, and once per channel it has set after a pveSync
    /// request. <c>value</c> null means that channel's default; <c>seq</c> is one per-match number
    /// shared by all channels.
    /// </summary>
    [System.Serializable]
    public class PveStateInfo : ServerMessage
    {
        public string channel;
        public string value;
        public int seq;
    }
}
