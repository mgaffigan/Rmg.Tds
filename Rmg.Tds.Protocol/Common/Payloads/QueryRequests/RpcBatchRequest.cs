using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class RpcBatchRequest : QueryRequest, IReadOnlyList<RpcRequest>
    {
        public IReadOnlyList<RpcRequest> Requests { get; }

        private byte BatchFlag => TdsVersion.Is72OrGreater() ? (byte)0xff : (byte)0x80;

        public RpcBatchRequest(TdsMessage message)
            : base(message, TdsPacketType.RPC)
        {
            var reader = new TdsPayloadReader(((ReadOnlySpan<byte>)message.Data).Slice(HeadersLength));
            var results = new List<RpcRequest>();
            results.Add(new RpcRequest(ref reader, message.Context.TdsVersion));
            while (results.Last().BatchFlag != RpcRequestBatchFlag.None
                && !reader.IsFullyConsumed)
            {
                results.Add(new RpcRequest(ref reader, message.Context.TdsVersion));
            }
            this.Requests = results.AsReadOnly();
        }

        public RpcBatchRequest(IEnumerable<RpcRequest> requests, IEnumerable<QueryHeader> headers, TdsVersion ver)
            : base(headers, ver, TdsPacketType.RPC)
        {
            this.Requests = requests.ToList().AsReadOnly();
            bool isLast = true;
            foreach (var req in Requests.Reverse())
            {
                if (isLast && !(req.BatchFlag == RpcRequestBatchFlag.NoExec || req.BatchFlag == RpcRequestBatchFlag.None))
                {
                    throw new InvalidOperationException("Last request must not have a flag or must have a NoExec flag");
                }
                else if (!isLast && req.BatchFlag == RpcRequestBatchFlag.None)
                {
                    throw new InvalidOperationException("Non-terminal requests must have a continuation flag");
                }
                isLast = false;
            }
        }

        public override int SerializedLength => HeadersLength + Requests.Sum(r => r.SerializedLength);

        public override void Serialize(Span<byte> data)
        {
            SerializeHeaders(data);
            var writer = new TdsPayloadWriter(data.Slice(HeadersLength));
            foreach (var req in Requests)
            {
                req.Serialize(ref writer);
            }
        }

        #region IList<RpcRequest>

        public int Count => ((IReadOnlyList<RpcRequest>)Requests).Count;

        public RpcRequest this[int index] => ((IReadOnlyList<RpcRequest>)Requests)[index];

        public IEnumerator<RpcRequest> GetEnumerator()
        {
            return ((IReadOnlyList<RpcRequest>)Requests).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IReadOnlyList<RpcRequest>)Requests).GetEnumerator();
        }

        #endregion

        public override string ToString()
        {
            var sbResp = new StringBuilder();
            foreach (var request in Requests)
            {
                if (sbResp.Length > 0)
                {
                    sbResp.Append("; ");
                }
                sbResp.Append(request.ToString());
            }
            return sbResp.ToString();
        }
    }
}
