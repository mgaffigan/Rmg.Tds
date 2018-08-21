using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class TabularResultMessage : TdsMessagePayload
    {
        public TabularResultMessage(TdsMessage message)
            : base(message, TdsPacketType.TabularResult)
        {
            ReadOnlySpan<byte> data = message.Data;

            this.Tokens = Token.Deserialize(data, message.Context);
        }

        public IReadOnlyList<Token> Tokens { get; }

        public override int SerializedLength => throw new NotSupportedException();

        public override void Serialize(Span<byte> data)
        {
            throw new NotSupportedException();
        }
    }
}
