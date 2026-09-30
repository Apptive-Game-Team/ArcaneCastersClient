namespace Global.Udp
{
    /// <summary>
    /// 신뢰 패킷의 반복 도착을 버린다. 가장 높은 seq와 그 아래 64개의 창을 기억하고,
    /// 창보다 오래된 seq는 이미 본 것으로 친다. 보낸 쪽이 오래전에 포기한 패킷이기 때문이다.
    /// </summary>
    public sealed class UdpReliableWindow
    {
        private const int Window = 64;

        private ulong _highest;
        private ulong _seen;

        /// <summary>이 seq가 처음 오면 true, 반복이면 false.</summary>
        public bool Accept(uint seq)
        {
            if (seq > _highest)
            {
                ulong shift = seq - _highest;
                _seen = shift >= Window ? 0UL : _seen << (int)shift;
                _seen |= 1UL;
                _highest = seq;
                return true;
            }

            ulong diff = _highest - seq;
            if (diff >= Window) return false;

            ulong bit = 1UL << (int)diff;
            if ((_seen & bit) != 0) return false;

            _seen |= bit;
            return true;
        }
    }
}
