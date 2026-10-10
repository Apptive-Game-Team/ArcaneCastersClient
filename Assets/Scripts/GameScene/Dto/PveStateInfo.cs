using System.Collections.Generic;

namespace GameScene.Dto
{
    /// <summary>
    /// One PVE state channel value: <c>{"channel":"bgm","value":"boss","seq":3}</c>. Pushed live when
    /// the server changes a channel; <c>value</c> null means that channel's default. <c>seq</c> is one
    /// per-match number shared by all channels. The same message may instead carry <see cref="states"/>,
    /// every channel set so far, as the answer to a pveSync request.
    /// </summary>
    [System.Serializable]
    public class PveStateInfo : ServerMessage
    {
        public string channel;
        public string value;
        public int seq;
        public List<PveStateDto> states;
    }

    [System.Serializable]
    public class PveStateDto
    {
        public string channel;
        public string value;
        public int seq;
    }
}
