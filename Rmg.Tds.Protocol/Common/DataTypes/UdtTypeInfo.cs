namespace Rmg.Tds.Protocol
{
    public sealed class UdtTypeInfo
    {
        internal UdtTypeInfo(ref TdsPayloadReader reader, TdsVersion version, bool isPartOfColMetadaa)
        {
            this.IsPartOfColMetadata = isPartOfColMetadaa;

            if (isPartOfColMetadaa)
            {
                this.MaxByteSize = reader.ReadInt16LE();
            }
            this.DbName = reader.ReadByteCchUcs2String();
            this.SchemaName = reader.ReadByteCchUcs2String();
            this.TypeName = reader.ReadByteCchUcs2String();
            if (isPartOfColMetadaa)
            {
                this.AssemblyQualifiedName = reader.ReadUcs2StringCch(reader.ReadInt16LE());
            }
        }

        public int MaxByteSize { get; }
        public string DbName { get; }
        public string SchemaName { get; }
        public string TypeName { get; }
        public string AssemblyQualifiedName { get; }
        public bool IsPartOfColMetadata { get; }

        internal int SerializedLength => 
            + 3 /* cch's */
            + ((DbName + TypeName + SchemaName).Length * 2)
            + (!IsPartOfColMetadata ? 0 : 
                (
                    2 /* max bytes */
                    + 2 /* cch */ + (AssemblyQualifiedName.Length * 2)
                )
            );

        internal void Serialize(ref TdsPayloadWriter writer)
        {
            if (IsPartOfColMetadata)
            {
                writer.WriteInt16LE(MaxByteSize);
            }
            writer.WriteByteCchUcs2String(DbName);
            writer.WriteByteCchUcs2String(SchemaName);
            writer.WriteByteCchUcs2String(DbName);
            if (IsPartOfColMetadata)
            {
                writer.WriteInt16LE(AssemblyQualifiedName.Length);
                writer.WriteUcs2String(AssemblyQualifiedName);
            }
        }
    }
}