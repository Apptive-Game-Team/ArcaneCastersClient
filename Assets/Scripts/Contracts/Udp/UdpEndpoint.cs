using System;

namespace Global.Udp
{
    /// <summary>
    /// UDP 전송에 넘기는 접속 주소: <c>udp://host:port?session=...&amp;user=...</c>.
    /// 전송 계층은 URL 문자열 하나만 받으므로 세션과 유저도 여기에 실어 보낸다.
    /// </summary>
    public static class UdpEndpoint
    {
        public const string Scheme = "udp";

        public static string Build(string host, int port, string sessionId, long userId)
        {
            return Scheme + "://" + host + ":" + port
                   + "?session=" + Uri.EscapeDataString(sessionId)
                   + "&user=" + userId;
        }

        public static bool TryParse(string url, out string host, out int port, out string sessionId, out long userId)
        {
            host = null;
            port = 0;
            sessionId = null;
            userId = 0;

            Uri uri;
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out uri)) return false;
            if (uri.Scheme != Scheme || string.IsNullOrEmpty(uri.Host) || uri.Port <= 0) return false;

            string query = uri.Query;
            if (string.IsNullOrEmpty(query)) return false;

            string[] pairs = query.TrimStart('?').Split('&');
            for (int i = 0; i < pairs.Length; i++)
            {
                int equals = pairs[i].IndexOf('=');
                if (equals <= 0) continue;

                string key = pairs[i].Substring(0, equals);
                string value = Uri.UnescapeDataString(pairs[i].Substring(equals + 1));
                if (key == "session") sessionId = value;
                else if (key == "user") long.TryParse(value, out userId);
            }

            if (string.IsNullOrEmpty(sessionId) || userId <= 0) return false;

            host = uri.Host;
            port = uri.Port;
            return true;
        }
    }
}
