using UnityEngine;

namespace Global.Stomp
{
    /// <summary>
    /// 기본 선택 규칙: WebGL 빌드는 WebSocket(jslib), 에디터와 네이티브는 네이티브 구현.
    /// 새 전송(UDP 등)은 이 클래스에 분기를 늘리지 말고 별도 팩토리로 추가한다.
    /// </summary>
    public class DefaultStompTransportFactory : IStompTransportFactory
    {
        public IStompTransport Create(GameObject host)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return host.AddComponent<WebGLStompTransport>();
#else
            return host.AddComponent<NativeStompTransport>();
#endif
        }
    }
}
