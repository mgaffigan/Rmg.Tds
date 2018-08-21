using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal class ByteLenParser : DataTypeParser<byte[]>
    {
        public override byte[] DeserializeValue(ref TdsPayloadReader reader)
        {
            var len = reader.ReadByte();
            return reader.ReadData(len);
        }

        public override int GetSerializedValueLength(byte[] value)
        {
            return 1 + value.Length;
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, byte[] value)
        {
            writer.WriteByte((byte)value.Length);
            writer.WriteData(value);
        }
    }
}
