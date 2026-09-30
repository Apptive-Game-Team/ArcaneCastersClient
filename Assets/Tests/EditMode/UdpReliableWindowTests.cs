using Global.Udp;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class UdpReliableWindowTests
    {
        [Test]
        public void AcceptsEachSeqOnce()
        {
            var window = new UdpReliableWindow();

            Assert.IsTrue(window.Accept(1));
            Assert.IsFalse(window.Accept(1));
            Assert.IsTrue(window.Accept(2));
        }

        [Test]
        public void AcceptsAnOlderSeqThatArrivesLateOnce()
        {
            var window = new UdpReliableWindow();
            window.Accept(5);

            Assert.IsTrue(window.Accept(3));
            Assert.IsFalse(window.Accept(3));
        }

        [Test]
        public void TreatsSeqsBelowTheWindowAsSeen()
        {
            var window = new UdpReliableWindow();
            window.Accept(200);

            Assert.IsFalse(window.Accept(100));
        }

        [Test]
        public void ABigJumpForgetsTheOldWindow()
        {
            var window = new UdpReliableWindow();
            window.Accept(1);
            window.Accept(1000);

            Assert.IsFalse(window.Accept(1000));
            Assert.IsTrue(window.Accept(999));
        }
    }
}
