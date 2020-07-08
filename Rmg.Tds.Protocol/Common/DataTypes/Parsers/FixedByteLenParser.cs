using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal class FixedByteLenParser : DataTypeParser<byte[]>
    {
        public override byte[] DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            return reader.ReadData(type.Length.Value);
        }

        public override int GetSerializedValueLength(byte[] value, TdsTypeInfo type)
        {
            if (value.Length != type.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Wrong length");
            }

            return value.Length;
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, byte[] value, TdsTypeInfo type)
        {
            if (value.Length != type.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Wrong length");
            }

            writer.WriteData(value);
        }
    }
}
