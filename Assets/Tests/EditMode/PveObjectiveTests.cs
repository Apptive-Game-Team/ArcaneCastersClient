using GameScene.Dto;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// The <c>pveObjective</c> message lands on <see cref="PveObjectiveInfo"/>, and the HUD line is built
    /// only from what the message says.
    /// </summary>
    public class PveObjectiveTests
    {
        [Test]
        public void MessageResolvesToPveObjectiveInfo()
        {
            const string json = @"{""type"":""pveObjective"",""winCondition"":""Survive"",
                ""surviveSeconds"":90,""remainingSeconds"":65,""objectivesTotal"":0,""objectivesRemaining"":0}";

            ServerMessage message = JsonCodec.Deserialize<ServerMessage>(json);

            Assert.IsInstanceOf<PveObjectiveInfo>(message);
            PveObjectiveInfo info = (PveObjectiveInfo)message;
            Assert.AreEqual("Survive", info.winCondition);
            Assert.AreEqual(90, info.surviveSeconds);
            Assert.AreEqual(65, info.remainingSeconds);
        }

        [Test]
        public void DestroyObjectivesShowsDestroyedOverTotal()
        {
            PveObjectiveInfo info = new PveObjectiveInfo
            {
                winCondition = "DestroyObjectives", objectivesTotal = 5, objectivesRemaining = 3
            };

            Assert.AreEqual(PveObjectiveText.DestroyKey, PveObjectiveText.KeyFor(info));
            Assert.AreEqual("Destroy the targets 2/5", PveObjectiveText.Render(info, PveObjectiveText.DestroyFallback));
            Assert.AreEqual("목표를 파괴하세요 2/5", PveObjectiveText.Render(info, "목표를 파괴하세요 {0}/{1}"));
        }

        [Test]
        public void SurviveShowsRemainingTimeAsMinutesAndSeconds()
        {
            PveObjectiveInfo info = new PveObjectiveInfo
            {
                winCondition = "Survive", surviveSeconds = 90, remainingSeconds = 65
            };

            Assert.AreEqual(PveObjectiveText.SurviveKey, PveObjectiveText.KeyFor(info));
            Assert.AreEqual("Survive 1:05", PveObjectiveText.Render(info, PveObjectiveText.SurviveFallback));
        }

        [TestCase(0, "0:00")]
        [TestCase(9, "0:09")]
        [TestCase(60, "1:00")]
        [TestCase(600, "10:00")]
        [TestCase(-3, "0:00")]
        public void ClockFormatsMinutesAndSeconds(int seconds, string expected)
        {
            Assert.AreEqual(expected, PveObjectiveText.FormatClock(seconds));
        }

        [Test]
        public void UnknownWinConditionRendersNothing()
        {
            PveObjectiveInfo info = new PveObjectiveInfo { winCondition = "Escort" };

            Assert.IsNull(PveObjectiveText.KeyFor(info));
            Assert.IsNull(PveObjectiveText.Render(info, "{0}"));
        }

        [Test]
        public void DestroyedCountStaysWithinTotal()
        {
            PveObjectiveInfo info = new PveObjectiveInfo
            {
                winCondition = "DestroyObjectives", objectivesTotal = 2, objectivesRemaining = 5
            };

            Assert.AreEqual("0/2", PveObjectiveText.Render(info, "{0}/{1}"));
        }
    }
}
