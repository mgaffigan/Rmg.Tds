using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal interface IDataTypeParser
    {
        int GetSerializedValueLength(object value, TdsTypeInfo type);

        void SerializeValue(ref TdsPayloadWriter writer, object value, TdsTypeInfo type);

        object DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type);
    }

    internal abstract class DataTypeParser<TValue> : IDataTypeParser
    {
        public abstract int GetSerializedValueLength(TValue value, TdsTypeInfo type);

        public abstract void SerializeValue(ref TdsPayloadWriter writer, TValue value, TdsTypeInfo type);

        public abstract TValue DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type);

        int IDataTypeParser.GetSerializedValueLength(object value, TdsTypeInfo type) => GetSerializedValueLength((TValue)value, type);
        void IDataTypeParser.SerializeValue(ref TdsPayloadWriter writer, object value, TdsTypeInfo type) => SerializeValue(ref writer, (TValue)value, type);
        object IDataTypeParser.DeserializeValue(ref TdsPayloadReader reader, TdsTypeInfo type) => DeserializeValue(ref reader, type);
    }
}
