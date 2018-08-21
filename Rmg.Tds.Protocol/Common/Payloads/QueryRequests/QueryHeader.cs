using Rmg.Tds.Protocol.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    // Represensts the ALL_HEADERS block
    public abstract class QueryHeader
    {
        public abstract int DataLength { get; }

        public int Length => DataLength + 4 /* len */ + 2 /* type */;

        public abstract QueryHeaderType Type { get; }

        public static IReadOnlyList<QueryHeader> Deserialize(ReadOnlySpan<byte> data)
        {
            var results = new List<QueryHeader>();
            var reader = new TdsPayloadReader(data);
            while (!reader.IsFullyConsumed)
            {
                var headerLen = reader.ReadInt32LE();
                var headerType = (QueryHeaderType)reader.ReadInt16LE();
                results.Add(DeserializeHeader(headerLen - 4 /* len */ - 2 /* type */, headerType, ref reader));
            }
            return results.AsReadOnly();
        }

        private static QueryHeader DeserializeHeader(int len, QueryHeaderType type, ref TdsPayloadReader reader)
        {
            switch (type)
            {
                default: return new UnknownQueryHeader(len, type, ref reader);
            }
        }

        public void Serialize(Span<byte> d)
        {
            if (d.Length < Length)
            {
                throw new InvalidOperationException();
            }

            d.WriteLittleEndianInt32(0, 4, Length);
            d.WriteLittleEndianInt32(4, 2, (int)Type);
            SerializeInternal(d.Slice(6, DataLength));
        }

        internal abstract void SerializeInternal(Span<byte> span);
    }

    public sealed class UnknownQueryHeader : QueryHeader
    {
        public override int DataLength { get; }

        public override QueryHeaderType Type { get; }

        public byte[] Data { get; }

        internal UnknownQueryHeader(int len, QueryHeaderType type, ref TdsPayloadReader reader)
        {
            this.DataLength = len;
            this.Type = type;
            this.Data = reader.ReadData(len);
        }

        internal override void SerializeInternal(Span<byte> span)
        {
            Data.CopyTo(span);
        }
    }
}
