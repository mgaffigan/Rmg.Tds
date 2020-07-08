using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal sealed class PlpVarcharParser : DataTypeParser<string>
    {
        private static readonly PlpVarBinaryParser BaseParser = new PlpVarBinaryParser();

        public override string DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            var byteStream = BaseParser.DeserializeValue(ref reader, type);
            if (byteStream == null)
            {
                return null;
            }
            return Encoding.ASCII.GetString(byteStream);
        }

        public override int GetSerializedValueLength(string value, TdsTypeInfo type)
        {
            if (value == null)
            {
                return BaseParser.GetPlpLen(null);
            }

            return BaseParser.GetPlpLen(Encoding.ASCII.GetByteCount(value));
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, string value, TdsTypeInfo type)
        {
            if (value == null)
            {
                BaseParser.SerializeValue(ref writer, null, type);
            }
            else
            {
                var data = Encoding.ASCII.GetBytes(value);
                BaseParser.SerializeValue(ref writer, data, type);
            }
        }
    }

    internal sealed class PlpNVarcharParser : DataTypeParser<string>
    {
        private static readonly PlpVarBinaryParser BaseParser = new PlpVarBinaryParser();

        public override string DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            var byteStream = BaseParser.DeserializeValue(ref reader, type);
            if (byteStream == null)
            {
                return null;
            }
            return Encoding.Unicode.GetString(byteStream);
        }

        public override int GetSerializedValueLength(string value, TdsTypeInfo type)
        {
            if (value == null)
            {
                return BaseParser.GetPlpLen(null);
            }

            return BaseParser.GetPlpLen(Encoding.Unicode.GetByteCount(value));
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, string value, TdsTypeInfo type)
        {
            if (value == null)
            {
                BaseParser.SerializeValue(ref writer, null, type);
            }
            else
            {
                var data = Encoding.Unicode.GetBytes(value);
                BaseParser.SerializeValue(ref writer, data, type);
            }
        }
    }
}
