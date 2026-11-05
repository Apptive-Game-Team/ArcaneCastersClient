using System;
using System.IO;
using System.Text;
using Data.Preview;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class ReplayFileCacheTests
    {
        private string root;
        private byte[] payload;
        private PreviewEntry entry;

        [SetUp] public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "preview-cache-" + Guid.NewGuid().ToString("N"));
            payload = Encoding.UTF8.GetBytes("{\"version\":2,\"magic\":\"fire_shot\"}");
            entry = new PreviewEntry { name = "fire_shot", hash = ReplayFileCache.Hash(payload), bytes = payload.Length, available = true };
        }

        [Test] public void CacheSurvivesNewInstanceButCannotCrossServerOrContentRevision()
        {
            Assert.That(new ReplayFileCache(root, "https://first").TrySave(entry, payload), Is.True);
            Assert.That(new ReplayFileCache(root, "https://first").TryLoad(entry, out byte[] loaded), Is.True);
            Assert.That(loaded, Is.EqualTo(payload));
            Assert.That(new ReplayFileCache(root, "https://second").TryLoad(entry, out _), Is.False);
            entry.hash = new string('a', 64);
            Assert.That(new ReplayFileCache(root, "https://first").TryLoad(entry, out _), Is.False);
        }

        [Test] public void CorruptBytesAndUnsafeNamesCannotBeAdopted()
        {
            Assert.That(ReplayFileCache.Verify(entry, Encoding.UTF8.GetBytes("wrong")), Is.False);
            entry.name = "../fire_shot";
            Assert.That(new ReplayFileCache(root, "https://first").TrySave(entry, payload), Is.False);
            entry.name = "fire_shot";
            entry.bytes = ReplayFileCache.MaxBytes + 1;
            Assert.That(ReplayFileCache.Valid(entry), Is.False);
        }

        [Test] public void MissingOrUnavailableCatalogEntryCannotUnlockCachedReplay()
        {
            var files = new ReplayFileCache(root, "https://first");
            files.TrySave(entry, payload);
            Assert.That(files.TryLoad(null, out _), Is.False);
            entry.available = false;
            Assert.That(files.TryLoad(entry, out _), Is.False);
        }

        [Test] public void PruneDropsOldContentWithoutRemovingCurrentRevision()
        {
            var files = new ReplayFileCache(root, "https://first");
            files.TrySave(entry, payload);
            files.Prune(new[] { entry.hash });
            Assert.That(files.TryLoad(entry, out _), Is.True);
            files.Prune(Array.Empty<string>());
            Assert.That(files.TryLoad(entry, out _), Is.False);
        }

        [Test] public void CatalogRoundTripsNullableServerAndPerMagicMetadata()
        {
            var catalog = new PreviewCatalog { status = "ready", revision = new string('b', 64), serverId = 42, magics = new[] { entry } };
            PreviewCatalog restored = JsonCodec.Deserialize<PreviewCatalog>(JsonCodec.Serialize(catalog));
            Assert.That(restored.serverId, Is.EqualTo(42));
            Assert.That(restored.magics[0].hash, Is.EqualTo(entry.hash));
            Assert.That(restored.magics[0].bytes, Is.EqualTo(payload.Length));
            Assert.That(JsonCodec.Deserialize<PreviewCatalog>("{\"status\":\"generating\"}").serverId, Is.Null);
        }

        [TearDown] public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
