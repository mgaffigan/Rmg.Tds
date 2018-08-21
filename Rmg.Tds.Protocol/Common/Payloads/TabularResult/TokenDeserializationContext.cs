using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    internal sealed class TokenDeserializationContext
    {
        public TdsSerializationContext Context { get; }

        public TdsVersion TdsVersion => Context.TdsVersion;

        public TokenDeserializationContext(TdsSerializationContext tdsContext)
        {
            this.Context = tdsContext;
        }
    }
}
