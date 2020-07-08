using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal class LongLenParser : DataTypeParser<byte[]>
    {
        public override byte[] DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            var len = reader.ReadInt32LE();
            if (len == -1)
            {
                return null;
            }
            return reader.ReadData(len);
        }

        public override int GetSerializedValueLength(byte[] value, TdsTypeInfo type)
        {
            return 4 
                + (value?.Length ?? 0);
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, byte[] value, TdsTypeInfo type)
        {
            if (value == null)
            {
                writer.WriteInt32LE(-1);
                return;
            }

            writer.WriteInt32LE(value.Length);
            writer.WriteData(value);
        }
    }

    internal class NTextParser : DataTypeParser<string>
    {
        public override string DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            var len = reader.ReadInt32LE();
            if (len == -1)
            {
                return null;
            }
            return reader.ReadUcs2StringCb(len);
        }

        public override int GetSerializedValueLength(string value, TdsTypeInfo type)
        {
            return 4 + (value == null ? 0 : Encoding.Unicode.GetByteCount(value));
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, string value, TdsTypeInfo type)
        {
            if (value == null)
            {
                writer.WriteInt32LE(-1);
                return;
            }

            writer.WriteInt32LE(Encoding.Unicode.GetByteCount(value));
            writer.WriteUcs2String(value);
        }
    }

    internal class TextParser : DataTypeParser<string>
    {
        public override string DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            var len = reader.ReadInt32LE();
            if (len == -1)
            {
                return null;
            }
            return reader.ReadAsciiString(len);
        }

        public override int GetSerializedValueLength(string value, TdsTypeInfo type)
        {
            return 4 + (value?.Length ?? 0);
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, string value, TdsTypeInfo type)
        {
            if (value == null)
            {
                writer.WriteInt32LE(-1);
                return;
            }

            writer.WriteInt32LE(Encoding.ASCII.GetByteCount(value));
            writer.WriteAsciiString(value);
        }
    }
}
