using System;
using System.Threading.Tasks;

namespace Rmg.Tds.Protocol.Server
{
    public interface ITdsServerHandler : IDisposable
    {
        void OnTerminatingException(Exception ex);
        void OnException(Exception ex);
        Task HandleMessageAsync(TdsMessage message, ITdsServerCommandHandle handle);
    }
}