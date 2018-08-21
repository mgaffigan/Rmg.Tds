using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public enum QueryHeaderType
    {
        QueryNotifications = 1,
        TransactionDescriptor = 2,
        TraceActivity = 3
    }
}
