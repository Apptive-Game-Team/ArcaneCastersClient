using System;
using System.Collections.Generic;
using Global;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class IDMakerTests
    {
        [Test]
        public void GetUserID_ReturnsValidGuidString()
        {
            string id = IDMaker.GetUserID();

            Assert.That(id, Is.Not.Null.And.Not.Empty);
            Assert.That(Guid.TryParse(id, out Guid parsedGuid), Is.True);
            Assert.That(parsedGuid, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void GetUserID_GeneratesUniqueIDs()
        {
            var generatedIds = new HashSet<string>();
            const int count = 100;

            for (int i = 0; i < count; i++)
            {
                string id = IDMaker.GetUserID();
                Assert.That(generatedIds.Add(id), Is.True, $"Duplicate ID generated: {id}");
            }

            Assert.That(generatedIds.Count, Is.EqualTo(count));
        }

        [Test]
        public void GetCardUseInputID_IncrementsSequentially()
        {
            int first = IDMaker.GetCardUseInputID();
            int second = IDMaker.GetCardUseInputID();

            Assert.That(second, Is.EqualTo(first + 1));
        }
    }
}
