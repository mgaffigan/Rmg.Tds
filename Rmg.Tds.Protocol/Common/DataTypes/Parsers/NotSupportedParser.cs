using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal sealed class NotSupportedParser : DataTypeParser<object>
    {
        public override object DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            throw new NotSupportedException();
        }

        public override int GetSerializedValueLength(object value, TdsTypeInfo type)
        {
            throw new NotSupportedException();
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, object value, TdsTypeInfo type)
        {
            throw new NotSupportedException();
        }
    }
}
