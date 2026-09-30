using Admin.Dto;
using Data.Net;
using Global.Udp;

namespace Data
{
    [System.Serializable]
    public class MatchedInfoDto
    {
        public string message;
        public string server;
        public string webSocketUrl;
        public User leftUser;
        public User rightUser;
        public string sessionId;

        /// <summary>
        /// 게임 서버가 UDP로 받는 주소. 서버가 UDP를 켜고 알려줄 때만 채워진다.
        /// 비어 있으면 WebSocket으로 접속한다.
        /// </summary>
        public string udpHost;
        public int udpPort;

        /// <summary>접속 주소를 어느 필드에서 가져왔는지. 진단 로그용이다.</summary>
        public string ConnectionSource => string.IsNullOrEmpty(webSocketUrl) ? server : webSocketUrl;

        /// <summary>
        /// 게임 서버 웹소켓 접속 엔드포인트를 만든다.
        ///
        /// 서버가 <c>webSocketUrl</c>을 내려주면 경로까지 서버가 정한 값이므로 그대로 쓰고,
        /// <c>server</c>만 오는 응답이면 base URL이므로 <paramref name="fallbackPath"/>를 붙인다.
        /// 토큰 같은 쿼리는 호출부가 <see cref="ServerEndpoint.Query"/>로 덧붙인다.
        /// </summary>
        public bool TryResolveWebSocket(string fallbackPath, out ServerEndpoint endpoint)
        {
            if (!string.IsNullOrEmpty(webSocketUrl))
            {
                if (!ServerEndpoint.TryOf(webSocketUrl, out endpoint)) return false;

                endpoint = endpoint.AsWebSocket();
                return true;
            }

            if (!ServerEndpoint.TryOf(server, out endpoint)) return false;

            endpoint = endpoint.AsWebSocket().Path(fallbackPath);
            return true;
        }

        /// <summary>
        /// UDP 접속 주소를 만든다. 서버가 UDP 주소를 내려주지 않았거나 세션·유저를 알 수 없으면
        /// false이고, 호출부는 WebSocket을 쓴다.
        /// </summary>
        public bool TryResolveUdp(long userId, out string url)
        {
            url = null;
            if (string.IsNullOrEmpty(udpHost) || udpPort <= 0) return false;
            if (string.IsNullOrEmpty(sessionId) || userId <= 0) return false;

            url = UdpEndpoint.Build(udpHost, udpPort, sessionId, userId);
            return true;
        }

        public static MatchedInfoDto CreateDebugSession(string sessionId, string userSide, long userId)
        {
            long userIdLeft = userSide == "left" ? userId : -1;
            long userIdRight = userSide == "right" ? userId : -1;

            return new MatchedInfoDto
            {
                message = "debug session",
                server = "http://localhost:7777",
                leftUser = new User(userIdLeft, "debugger_left", "debugger_left@team6515.com", -1),
                rightUser = new User(userIdRight, "debugger_right", "debugger_right@team6515.com", -1),
                sessionId = sessionId
            };
        }

        public static MatchedInfoDto CreateSpectatingSession(RoomInfo roomInfo)
        {
            return new MatchedInfoDto
            {
                message = "spectating session",
                server = roomInfo.serverUrl,
                leftUser = new User(roomInfo.leftUserId, "debugger_left", "debugger_left@team6515.com", -1),
                rightUser = new User(roomInfo.rightUserId, "debugger_right", "debugger_right@team6515.com", -1),
                sessionId = roomInfo.sessionId
            };
        }
    }
}
