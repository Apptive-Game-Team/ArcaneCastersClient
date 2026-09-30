using UnityEngine;

namespace Global.Stomp
{
    /// <summary>
    /// 플랫폼과 설정에 맞는 <see cref="IStompTransport"/> 구현을 만든다.
    /// 연결 오케스트레이터는 구현 클래스를 직접 알지 않고 이 팩토리만 안다.
    /// </summary>
    public interface IStompTransportFactory
    {
        /// <summary>
        /// 전송 구현을 <paramref name="host"/>에 컴포넌트로 붙여 돌려준다.
        /// WebGL 구현은 JS가 SendMessage로 이 GameObject를 호출하므로 반드시 같은 오브젝트여야 한다.
        /// </summary>
        IStompTransport Create(GameObject host);
    }
}
