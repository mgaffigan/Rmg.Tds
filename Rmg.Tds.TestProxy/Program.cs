using Rmg.Tds.Protocol.Server;
using System;
using System.Net;

namespace Rmg.Tds.TestProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new TdsServerListener(new IPEndPoint(IPAddress.Loopback, 3341), new ProxyHandlerFactory());
            Console.WriteLine("Waiting for connection.");
            Console.ReadLine();
        }
    }
}
