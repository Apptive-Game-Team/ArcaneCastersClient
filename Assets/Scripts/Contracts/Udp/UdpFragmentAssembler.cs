using System;
using System.Collections.Generic;

namespace Global.Udp
{
    /// <summary>
    /// 분할된 페이로드를 다시 붙인다. 조각이 도착하는 순서는 상관없고, 하나라도 빠진 그룹은
    /// 끝내 완성되지 않은 채 오래된 순으로 밀려난다.
    /// </summary>
    public sealed class UdpFragmentAssembler
    {
        /// <summary>동시에 붙들고 있는 미완성 그룹의 상한. 유실이 쌓여도 메모리가 자라지 않게 한다.</summary>
        private const int MaxGroups = 8;

        private sealed class Group
        {
            public int Count;
            public int Received;
            public byte[][] Chunks;
        }

        private readonly Dictionary<uint, Group> _groups = new Dictionary<uint, Group>();

        /// <summary>
        /// 패킷 하나를 넣는다. 조각이 아니면 본문을 그대로 돌려주고, 조각이면 그룹이 완성됐을 때만 true다.
        /// <paramref name="groupBase"/>는 페이로드의 첫 seq로, 프레임 순서를 비교하는 기준이다.
        /// </summary>
        public bool TryAdd(UdpPacket packet, out byte[] payload, out uint groupBase)
        {
            payload = null;
            groupBase = packet.Seq;

            if (!packet.IsFragment)
            {
                payload = packet.Body;
                return true;
            }

            byte[] body = packet.Body;
            if (body.Length < UdpPacket.FragmentPrefixSize) return false;

            int index = body[0];
            int count = body[1];
            if (count == 0 || index >= count) return false;

            groupBase = packet.Seq - (uint)index;

            Group group;
            if (!_groups.TryGetValue(groupBase, out group))
            {
                if (_groups.Count >= MaxGroups) DropOldest();
                group = new Group { Count = count, Chunks = new byte[count][] };
                _groups[groupBase] = group;
            }

            // 같은 그룹인데 조각 수가 다르면 서로 다른 페이로드가 섞인 것이다.
            if (group.Count != count) return false;
            if (group.Chunks[index] != null) return false;

            byte[] chunk = new byte[body.Length - UdpPacket.FragmentPrefixSize];
            Buffer.BlockCopy(body, UdpPacket.FragmentPrefixSize, chunk, 0, chunk.Length);
            group.Chunks[index] = chunk;
            group.Received++;

            if (group.Received < group.Count) return false;

            _groups.Remove(groupBase);
            int total = 0;
            for (int i = 0; i < group.Chunks.Length; i++) total += group.Chunks[i].Length;

            payload = new byte[total];
            int offset = 0;
            for (int i = 0; i < group.Chunks.Length; i++)
            {
                Buffer.BlockCopy(group.Chunks[i], 0, payload, offset, group.Chunks[i].Length);
                offset += group.Chunks[i].Length;
            }
            return true;
        }

        /// <summary>이 기준보다 오래된 미완성 그룹을 버린다. 더 새로운 프레임이 이미 완성됐을 때 쓴다.</summary>
        public void DropBelow(uint groupBase)
        {
            List<uint> stale = new List<uint>();
            foreach (uint key in _groups.Keys)
            {
                if (key < groupBase) stale.Add(key);
            }
            for (int i = 0; i < stale.Count; i++) _groups.Remove(stale[i]);
        }

        public void Clear()
        {
            _groups.Clear();
        }

        private void DropOldest()
        {
            bool found = false;
            uint oldest = 0;
            foreach (uint key in _groups.Keys)
            {
                if (!found || key < oldest)
                {
                    oldest = key;
                    found = true;
                }
            }
            if (found) _groups.Remove(oldest);
        }
    }
}
