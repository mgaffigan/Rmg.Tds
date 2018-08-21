using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class XmlTypeInfo
    {
        internal XmlTypeInfo(ref TdsPayloadReader reader, TdsVersion ver)
        {
            this.IsSchemaPresent = reader.ReadByte() == 1;
            if (IsSchemaPresent)
            {
                this.DbName = reader.ReadByteCchUcs2String();
                this.OwningSchema = reader.ReadByteCchUcs2String();
                this.XmlSchemaCollection = reader.ReadUcs2StringCch(reader.ReadInt16LE());
            }
        }

        public bool IsSchemaPresent { get; }
        public string DbName { get; }
        public string OwningSchema { get; }
        public string XmlSchemaCollection { get; }

        internal int SerializedLength => 1 /* IsPresent */ 
            + 4 /* cch's */
            + ((DbName + OwningSchema + XmlSchemaCollection).Length * 2);

        internal void Serialize(ref TdsPayloadWriter writer)
        {
            writer.WriteByte(IsSchemaPresent ? (byte)1 : (byte)0);
            if (IsSchemaPresent)
            {
                writer.WriteByteCchUcs2String(DbName);
                writer.WriteByteCchUcs2String(OwningSchema);
                writer.WriteInt16LE(XmlSchemaCollection.Length);
                writer.WriteUcs2String(XmlSchemaCollection);
            }
        }
    }
}
