using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public enum TvpColumnMetadataFlags
    {
        Nullable = 1,
        CaseSensitive = 2,
        Updatable_ReadWrite = 0x04,
        Updatable_Unknown = 0x08,
        Identity = 0x10,
        Computed = 0x20,
        FixedLengthClrType = 0x100,
        Default = 0x200
    }
}
