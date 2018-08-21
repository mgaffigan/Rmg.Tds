using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rmg.Tds.Protocol
{
    public class TvpTypeInfo
    {
        internal TvpTypeInfo(ref TdsPayloadReader reader, TdsVersion version)
        {
            this.DbName = reader.ReadByteCchUcs2String();
            this.OwningSchema = reader.ReadByteCchUcs2String();
            this.TypeName = reader.ReadByteCchUcs2String();

            var context = new TvpSerializationContext(version);
            this.Columns = TvpColumnMetadata.Deserialize(ref reader, version);
            context.Columns = Columns;
            this.OptionTokens = TvpToken.Deserialize(ref reader, context);
            context.OptionalTokens = OptionTokens;

            this.Rows = TvpToken.Deserialize(ref reader, context).Cast<TvpRowToken>().ToList().AsReadOnly();
        }

        public string DbName { get; }
        public string OwningSchema { get; }
        public string TypeName { get; }
        public IReadOnlyList<TvpColumnMetadata> Columns { get; }
        public IReadOnlyList<TvpToken> OptionTokens { get; }

        internal ReadOnlyCollection<TvpRowToken> Rows { get; }

        internal int SerializedLength =>
            3 // cch for dbname, owning, typename
            + ((DbName + OwningSchema + TypeName).Length * 2)
            + 2 /* count of columns */ + Columns.Sum(c => c.SerializedLength)
            + OptionTokens.Sum(c => c.SerializedLength) + 1 /* terminator */
            + Rows.Sum(c => c.SerializedLength) + 1 /* terminator */;

        internal void Serialize(ref TdsPayloadWriter writer, TdsVersion ver)
        {
            writer.WriteByteCchUcs2String(DbName);
            writer.WriteByteCchUcs2String(OwningSchema);
            writer.WriteByteCchUcs2String(TypeName);

            var context = new TvpSerializationContext(ver);
            context.Columns = Columns;
            context.OptionalTokens = OptionTokens;

            writer.WriteInt16LE(Columns.Count);
            foreach (var col in Columns)
            {
                col.Serialize(ref writer);
            }

            foreach (var opt in OptionTokens)
            {
                opt.Serialize(ref writer, context);
            }
            writer.WriteByte((byte)TvpTokenType.Terminator);

            foreach (var row in Rows)
            {
                row.Serialize(ref writer, context);
            }
            writer.WriteByte((byte)TvpTokenType.Terminator);
        }
    }
}