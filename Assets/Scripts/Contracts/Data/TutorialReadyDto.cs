using System;

namespace Data
{
    [Serializable, UnityEngine.Scripting.Preserve]
    public sealed class TutorialReadyDto
    {
        [UnityEngine.Scripting.Preserve] public TutorialReadyDto() { }
        public string battleId;
        public long ownerId;
        public string faction;
        public string server;
        public string expiresAt;
    }
}
