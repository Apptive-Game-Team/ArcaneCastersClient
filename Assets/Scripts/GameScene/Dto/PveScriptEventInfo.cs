using System.Collections.Generic;

namespace GameScene.Dto
{
    [System.Serializable]
    public class PveScriptEventInfo : ServerMessage
    {
        public string key;
        public int speakerObjectId;
        public List<string> lines;

        /// <summary>Per-match increasing number (>= 1) of this event; 0 or missing from an older server.</summary>
        public int seq;
    }
}
