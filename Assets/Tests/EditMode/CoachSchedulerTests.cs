using System.Collections.Generic;
using Coach;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class CoachSchedulerTests
    {
        private const float Frame = 0.1f;
        private const float Cooldown = 12f;
        private const float MaxVisible = 8f;
        private const float Grace = 5f;
        private const float SatisfyWindow = 5f;

        private static readonly float[] Backoff = { 30f, 60f, 120f };

        private float now;

        [SetUp]
        public void SetUp()
        {
            now = 100f; // 시각 0을 가정하고 있지 않은지 드러내려고 임의 시각에서 시작한다.
        }

        private CoachScheduler NewScheduler()
        {
            return new CoachScheduler(now, Cooldown, MaxVisible, Grace, SatisfyWindow, Backoff);
        }

        /// <summary>지정한 시간만큼 틱을 돌리고 그 사이에 나온 동작을 모은다.</summary>
        private CoachAction[] Advance(CoachScheduler scheduler, float seconds)
        {
            var actions = new List<CoachAction>();
            float target = now + seconds;

            while (now < target)
            {
                now += Frame;
                CoachAction action = scheduler.Tick(now, Frame);
                if (action.Kind != CoachActionKind.None)
                {
                    actions.Add(action);
                }
            }

            return actions.ToArray();
        }

        /// <summary>
        /// 원하는 동작이 나오는 순간까지만 돌린다. 그 시점을 기준으로 다음 단계를 이어가야
        /// 백오프와 유예 시간을 프레임 단위 오차 없이 검증할 수 있다.
        /// </summary>
        private float AdvanceUntil(CoachScheduler scheduler, CoachActionKind kind, CoachRuleId expected, float maxSeconds)
        {
            float deadline = now + maxSeconds;

            while (now < deadline)
            {
                now += Frame;
                CoachAction action = scheduler.Tick(now, Frame);
                if (action.Kind == kind)
                {
                    Assert.AreEqual(expected, action.RuleId);
                    return now;
                }

                Assert.AreEqual(CoachActionKind.None, action.Kind, $"{kind} 전에 다른 동작이 나왔다.");
            }

            Assert.Fail($"{maxSeconds}초 안에 {kind}가 나오지 않았다.");
            return now;
        }

        private static void AssertSingleShow(CoachAction[] actions, CoachRuleId expected)
        {
            Assert.AreEqual(1, actions.Length, "동작이 정확히 하나여야 한다.");
            Assert.AreEqual(CoachActionKind.Show, actions[0].Kind);
            Assert.AreEqual(expected, actions[0].RuleId);
        }

        private static CoachScheduler WithRule(
            CoachScheduler scheduler,
            CoachRuleId ruleId,
            float dwellSeconds,
            int priority,
            int maxShowsPerSession = 3,
            bool retired = false)
        {
            scheduler.Register(ruleId, dwellSeconds, priority, maxShowsPerSession, retired);
            scheduler.SetActive(ruleId, true);
            return scheduler;
        }

        [Test]
        public void AHintWaitsUntilTheProblemHasPersistedForItsDwell()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.ManaBarUnopened, dwellSeconds: 20f, priority: 5);

            Assert.IsEmpty(Advance(scheduler, 19f), "dwell을 채우기 전에는 뜨지 않는다.");
            AssertSingleShow(Advance(scheduler, 2f), CoachRuleId.ManaBarUnopened);
        }

        [Test]
        public void NoHintFiresDuringTheStartupGrace()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.MagicFailing, dwellSeconds: 0f, priority: 3);

            Assert.IsEmpty(Advance(scheduler, Grace - 1f));
            AssertSingleShow(Advance(scheduler, 2f), CoachRuleId.MagicFailing);
        }

        [Test]
        public void ABreakInTheProblemRestartsTheDwellFromZero()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.ManaBarUnopened, dwellSeconds: 8f, priority: 2);

            Advance(scheduler, 7f);

            scheduler.SetActive(CoachRuleId.ManaBarUnopened, false);
            Advance(scheduler, 1f);
            scheduler.SetActive(CoachRuleId.ManaBarUnopened, true);

            Assert.IsEmpty(Advance(scheduler, 7f));
            AssertSingleShow(Advance(scheduler, 2f), CoachRuleId.ManaBarUnopened);
        }

        [Test]
        public void AProblemClearingWhileVisibleHidesTheHintAndCountsAsFollowed()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);
            AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 20f);
            Assert.IsTrue(scheduler.HasVisibleHint);

            scheduler.SetActive(CoachRuleId.FieldSelectIdle, false);
            AdvanceUntil(scheduler, CoachActionKind.Hide, CoachRuleId.FieldSelectIdle, 1f);

            Assert.IsTrue(scheduler.TryDequeueSatisfied(out CoachRuleId satisfied));
            Assert.AreEqual(CoachRuleId.FieldSelectIdle, satisfied);
        }

        [Test]
        public void AnIgnoredHintHidesItselfAndDoesNotCountAsFollowed()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);

            float shownAt = AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 20f);
            float hiddenAt = AdvanceUntil(scheduler, CoachActionKind.Hide, CoachRuleId.FieldSelectIdle, MaxVisible + 1f);

            Assert.GreaterOrEqual(hiddenAt - shownAt, MaxVisible);
            Assert.IsFalse(scheduler.TryDequeueSatisfied(out _));
        }

        [Test]
        public void IgnoringAHintPushesItsNextShowOutByTheBackoff()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);

            AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 20f);
            float hiddenAt = AdvanceUntil(scheduler, CoachActionKind.Hide, CoachRuleId.FieldSelectIdle, MaxVisible + 1f);

            Assert.IsEmpty(Advance(scheduler, Backoff[0] - 1f), "백오프가 끝나기 전에는 다시 뜨지 않는다.");
            float reshownAt = AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 5f);

            Assert.GreaterOrEqual(reshownAt - hiddenAt, Backoff[0]);
        }

        [Test]
        public void ActingJustAfterAHintHidesStillCountsAsFollowed()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);

            AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 20f);
            AdvanceUntil(scheduler, CoachActionKind.Hide, CoachRuleId.FieldSelectIdle, MaxVisible + 1f);
            Assert.IsFalse(scheduler.TryDequeueSatisfied(out _));

            scheduler.SetActive(CoachRuleId.FieldSelectIdle, false);
            Advance(scheduler, SatisfyWindow - 1f);

            Assert.IsTrue(scheduler.TryDequeueSatisfied(out CoachRuleId satisfied));
            Assert.AreEqual(CoachRuleId.FieldSelectIdle, satisfied);
        }

        [Test]
        public void ActingLongAfterAHintHidesDoesNotCountAsFollowed()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);

            AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 20f);
            AdvanceUntil(scheduler, CoachActionKind.Hide, CoachRuleId.FieldSelectIdle, MaxVisible + 1f);
            Advance(scheduler, SatisfyWindow + 1f);

            scheduler.SetActive(CoachRuleId.FieldSelectIdle, false);
            Advance(scheduler, 1f);

            Assert.IsFalse(scheduler.TryDequeueSatisfied(out _));
        }

        [Test]
        public void TheGlobalCooldownHoldsBackEvenADifferentRule()
        {
            CoachScheduler scheduler = NewScheduler();
            WithRule(scheduler, CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);
            WithRule(scheduler, CoachRuleId.ManaBarUnopened, dwellSeconds: 6f, priority: 5);

            AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 20f);
            scheduler.SetActive(CoachRuleId.FieldSelectIdle, false);
            float hiddenAt = AdvanceUntil(scheduler, CoachActionKind.Hide, CoachRuleId.FieldSelectIdle, 1f);

            Assert.IsEmpty(Advance(scheduler, Cooldown - 1f), "쿨다운 안에는 다음 힌트가 뜨지 않는다.");
            float shownAt = AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.ManaBarUnopened, 5f);

            Assert.GreaterOrEqual(shownAt - hiddenAt, Cooldown);
        }

        [Test]
        public void TheHigherPriorityRuleWinsWhenBothAreReady()
        {
            CoachScheduler scheduler = NewScheduler();
            WithRule(scheduler, CoachRuleId.ManaBarUnopened, dwellSeconds: 6f, priority: 5);
            WithRule(scheduler, CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);

            AssertSingleShow(Advance(scheduler, 12f), CoachRuleId.FieldSelectIdle);
        }

        [Test]
        public void OnlyOneHintIsVisibleAtATime()
        {
            CoachScheduler scheduler = NewScheduler();
            WithRule(scheduler, CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);
            WithRule(scheduler, CoachRuleId.ManaBarUnopened, dwellSeconds: 6f, priority: 2);

            Assert.AreEqual(1, Advance(scheduler, 12f).Length);
        }

        [Test]
        public void AHintNeverExceedsItsPerSessionShowLimit()
        {
            CoachScheduler scheduler = WithRule(
                NewScheduler(), CoachRuleId.MagicUnused, dwellSeconds: 1f, priority: 4, maxShowsPerSession: 2);

            int shows = 0;
            foreach (CoachAction action in Advance(scheduler, 600f))
            {
                if (action.Kind == CoachActionKind.Show)
                {
                    shows++;
                }
            }

            Assert.AreEqual(2, shows);
        }

        [Test]
        public void ARetiredRuleNeverFires()
        {
            CoachScheduler scheduler = WithRule(
                NewScheduler(), CoachRuleId.LobbyIdle, dwellSeconds: 1f, priority: 6, retired: true);

            Assert.IsEmpty(Advance(scheduler, 600f));
        }

        [Test]
        public void DismissingAHintHidesItAtOnceAndIsNotCountedAsFollowed()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);
            AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 20f);

            CoachAction action = scheduler.Dismiss(now);

            Assert.AreEqual(CoachActionKind.Hide, action.Kind);
            Assert.AreEqual(CoachRuleId.FieldSelectIdle, action.RuleId);
            Assert.IsFalse(scheduler.HasVisibleHint);
            Assert.IsTrue(scheduler.TryDequeueDismissed(out CoachRuleId dismissed));
            Assert.AreEqual(CoachRuleId.FieldSelectIdle, dismissed);
            Assert.IsFalse(scheduler.TryDequeueSatisfied(out _));
        }

        [Test]
        public void DismissingAHintJumpsStraightToTheLongestBackoff()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);
            AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 20f);

            scheduler.Dismiss(now);
            float dismissedAt = now;

            float longest = Backoff[Backoff.Length - 1];
            Assert.IsEmpty(Advance(scheduler, longest - 1f));
            float reshownAt = AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 5f);

            Assert.GreaterOrEqual(reshownAt - dismissedAt, longest);
        }

        [Test]
        public void ActingAfterADismissIsNotCreditedAsFollowingTheHint()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);
            AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.FieldSelectIdle, 20f);

            scheduler.Dismiss(now);
            scheduler.SetActive(CoachRuleId.FieldSelectIdle, false);
            Advance(scheduler, 1f);

            Assert.IsFalse(scheduler.TryDequeueSatisfied(out _));
        }

        [Test]
        public void DismissingWithNothingOnScreenDoesNothing()
        {
            CoachScheduler scheduler = WithRule(NewScheduler(), CoachRuleId.FieldSelectIdle, dwellSeconds: 6f, priority: 1);

            CoachAction action = scheduler.Dismiss(now);

            Assert.AreEqual(CoachActionKind.None, action.Kind);
            Assert.IsFalse(scheduler.TryDequeueDismissed(out _));
        }

        [Test]
        public void RetiringARuleMidSessionStopsItForGood()
        {
            CoachScheduler scheduler = WithRule(
                NewScheduler(), CoachRuleId.LobbyIdle, dwellSeconds: 1f, priority: 6, maxShowsPerSession: 5);

            AdvanceUntil(scheduler, CoachActionKind.Show, CoachRuleId.LobbyIdle, 20f);
            AdvanceUntil(scheduler, CoachActionKind.Hide, CoachRuleId.LobbyIdle, MaxVisible + 1f);

            scheduler.SetRetired(CoachRuleId.LobbyIdle, true);

            Assert.IsEmpty(Advance(scheduler, 600f));
        }
    }
}
