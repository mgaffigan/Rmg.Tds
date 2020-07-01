using Rmg.Tds.Protocol.Ssrp;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rmg.Ssrp.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
        }

        private static async Task Run()
        {
            var server = IPEndpointParser.Parse("v-dbserver1.intouchpharma.local", 1434);
            using (var client = new UnicastSsrpClient(server.AddressFamily, Encoding.ASCII))
            {
                await client.SendInstanceRequestAsync(server, "PROD");
                var resp = await client.ReadResponseAsync(CancellationToken.None);
            }
        }
    }
}
