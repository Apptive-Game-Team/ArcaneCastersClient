using System;
using System.Collections.Generic;

namespace LobbyScene
{
    /// <summary>
    /// <c>POST /api/match/tickets</c> 의 요청 본문.
    /// lobby 서버는 본문이 없으면 <see cref="MatchDeckMode.Selected"/> 로 친다.
    /// </summary>
    [Serializable]
    public sealed class MatchTicketRequest
    {
        public string deckMode;

        /// <summary>
        /// 이 클라이언트에서 각 게임 서버까지 잰 왕복 시간. lobby 는 두 유저의 최대 핑이 낮은 서버에 매치를 배치한다.
        /// 못 쟀으면 비워 둔다. 매칭은 그대로 되고 그 서버들의 우선순위만 뒤로 밀린다.
        /// </summary>
        public List<ServerPing> serverPings;
    }

    /// <summary><see cref="MatchTicketRequest.serverPings"/> 의 항목. 필드명은 lobby 계약이다.</summary>
    [Serializable]
    public sealed class ServerPing
    {
        public long serverId;
        public long rttMs;
    }

    /// <summary><c>GET /api/match/servers</c> 응답 항목. 핑을 잴 게임 서버 주소다.</summary>
    [Serializable]
    public sealed class GameServerEndpoint
    {
        public long serverId;
        public string url;
    }

    /// <summary>
    /// <see cref="MatchTicketRequest.deckMode"/> 에 실리는 두 값.
    /// lobby 서버와 맞춘 계약이라 철자를 바꾸면 매칭이 거부된다.
    /// </summary>
    public static class MatchDeckMode
    {
        public const string Selected = "SELECTED";
        public const string Random = "RANDOM";
    }
}
