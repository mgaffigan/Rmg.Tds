using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class FeatureExtAckToken : Token
    {
        public override TokenType Type => TokenType.FeatureExtAck;

        public IReadOnlyList<FeatureToken> Features { get; }

        internal FeatureExtAckToken(ref TdsPayloadReader reader)
        {
            this.Features = FeatureToken.Deserialize(ref reader);
        }
    }
}
