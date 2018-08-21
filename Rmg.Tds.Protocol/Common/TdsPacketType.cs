using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public enum TdsPacketType
    {
        SqlBatch = 1,
        PreTds7Login = 2,
        RPC = 3,
        TabularResult = 4,
        Attention = 6,
        BulkLoadData = 7,
        FederatedAuthenticationToken = 8,
        TransactionManagerRequest = 14,
        Tds7Login = 16,
        SSPI = 17,
        PreLogin = 18
    }
}
