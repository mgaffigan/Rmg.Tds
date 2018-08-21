using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class TvpColumnOrderingToken : TvpToken
    {
        public override TvpTokenType Type => TvpTokenType.ColumnOrdering;

        public int[] Order { get; }

        internal TvpColumnOrderingToken(ref TdsPayloadReader reader)
        {
            var count = reader.ReadInt16LE();
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = reader.ReadInt16LE();
            }
            this.Order = result;
        }

        internal override int DataLength =>
            + 2 // count
            + (Order.Length * 2);

        internal override void SerializeInternal(ref TdsPayloadWriter writer, TvpSerializationContext context)
        {
            writer.WriteInt16LE(Order.Length);
            foreach (var c in Order)
            {
                writer.WriteInt16LE(c);
            }
        }
    }
}
