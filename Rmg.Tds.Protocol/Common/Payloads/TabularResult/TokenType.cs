using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public enum TokenType
    {
        AlternativeMetadata = 0x88,
        AlternativeRow = 0xD3,
        ColumnMetadata = 0x81,
        ColumnInfo = 0xA5,
        Done = 0xFD,
        DoneProcedure = 0xFE,
        DoneInProc = 0xFF,
        EnvironmentChange = 0xE3,
        Error = 0xAA,
        FeatureExtAck = 0xAE,
        FedAuthInfo = 0xEE,
        Info = 0xAB,
        LoginAcknowledgement = 0xAD,
        NBCRow = 0xD2,
        Offset = 0x78,
        Order = 0xA9,
        ReturnStatus = 0x79,
        ReturnValue = 0xAC,
        Row = 0xD1,
        SSPI = 0xED,
        TableName = 0xA4,
        SessionState = 0xE4,
        Terminator = 0
    }
}
