using System;

namespace Rmg.Tds.Protocol
{
    public sealed class TdsPacket
    {
        public TdsPacketHeader Header { get; }
        public byte[] Data { get; }

        public TdsPacket(TdsPacketHeader header, byte[] data)
        {
            if (data.Length != header.DataLength)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            this.Header = header;
            this.Data = data;
        }
    }
}