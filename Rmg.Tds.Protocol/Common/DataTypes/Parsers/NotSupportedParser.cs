using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal sealed class NotSupportedParser : DataTypeParser<object>
    {
        public override object DeserializeValue(ref TdsPayloadReader reader)
        {
            throw new NotSupportedException();
        }

        public override int GetSerializedValueLength(object value)
        {
            throw new NotSupportedException();
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, object value)
        {
            throw new NotSupportedException();
        }
    }
}
