namespace GameScene
{
    /// <summary>프레임 무음이 어디까지 악화됐는지. 값이 클수록 심각하다.</summary>
    public enum SilenceEscalation
    {
        /// <summary>아직 조치할 단계가 아니다.</summary>
        None = 0,

        /// <summary>제자리 재연결을 시작할 단계. 게임씬은 유지한다.</summary>
        Reconnect = 1,

        /// <summary>세션을 포기하고 신고할 단계.</summary>
        Abandon = 2
    }

    /// <summary>
    /// FrameInfo 무음을 단계로 나눠 판정한다.
    ///
    /// FrameInfo는 초당 20회 도착하므로 5초 무음이면 이미 20프레임이 비어 있다. 그 시점에는
    /// 아직 게임을 살릴 수 있으므로 씬을 유지한 채 재연결만 시도하고, 30초까지 회복하지 못하면
    /// 그때 세션을 포기한다.
    ///
    /// 각 단계는 한 무음 구간에서 한 번만 보고한다. 매 프레임 <see cref="Tick"/>을 부르는
    /// 호출부가 같은 지시를 반복해서 받지 않게 하기 위해서다.
    ///
    /// 브라우저는 백그라운드 탭의 루프를 스로틀링한다. 돌아왔을 때의 시간 점프는 연결이
    /// 끊겼다는 증거가 아니므로, 한 틱 간격이 <see cref="DefaultLoopStallSeconds"/>를 넘으면
    /// 무음으로 세지 않고 기준 시각을 다시 잡는다.
    /// </summary>
    public sealed class FrameSilenceWatch
    {
        /// <summary>무음 100프레임. 여기서 제자리 재연결을 시작한다.</summary>
        public const float DefaultReconnectAfterSeconds = 5f;

        /// <summary>재연결 사다리가 다 돌아갈 만큼 기다린 뒤 포기하는 지점.</summary>
        public const float DefaultAbandonAfterSeconds = 30f;

        /// <summary>
        /// 한 틱 간격이 이 값을 넘으면 게임 루프 자체가 멈춰 있었다고 본다.
        /// 정상 프레임 간격(수십 ms)과 스로틀링 복귀(수 초) 사이에서 넉넉히 잡았다.
        /// GC나 에셋 로드로 인한 히칭도 여기에 걸리지만, 그때 무음 판정을 미루는 쪽이 안전하다.
        /// </summary>
        public const float DefaultLoopStallSeconds = 0.5f;

        private readonly float _reconnectAfterSeconds;
        private readonly float _abandonAfterSeconds;
        private readonly float _loopStallSeconds;

        private float _lastSignalTime;
        private SilenceEscalation _reached;

        public FrameSilenceWatch(
            float reconnectAfterSeconds = DefaultReconnectAfterSeconds,
            float abandonAfterSeconds = DefaultAbandonAfterSeconds,
            float loopStallSeconds = DefaultLoopStallSeconds)
        {
            _reconnectAfterSeconds = reconnectAfterSeconds;
            _abandonAfterSeconds = abandonAfterSeconds;
            _loopStallSeconds = loopStallSeconds;
        }

        /// <summary>지금까지 이 무음 구간에서 보고한 가장 높은 단계.</summary>
        public SilenceEscalation Reached => _reached;

        /// <summary>기준 시각의 경과 무음 시간.</summary>
        public float SilenceSeconds(float now) => now - _lastSignalTime;

        /// <summary>
        /// 연결이 살아있다는 증거를 받았다. FrameInfo 수신, 감시 시작, 스로틀링 복귀가 모두 여기에 해당한다.
        /// </summary>
        public void NoteSignal(float now)
        {
            _lastSignalTime = now;
            _reached = SilenceEscalation.None;
        }

        /// <summary>
        /// 한 프레임 진행하고 조치할 단계를 돌려준다. 조치가 없으면 <see cref="SilenceEscalation.None"/>.
        /// </summary>
        /// <param name="now">현재 실시간(초). <c>Time.unscaledTime</c>.</param>
        /// <param name="sinceLastTick">직전 틱과의 실시간 간격(초). <c>Time.unscaledDeltaTime</c>.</param>
        public SilenceEscalation Tick(float now, float sinceLastTick)
        {
            if (sinceLastTick >= _loopStallSeconds)
            {
                // 루프가 멈춰 있던 동안 프레임이 없던 것은 서버가 아니라 브라우저 사정이다.
                NoteSignal(now);
                return SilenceEscalation.None;
            }

            float silence = now - _lastSignalTime;

            if (silence >= _abandonAfterSeconds) return Raise(SilenceEscalation.Abandon);
            if (silence >= _reconnectAfterSeconds) return Raise(SilenceEscalation.Reconnect);
            return SilenceEscalation.None;
        }

        private SilenceEscalation Raise(SilenceEscalation level)
        {
            if (level <= _reached) return SilenceEscalation.None;
            _reached = level;
            return level;
        }
    }
}
