using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal interface IDataTypeParser
    {
        int GetSerializedValueLength(object value);

        void SerializeValue(ref TdsPayloadWriter writer, object value);

        object DeserializeValue(ref TdsPayloadReader reader);
    }

    internal abstract class DataTypeParser<TValue> : IDataTypeParser
    {
        public abstract int GetSerializedValueLength(TValue value);

        public abstract void SerializeValue(ref TdsPayloadWriter writer, TValue value);

        public abstract TValue DeserializeValue(ref TdsPayloadReader reader);

        int IDataTypeParser.GetSerializedValueLength(object value) => GetSerializedValueLength((TValue)value);
        void IDataTypeParser.SerializeValue(ref TdsPayloadWriter writer, object value) => SerializeValue(ref writer, (TValue)value);
        object IDataTypeParser.DeserializeValue(ref TdsPayloadReader reader) => DeserializeValue(ref reader);
    }
}
