using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public abstract class FeatureToken
    {
        public abstract FeatureTokenType Type { get; }

        internal static IReadOnlyList<FeatureToken> Deserialize(ref TdsPayloadReader reader)
        {
            var results = new List<FeatureToken>();
            FeatureTokenType type;
            while ((type = (FeatureTokenType)reader.ReadByte()) != FeatureTokenType.Terminator)
            {
                var len = reader.ReadInt32LE();
                results.Add(DeserializeToken(ref reader, type, len));
            }
            return results.AsReadOnly();
        }

        private static FeatureToken DeserializeToken(ref TdsPayloadReader reader, FeatureTokenType type, int len)
        {
            switch (type)
            {
                default:
                    return new UnknownFeatureToken(type, len, ref reader);
            }
        }
    }

    public sealed class UnknownFeatureToken : FeatureToken
    {
        public byte[] Data { get; }

        public override FeatureTokenType Type { get; }

        internal UnknownFeatureToken(FeatureTokenType type, int len, ref TdsPayloadReader reader)
        {
            this.Data = reader.ReadData(len);
        }
    }
}
