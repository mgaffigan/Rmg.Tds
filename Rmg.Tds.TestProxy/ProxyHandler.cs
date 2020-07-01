using Rmg.Tds.Protocol;
using Rmg.Tds.Protocol.Client;
using Rmg.Tds.Protocol.Payloads;
using Rmg.Tds.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Rmg.Tds.TestProxy
{
    internal class ProxyHandler : ITdsServerHandler, ITdsClientReceiveSink
    {
        private readonly SqlTranslationTable TranslationTable;
        private readonly Task<TdsClientConnection> UpstreamConnectionPromise;
        private ITdsServerCommandHandle handle;

        public ProxyHandler(SqlTranslationTable translationTable)
        {
            this.TranslationTable = translationTable ?? throw new ArgumentNullException(nameof(translationTable));
            this.UpstreamConnectionPromise = TdsClientConnection.ConnectAsync(new IPEndPoint(IPAddress.Parse("10.200.36.49"), 16143));
        }

        public async Task HandleMessageAsync(TdsMessage message, ITdsServerCommandHandle handle)
        {
            this.handle = handle;
            handle.MessageHandler = SendMessageInternalAsync;
            await SendMessageInternalAsync(message);
            ReadAsync();
        }

        private async void ReadAsync()
        {
            while (true)
            {
                var target = await UpstreamConnectionPromise;
                await target.ReceiveAsync(this);
            }
        }

        async Task<bool> ITdsClientReceiveSink.HandlePacket(TdsPacket packet)
        {
            if (LastRequest == TdsPacketType.SqlBatch
                || LastRequest == TdsPacketType.RPC)
            {
                // do not inspect responses 
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"Forwarding packet {{{packet.Header}}} without inspection");
                await handle.WritePartialResponseAsync(packet);
                return true;
            }
            else
            {
                return false;
            }
        }

        async Task ITdsClientReceiveSink.HandleMessage(TdsMessage resp, bool wasHandledPerPacket)
        {
            // even if we forwarded the packets, we still need to update our state
            resp = InspectResponse(resp);
            if (!wasHandledPerPacket)
            {
                await handle.WriteResponseAsync(resp);
            }
        }

        private int RequestCount;
        private TdsPacketType LastRequest;
        private TdsMessage InspectResponse(TdsMessage resp)
        {
            if (LastRequest == TdsPacketType.PreLogin && resp.Type == TdsPacketType.TabularResult && RequestCount == 1)
            {
                // PRELOGIN Response
                var preloginResp = resp.DeserializeAs<PreloginResponse>();
                var newOpts = new List<PreloginOption>(preloginResp.Options.Count);
                foreach (var opt in preloginResp.Options)
                {
                    if (opt is PreloginEncryptionOption)
                    {
                        newOpts.Add(new PreloginEncryptionOption(TdsEncryptionMode.NotSupported));
                    }
                    else if (opt is PreloginMarsOption)
                    {
                        newOpts.Add(new PreloginMarsOption(false));
                    }
                    else
                    {
                        newOpts.Add(opt);
                    }
                }
                return resp.WithReplacedPayload(new PreloginResponse(newOpts, preloginResp.TdsVersion));
            }
            else
            {
                return resp;
            }
        }

        private async Task SendMessageInternalAsync(TdsMessage message)
        {
            var upstream = await UpstreamConnectionPromise;
            //Console.ForegroundColor = ConsoleColor.Gray;
            //Console.WriteLine($"Received {message.Type} from client");

            RequestCount += 1;
            LastRequest = message.Type;
            message = InspectRequest(message);

            await upstream.SendAsync(message);
        }

        private TdsMessage InspectRequest(TdsMessage msg)
        {
            if (msg.Type == TdsPacketType.PreLogin && RequestCount == 1)
            {
                // PRELOGIN Response
                var req = msg.DeserializeAs<PreloginRequest>();
                var newOpts = new List<PreloginOption>(req.Options.Count);
                foreach (var opt in req.Options)
                {
                    if (opt is PreloginEncryptionOption)
                    {
                        newOpts.Add(new PreloginEncryptionOption(TdsEncryptionMode.NotSupported));
                    }
                    else if (opt is PreloginMarsOption)
                    {
                        newOpts.Add(new PreloginMarsOption(false));
                    }
                    else
                    {
                        newOpts.Add(opt);
                    }
                }
                return msg.WithReplacedPayload(new PreloginRequest(newOpts, req.TdsVersion));
            }
            else if (msg.Type == TdsPacketType.Tds7Login)
            {
                var l7r = msg.DeserializeAs<Login7Request>();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("New login\r\n"
                    + "\tApplication:   {0}\r\n"
                    + "\tHostname:      {1}\r\n"
                    + "\tDatabase:      {2}\r\n"
                    + "\tTDS Version:   {3} (0x{6:x8})\r\n"
                    + "\tClient PID:    {4}\r\n"
                    + "\tUsername:      {5}",
                    l7r.AppName, l7r.HostName, l7r.Database, l7r.TdsVersion, l7r.ClientPid, l7r.Username, (uint)l7r.TdsVersion);

                if (l7r.TdsVersion.IsAfter74())
                {
                    l7r = new Login7Request(TdsVersion.Tds74, l7r.PacketSize, l7r.Data);
                }

                return new TdsMessage(l7r, TdsMessageStatuses.None, new TdsSerializationContext(l7r.TdsVersion, 0));
            }
            else if (msg.Type == TdsPacketType.SqlBatch)
            {
                var sb = msg.DeserializeAs<SqlBatchRequest>();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Request: {0}", sb.CommandText);
                if (sb.CommandText.Contains("{now}"))
                {
                    sb = new SqlBatchRequest(sb.Headers, sb.CommandText.Replace("{now}", DateTime.Now.ToString()), sb.TdsVersion);
                    return msg.WithReplacedPayload(sb);
                }
                return msg;
            }
            else if (msg.Type == TdsPacketType.RPC)
            {
                var sb = msg.DeserializeAs<RpcBatchRequest>();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Request: {0}", sb);
                return msg.WithReplacedPayload(sb);
            }
            else
            {
                return msg;
            }
        }

        public void OnException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Exception: {ex}");
        }

        public void OnTerminatingException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failing: {ex}");
        }

        public void Dispose()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Disconnected");
        }
    }
}
