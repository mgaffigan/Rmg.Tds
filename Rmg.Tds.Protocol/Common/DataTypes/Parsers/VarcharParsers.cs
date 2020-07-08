using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal sealed class VarcharParser : DataTypeParser<string>
    {
        public override string DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            var len = reader.ReadInt16LE();
            return reader.ReadAsciiString(len);
        }

        public override int GetSerializedValueLength(string value, TdsTypeInfo type)
        {
            return 2 + Encoding.ASCII.GetByteCount(value ?? "");
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, string value, TdsTypeInfo type)
        {
            if (value == null)
            {
                writer.WriteInt16LE(0xffff);
            }
            else
            {
                writer.WriteInt16LE(Encoding.ASCII.GetByteCount(value));
                writer.WriteAsciiString(value);
            }
        }
    }

    internal sealed class NVarcharParser : DataTypeParser<string>
    {
        public override string DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            var len = reader.ReadInt16LE();
            return reader.ReadUcs2StringCb(len);
        }

        public override int GetSerializedValueLength(string value, TdsTypeInfo type)
        {
            return 2 + Encoding.Unicode.GetByteCount(value ?? "");
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, string value, TdsTypeInfo type)
        {
            if (value == null)
            {
                writer.WriteInt16LE(0xffff);
            }
            else
            {
                writer.WriteInt16LE(Encoding.Unicode.GetByteCount(value));
                writer.WriteUcs2String(value);
            }
        }
    }
}
