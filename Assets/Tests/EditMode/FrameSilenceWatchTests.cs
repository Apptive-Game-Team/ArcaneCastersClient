using GameScene;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class FrameSilenceWatchTests
    {
        private const float Frame = 0.05f; // FrameInfo 20Hz 기준 한 프레임

        [Test]
        public void Defaults_AllowThirtySecondsForRecovery()
        {
            var watch = new FrameSilenceWatch();
            watch.NoteSignal(0f);
            Assert.That(watch.Tick(3f, Frame), Is.EqualTo(SilenceEscalation.None));
            Assert.That(watch.Tick(5f, Frame), Is.EqualTo(SilenceEscalation.Reconnect));
            Assert.That(watch.Tick(29f, Frame), Is.EqualTo(SilenceEscalation.None));
            Assert.That(watch.Tick(30f, Frame), Is.EqualTo(SilenceEscalation.Abandon));
        }

        private static FrameSilenceWatch NewWatch() =>
            new FrameSilenceWatch(reconnectAfterSeconds: 1f, abandonAfterSeconds: 3f, loopStallSeconds: 0.5f);

        /// <summary>프레임이 계속 도착하는 상태를 흉내낸다.</summary>
        private static float RunWithFrames(FrameSilenceWatch watch, float from, float seconds)
        {
            float now = from;
            float end = from + seconds;
            while (now < end)
            {
                now += Frame;
                watch.NoteSignal(now);
                Assert.That(watch.Tick(now, Frame), Is.EqualTo(SilenceEscalation.None));
            }
            return now;
        }

        /// <summary>프레임이 끊긴 상태를 흉내내고 그동안 나온 단계를 순서대로 모은다.</summary>
        private static SilenceEscalation RunSilent(FrameSilenceWatch watch, ref float now, float seconds)
        {
            SilenceEscalation last = SilenceEscalation.None;
            float end = now + seconds;
            while (now < end)
            {
                now += Frame;
                SilenceEscalation escalation = watch.Tick(now, Frame);
                if (escalation != SilenceEscalation.None) last = escalation;
            }
            return last;
        }

        [Test]
        public void FramesArrivingKeepsTheWatchQuiet()
        {
            FrameSilenceWatch watch = NewWatch();
            watch.NoteSignal(0f);

            RunWithFrames(watch, 0f, 10f);

            Assert.That(watch.Reached, Is.EqualTo(SilenceEscalation.None));
        }

        [Test]
        public void OneSecondOfSilenceAsksForReconnectOnly()
        {
            FrameSilenceWatch watch = NewWatch();
            watch.NoteSignal(0f);

            float now = 0f;
            SilenceEscalation escalation = RunSilent(watch, ref now, 1.5f);

            Assert.That(escalation, Is.EqualTo(SilenceEscalation.Reconnect));
            Assert.That(watch.Reached, Is.EqualTo(SilenceEscalation.Reconnect));
        }

        [Test]
        public void ThreeSecondsOfSilenceEscalatesToAbandon()
        {
            FrameSilenceWatch watch = NewWatch();
            watch.NoteSignal(0f);

            float now = 0f;
            SilenceEscalation escalation = RunSilent(watch, ref now, 3.5f);

            Assert.That(escalation, Is.EqualTo(SilenceEscalation.Abandon));
        }

        [Test]
        public void EachStageIsReportedOncePerSilenceSpell()
        {
            FrameSilenceWatch watch = NewWatch();
            watch.NoteSignal(0f);

            int reconnects = 0;
            int abandons = 0;
            float now = 0f;
            while (now < 5f)
            {
                now += Frame;
                switch (watch.Tick(now, Frame))
                {
                    case SilenceEscalation.Reconnect: reconnects++; break;
                    case SilenceEscalation.Abandon: abandons++; break;
                }
            }

            Assert.That(reconnects, Is.EqualTo(1));
            Assert.That(abandons, Is.EqualTo(1));
        }

        [Test]
        public void AFrameAfterReconnectRearmsTheWatch()
        {
            FrameSilenceWatch watch = NewWatch();
            watch.NoteSignal(0f);

            float now = 0f;
            Assert.That(RunSilent(watch, ref now, 1.5f), Is.EqualTo(SilenceEscalation.Reconnect));

            // 제자리 재연결이 성공해 프레임이 다시 흐른다.
            now = RunWithFrames(watch, now, 1f);
            Assert.That(watch.Reached, Is.EqualTo(SilenceEscalation.None));

            // 다시 끊기면 처음부터 사다리를 탄다.
            Assert.That(RunSilent(watch, ref now, 1.5f), Is.EqualTo(SilenceEscalation.Reconnect));
        }

        [Test]
        public void ATickGapFromTabThrottlingResetsInsteadOfEjecting()
        {
            FrameSilenceWatch watch = NewWatch();
            watch.NoteSignal(0f);

            // 탭을 30초 백그라운드로 보냈다 돌아왔다. 한 틱에 30초가 흐른 것으로 보인다.
            Assert.That(watch.Tick(30f, 30f), Is.EqualTo(SilenceEscalation.None));
            Assert.That(watch.Reached, Is.EqualTo(SilenceEscalation.None));
            Assert.That(watch.SilenceSeconds(30f), Is.EqualTo(0f));

            // 복귀 직후 아직 프레임이 안 왔더라도 예산은 새로 준다.
            float now = 30f;
            Assert.That(RunSilent(watch, ref now, 0.9f), Is.EqualTo(SilenceEscalation.None));
        }

        [Test]
        public void SilenceIsStillDetectedAfterReturningFromThrottling()
        {
            FrameSilenceWatch watch = NewWatch();
            watch.NoteSignal(0f);

            watch.Tick(30f, 30f);

            // 돌아온 뒤에도 프레임이 오지 않으면 정상적으로 사다리를 탄다.
            float now = 30f;
            Assert.That(RunSilent(watch, ref now, 3.5f), Is.EqualTo(SilenceEscalation.Abandon));
        }

        [Test]
        public void ExplicitResumeSignalClearsRaisedStage()
        {
            FrameSilenceWatch watch = NewWatch();
            watch.NoteSignal(0f);

            float now = 0f;
            RunSilent(watch, ref now, 1.5f);
            Assert.That(watch.Reached, Is.EqualTo(SilenceEscalation.Reconnect));

            // Application.focusChanged(true) 경로.
            watch.NoteSignal(now);

            Assert.That(watch.Reached, Is.EqualTo(SilenceEscalation.None));
            Assert.That(watch.SilenceSeconds(now), Is.EqualTo(0f));
        }

        [Test]
        public void ReconnectLadderFitsInsideTheAbandonBudget()
        {
            // 인게임 재연결 프로파일(즉시 → 0.5초 → 1초)이 무음 1초에 시작해
            // 무음 3초 전에 소진되는지 확인한다. 소진되지 않으면 사다리가 무의미해진다.
            const float reconnectStartsAt = FrameSilenceWatch.DefaultReconnectAfterSeconds;
            float[] delays = { 0f, 0.5f, 1f };

            float exhaustedAt = reconnectStartsAt;
            foreach (float delay in delays) exhaustedAt += delay;

            Assert.That(exhaustedAt, Is.LessThan(FrameSilenceWatch.DefaultAbandonAfterSeconds));
        }
    }
}
