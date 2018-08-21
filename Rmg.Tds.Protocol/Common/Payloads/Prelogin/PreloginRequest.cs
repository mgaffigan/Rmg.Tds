using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol.Payloads
{
    public sealed class PreloginRequest : TdsMessagePayload
    {
        public IReadOnlyList<PreloginOption> Options { get; }

        public PreloginRequest(TdsMessage message)
            : base(message, TdsPacketType.PreLogin)
        {
            ReadOnlySpan<byte> data = message.Data;

            Options = PreloginOption.Deserialize(data);
        }

        public PreloginRequest(IEnumerable<PreloginOption> opts, TdsVersion version)
            : base(version, TdsPacketType.PreLogin)
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
