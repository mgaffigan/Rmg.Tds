using System.Linq;

namespace Rmg.Tds.Protocol
{
    public class TvpRowToken : TvpToken
    {
        public override TvpTokenType Type => TvpTokenType.Row;

        public TvpCellValue[] Cells { get; }

        internal TvpRowToken(ref TdsPayloadReader reader, TvpSerializationContext context)
        {
            var columns = context.GetColumnsInResponseOrder();
            var cells = new TvpCellValue[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                cells[i] = new TvpCellValue(columns[i], columns[i].TypeInfo.DeserializeValue(ref reader));
            }
            this.Cells = cells;
        }

        internal override int DataLength => Cells.Sum(c => c.SerializedLength);
        internal override void SerializeInternal(ref TdsPayloadWriter writer, TvpSerializationContext context)
        {
            foreach (var cell in Cells)
            {
                cell.Serialize(ref writer);
            }
        }
    }

    public class TvpCellValue
    {
        public TvpCellValue(TvpColumnMetadata cm, object value)
        {
            this.Column = cm;
            this.Value = value;
        }

        public TvpColumnMetadata Column { get; }
        public object Value { get; }

        internal int SerializedLength => Column.TypeInfo.GetSerializedValueLength(Value);

        internal void Serialize(ref TdsPayloadWriter writer)
        {
            Column.TypeInfo.SerializeValue(ref writer, Value);
        }
    }
}