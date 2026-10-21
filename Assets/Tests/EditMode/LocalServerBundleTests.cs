using System;
using System.IO;
using LocalServer;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class LocalServerBundleTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "Rust server 한글 " + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(directory, true);

        [TestCase(true, false)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public void FindsNativeBundleWithOrWithoutArchiveFolder(bool windows, bool nested)
        {
            string root = nested ? Path.Combine(directory, "local-server") : directory;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, windows ? "ac-game.exe" : "ac-game"), "fixture");
            Assert.That(LocalServerBundle.ResolveRoot(directory, windows), Is.EqualTo(root));
            Assert.That(LocalServerBundle.ResolveRoot(directory, !windows), Is.Null);
        }

        [Test]
        public void LegacyJavaBundleIsNotAnInstalledRustServer()
        {
            string root = Path.Combine(directory, "local-server");
            string javaBin = Path.Combine(root, "jre", "bin");
            Directory.CreateDirectory(javaBin);
            File.WriteAllText(Path.Combine(root, "local-server.jar"), "fixture");
            File.WriteAllText(Path.Combine(javaBin, "java.exe"), "fixture");
            File.WriteAllText(Path.Combine(javaBin, "java"), "fixture");
            Assert.That(LocalServerBundle.ResolveRoot(directory, true), Is.Null);
            Assert.That(LocalServerBundle.ResolveRoot(directory, false), Is.Null);
        }

        [Test]
        public void IncompleteBundleIsNotSelected()
        {
            File.WriteAllText(Path.Combine(directory, "VERSION"), "0.1.0");
            Directory.CreateDirectory(Path.Combine(directory, "ac-game.exe"));
            Assert.That(LocalServerBundle.ResolveRoot(directory, true), Is.Null);
            Assert.That(LocalServerBundle.ResolveRoot(directory, false), Is.Null);
        }
    }
}
