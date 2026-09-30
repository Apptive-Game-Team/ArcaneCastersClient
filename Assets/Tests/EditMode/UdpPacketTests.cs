using System.Text;
using Global.Udp;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class UdpPacketTests
    {
        // 서버 UdpPacketTest.headerIsSixteenBytesInWireOrder와 같은 벡터다. 한쪽만 바뀌면 안 된다.
        private static readonly byte[] GoldenFrame =
        {
            0xAC, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02
        };

        [Test]
        public void Encode_MatchesTheServerWireFormat()
        {
            var packet = new UdpPacket(UdpPacketType.Frame, 0, 1u, 2UL, new byte[0]);

            Assert.AreEqual(GoldenFrame, packet.Encode());
        }

        [Test]
        public void RoundTrip_KeepsEveryField()
        {
            byte[] body = { 1, 2, 3 };
            var packet = new UdpPacket(UdpPacketType.Input, UdpPacket.FlagReliable, 0xFFFFFFF0u, 0x0123456789ABCDEFUL, body);
            byte[] wire = packet.Encode();

            UdpPacket decoded;
            Assert.IsTrue(UdpPacket.TryDecode(wire, wire.Length, out decoded));

            Assert.AreEqual(UdpPacketType.Input, decoded.Type);
            Assert.IsTrue(decoded.IsReliable);
            Assert.IsFalse(decoded.IsFragment);
            Assert.AreEqual(0xFFFFFFF0u, decoded.Seq);
            Assert.AreEqual(0x0123456789ABCDEFUL, decoded.ConnectionId);
            Assert.AreEqual(body, decoded.Body);
        }

        [Test]
        public void TryDecode_RejectsWhatIsNotOurs()
        {
            byte[] valid = new UdpPacket(UdpPacketType.Hello, 0, 1u, 0UL, new byte[0]).Encode();
            byte[] wrongMagic = (byte[])valid.Clone();
            wrongMagic[0] = 0;
            byte[] unknownType = (byte[])valid.Clone();
            unknownType[1] = 99;
            UdpPacket packet;

            Assert.IsFalse(UdpPacket.TryDecode(wrongMagic, wrongMagic.Length, out packet));
            Assert.IsFalse(UdpPacket.TryDecode(unknownType, unknownType.Length, out packet));
            Assert.IsFalse(UdpPacket.TryDecode(valid, UdpPacket.HeaderSize - 1, out packet));
            Assert.IsFalse(UdpPacket.TryDecode(null, 0, out packet));
        }

        [Test]
        public void TryDecode_UsesOnlyTheReceivedLengthOfALargerBuffer()
        {
            byte[] wire = new UdpPacket(UdpPacketType.Input, 0, 1u, 2UL, new byte[] { 7 }).Encode();
            byte[] buffer = new byte[2048];
            System.Buffer.BlockCopy(wire, 0, buffer, 0, wire.Length);
            UdpPacket decoded;

            Assert.IsTrue(UdpPacket.TryDecode(buffer, wire.Length, out decoded));

            Assert.AreEqual(new byte[] { 7 }, decoded.Body);
        }

        [Test]
        public void Hello_BodyIsUtf8()
        {
            byte[] body = Encoding.UTF8.GetBytes("{\"token\":\"t\"}");
            var packet = new UdpPacket(UdpPacketType.Hello, 0, 1u, 0UL, body);
            UdpPacket decoded;

            Assert.IsTrue(UdpPacket.TryDecode(packet.Encode(), packet.Encode().Length, out decoded));

            Assert.AreEqual("{\"token\":\"t\"}", Encoding.UTF8.GetString(decoded.Body));
        }
    }
}
