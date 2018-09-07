using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rmg.Tds.Protocol.Client
{
    internal sealed class NullTdsReceiveSink : ITdsClientReceiveSink
    {
        public Task HandleMessage(TdsMessage message, bool wasHandledPerPacket)
        {
            return Task.CompletedTask;
        }

        public Task<bool> HandlePacket(TdsPacket packet)
        {
            return Task.FromResult(false);
        }
    }
}
