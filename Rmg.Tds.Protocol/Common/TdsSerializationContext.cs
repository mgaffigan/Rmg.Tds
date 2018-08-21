using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public struct TdsSerializationContext
    {
        public TdsVersion TdsVersion { get; }

        public int SPID { get; }

        public TdsSerializationContext(TdsVersion ver, int spid)
        {
            this.TdsVersion = ver;
            this.SPID = spid;
        }
    }
}
