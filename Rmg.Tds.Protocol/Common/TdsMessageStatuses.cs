using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    // Must be in sync with TdsPacketStatuses
    [Flags]
    public enum TdsMessageStatuses
    {
        None = 0,
        Ignore = 0x2,
        ResetConnection = 0x8,
        ResetConnectionSkipTran = 0x10,
        All = Ignore | ResetConnection | ResetConnectionSkipTran
    }
}
