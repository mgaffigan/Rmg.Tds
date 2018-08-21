using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal class VarbinaryParser : DataTypeParser<byte[]>
    {
        public override byte[] DeserializeValue(ref TdsPayloadReader reader)
        {
            var len = reader.ReadInt16LE();
            if (len == 0xffff)
            {
                return null;
            }
            else
            {
                return reader.ReadData(len);
            }
        }

        public override int GetSerializedValueLength(byte[] value)
        {
            return 2 + (value == null ? 0 : value.Length);
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, byte[] value)
        {
            if (value == null)
            {
                writer.WriteInt16LE(0xffff);
            }
            else
            {
                writer.WriteInt16LE(value.Length);
                writer.WriteData(value);
            }
        }
    }
}
