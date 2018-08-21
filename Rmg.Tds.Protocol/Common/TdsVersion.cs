using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public enum TdsVersion : uint
    {
        SqlServer70 = 0x7000_0000,
        SqlServer2000 = 0x7100_0000,
        Tds71 = SqlServer2000,
        SqlServer2000Sp1 = 0x7100_0001,
        SqlServer2005 = 0x7209_0002,
        Tds72 = SqlServer2005,
        SqlServer2008 = 0x730a_0003,
        SqlServer2008R2 = 0x730b_0003,
        SqlServer2012 = 0x7400_0004,
        Tds74 = SqlServer2012
    }

    public static class TdsVersionExtensions
    {
        public static bool Is72OrGreater(this TdsVersion v)
        {
            return (((uint)v) >> 24) > 0x71;
        }

        public static bool Is73OrGreater(this TdsVersion v)
        {
            return (((uint)v) >> 24) > 0x72;
        }

        public static bool Is74OrGreater(this TdsVersion v)
        {
            return (((uint)v) >> 24) > 0x73;
        }

        public static bool IsAfter74(this TdsVersion v)
        {
            return (((uint)v) >> 24) > 0x74;
        }
    }
}
