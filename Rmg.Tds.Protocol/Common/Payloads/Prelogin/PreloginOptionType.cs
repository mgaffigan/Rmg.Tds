using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.Payloads
{
    public enum PreloginOptionType
    {
        Version = 0,
        Encryption = 1,
        InstOpt = 2,
        ThreadID = 3,
        MARS = 4,
        TraceID = 5,
        FedAuthRequired = 6,
        NonceOpt = 7,
        Terminator = 0xff
    }
}
