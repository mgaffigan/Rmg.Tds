using Rmg.Tds.Protocol.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public struct TdsPacketHeader
    {
        public const int HeaderLength = 8;

        public TdsPacketType Type { get; }
        public TdsPacketStatuses Statuses { get; }
        public int Length { get; }
        public int SPID { get; }
        public int PacketID { get; }
        public int Window { get; }

        public int DataLength => Length - HeaderLength;

        public TdsPacketHeader(ReadOnlySpan<byte> data)
        {
            if (data.Length < HeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "Data is too short");
            }

            this.Type = (TdsPacketType)data[0];
            this.Statuses = (TdsPacketStatuses)data[1];
            this.Length = data.ReadBigEndianInt32(2, 2);
            this.SPID = data.ReadBigEndianInt32(4, 2);
            this.PacketID = data[6];
            this.Window = data[7];

            if (this.Window != 0)
            {
                throw new ProtocolViolationException("Window is not 0");
            }
            if (!Enum.IsDefined(typeof(TdsPacketType), this.Type))
            {
                throw new ProtocolViolationException("Unknown packet type");
            }
        }

        public TdsPacketHeader(TdsPacketType type, TdsPacketStatuses status, int len, int spid, int pid, int window = 0)
        {
            if (len < HeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(len));
            }

            this.Type = type;
            this.Statuses = status;
            this.Length = len;
            this.SPID = spid;
            this.PacketID = pid;
            this.Window = window;
        }

        public void Serialize(Span<byte> data)
        {
            if (data.Length < HeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "Data is too short");
            }

            data[0] = (byte)this.Type;
            data[1] = (byte)this.Statuses;
            data.WriteBigEndianInt32(2, 2, this.Length);
            data.WriteBigEndianInt32(4, 2, this.SPID);
            data[6] = (byte)(this.PacketID & 0xff);
            data[7] = (byte)(this.Window & 0xff);
        }

        public byte[] ToArray()
        {
            var arr = new byte[HeaderLength];
            Serialize(arr);
            return arr;
        }
    }
}
