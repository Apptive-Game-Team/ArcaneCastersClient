using System.Collections.Generic;

namespace GameScene.Dto
{
    [System.Serializable]
    public class PveScriptEventInfo : ServerMessage
    {
        public string key;
        public int speakerObjectId;
        public List<string> lines;
    }
}
