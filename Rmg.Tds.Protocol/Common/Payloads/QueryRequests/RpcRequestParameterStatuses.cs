using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    [Flags]
    public enum RpcRequestParameterStatuses
    {
        ByRef = 1,
        DefaultValue = 2,
        Encrypted = 8,
        None = 0
    }
}
