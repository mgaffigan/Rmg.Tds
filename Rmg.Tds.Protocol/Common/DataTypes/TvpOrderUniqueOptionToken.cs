using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class TvpOrderUniqueOptionToken : TvpToken
    {
        public override TvpTokenType Type => TvpTokenType.OrderUnique;
        const int CbPerCount = 3;

        public int Count { get; }
        public byte[] Data { get; }

        internal TvpOrderUniqueOptionToken(ref TdsPayloadReader reader)
        {
            this.Count = reader.ReadInt16LE();
            this.Data = reader.ReadData(Count * CbPerCount);
        }

        internal override int DataLength =>
            + 2 // count
            + Data.Length;

        internal override void SerializeInternal(ref TdsPayloadWriter writer, TvpSerializationContext context)
        {
            writer.WriteInt16LE(Count);
            writer.WriteData(Data);
        }
    }
}
