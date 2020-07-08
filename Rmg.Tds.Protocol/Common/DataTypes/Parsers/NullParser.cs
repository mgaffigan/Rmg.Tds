using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal sealed class NullParser : DataTypeParser<object>
    {
        public override object DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type)
        {
            return null;
        }

        public override int GetSerializedValueLength(object value, TdsTypeInfo type) => 0;

        public override void SerializeValue(ref TdsPayloadWriter writer, object value, TdsTypeInfo type)
        {
            // no-op
        }
    }
}
