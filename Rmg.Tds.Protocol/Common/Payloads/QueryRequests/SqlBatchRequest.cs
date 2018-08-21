using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class SqlBatchRequest : QueryRequest
    {
        public SqlBatchRequest(TdsMessage message) 
            : base(message, TdsPacketType.SqlBatch)
        {
            ReadOnlySpan<byte> data = message.Data;

            var batchText = data.Slice(HeadersLength);
            this.CommandText = Encoding.Unicode.GetString(batchText);
        }

        public SqlBatchRequest(IEnumerable<QueryHeader> headers, string command, TdsVersion version)
            : base(headers, version, TdsPacketType.SqlBatch)
        {
            this.CommandText = command ?? throw new ArgumentNullException(nameof(command));
        }

        public string CommandText { get; }

        public override int SerializedLength => HeadersLength + Encoding.Unicode.GetByteCount(CommandText);

        public override void Serialize(Span<byte> data)
        {
            SerializeHeaders(data);
            Encoding.Unicode.GetBytes(CommandText, data.Slice(HeadersLength));
        }
    }
}
