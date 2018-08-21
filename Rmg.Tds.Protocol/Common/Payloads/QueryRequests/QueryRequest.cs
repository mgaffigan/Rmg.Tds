using Rmg.Tds.Protocol.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public abstract class QueryRequest : TdsMessagePayload
    {
        protected readonly int HeadersLength;

        public IReadOnlyList<QueryHeader> Headers { get; }

        public QueryRequest(TdsMessage message, TdsPacketType type)
            : base(message, type)
        {
            ReadOnlySpan<byte> data = message.Data;

            if (TdsVersion.Is72OrGreater())
            {
                // get headers
                this.HeadersLength = data.ReadLittleEndianInt32(0, 4);
                var headerLen = HeadersLength - 4 /* len itself */;
                this.Headers = QueryHeader.Deserialize(data.Slice(4, headerLen));
            }
            else
            {
                this.HeadersLength = 0;
                this.Headers = new List<QueryHeader>().AsReadOnly();
            }
        }

        protected QueryRequest(IEnumerable<QueryHeader> headers, TdsVersion ver, TdsPacketType type)
            : base(ver, type)
        {
            if (ver.Is72OrGreater())
            {
                this.Headers = headers.ToList().AsReadOnly();
                this.HeadersLength = this.Headers.Sum(h => h.Length) + 4 /* len itself */;
            }
            else
            {
                if (headers.Any())
                {
                    throw new NotSupportedException("TDS versions prior to 7.2 do not support Query headers");
                }

                this.HeadersLength = 0;
                this.Headers = new List<QueryHeader>().AsReadOnly();
            }
        }

        protected void SerializeHeaders(Span<byte> data)
        {
            // The ALL_HEADERS rule, the Query Notifications header, and the Transaction Descriptor 
            // header were introduced in TDS 7.2. The Trace Activity header was introduced in TDS 7.4.
            if (!TdsVersion.Is72OrGreater())
            {
                return;
            }

            data.WriteLittleEndianInt32(0, 4, HeadersLength);

            int i = 4;
            foreach (var header in Headers)
            {
                header.Serialize(data.Slice(i, header.Length));
                i += header.Length;
            }

            if (i != HeadersLength)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
