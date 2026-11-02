using GameScene;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class StompConnectGateTests
    {
        private const float Handshake = 2f;

        private static StompConnectGate NewGate() => new StompConnectGate(handshakeSeconds: Handshake);

        [Test]
        public void FirstAttemptConnects()
        {
            StompConnectGate gate = NewGate();

            Assert.That(gate.Decide(transportConnected: false, now: 0f),
                Is.EqualTo(ConnectGateDecision.Connect));
        }

        [Test]
        public void OpenSocketIsNotReopened()
        {
            StompConnectGate gate = NewGate();

            Assert.That(gate.Decide(transportConnected: true, now: 0f),
                Is.EqualTo(ConnectGateDecision.AlreadyConnected));
        }

        /// <summary>인게임 사다리(0초 → 0.5초 → 1.5초)가 진행 중인 핸드셰이크를 죽이지 않는다.</summary>
        [TestCase(0.5f)]
        [TestCase(1.5f)]
        [TestCase(1.99f)]
        public void LadderTicksWaitWhileAHandshakeIsInFlight(float now)
        {
            StompConnectGate gate = NewGate();
            gate.NoteHandshakeStarted(0f);

            Assert.That(gate.Decide(transportConnected: false, now: now),
                Is.EqualTo(ConnectGateDecision.HandshakeInFlight));
        }

        /// <summary>핸드셰이크가 실패하면 곧바로 다음 시도를 보낼 수 있어야 한다.</summary>
        [Test]
        public void SettledHandshakeReleasesTheGateImmediately()
        {
            StompConnectGate gate = NewGate();
            gate.NoteHandshakeStarted(0f);
            gate.NoteSettled();

            Assert.That(gate.HandshakeInFlight, Is.False);
            Assert.That(gate.Decide(transportConnected: false, now: 0.5f),
                Is.EqualTo(ConnectGateDecision.Connect));
        }

        /// <summary>
        /// 콜백이 끝내 오지 않는 핸드셰이크가 재연결을 영구히 막으면 안 된다.
        /// 최초 연결 재시도(<see cref="ConnectAttemptSchedule"/>)가 도는 시점에는 풀려 있어야 한다.
        /// </summary>
        [TestCase(2f)]
        [TestCase(10f)]
        public void SilentHandshakeStopsBlockingAfterTheBudget(float now)
        {
            StompConnectGate gate = NewGate();
            gate.NoteHandshakeStarted(0f);

            Assert.That(gate.Decide(transportConnected: false, now: now),
                Is.EqualTo(ConnectGateDecision.Connect));
        }

        [Test]
        public void ConnectedTransportWinsOverAnInFlightHandshake()
        {
            StompConnectGate gate = NewGate();
            gate.NoteHandshakeStarted(0f);

            Assert.That(gate.Decide(transportConnected: true, now: 0.5f),
                Is.EqualTo(ConnectGateDecision.AlreadyConnected));
        }

        /// <summary>새 핸드셰이크를 시작하면 창이 다시 열린다.</summary>
        [Test]
        public void RestartingTheHandshakeRestartsTheWindow()
        {
            StompConnectGate gate = NewGate();
            gate.NoteHandshakeStarted(0f);
            gate.NoteHandshakeStarted(3f);

            Assert.That(gate.Decide(transportConnected: false, now: 4f),
                Is.EqualTo(ConnectGateDecision.HandshakeInFlight));
            Assert.That(gate.Decide(transportConnected: false, now: 5f),
                Is.EqualTo(ConnectGateDecision.Connect));
        }

        /// <summary>
        /// 기본 창은 최초 연결이 한 시도에 주는 예산과 같아야 한다. 더 길면 응답 없는
        /// 핸드셰이크가 재시도를 막고, 더 짧으면 재시도가 자기 핸드셰이크를 죽인다.
        /// </summary>
        [Test]
        public void DefaultWindowMatchesTheConnectAttemptBudget()
        {
            Assert.That(StompConnectGate.DefaultHandshakeSeconds,
                Is.EqualTo(ConnectAttemptSchedule.DefaultAttemptSeconds));
        }

        [Test]
        public void DefaultGate_DoesNotReplaceFiveSecondHandshakeAndExpiresAtTenSeconds()
        {
            var gate = new StompConnectGate();
            gate.NoteHandshakeStarted(0f);
            Assert.That(gate.Decide(transportConnected: false, now: 5.1f),
                Is.EqualTo(ConnectGateDecision.HandshakeInFlight));
            Assert.That(gate.Decide(transportConnected: false, now: 9.99f),
                Is.EqualTo(ConnectGateDecision.HandshakeInFlight));
            Assert.That(gate.Decide(transportConnected: false, now: 10f),
                Is.EqualTo(ConnectGateDecision.Connect));
        }
    }
}
