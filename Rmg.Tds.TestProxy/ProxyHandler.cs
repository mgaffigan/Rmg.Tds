using Rmg.Tds.Protocol;
using Rmg.Tds.Protocol.Client;
using Rmg.Tds.Protocol.Payloads;
using Rmg.Tds.Protocol.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rmg.Tds.TestProxy
{
    internal class ProxyHandler : ITdsServerHandler, ITdsClientReceiveSink
    {
        private readonly Func<SqlTranslationTable> GetTranslationTable;
        private readonly Task<TdsClientConnection> UpstreamConnectionPromise;
        private ITdsServerCommandHandle handle;

        public ProxyHandler(Func<SqlTranslationTable> translationTable)
        {
            this.GetTranslationTable = translationTable ?? throw new ArgumentNullException(nameof(translationTable));
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

            RequestCount += 1;
            LastRequest = message.Type;
            message = InspectRequest(message);

            await upstream.SendAsync(message);
        }

        private TdsMessage InspectRequest(TdsMessage msg)
        {
            if (msg.Type == TdsPacketType.PreLogin && RequestCount == 1)
            {
                return ProxyPrelogin(msg, msg.DeserializeAs<PreloginRequest>());
            }
            else if (msg.Type == TdsPacketType.Tds7Login)
            {
                return ProxyLogin(msg.DeserializeAs<Login7Request>());
            }
            else if (msg.Type == TdsPacketType.SqlBatch)
            {
                return ProxySqlBatch(msg, msg.DeserializeAs<SqlBatchRequest>());
            }
            else if (msg.Type == TdsPacketType.RPC)
            {
                return ProxyRpcBatch(msg, msg.DeserializeAs<RpcBatchRequest>());
            }
            else
            {
                return msg;
            }
        }

        private static TdsMessage ProxyPrelogin(TdsMessage msg, PreloginRequest req)
        {
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

        private static TdsMessage ProxyLogin(Login7Request l7r)
        {
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

        private TdsMessage ProxyRpcBatch(TdsMessage msg, RpcBatchRequest sb)
        {
            bool replace = false;
            var replacedRpcRequests = ((IReadOnlyList<RpcRequest>)sb).ToArray();
            for (int i = 0; i < sb.Count; i++)
            {
                var req = sb[i];

                if (req.ProcedureName == "sp_executesql" && req.Parameters.Count > 0 && req.Parameters[0].Value is string commandText)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Request: {0}", commandText);

                    if (IsProxyReplacementForExecuteSql(req, commandText, out var replacement))
                    {
                        replacedRpcRequests[i] = replacement;
                        replace = true;
                    }
                }
                else if (req.ProcedureId == null)
                {
                    var (equivText, paramNames) = GetEquivelentSqlAndParamNames(req);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Request: {0}", equivText);

                    if (IsProxyReplacementForSproc(req, equivText, paramNames, sb.TdsVersion, out var replacement))
                    {
                        replacedRpcRequests[i] = replacement;
                        replace = true;
                    }
                }
            }

            if (!replace)
            {
                return msg;
            }

            return msg.WithReplacedPayload(new RpcBatchRequest(replacedRpcRequests, sb.Headers, sb.TdsVersion));
        }

        private static (string equiv, string param) GetEquivelentSqlAndParamNames(RpcRequest req)
        {
            var paramNamesAndTypes = new StringBuilder();
            var equivelentSql = new StringBuilder();
            equivelentSql.Append("EXEC ");
            equivelentSql.Append(req.ProcedureName);
            for (int p = 0; p < req.Parameters.Count; p++)
            {
                var param = req.Parameters[p];

                if (p == 0)
                {
                    equivelentSql.Append(" ");
                }
                else
                {
                    paramNamesAndTypes.Append(", ");
                    equivelentSql.Append(", ");
                }

                if (!string.IsNullOrWhiteSpace(param.Name))
                {
                    paramNamesAndTypes.Append(param.Name);
                    equivelentSql.Append(param.Name);
                }
                else
                {
                    paramNamesAndTypes.Append(p);
                    equivelentSql.Append(p);
                }

                paramNamesAndTypes.Append(" ");
                paramNamesAndTypes.Append(param.TypeInfo.GetSqlTypeName());
            }
            equivelentSql.Append(";");

            return (equivelentSql.ToString(), paramNamesAndTypes.ToString());
        }

        private bool IsProxyReplacementForExecuteSql(RpcRequest req, string commandText, out RpcRequest replacement)
        {
            if (!GetTranslationTable().TryApply(commandText, out string newCommandText, out string replacementName))
            {
                replacement = null;
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("   Applying replacement {0}: {1}", replacementName, newCommandText.Trim());

            var parameters = req.Parameters.ToList();
            parameters[0] = parameters[0].WithNewValue(newCommandText);
            replacement = req.With(parameters: parameters);
            return true;
        }

        private bool IsProxyReplacementForSproc(RpcRequest req, string commandText, string paramNames, TdsVersion ver, out RpcRequest replacement)
        {
            if (!GetTranslationTable().TryApply(commandText, out string newCommandText, out string replacementName))
            {
                replacement = null;
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("   Applying replacement {0}: {1}", replacementName, newCommandText.Trim());

            var parameters = req.Parameters.ToList();
            parameters.Insert(0, RpcRequestParameter.ForNVarChar("", newCommandText, ver));
            parameters.Insert(1, RpcRequestParameter.ForNVarChar("", paramNames, ver));
            replacement = req.With(procedureName: "sp_executesql", parameters: parameters);
            return true;
        }

        private TdsMessage ProxySqlBatch(TdsMessage msg, SqlBatchRequest sb)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Request: {0}", sb.CommandText);
            if (GetTranslationTable().TryApply(sb.CommandText, out string newCommandText, out string replacementName))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("   Applying replacement {0}: {1}", replacementName, newCommandText.Trim());
                sb = new SqlBatchRequest(sb.Headers, newCommandText, sb.TdsVersion);
                return msg.WithReplacedPayload(sb);
            }
            return msg;
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
