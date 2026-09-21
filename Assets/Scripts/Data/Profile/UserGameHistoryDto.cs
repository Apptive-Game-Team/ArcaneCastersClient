using System;

namespace Data.Profile
{
    [Serializable]
    public class UserGameHistoryDto
    {
        public long opponentId;
        public string opponentName;
        public string result;

        // gameType is one of "PVP", "Practice", "PVE" (issue #98). Kept as a plain string
        // instead of an enum for the same reason as result: StringEnumConverter throws on an
        // unrecognized value and JsonCodec.TryDeserialize discards the whole response, so a
        // new server value must degrade instead of breaking the whole game history list.
        public string gameType;

        public long OpponentId => opponentId;

        // result is one of "win", "lose", "draw" (contract fixed in ArcaneCastersLobby#27).
        // Kept as a plain string instead of an enum: StringEnumConverter throws on an
        // unrecognized value and JsonCodec.TryDeserialize discards the whole response.
        public bool IsWin => string.Equals(result, "win", StringComparison.OrdinalIgnoreCase);
        public bool IsDraw => string.Equals(result, "draw", StringComparison.OrdinalIgnoreCase);
        public bool IsLose => string.Equals(result, "lose", StringComparison.OrdinalIgnoreCase);
    }
}
