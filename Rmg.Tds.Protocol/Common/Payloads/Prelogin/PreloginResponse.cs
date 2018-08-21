using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol.Payloads
{
    public sealed class PreloginResponse : TdsMessagePayload
    {
        public IReadOnlyList<PreloginOption> Options { get; }

        public PreloginResponse(TdsMessage message)
            : base(message, TdsPacketType.TabularResult)
        {
            ReadOnlySpan<byte> data = message.Data;

            Options = PreloginOption.Deserialize(data);
        }

        public PreloginResponse(IEnumerable<PreloginOption> opts, TdsVersion version)
            : base(version, TdsPacketType.TabularResult)
        {
            this.Options = opts.ToList().AsReadOnly();
        }

        public override int SerializedLength => Options.GetSerializedSize();

        public override void Serialize(Span<byte> data)
        {
            Options.Serialize(data);
        }
    }
}
