using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Rmg.Tds.Protocol.Server
{
    public sealed class TdsServerListener : IDisposable
    {
        private readonly ITdsServerHandlerFactory Factory;
        private readonly Socket Socket;
        private readonly Task AcceptPromise;
        private bool IsDisposed;

        public TdsServerListener(IPEndPoint ep, ITdsServerHandlerFactory factory)
        {
            if (ep == null)
            {
                throw new ArgumentNullException(nameof(ep));
            }

            this.Factory = factory ?? throw new ArgumentNullException();

            this.Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            this.Socket.Bind(ep);
            this.Socket.Listen(120);

            this.AcceptPromise = Socket_AcceptAsync();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;

            this.Socket.Dispose();
            this.AcceptPromise.Wait();
        }

        private async Task Socket_AcceptAsync()
        {
            while (!IsDisposed)
            {
                Socket accepted;
                try
                {
                    accepted = await Socket.AcceptAsync();
                }
                catch (ObjectDisposedException)
                {
                    // shutdown
                    break;
                }

                try
                {
                    _ = new TdsServerConnection(this, accepted, Factory.CreateHandler());
                }
                catch
                {
                    accepted.Dispose();
                    throw;
                }
            }
        }
    }
}