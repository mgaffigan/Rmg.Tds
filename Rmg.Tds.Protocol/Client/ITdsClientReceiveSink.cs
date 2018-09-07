using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rmg.Tds.Protocol.Client
{
    public interface ITdsClientReceiveSink
    {
        Task<bool> HandlePacket(TdsPacket packet);

        Task HandleMessage(TdsMessage message, bool wasHandledPerPacket);
    }
}
