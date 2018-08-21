using Rmg.Tds.Protocol.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class Login7Request : TdsMessagePayload
    {
        public Login7Request(TdsMessage message)
            : base(ReadVersion(message.Data), TdsPacketType.Tds7Login)
        {
            ReadOnlySpan<byte> data = message.Data;

            // for some ass-backwards reason, the entire message is little endian
            var length = data.ReadBigEndianInt32(0, 4);
            this.PacketSize = data.ReadLittleEndianInt32(8, 4);

            this.Data = data.ToArray();
        }

        public Login7Request(TdsVersion newTdsVersion, int packetSize, ReadOnlySpan<byte> data)
            : base(newTdsVersion, TdsPacketType.Tds7Login)
        {
            this.PacketSize = packetSize;
            this.Data = data.ToArray();
            Span<byte> span = this.Data;
            span.WriteLittleEndianInt32(4, 4, unchecked((int)TdsVersion));
            span.WriteLittleEndianInt32(8, 4, PacketSize);
        }

        private static TdsVersion ReadVersion(ReadOnlySpan<byte> data)
        {
            return (TdsVersion)unchecked((uint)data.ReadLittleEndianInt32(4, 4));
        }

        public int PacketSize { get; }
        public byte[] Data { get; }

        public string HostName => GetStringByOffset(36);

        public string Username => GetStringByOffset(40);

        public string AppName => GetStringByOffset(48);

        public string Database => GetStringByOffset(68);

        public int ClientPid => ((ReadOnlySpan<byte>)Data).ReadBigEndianInt32(6, 4);

        private string GetStringByOffset(int offset)
        {
            var r = (ReadOnlySpan<byte>)Data;
            int p = r.ReadLittleEndianInt32(offset, 2), cch = r.ReadLittleEndianInt32(offset + 2, 2);
            return r.ReadUnicodeStringCch(p, cch);
        }

        public override int SerializedLength => Data.Length;

        public override void Serialize(Span<byte> data)
        {
            this.Data.CopyTo(data);
        }
    }
}
