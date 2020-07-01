using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.Ssrp
{
    internal static class RequestType
    {
        public const byte
            ClientBroadcast = 2,
            ClientUnicast = 3,
            ClientUnicastInstance = 4,
            ServerResponse = 5,
            ClientUnicastDac = 0xf;
    }
}
