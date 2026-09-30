using Global.Udp;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class UdpFragmentAssemblerTests
    {
        private static UdpPacket Fragment(uint seq, int index, int count, params byte[] chunk)
        {
            byte[] body = new byte[UdpPacket.FragmentPrefixSize + chunk.Length];
            body[0] = (byte)index;
            body[1] = (byte)count;
            System.Buffer.BlockCopy(chunk, 0, body, UdpPacket.FragmentPrefixSize, chunk.Length);
            return new UdpPacket(UdpPacketType.Frame, UdpPacket.FlagFragment, seq, 1UL, body);
        }

        [Test]
        public void APacketThatIsNotAFragmentPassesThrough()
        {
            var assembler = new UdpFragmentAssembler();
            byte[] payload;
            uint groupBase;

            Assert.IsTrue(assembler.TryAdd(new UdpPacket(UdpPacketType.Frame, 0, 7u, 1UL, new byte[] { 1, 2 }), out payload, out groupBase));

            Assert.AreEqual(new byte[] { 1, 2 }, payload);
            Assert.AreEqual(7u, groupBase);
        }

        [Test]
        public void RebuildsThePayloadRegardlessOfArrivalOrder()
        {
            var assembler = new UdpFragmentAssembler();
            byte[] payload;
            uint groupBase;

            Assert.IsFalse(assembler.TryAdd(Fragment(12, 2, 3, 5, 6), out payload, out groupBase));
            Assert.IsFalse(assembler.TryAdd(Fragment(10, 0, 3, 1, 2), out payload, out groupBase));
            Assert.IsTrue(assembler.TryAdd(Fragment(11, 1, 3, 3, 4), out payload, out groupBase));

            Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, payload);
            Assert.AreEqual(10u, groupBase);
        }

        [Test]
        public void ARepeatedFragmentDoesNotCompleteTheGroupEarly()
        {
            var assembler = new UdpFragmentAssembler();
            byte[] payload;
            uint groupBase;

            assembler.TryAdd(Fragment(10, 0, 2, 1), out payload, out groupBase);

            Assert.IsFalse(assembler.TryAdd(Fragment(10, 0, 2, 1), out payload, out groupBase));
            Assert.IsTrue(assembler.TryAdd(Fragment(11, 1, 2, 2), out payload, out groupBase));
            Assert.AreEqual(new byte[] { 1, 2 }, payload);
        }

        [Test]
        public void RejectsMalformedFragments()
        {
            var assembler = new UdpFragmentAssembler();
            byte[] payload;
            uint groupBase;

            Assert.IsFalse(assembler.TryAdd(Fragment(10, 3, 3, 1), out payload, out groupBase));
            Assert.IsFalse(assembler.TryAdd(Fragment(10, 0, 0, 1), out payload, out groupBase));
            Assert.IsFalse(assembler.TryAdd(new UdpPacket(UdpPacketType.Frame, UdpPacket.FlagFragment, 10u, 1UL, new byte[] { 0 }), out payload, out groupBase));
        }

        [Test]
        public void DropBelowForgetsOlderIncompleteGroups()
        {
            var assembler = new UdpFragmentAssembler();
            byte[] payload;
            uint groupBase;

            assembler.TryAdd(Fragment(10, 0, 2, 1), out payload, out groupBase);
            assembler.DropBelow(20);

            // 이 그룹은 잊혔으므로 나머지 조각만으로는 완성되지 않는다.
            Assert.IsFalse(assembler.TryAdd(Fragment(11, 1, 2, 2), out payload, out groupBase));
        }

        [Test]
        public void IncompleteGroupsDoNotGrowWithoutBound()
        {
            var assembler = new UdpFragmentAssembler();
            byte[] payload;
            uint groupBase;

            for (uint i = 0; i < 100; i++)
            {
                assembler.TryAdd(Fragment(i * 10, 0, 2, 1), out payload, out groupBase);
            }

            // 가장 오래된 그룹은 밀려났고, 최근 그룹은 여전히 완성할 수 있다.
            Assert.IsFalse(assembler.TryAdd(Fragment(1, 1, 2, 2), out payload, out groupBase));
            Assert.IsTrue(assembler.TryAdd(Fragment(991, 1, 2, 2), out payload, out groupBase));
        }
    }
}
