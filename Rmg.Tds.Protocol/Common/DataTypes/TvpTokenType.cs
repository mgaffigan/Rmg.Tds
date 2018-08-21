using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public enum TvpTokenType
    {
        Terminator = 0,
        Row = 1,
        ColumnOrdering = 0x11,
        OrderUnique = 0x10
    }
}
