using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public abstract class TdsMessagePayload
    {
        // type is expected to have a constructor .ctor(TdsMessage)

        public TdsVersion TdsVersion { get; }

        public TdsPacketType PacketType { get; }

        public TdsMessagePayload(TdsMessage message, TdsPacketType type)
        {
            this.TdsVersion = message.Context.TdsVersion;
            this.PacketType = type;
        }

        public TdsMessagePayload(TdsVersion ver, TdsPacketType type)
        {
            this.TdsVersion = ver;
            this.PacketType = type;
        }

        public abstract int SerializedLength { get; }

        public abstract void Serialize(Span<byte> data);

        public byte[] ToArray()
        {
            var b = new byte[SerializedLength];
            Serialize(b);
            return b;
        }
    }
}
