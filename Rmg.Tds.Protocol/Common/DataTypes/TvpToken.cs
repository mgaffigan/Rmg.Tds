using System;
using System.Collections.Generic;

namespace Rmg.Tds.Protocol
{
    public abstract class TvpToken
    {
        public abstract TvpTokenType Type { get; }
        
        internal static IReadOnlyList<TvpToken> Deserialize(ref TdsPayloadReader reader, TvpSerializationContext context)
        {
            var result = new List<TvpToken>();
            TvpTokenType type;
            while ((type = (TvpTokenType)reader.ReadByte()) != TvpTokenType.Terminator)
            {
                result.Add(DeserializeToken(type, ref reader, context));
            }
            return result;
        }

        private static TvpToken DeserializeToken(TvpTokenType type, ref TdsPayloadReader reader, TvpSerializationContext context)
        {
            switch (type)
            {
                case TvpTokenType.ColumnOrdering:
                    return new TvpColumnOrderingToken(ref reader);
                case TvpTokenType.OrderUnique:
                    return new TvpOrderUniqueOptionToken(ref reader);
                case TvpTokenType.Row:
                    return new TvpRowToken(ref reader, context);
                default:
                    throw new NotSupportedException($"Unknown TVP Token type {type}");
            }
        }

        internal abstract int DataLength { get; }
        internal int SerializedLength => 1 /* Type */ + DataLength;

        internal abstract void SerializeInternal(ref TdsPayloadWriter writer, TvpSerializationContext context);
        internal void Serialize(ref TdsPayloadWriter writer, TvpSerializationContext context)
        {
            writer.WriteByte((byte)Type);
            SerializeInternal(ref writer, context);
        }
    }
}