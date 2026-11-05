using System.Linq;
using LocalServer;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class LocalTickRatePolicyTests
    {
        private static LocalTickRatePolicy NewPolicy(double refreshRate = 144) =>
            new LocalTickRatePolicy(new LocalTickRateState(), refreshRate);

        /// <summary>지금 tick rate 로 치른 경기 하나를 넣는다. ratio 는 하위 10% FPS 를 목표로 나눈 값.</summary>
        private static bool PlayMatch(LocalTickRatePolicy policy, double ratio) =>
            policy.ApplyMatchStats(policy.TickRate, policy.TickRate * ratio);

        [TestCase(0, 60)]
        [TestCase(-1, 60)]
        [TestCase(double.NaN, 60)]
        [TestCase(59.94, 60)]
        [TestCase(74.6, 75)]
        [TestCase(143.9, 144)]
        [TestCase(360, 240)]
        [TestCase(10, 20)]
        public void CapComesFromTheRoundedRefreshRate(double refreshRate, int cap)
        {
            Assert.That(LocalTickRatePolicy.CapFromRefreshRate(refreshRate), Is.EqualTo(cap));
        }

        [Test]
        public void LadderEndsAtAnIntermediateCap()
        {
            Assert.That(LocalTickRatePolicy.Ladder(75).ToArray(), Is.EqualTo(new[] { 20, 30, 60, 75 }));
            Assert.That(LocalTickRatePolicy.Ladder(60).ToArray(), Is.EqualTo(new[] { 20, 30, 60 }));
            Assert.That(LocalTickRatePolicy.Ladder(240).ToArray(), Is.EqualTo(new[] { 20, 30, 60, 120, 144, 240 }));
        }

        [Test]
        public void StartsAtTwenty()
        {
            Assert.That(NewPolicy().TickRate, Is.EqualTo(20));
        }

        [Test]
        public void RaisesOneStepPerGoodMatchWhileRamping()
        {
            LocalTickRatePolicy policy = NewPolicy(144);

            Assert.That(PlayMatch(policy, 0.99), Is.True);
            Assert.That(policy.TickRate, Is.EqualTo(30));
            PlayMatch(policy, 0.98);
            Assert.That(policy.TickRate, Is.EqualTo(60));
            PlayMatch(policy, 1.0);
            Assert.That(policy.TickRate, Is.EqualTo(120));
            PlayMatch(policy, 1.0);
            Assert.That(policy.TickRate, Is.EqualTo(144));

            Assert.That(PlayMatch(policy, 1.0), Is.False, "the monitor caps it at 144");
            Assert.That(policy.TickRate, Is.EqualTo(144));
        }

        [Test]
        public void RampStopsAtAnIntermediateCap()
        {
            LocalTickRatePolicy policy = NewPolicy(75);

            for (int i = 0; i < 6; i++) PlayMatch(policy, 1.0);

            Assert.That(policy.TickRate, Is.EqualTo(75));
        }

        [Test]
        public void StaysBetweenNinetyAndNinetyEightPercent()
        {
            LocalTickRatePolicy policy = NewPolicy();
            PlayMatch(policy, 1.0);

            Assert.That(PlayMatch(policy, 0.95), Is.False);
            Assert.That(policy.TickRate, Is.EqualTo(30));
        }

        [Test]
        public void LowersOneStepBelowNinetyPercent()
        {
            LocalTickRatePolicy policy = NewPolicy();
            PlayMatch(policy, 1.0);
            PlayMatch(policy, 1.0);
            Assert.That(policy.TickRate, Is.EqualTo(60));

            Assert.That(PlayMatch(policy, 0.89), Is.True);
            Assert.That(policy.TickRate, Is.EqualTo(30));
            Assert.That(policy.State.Lowered, Is.True);
        }

        [Test]
        public void NeverGoesBelowTwenty()
        {
            LocalTickRatePolicy policy = NewPolicy();

            Assert.That(PlayMatch(policy, 0.5), Is.False);
            Assert.That(policy.TickRate, Is.EqualTo(20));
        }

        [Test]
        public void AfterLoweringNeedsThreeGoodMatchesInARow()
        {
            LocalTickRatePolicy policy = NewPolicy();
            PlayMatch(policy, 1.0); // 30
            PlayMatch(policy, 1.0); // 60
            PlayMatch(policy, 0.5); // 30, lowered

            PlayMatch(policy, 1.0);
            PlayMatch(policy, 1.0);
            Assert.That(policy.TickRate, Is.EqualTo(30));

            PlayMatch(policy, 0.95); // breaks the streak
            PlayMatch(policy, 1.0);
            PlayMatch(policy, 1.0);
            Assert.That(policy.TickRate, Is.EqualTo(30));

            Assert.That(PlayMatch(policy, 1.0), Is.True);
            Assert.That(policy.TickRate, Is.EqualTo(60));

            // 한 번 낮춘 뒤로는 계속 3경기씩 필요하다.
            PlayMatch(policy, 1.0);
            Assert.That(policy.TickRate, Is.EqualTo(60));
        }

        [Test]
        public void IgnoresStatsFromAMatchAtAnotherTickRate()
        {
            LocalTickRatePolicy policy = NewPolicy();

            Assert.That(policy.ApplyMatchStats(60, 10), Is.False);
            Assert.That(policy.TickRate, Is.EqualTo(20));
        }

        [Test]
        public void ManualChoiceDisablesAutomaticChanges()
        {
            LocalTickRatePolicy policy = NewPolicy();
            policy.SetManual(120);

            Assert.That(policy.TickRate, Is.EqualTo(120));
            Assert.That(policy.ApplyMatchStats(120, 10), Is.False);
            Assert.That(policy.TickRate, Is.EqualTo(120));

            policy.SetManual(0);
            Assert.That(policy.TickRate, Is.EqualTo(20));
        }

        [Test]
        public void ALowerRefreshRateSnapsTheStoredValueDown()
        {
            var state = new LocalTickRateState { Current = 144 };
            var policy = new LocalTickRatePolicy(state, 144);
            Assert.That(policy.TickRate, Is.EqualTo(144));

            policy.UpdateRefreshRate(75);
            Assert.That(policy.TickRate, Is.EqualTo(75));

            policy.UpdateRefreshRate(60);
            Assert.That(policy.TickRate, Is.EqualTo(60));
        }
    }
}
