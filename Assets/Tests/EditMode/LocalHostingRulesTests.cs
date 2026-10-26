using System;
using System.IO;
using LocalServer;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class LocalHostingRulesTests
    {
        private static LocalHostingConditions AllMet() => new LocalHostingConditions
        {
            PlatformSupported = true,
            SettingEnabled = true,
            LocalSessionsEnabled = true,
            InstalledVersion = "1.3.0",
            MinimumVersion = "1.3.0",
            ServerReady = true,
            ServerStaticVersion = "abc",
            LobbyStaticVersion = "abc"
        };

        [Test]
        public void AllConditionsMetAllowsALocalMatch()
        {
            Assert.That(LocalHostingRules.Evaluate(AllMet()), Is.EqualTo(LocalHostingBlock.None));
        }

        [Test]
        public void EachMissingConditionFallsBackToTheSharedServer()
        {
            LocalHostingConditions c = AllMet();
            c.PlatformSupported = false;
            Assert.That(LocalHostingRules.Evaluate(c), Is.EqualTo(LocalHostingBlock.PlatformNotSupported));

            c = AllMet();
            c.SettingEnabled = false;
            Assert.That(LocalHostingRules.Evaluate(c), Is.EqualTo(LocalHostingBlock.SettingDisabled));

            c = AllMet();
            c.LocalSessionsEnabled = false;
            Assert.That(LocalHostingRules.Evaluate(c), Is.EqualTo(LocalHostingBlock.LocalSessionsDisabled));

            c = AllMet();
            c.InstalledVersion = null;
            Assert.That(LocalHostingRules.Evaluate(c), Is.EqualTo(LocalHostingBlock.NotInstalled));

            c = AllMet();
            c.InstalledVersion = "1.2.9";
            Assert.That(LocalHostingRules.Evaluate(c), Is.EqualTo(LocalHostingBlock.BelowMinimumVersion));

            c = AllMet();
            c.ServerReady = false;
            Assert.That(LocalHostingRules.Evaluate(c), Is.EqualTo(LocalHostingBlock.ServerNotReady));

            c = AllMet();
            c.ServerStaticVersion = "old";
            Assert.That(LocalHostingRules.Evaluate(c), Is.EqualTo(LocalHostingBlock.StaticDataOutdated));
        }

        [TestCase("1.3.0", "1.3.0", 0)]
        [TestCase("1.10.0", "1.9.0", 1)]
        [TestCase("1.3", "1.3.0", 0)]
        [TestCase("v1.3.1", "1.3.0", 1)]
        [TestCase("1.3.0-rc1", "1.3.0", 0)]
        [TestCase("1.2.9", "1.3.0", -1)]
        public void ComparesVersionsNumerically(string left, string right, int expected)
        {
            Assert.That(Math.Sign(LocalHostingRules.CompareVersions(left, right)), Is.EqualTo(expected));
        }

        [Test]
        public void ReadsTheChecksumFileInBothShapes()
        {
            string hex = new string('a', 64);
            Assert.That(LocalHostingRules.ParseSha256File(hex + "\n"), Is.EqualTo(hex));
            Assert.That(LocalHostingRules.ParseSha256File(hex.ToUpperInvariant() + "  local-server-linux-x64.zip\n"), Is.EqualTo(hex));
            Assert.That(LocalHostingRules.ParseSha256File("not a hash"), Is.Null);
            Assert.That(LocalHostingRules.ParseSha256File(""), Is.Null);
        }

        [Test]
        public void TokenHexIsLowercaseAndTwiceTheByteCount()
        {
            Assert.That(LocalHostingRules.ToHex(new byte[] { 0x00, 0xAB, 0x0F }), Is.EqualTo("00ab0f"));
            Assert.That(LocalHostingRules.ToHex(new byte[32]).Length, Is.EqualTo(64));
        }

        [Test]
        public void PendingResultsExpireAfterTwoHours()
        {
            DateTime now = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
            Assert.That(LocalHostingRules.IsPendingResultFresh(now.AddMinutes(-119), now), Is.True);
            Assert.That(LocalHostingRules.IsPendingResultFresh(now.AddHours(-2), now), Is.False);
        }

        [TestCase(false, 0, PendingResultAction.Keep)]
        [TestCase(true, 200, PendingResultAction.Drop)]
        [TestCase(true, 404, PendingResultAction.Drop)]
        [TestCase(true, 409, PendingResultAction.Drop)]
        [TestCase(true, 400, PendingResultAction.Drop)]
        [TestCase(true, 401, PendingResultAction.Keep)]
        [TestCase(true, 503, PendingResultAction.Keep)]
        public void DecidesWhatToDoWithAPendingResult(bool reachedServer, long code, PendingResultAction expected)
        {
            Assert.That(LocalHostingRules.DecideAfterReport(reachedServer, code), Is.EqualTo(expected));
        }

        [Test]
        public void ReadsTheStaticDataHeaderWithoutTheTables()
        {
            const string json = "{\"schemaVersion\":1,\"generatedAt\":\"2026-10-12T03:00:00Z\"," +
                                "\"tables\":{\"magics\":{\"columns\":[\"id\"],\"rows\":[[1]]}}," +
                                "\"staticVersion\":\"ff00\"}";

            StaticDataHeader header = StaticDataFile.ReadHeader(new StringReader(json));

            Assert.That(header.SchemaVersion, Is.EqualTo(1));
            Assert.That(header.StaticVersion, Is.EqualTo("ff00"));
            Assert.That(header.IsUsable, Is.True);
        }

        [Test]
        public void AnotherSchemaVersionIsNotUsable()
        {
            StaticDataHeader header = StaticDataFile.ReadHeader(new StringReader("{\"schemaVersion\":2,\"staticVersion\":\"x\"}"));

            Assert.That(header.IsUsable, Is.False);
        }
    }
}
