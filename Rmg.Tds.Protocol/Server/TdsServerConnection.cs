using Rmg.Tds.Protocol.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rmg.Tds.Protocol.Server
{
    internal class TdsServerConnection
    {
        private readonly TdsServerListener Parent;
        private readonly Socket Socket;
        private ITdsServerHandler Handler;
        private Task ReadPromise;
        private readonly object syncWriteCommand;
        private bool IsWriteCommandActive;
        private readonly object syncCurrentCommand;
        private TdsServerCommandHandle CurrentCommand;
        private int PacketSize;
        private int SPID;
        private TdsSerializationContext SerializationContext;

        public TdsServerConnection(TdsServerListener parent, Socket socket, ITdsServerHandler handler)
        {
            this.syncWriteCommand = new object();
            this.syncCurrentCommand = new object();
            this.SerializationContext = new TdsSerializationContext(TdsVersion.Tds74, 0);

            this.Parent = parent;
            this.Socket = socket;
            this.Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this.PacketSize = TdsConstants.DefaultPacketSize;

            Socket_ReadAsync();
        }

        private async void Socket_ReadAsync()
        {
            try
            {
                while (true)
                {
                    var message = await ReadMessageAsync();

                    bool newCommand;
                    TdsServerCommandHandle handle;
                    lock (syncCurrentCommand)
                    {
                        handle = CurrentCommand;
                        newCommand = handle == null;
                        if (newCommand)
                        {
                            CurrentCommand = handle = new TdsServerCommandHandle(this);
                        }
                    }

                    ThreadPool.QueueUserWorkItem(async _1 =>
                    {
                        try
                        {
                            if (newCommand)
                            {
                                await Handler.HandleMessageAsync(message, handle);
                            }
                            else
                            {
                                await handle.HandleMessageAsync(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Handler.OnTerminatingException(ex);
                            OnProtocolViolation();
                        }
                    }, null);
                }
            }
            catch (OperationCanceledException)
            {
                Handler.Dispose();
            }
            catch (Exception ex)
            {
                Handler.OnTerminatingException(ex);
            }
            finally
            {
                try
                {
                    Socket.Dispose();
                }
                catch (Exception ex)
                {
                    Handler.OnException(ex);
                }
            }
        }

        private void OnProtocolViolation()
        {
            try
            {
                Socket.Dispose();
            }
            catch (Exception ex)
            {
                Handler.OnException(ex);
            }
        }

        private async Task<TdsMessage> ReadMessageAsync()
        {
            var firstPacket = await ReadPacketAsync(cancelOn0: true);
            if (firstPacket.Header.Statuses.HasFlag(TdsPacketStatuses.EndOfMessage))
            {
                return new TdsMessage(firstPacket.Header.Type, firstPacket.Header.Statuses.AsMessageStatuses(),
                    firstPacket.Data, SerializationContext);
            }

            var statuses = firstPacket.Header.Statuses;
            var msResult = new MemoryStream(firstPacket.Header.DataLength * 3);
            msResult.Write(firstPacket.Data);

            while (!statuses.HasFlag(TdsPacketStatuses.EndOfMessage))
            {
                var secondPacket = await ReadPacketAsync();

                if (secondPacket.Header.Type != firstPacket.Header.Type)
                {
                    throw new ProtocolViolationException("Invalid change of type mid-message");
                }
                if (secondPacket.Header.SPID != firstPacket.Header.SPID)
                {
                    throw new ProtocolViolationException("Invalid change of SPID mid-message");
                }

                statuses |= secondPacket.Header.Statuses & (TdsPacketStatuses.EndOfMessage | TdsPacketStatuses.Ignore);
                msResult.Write(secondPacket.Data);
            }

            return new TdsMessage(firstPacket.Header.Type, statuses.AsMessageStatuses(),
                msResult.ToArray(), SerializationContext);
        }

        private async Task<TdsPacket> ReadPacketAsync(bool cancelOn0 = false)
        {
            var header = new TdsPacketHeader(await Socket.ReceiveExactSizeAsync(TdsPacketHeader.HeaderLength, cancelOn0));
            var data = await Socket.ReceiveExactSizeAsync(header.DataLength);

            return new TdsPacket(header, data);
        }

        private class TdsServerCommandHandle : ITdsServerCommandHandle
        {
            private TdsServerConnection Parent;
            private readonly CancellationTokenSource CTS;

            public CancellationToken CancellationToken => CTS.Token;
            public Func<TdsMessage, Task> MessageHandler { get; set; }

            public TdsServerCommandHandle(TdsServerConnection parent)
            {
                this.Parent = parent ?? throw new ArgumentNullException(nameof(parent));
                this.CTS = new CancellationTokenSource();
            }

            internal async Task HandleMessageAsync(TdsMessage message)
            {
                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                var mh = MessageHandler;
                if (mh != null)
                {
                    await mh(message);
                }
                else if (message.Type == TdsPacketType.Attention)
                {
                    CTS.Cancel();
                }
                else
                {
                    throw new ProtocolViolationException("Unexpected message type");
                }
            }

            public void Complete()
            {
                lock (Parent.syncCurrentCommand)
                {
                    if (this != Parent.CurrentCommand)
                    {
                        throw new InvalidOperationException("Invalid command handle");
                    }

                    CTS.Token.ThrowIfCancellationRequested();

                    Parent.CurrentCommand = null;
                }
            }

            public Task WriteResponseAsync(TdsMessage message)
            {
                CTS.Token.ThrowIfCancellationRequested();

                return Parent.WriteResponseAsync(message);
            }

            public Task WritePartialResponseAsync(TdsPacket packet)
            {
                CTS.Token.ThrowIfCancellationRequested();

                return Parent.WriteResponseAsync(packet);
            }
        }

        private async Task WriteResponseAsync(TdsMessage message)
        {
            await SyncWrite(async () =>
            {
                this.SerializationContext = message.Context;

                var packetNo = 0;
                int i = 0;
                do
                {
                    packetNo += 1;
                    var iEnd = Math.Min(i + PacketSize - TdsPacketHeader.HeaderLength, message.Data.Length);
                    var length = iEnd - i;
                    bool isLast = iEnd >= message.Data.Length;

                    var status = ((TdsPacketStatuses)message.Statuses) | (isLast ? TdsPacketStatuses.EndOfMessage : 0);
                    var header = new TdsPacketHeader(message.Type, status, length + TdsPacketHeader.HeaderLength, SPID, packetNo);

                    var packetData = new[]
                    {
                        new ArraySegment<byte>(header.ToArray()),
                        new ArraySegment<byte>(message.Data, i, length)
                    };
                    var sent = await Socket.SendAsync(packetData, SocketFlags.None);
                    if (sent != header.Length)
                    {
                        throw new ProtocolViolationException("Write completed partially");
                    }
                    i = iEnd;
                }
                while (i < message.Data.Length);
            });
        }

        private async Task WriteResponseAsync(TdsPacket packet)
        {
            await SyncWrite(async () =>
            {
                var packetData = new[]
                {
                    new ArraySegment<byte>(packet.Header.ToArray()),
                    new ArraySegment<byte>(packet.Data)
                };
                var sent = await Socket.SendAsync(packetData, SocketFlags.None);
                if (sent != packet.Header.Length)
                {
                    throw new ProtocolViolationException("Write completed partially");
                }
            });
        }

        private async Task SyncWrite(Func<Task> func)
        {
            lock (syncWriteCommand)
            {
                if (IsWriteCommandActive)
                {
                    throw new InvalidOperationException("Write request already active");
                }
                IsWriteCommandActive = false;
            }
            try
            {
                await func();
            }
            finally
            {
                lock (syncWriteCommand)
                {
                    IsWriteCommandActive = false;
                }
            }
        }
    }
}