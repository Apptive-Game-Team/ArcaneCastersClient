using System;

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
