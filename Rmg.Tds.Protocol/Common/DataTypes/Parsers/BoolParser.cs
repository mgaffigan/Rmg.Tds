using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal class BoolParser : DataTypeParser<bool>
    {
        public override int GetSerializedValueLength(bool value) => 1;

        public override bool DeserializeValue(ref TdsPayloadReader reader)
        {
            return reader.ReadByte() != 0;
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, bool value)
        {
            writer.WriteByte((byte)(value ? 1 : 0));
        }
    }
}
