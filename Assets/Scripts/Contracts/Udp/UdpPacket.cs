using System;

namespace Global.Udp
{
    /// <summary>UDP 게임 채널의 패킷 종류. 값은 와이어의 type 바이트다. 서버 UdpPacketType과 같아야 한다.</summary>
    public enum UdpPacketType : byte
    {
        /// <summary>클라이언트→서버. JSON {token, sessionId, userId}. 응답이 올 때까지 반복한다.</summary>
        Hello = 1,

        /// <summary>서버→클라이언트. 헤더의 connectionId가 이후 모든 패킷의 키다.</summary>
        HelloAck = 2,

        /// <summary>서버→클라이언트. 인증이나 세션 확인에 실패했다. 본문은 짧은 사유.</summary>
        HelloReject = 3,

        /// <summary>클라이언트→서버. InputRequestDto JSON.</summary>
        Input = 4,

        /// <summary>서버→클라이언트. 이 seq의 Input이 도착했다.</summary>
        InputAck = 5,

        /// <summary>서버→클라이언트. FrameInfo/SyncInfo JSON. 비신뢰이며 오래된 것은 버린다.</summary>
        Frame = 6,

        /// <summary>서버→클라이언트. 결과·입력 응답·이모트 등 그 밖의 페이로드. 신뢰 전송이다.</summary>
        Message = 7,

        /// <summary>클라이언트→서버. 이 seq의 Message가 도착했다.</summary>
        MessageAck = 8,

        /// <summary>양방향. 상대가 떠난다.</summary>
        Bye = 9
    }

    /// <summary>
    /// UDP 데이터그램 하나. 빅엔디언, 16바이트 헤더 뒤에 본문이 온다.
    /// <code>
    ///  0      1      2      3      4..7     8..15
    /// magic  type  flags  rsv    seq(u32)  connectionId(u64)
    /// </code>
    /// <see cref="FlagFragment"/> 패킷의 본문은 <c>idx(u8) count(u8)</c>로 시작하고 조각 하나를 담는다.
    /// 한 페이로드의 조각들은 연속된 seq를 받으므로 조각의 그룹은 <c>seq - idx</c>다.
    /// </summary>
    public sealed class UdpPacket
    {
        public const byte Magic = 0xAC;
        public const int HeaderSize = 16;
        public const int FragmentPrefixSize = 2;

        /// <summary>받는 쪽이 이 seq를 ack하고 반복 도착은 버린다.</summary>
        public const int FlagReliable = 0x01;

        /// <summary>본문이 idx, count로 시작하고 큰 페이로드의 조각 하나를 담는다.</summary>
        public const int FlagFragment = 0x02;

        public UdpPacketType Type { get; private set; }
        public int Flags { get; private set; }
        public uint Seq { get; private set; }
        public ulong ConnectionId { get; private set; }
        public byte[] Body { get; private set; }

        public UdpPacket(UdpPacketType type, int flags, uint seq, ulong connectionId, byte[] body)
        {
            Type = type;
            Flags = flags;
            Seq = seq;
            ConnectionId = connectionId;
            Body = body ?? new byte[0];
        }

        public bool IsReliable
        {
            get { return (Flags & FlagReliable) != 0; }
        }

        public bool IsFragment
        {
            get { return (Flags & FlagFragment) != 0; }
        }

        public byte[] Encode()
        {
            byte[] data = new byte[HeaderSize + Body.Length];
            data[0] = Magic;
            data[1] = (byte)Type;
            data[2] = (byte)Flags;
            data[3] = 0;
            WriteUInt32(data, 4, Seq);
            for (int i = 0; i < 8; i++)
            {
                data[8 + i] = (byte)(ConnectionId >> (56 - 8 * i));
            }
            Buffer.BlockCopy(Body, 0, data, HeaderSize, Body.Length);
            return data;
        }

        /// <summary>우리 패킷이 아니면(매직 불일치, 너무 짧음, 모르는 type) false.</summary>
        public static bool TryDecode(byte[] data, int length, out UdpPacket packet)
        {
            packet = null;
            if (data == null || length < HeaderSize || length > data.Length || data[0] != Magic) return false;
            if (!IsKnownType(data[1])) return false;

            uint seq = ReadUInt32(data, 4);
            ulong connectionId = 0;
            for (int i = 0; i < 8; i++)
            {
                connectionId = (connectionId << 8) | data[8 + i];
            }

            byte[] body = new byte[length - HeaderSize];
            Buffer.BlockCopy(data, HeaderSize, body, 0, body.Length);
            packet = new UdpPacket((UdpPacketType)data[1], data[2], seq, connectionId, body);
            return true;
        }

        private static bool IsKnownType(byte code)
        {
            return code >= (byte)UdpPacketType.Hello && code <= (byte)UdpPacketType.Bye;
        }

        private static void WriteUInt32(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)(value >> 24);
            data[offset + 1] = (byte)(value >> 16);
            data[offset + 2] = (byte)(value >> 8);
            data[offset + 3] = (byte)value;
        }

        private static uint ReadUInt32(byte[] data, int offset)
        {
            return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16)
                   | ((uint)data[offset + 2] << 8) | data[offset + 3];
        }
    }
}
