using System;

namespace Data.Profile
{
    [Serializable]
    public class UserGameHistoryDto
    {
        public long opponentId;
        public string opponentName;
        public string result;

        public long OpponentId => opponentId;

        // result is one of "win", "lose", "draw" (contract fixed in ArcaneCastersLobby#27).
        // Kept as a plain string instead of an enum: StringEnumConverter throws on an
        // unrecognized value and JsonCodec.TryDeserialize discards the whole response.
        public bool IsWin => string.Equals(result, "win", StringComparison.OrdinalIgnoreCase);
        public bool IsDraw => string.Equals(result, "draw", StringComparison.OrdinalIgnoreCase);
        public bool IsLose => string.Equals(result, "lose", StringComparison.OrdinalIgnoreCase);
    }
}
