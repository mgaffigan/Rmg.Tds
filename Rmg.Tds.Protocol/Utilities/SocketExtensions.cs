using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Rmg.Tds.Protocol.Utilities
{
    internal static class SocketExtensions
    {
        public static async Task<byte[]> ReceiveExactSizeAsync(this Socket sck, int size, bool cancelOn0 = false)
        {
            try
            {
                var msg = new byte[size];
                for (int i = 0; i < size;)
                {
                    var readBytes = await sck.ReceiveAsync(new Memory<byte>(msg, i, size - i), SocketFlags.None);
                    if (readBytes == 0 && cancelOn0)
                    {
                        throw new OperationCanceledException("End of stream");
                    }
                    if (readBytes < 1)
                    {
                        throw new ProtocolViolationException("Unexpected end of stream");
                    }
                    cancelOn0 = false;
                    i += readBytes;
                }
                return msg;
            }
            catch (SocketException sex) when (cancelOn0 && sex.SocketErrorCode == SocketError.ConnectionReset)
            {
                throw new OperationCanceledException("Connection reset", sex);
            }
        }
    }
}
