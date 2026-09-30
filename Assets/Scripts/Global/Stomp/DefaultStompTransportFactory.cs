using Data;
using UnityEngine;

namespace Global.Stomp
{
    /// <summary>
    /// 기본 선택 규칙.
    /// WebGL 빌드는 브라우저가 소켓을 열 수 없으므로 WebSocket(jslib)만 쓴다.
    /// 에디터와 네이티브는 서버가 UDP 주소를 알려주면 UDP로 붙고, 알려주지 않았거나
    /// UDP가 실패하면 WebSocket으로 넘어간다.
    /// 새 전송을 붙일 때는 이 클래스에 분기를 늘리지 말고 별도 팩토리로 추가한다.
    /// </summary>
    public class DefaultStompTransportFactory : IStompTransportFactory
    {
        public IStompTransport Create(GameObject host)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return host.AddComponent<WebGLStompTransport>();
#else
            IStompTransport webSocket = host.AddComponent<NativeStompTransport>();
            IStompTransport udp = host.AddComponent<UdpStompTransport>();
            return new FallbackStompTransport(udp, webSocket, ResolveUdpUrl);
#endif
        }

#if !(UNITY_WEBGL && !UNITY_EDITOR)
        private static string ResolveUdpUrl()
        {
            MatchedInfoDto matchInfo = SceneContext.MatchInfo;
            string url;
            return matchInfo != null && matchInfo.TryResolveUdp(SceneContext.UserID, out url) ? url : null;
        }
#endif
    }
}
