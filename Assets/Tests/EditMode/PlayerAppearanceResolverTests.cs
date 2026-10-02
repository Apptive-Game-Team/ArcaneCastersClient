using System.Collections.Generic;
using Data;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// Fixes the fallback order of <see cref="PlayerAppearanceResolver"/>: requested set, default set, serialized sprites.
    /// Strings stand in for sprites; a path present in the set means the asset exists.
    /// </summary>
    public class PlayerAppearanceResolverTests
    {
        private static string[] Set(string id)
        {
            return new[] { "PlayerAppearances/" + id + "/idle", "PlayerAppearances/" + id + "/raised", "PlayerAppearances/" + id + "/attacking" };
        }

        private static string Resolve(string appearance, ICollection<string> existing, out string idle, out string raised, out string attacking)
        {
            return PlayerAppearanceResolver.Resolve(appearance, path => existing.Contains(path) ? path : null, out idle, out raised, out attacking);
        }

        [Test]
        public void UsesRequestedSetWhenComplete()
        {
            var existing = new List<string>(Set("storm"));
            existing.AddRange(Set("default"));

            string id = Resolve("storm", existing, out string idle, out string raised, out string attacking);

            Assert.AreEqual("storm", id);
            Assert.AreEqual("PlayerAppearances/storm/idle", idle);
            Assert.AreEqual("PlayerAppearances/storm/raised", raised);
            Assert.AreEqual("PlayerAppearances/storm/attacking", attacking);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void EmptyAppearanceUsesDefaultSet(string appearance)
        {
            string id = Resolve(appearance, Set("default"), out string idle, out _, out _);

            Assert.AreEqual("default", id);
            Assert.AreEqual("PlayerAppearances/default/idle", idle);
        }

        [Test]
        public void IncompleteRequestedSetFallsBackToWholeDefaultSet()
        {
            var existing = new List<string> { "PlayerAppearances/blaze/idle", "PlayerAppearances/blaze/raised" };
            existing.AddRange(Set("default"));

            string id = Resolve("blaze", existing, out string idle, out string raised, out string attacking);

            Assert.AreEqual("default", id);
            Assert.AreEqual("PlayerAppearances/default/idle", idle);
            Assert.AreEqual("PlayerAppearances/default/raised", raised);
            Assert.AreEqual("PlayerAppearances/default/attacking", attacking);
        }

        [Test]
        public void UnknownIdFallsBackToDefaultSet()
        {
            string id = Resolve("no-such-look", Set("default"), out _, out _, out _);

            Assert.AreEqual("default", id);
        }

        [Test]
        public void ReturnsNullWhenDefaultSetIsAlsoMissing()
        {
            string id = Resolve("grass", new List<string>(), out string idle, out string raised, out string attacking);

            Assert.IsNull(id);
            Assert.IsNull(idle);
            Assert.IsNull(raised);
            Assert.IsNull(attacking);
        }

        [TestCase("../default")]
        [TestCase("a/b")]
        [TestCase("a\\b")]
        public void UnsafeIdIsTreatedAsDefault(string appearance)
        {
            Assert.AreEqual("default", PlayerAppearanceResolver.ResolveId(appearance));
        }

        [Test]
        public void AppearanceSurvivesJsonDeserialization()
        {
            MatchedInfoDto dto = JsonCodec.Deserialize<MatchedInfoDto>(
                @"{ ""leftUser"": { ""id"": 1, ""name"": ""bot"", ""appearance"": ""tide"" }, ""rightUser"": { ""id"": 2, ""name"": ""human"" } }");

            Assert.AreEqual("tide", dto.FindUserByMaster("LeftPlayer").appearance);
            Assert.IsNull(dto.FindUserByMaster("RightPlayer").appearance);
            Assert.IsNull(dto.FindUserByMaster("Other"));
        }
    }
}
