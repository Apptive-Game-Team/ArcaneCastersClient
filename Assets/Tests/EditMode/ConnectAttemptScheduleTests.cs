using GameScene;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class ConnectAttemptScheduleTests
    {
        private const float Frame = 0.05f;

        private static ConnectAttemptSchedule NewSchedule() =>
            new ConnectAttemptSchedule(maxAttempts: 3, attemptSeconds: 2f, loopStallSeconds: 0.5f);

        [Test]
        public void DefaultSchedule_WaitsForTenSecondsBeforeFirstRetry()
        {
            var schedule = new ConnectAttemptSchedule();
            Assert.That(ConnectAttemptSchedule.DefaultAttemptSeconds, Is.EqualTo(10f));
            for (int quarterSeconds = 1; quarterSeconds < 40; quarterSeconds++)
                Assert.That(schedule.Advance(0.25f), Is.EqualTo(ConnectAttemptDecision.Wait),
                    $"{quarterSeconds * 0.25f}초에는 진행 중인 연결을 기다려야 합니다.");
            Assert.That(schedule.Attempt, Is.EqualTo(1));
            Assert.That(schedule.Advance(0.25f), Is.EqualTo(ConnectAttemptDecision.Retry));
            Assert.That(schedule.Attempt, Is.EqualTo(2));
        }

        [Test]
        public void WaitsWhileTheAttemptBudgetRemains()
        {
            ConnectAttemptSchedule schedule = NewSchedule();

            for (int i = 0; i < 20; i++)
                Assert.That(schedule.Advance(Frame), Is.EqualTo(ConnectAttemptDecision.Wait));

            Assert.That(schedule.Attempt, Is.EqualTo(1));
        }

        [Test]
        public void RetriesTwiceThenGivesUp()
        {
            ConnectAttemptSchedule schedule = NewSchedule();

            int retries = 0;
            int giveUps = 0;
            float elapsed = 0f;

            while (elapsed < 10f)
            {
                elapsed += Frame;
                switch (schedule.Advance(Frame))
                {
                    case ConnectAttemptDecision.Retry: retries++; break;
                    case ConnectAttemptDecision.GiveUp: giveUps++; break;
                }

                if (giveUps > 0) break;
            }

            Assert.That(retries, Is.EqualTo(2), "3회 시도 = 최초 연결 + 재시도 2회");
            Assert.That(giveUps, Is.EqualTo(1));
            // 예산 2초 × 3회. 10초를 통째로 태우던 예전보다 짧다.
            Assert.That(elapsed, Is.LessThan(6.5f));
        }

        [Test]
        public void AttemptCounterTracksTheLadder()
        {
            ConnectAttemptSchedule schedule = NewSchedule();
            Assert.That(schedule.Attempt, Is.EqualTo(1));

            AdvanceUntilNotWait(schedule);
            Assert.That(schedule.Attempt, Is.EqualTo(2));

            AdvanceUntilNotWait(schedule);
            Assert.That(schedule.Attempt, Is.EqualTo(3));
        }

        [Test]
        public void ThrottledTicksDoNotBurnTheBudget()
        {
            ConnectAttemptSchedule schedule = NewSchedule();

            // 탭을 60초 백그라운드로 보냈다 돌아왔다.
            Assert.That(schedule.Advance(60f), Is.EqualTo(ConnectAttemptDecision.Wait));
            Assert.That(schedule.Attempt, Is.EqualTo(1));

            // 복귀 뒤에도 첫 시도의 2초 예산이 그대로 남아 있다.
            for (int i = 0; i < 20; i++)
                Assert.That(schedule.Advance(Frame), Is.EqualTo(ConnectAttemptDecision.Wait));
        }

        [Test]
        public void ResetGivesTheFullBudgetAgain()
        {
            ConnectAttemptSchedule schedule = NewSchedule();
            AdvanceUntilNotWait(schedule);
            AdvanceUntilNotWait(schedule);
            Assert.That(schedule.Attempt, Is.EqualTo(3));

            schedule.Reset();

            Assert.That(schedule.Attempt, Is.EqualTo(1));
            Assert.That(AdvanceUntilNotWait(schedule), Is.EqualTo(ConnectAttemptDecision.Retry));
        }

        private static ConnectAttemptDecision AdvanceUntilNotWait(ConnectAttemptSchedule schedule)
        {
            for (int i = 0; i < 1000; i++)
            {
                ConnectAttemptDecision decision = schedule.Advance(Frame);
                if (decision != ConnectAttemptDecision.Wait) return decision;
            }

            Assert.Fail("일정이 Wait에서 벗어나지 않았습니다.");
            return ConnectAttemptDecision.Wait;
        }
    }
}
