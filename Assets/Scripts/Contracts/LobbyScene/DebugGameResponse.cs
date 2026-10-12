using System;

namespace LobbyScene.Debugger
{
    [Serializable]
    public class DebugGameResponse
    {
        public string sessionId;

        /// <summary>
        /// Map kind the game server picked for this debug match, as the server spells it
        /// (<c>GRASSLAND</c>, <c>RIVER</c>, <c>FORTRESS</c>, <c>GATE</c>, <c>FOREST</c>). Null from
        /// an older server. A string on purpose: see <see cref="Data.MapKinds.Parse"/>.
        /// </summary>
        public string mapType;
    }
}
