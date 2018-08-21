using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal sealed class NullParser : DataTypeParser<object>
    {
        public override object DeserializeValue(ref TdsPayloadReader reader)
        {
            return null;
        }

        public override int GetSerializedValueLength(object value) => 0;

        public override void SerializeValue(ref TdsPayloadWriter writer, object value)
        {
            // no-op
        }
    }
}
