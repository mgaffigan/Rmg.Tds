using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public enum RpcRequestBatchFlag
    {
        None = 0,
        Pre72Continuation = 0x80,
        Continuation = 0xff,
        NoExec = 0xfe
    }
}
