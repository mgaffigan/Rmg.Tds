using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class RpcRequestParameter
    {
        public string Name { get; }

        public RpcRequestParameterStatuses Statuses { get; }

        public TdsTypeInfo TypeInfo { get; }

        public object Value { get; }

        internal RpcRequestParameter(int nameLen, ref TdsPayloadReader reader, TdsVersion version)
        {
            this.Name = reader.ReadUcs2StringCch(nameLen);
            this.Statuses = (RpcRequestParameterStatuses)reader.ReadByte();
            this.TypeInfo = new TdsTypeInfo(ref reader, version, false);
            this.Value = this.TypeInfo.DeserializeValue(ref reader);
        }

        public RpcRequestParameter(string name, RpcRequestParameterStatuses statuses, TdsTypeInfo typeInfo, object value)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Statuses = statuses;
            this.TypeInfo = typeInfo;
            this.Value = value;
        }

        public RpcRequestParameter WithNewValue(object value)
        {
            if (Value.GetType() != value.GetType())
            {
                throw new ArgumentOutOfRangeException($"Replacement value type does not match existing type.  Existing: {Value.GetType()}, new: {value.GetType()}");
            }
            var typeInfo = TypeInfo;
            if (typeInfo.Type.HasLength)
            {
                typeInfo = typeInfo.ForNewValue(value);
            }

            return new RpcRequestParameter(Name, Statuses, typeInfo, value);
        }

        public int SerializedLength =>
            1 /* name len */ + (Name.Length * 2)
            + 1 /* status */
            + TypeInfo.SerializedLength
            + TypeInfo.GetSerializedValueLength(Value);

        internal void Serialize(ref TdsPayloadWriter writer)
        {
            writer.WriteByte((byte)Name.Length);
            writer.WriteUcs2String(Name);
            writer.WriteByte((byte)Statuses);
            TypeInfo.Serialize(ref writer);
            TypeInfo.SerializeValue(ref writer, Value);
        }

        public override string ToString()
        {
            if (TypeInfo.Type.Type == TdsDataType.Table)
            {
                return "<<TVP>>";
            }
            else
            {
                return $"{TypeInfo} {Value}";
            }
        }
    }
}
