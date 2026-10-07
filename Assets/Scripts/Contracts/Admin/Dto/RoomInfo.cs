using System;

namespace Admin.Dto
{
    [Serializable]
    public class RoomInfo
    {
        public string sessionId;
        public long leftUserId;
        public long rightUserId;
        public string serverUrl;
        // Map kind of the session, as the game server spells it; null from an older server.
        public string mapType;
        // Typed rather than a raw string: JsonCodec leaves DateParseHandling at its default, so an
        // ISO-8601 instant read into a string member comes back reformatted and without its UTC kind.
        public DateTime createdAt;
        public string sourceName;
        public string sourceBaseUrl;
        public bool isLocalSource;
    }
}
