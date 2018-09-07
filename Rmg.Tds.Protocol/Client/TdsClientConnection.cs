using Rmg.Tds.Protocol.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rmg.Tds.Protocol.Client
{
    internal enum TdsClientState { Prelogin, LoginSent, LoggedIn, FinalState }
    public sealed class TdsClientConnection : IDisposable
    {
        private readonly Socket Socket;
        private bool IsDispoed;
        private readonly object syncWriteCommand;
        private bool IsWriteCommandActive;
        private readonly object syncReadCommand;
        private bool IsReadCommandActive;
        private int PacketSize;
        private int SPID;
        public TdsSerializationContext SerializationContext { get; private set; }

        private TdsClientState State;

        private TdsClientConnection(Socket socket)
        {
            this.syncWriteCommand = new object();
            this.syncReadCommand = new object();
            this.State = TdsClientState.Prelogin;
            this.SerializationContext = new TdsSerializationContext(TdsVersion.Tds74, 0);

            this.Socket = socket;
            this.PacketSize = TdsConstants.DefaultPacketSize;
        }

        public static async Task<TdsClientConnection> ConnectAsync(IPEndPoint target)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(target);
            return new TdsClientConnection(socket);
        }

        public void Dispose()
        {
            if (IsDispoed)
            {
                return;
            }
            IsDispoed = true;

            Socket.Dispose();
        }

        private void AssertAlive()
        {
            if (IsDispoed)
            {
                throw new ObjectDisposedException(nameof(TdsClientConnection));
            }
        }

        public async Task SendAsync(TdsMessage message, CancellationToken ct = default(CancellationToken))
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
                InspectRequestForStateChange(message);

                var packetNo = 0;
                int i = 0;
                do
                {
                    packetNo += 1;
                    // prevent immediate cancellation else caller will not get a "DONE_ERROR" token
                    if (ct.IsCancellationRequested && i > 0)
                    {
                        // write empty packet with IGNORE bit set
                        var ignoreHeaderData = new TdsPacketHeader(
                            message.Type, TdsPacketStatuses.EndOfMessage | TdsPacketStatuses.Ignore,
                            TdsPacketHeader.HeaderLength, SPID, packetNo).ToArray();
                        var sent = await Socket.SendAsync(ignoreHeaderData, SocketFlags.None);
                        if (sent != ignoreHeaderData.Length)
                        {
                            throw new ProtocolViolationException("Write completed partially");
                        }
                        throw new OperationCanceledException();
                    }
                    else
                    {
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
                }
                while (i < message.Data.Length);
            }
            finally
            {
                lock (syncWriteCommand)
                {
                    IsWriteCommandActive = false;
                }
            }
        }

        private void InspectRequestForStateChange(TdsMessage message)
        {
            SerializationContext = message.Context;
            switch (State)
            {
                case TdsClientState.Prelogin:
                    if (message.Type != TdsPacketType.PreLogin)
                    {
                        State = TdsClientState.LoginSent;
                    }
                    break;
                case TdsClientState.LoginSent:
                    if (message.Type != TdsPacketType.Tds7Login)
                    {
                        State = TdsClientState.LoggedIn;
                    }
                    break;
            }
        }

        public async Task<TdsMessage> ReceiveAsync(ITdsClientReceiveSink sink = null)
        {
            lock (syncReadCommand)
            {
                if (IsReadCommandActive)
                {
                    throw new InvalidOperationException("Read request already active");
                }
                IsReadCommandActive = false;
            }
            try
            {
                var result = await ReceiveInternalAsync(sink ?? new NullTdsReceiveSink());
                InspectForEnvChange(result);
                return result;
            }
            finally
            {
                lock (syncReadCommand)
                {
                    IsReadCommandActive = false;
                }
            }
        }

        private async Task<TdsMessage> ReceiveInternalAsync(ITdsClientReceiveSink sink)
        {
            var firstPacket = await ReadPacketAsync();
            bool isHandledByPacketSink = await sink.HandlePacket(firstPacket);

            if (firstPacket.Header.Statuses.HasFlag(TdsPacketStatuses.EndOfMessage))
            {
                // single packet message
                var message = new TdsMessage(firstPacket.Header.Type, firstPacket.Header.Statuses.AsMessageStatuses(),
                    firstPacket.Data, SerializationContext);
                await sink.HandleMessage(message, isHandledByPacketSink);
                return message;
            }
            else
            {
                // multi packet message
                var statuses = firstPacket.Header.Statuses;
                var msResult = new MemoryStream(firstPacket.Header.DataLength * 3);
                msResult.Write(firstPacket.Data);

                while (!statuses.HasFlag(TdsPacketStatuses.EndOfMessage))
                {
                    var secondPacket = await ReadPacketAsync();
                    if (isHandledByPacketSink)
                    {
                        await sink.HandlePacket(secondPacket);
                    }

                    statuses = secondPacket.Header.Statuses;
                    msResult.Write(secondPacket.Data);
                }

                var message = new TdsMessage(firstPacket.Header.Type, statuses.AsMessageStatuses(),
                    msResult.ToArray(), SerializationContext);
                await sink.HandleMessage(message, isHandledByPacketSink);
                return message;
            }
        }

        private async Task<TdsPacket> ReadPacketAsync()
        {
            var header = new TdsPacketHeader(await Socket.ReceiveExactSizeAsync(TdsPacketHeader.HeaderLength));
            var data = await Socket.ReceiveExactSizeAsync(header.DataLength);

            return new TdsPacket(header, data);
        }

        private void InspectForEnvChange(TdsMessage msg)
        {
            if (State == TdsClientState.Prelogin)
            {
                return;
            }

            if (msg.Type == TdsPacketType.TabularResult)
            {
                try
                {
                    var results = msg.DeserializeAs<TabularResultMessage>();
                    foreach (var token in results.Tokens)
                    {
                        if (token is EnvChangeToken ect
                            && ect.Change is PacketSizeChange psc)
                        {
                            this.PacketSize = psc.NewValue;
                        }
                    }
                }
                catch
                {
                    // noop, there are lots of messages we can't parse right now.
                }
            }
        }
    }
}
