using Rmg.Tds.Protocol.Utilities;
using System;

namespace Rmg.Tds.Protocol
{
    public struct TdsTypeCollation
    {
        public uint Collation { get; }

        public int LCID => Collation == 0 ? 0x409 : unchecked((int)(Collation & 0x0f_ffff));

        public int SortId { get; }

        internal TdsTypeCollation(ref TdsPayloadReader reader, TdsVersion version)
        {
            this.Collation = unchecked((uint)reader.ReadInt32LE());
            this.SortId = reader.ReadByte();
        }

        public TdsTypeCollation(uint collationId, int sortId)
        {
            this.Collation = collationId;
            this.SortId = sortId;
        }

        public TdsTypeCollation Default => new TdsTypeCollation();

        internal int SerializedLength => 5;

        internal void Serialize(ref TdsPayloadWriter writer)
        {
            var collation = Collation;
            if (collation == 0)
            {
                collation = 0x00d_00409;
            }
            var sortId = SortId;
            if (sortId == 0)
            {
                sortId = 52;
            }

            writer.WriteInt32LE(unchecked((int)collation));
            writer.WriteByte((byte)sortId);
        }
    }
}