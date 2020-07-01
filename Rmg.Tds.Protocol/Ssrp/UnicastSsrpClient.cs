using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rmg.Tds.Protocol.Ssrp
{
    public sealed class UnicastSsrpClient : IDisposable
    {
        private readonly UdpClient Socket;
        private readonly Encoding MbcsEncoding;

        public UnicastSsrpClient(AddressFamily addressFamily, Encoding encoding)
        {
            this.MbcsEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            this.Socket = new UdpClient(addressFamily);
        }

        public void Dispose()
        {
            Socket.Dispose();
        }

        public async Task SendInstanceRequestAsync(IPEndPoint target, string instanceName)
        {
            if (instanceName == null || instanceName.Length > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(instanceName));
            }

            var cbMbcsInstanceName = MbcsEncoding.GetByteCount(instanceName);
            var req = new byte[1 /* header */ + cbMbcsInstanceName + 1 /* null */];
            req[0] = RequestType.ClientUnicastInstance;
            MbcsEncoding.GetBytes(instanceName, 0, instanceName.Length, req, 1);

            await Socket.SendAsync(req, req.Length, target);
        }

        public async Task<IEnumerable<PublishedEndpoint>> ReadResponseAsync(CancellationToken ct)
        {
            var iar = await Socket.ReceiveAsync();
            var response = iar.Buffer;
            if (response[0] != RequestType.ServerResponse)
            {
                throw new ProtocolViolationException($"Unexpected response type 0x{response[0]:x}");
            }

            var respSize = (UInt16)(response[1] | (response[2] << 8));
            if (response.Length < 3 + respSize)
            {
                throw new ProtocolViolationException($"Server response was too short.  Expected {3 + respSize} actual {response.Length}");
            }

            var sResp = MbcsEncoding.GetString(response, 3, respSize);
            var sInstances = sResp.Split(";;");
            return sInstances
                .Take(sInstances.Length - 1)
                .Select(s => new PublishedEndpoint(s))
                .ToArray();
        }
    }
}
