using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal sealed class GuidParser : DataTypeParser<Guid?>
    {
        public override int GetSerializedValueLength(Guid? value) => value == null ? 0 : 16;

        public override void SerializeValue(ref TdsPayloadWriter writer, Guid? value)
        {
            writer.WriteByte((byte)GetSerializedValueLength(value));
            if (value != null)
            {
                writer.WriteData(value.Value.ToByteArray());
            }
        }

        public override Guid? DeserializeValue(ref TdsPayloadReader reader)
        {
            var cb = reader.ReadByte();
            if (cb == 0)
            {
                return null;
            }
            else if (cb == 16)
            {
                var data = reader.ReadData(16);
                return new Guid(data);
            }
            else throw new ProtocolViolationException("Guid must have length of 0 or 16");
        }
    }
}
