using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public enum RpcRequestOptionFlags
    {
        WithRecompile = 1,
        NoMetadata = 2,
        ReuseMetadata = 4
    }
}
