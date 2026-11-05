using System;
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class DesktopBuildScriptTests
    {
        private string root;
        private string previousProductName;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "arcane-mac-build-" + Guid.NewGuid().ToString("N"), "ArcaneCasters.app");
            Directory.CreateDirectory(Path.Combine(root, "Contents/MacOS"));
            previousProductName = UnityEditor.PlayerSettings.productName;
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(Path.GetDirectoryName(root), true);
        }

        private void WritePlist(string name)
        {
            File.WriteAllText(Path.Combine(root, "Contents/Info.plist"),
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">" +
                "<plist version=\"1.0\"><dict><key>CFBundleExecutable</key><string>" + name +
                "</string><key>CFBundleName</key><string>Arcane Casters</string></dict></plist>");
        }

        [TestCase("Arcane Casters")]
        [TestCase("ArcaneCasters")]
        public void MacExecutableMatchesUpdaterWithoutChangingProductIdentity(string name)
        {
            WritePlist(name);
            File.WriteAllText(Path.Combine(root, "Contents/MacOS", name), "game executable");

            DesktopBuildScript.NormalizeMacExecutable(root);
            DesktopBuildScript.NormalizeMacExecutable(root);

            Assert.That(File.ReadAllText(Path.Combine(root, "Contents/MacOS/ArcaneCasters")), Is.EqualTo("game executable"));
            var plist = new XmlDocument { XmlResolver = null };
            plist.Load(Path.Combine(root, "Contents/Info.plist"));
            Assert.That(plist.SelectSingleNode("/plist/dict/key[.='CFBundleExecutable']/following-sibling::string[1]").InnerText,
                Is.EqualTo("ArcaneCasters"));
            Assert.That(plist.SelectSingleNode("/plist/dict/key[.='CFBundleName']/following-sibling::string[1]").InnerText,
                Is.EqualTo("Arcane Casters"));
            Assert.That(UnityEditor.PlayerSettings.productName, Is.EqualTo(previousProductName));
        }

        [TestCase("../outside")]
        [TestCase("")]
        public void InvalidExecutableNameIsRejected(string name)
        {
            WritePlist(name);
            Assert.Throws<InvalidDataException>(() => DesktopBuildScript.NormalizeMacExecutable(root));
        }

        [Test]
        public void MissingExecutableDoesNotRewritePlist()
        {
            WritePlist("Arcane Casters");
            string path = Path.Combine(root, "Contents/Info.plist");
            string before = File.ReadAllText(path);
            Assert.Throws<FileNotFoundException>(() => DesktopBuildScript.NormalizeMacExecutable(root));
            Assert.That(File.ReadAllText(path), Is.EqualTo(before));
        }
    }
}
