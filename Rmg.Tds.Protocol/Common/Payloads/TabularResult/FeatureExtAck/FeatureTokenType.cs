using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public enum FeatureTokenType
    {
        SessionRecovery = 1,
        FedAuth = 2,
        ColumnEncryption = 4,
        Globaltransactions = 5,
        AzureSqlSupport = 8,
        Terminator = 0xff
    }
}
