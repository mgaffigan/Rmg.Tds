using Rmg.Tds.Protocol.Server;
using System;
using System.Linq;
using System.Net;

namespace Rmg.Tds.TestProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            var proxyFactory = new ProxyHandlerFactory();
            var server = new TdsServerListener(new IPEndPoint(IPAddress.Loopback, 33041), proxyFactory);
            Console.WriteLine("Waiting for connection.  Press enter to reload translations.");
            while (true)
            {
                Console.ReadLine();
                try
                {
                    var newTable = proxyFactory.ReloadTranslationTable();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Reload successful.  Loaded: {string.Join(", ", newTable.Translations.Select(t => t.Name))}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to reload: {ex}");
                }
            }
        }
    }
}
