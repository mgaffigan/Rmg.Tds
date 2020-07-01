using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Rmg.Tds.Protocol.Ssrp
{
    public sealed class PublishedEndpoint
    {
        internal PublishedEndpoint(string s)
        {
            var parameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var sParams = s.Split(';');
            if (sParams.Length % 2 != 0)
            {
                throw new ProtocolViolationException($"Unmatched KVP list for instance: '{s}'");
            }

            for (int i = 0; i < sParams.Length; i += 2)
            {
                try
                {
                    parameters.Add(sParams[i], sParams[i + 1]);
                }
                catch
                {
                    throw new ProtocolViolationException($"Duplicate KVP {i} for '{s}'");
                }
            }

            this.Parameters = parameters;
        }

        public IReadOnlyDictionary<string, string> Parameters { get; }

        internal void ToString(StringBuilder sb)
        {
            var isFirst = true;
            foreach (var kvp in Parameters)
            {
                if (!isFirst)
                {
                    sb.Append(';');
                }
                isFirst = false;

                sb.Append(kvp.Key);
                sb.Append(';');
                sb.Append(kvp.Value);
            }
        }
    }
}
