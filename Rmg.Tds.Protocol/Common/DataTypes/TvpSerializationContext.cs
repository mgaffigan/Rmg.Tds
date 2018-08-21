using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol
{
    internal class TvpSerializationContext
    {
        public TvpSerializationContext(TdsVersion version)
        {
        }

        public IReadOnlyList<TvpColumnMetadata> Columns { get; internal set; }
        public IReadOnlyList<TvpToken> OptionalTokens { get; internal set; }

        private TvpColumnMetadata[] ColumnsInResponse;
        public TvpColumnMetadata[] GetColumnsInResponseOrder()
        {
            if (ColumnsInResponse != null)
            {
                return ColumnsInResponse;
            }

            var order = new List<TvpColumnMetadata>();
            var colOrdToken = OptionalTokens.OfType<TvpColumnOrderingToken>().FirstOrDefault();
            if (colOrdToken != null)
            {
                foreach (var ord in colOrdToken.Order)
                {
                    var metadata = Columns[ord - 1 /* 1-indexed for some reason */];
                    if (!metadata.Flags.HasFlag(TvpColumnMetadataFlags.Default))
                    {
                        order.Add(metadata);
                    }
                }
            }
            else
            {
                order.AddRange(Columns.Where(c => !c.Flags.HasFlag(TvpColumnMetadataFlags.Default)));
            }

            return ColumnsInResponse = order.ToArray();
        }
    }
}
