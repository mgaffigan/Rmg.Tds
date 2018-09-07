using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rmg.Tds.Protocol.Server
{
    public interface ITdsServerCommandHandle
    {
        Func<TdsMessage, Task> MessageHandler { get; set; }

        CancellationToken CancellationToken { get; }

        void Complete();

        Task WriteResponseAsync(TdsMessage message);

        Task WritePartialResponseAsync(TdsPacket packet);
    }
}