using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal sealed class Int1Parser : DataTypeParser<byte>
    {
        public override int GetSerializedValueLength(byte value) => 1;

        public override byte DeserializeValue(ref TdsPayloadReader reader)
        {
            return reader.ReadByte();
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, byte value)
        {
            writer.WriteByte(value);
        }
    }

    internal sealed class Int2Parser : DataTypeParser<short>
    {
        public override int GetSerializedValueLength(short value) => 2;

        public override short DeserializeValue(ref TdsPayloadReader reader)
        {
            return (short)reader.ReadInt16LE();
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, short value)
        {
            writer.WriteInt16LE(value);
        }
    }

    internal sealed class Int4Parser : DataTypeParser<int>
    {
        public override int GetSerializedValueLength(int value) => 4;

        public override int DeserializeValue(ref TdsPayloadReader reader)
        {
            return reader.ReadInt32LE();
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, int value)
        {
            writer.WriteInt32LE(value);
        }
    }

    internal sealed class Int8Parser : DataTypeParser<ulong>
    {
        public override int GetSerializedValueLength(ulong value) => 8;

        public override ulong DeserializeValue(ref TdsPayloadReader reader)
        {
            return reader.ReadInt64LE();
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, ulong value)
        {
            writer.WriteInt64LE(value);
        }
    }

    internal sealed class NullableIntParser : DataTypeParser<object>
    {
        public override int GetSerializedValueLength(object value)
        {
            if (value == null)
            {
                return 1 + 0;
            }
            else if (value is byte || value is sbyte)
            {
                return 1 + 1;
            }
            else if (value is short || value is ushort)
            {
                return 1 + 2;
            }
            else if (value is int || value is uint)
            {
                return 1 + 4;
            }
            else if (value is long || value is ulong)
            {
                return 1 + 8;
            }
            else throw new NotSupportedException();
        }

        public override object DeserializeValue(ref TdsPayloadReader reader)
        {
            switch (reader.ReadByte())
            {
                case 0: return null;
                case 1: return reader.ReadByte();
                case 2: return (short)reader.ReadInt16LE();
                case 4: return reader.ReadInt32LE();
                case 8: return reader.ReadInt64LE();
                default: throw new NotSupportedException();
            }
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, object value)
        {
            var len = GetSerializedValueLength(value) - 1;
            writer.WriteByte((byte)len);
            if (len == 0)
            {
                // nop, null
            }
            else if (len == 1)
            {
                writer.WriteByte(Convert.ToByte(value));
            }
            else if (len <= 4)
            {
                writer.WriteIntLE(len, Convert.ToInt32(value));
            }
            else if (len == 8)
            {
                writer.WriteInt64LE(Convert.ToUInt64(value));
            }
            else throw new NotSupportedException();
        }
    }
}
