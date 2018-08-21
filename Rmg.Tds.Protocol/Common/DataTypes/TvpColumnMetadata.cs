using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public class TvpColumnMetadata
    {
        internal TvpColumnMetadata(ref TdsPayloadReader reader, TdsVersion version)
        {
            this.UserType = reader.ReadInt32LE();
            this.Flags = (TvpColumnMetadataFlags)reader.ReadInt16LE();
            this.TypeInfo = new TdsTypeInfo(ref reader, version, true);
            this.Name = reader.ReadByteCchUcs2String();
        }

        public int UserType { get; }
        public TvpColumnMetadataFlags Flags { get; }
        public TdsTypeInfo TypeInfo { get; }
        public string Name { get; }

        internal static IReadOnlyList<TvpColumnMetadata> Deserialize(ref TdsPayloadReader reader, TdsVersion version)
        {
            var count = reader.ReadInt16LE();
            var results = new List<TvpColumnMetadata>();
            if (count != 0xffff /* null token? */)
            {
                for (int i = 0; i < count; i++)
                {
                    results.Add(new TvpColumnMetadata(ref reader, version));
                }
            }
            return results.AsReadOnly();
        }

        internal int SerializedLength => 
            4 // Usertype
            + 2 // flgas
            + TypeInfo.SerializedLength 
            + 1 /* cch */ + Encoding.Unicode.GetByteCount(Name);

        internal void Serialize(ref TdsPayloadWriter writer)
        {
            writer.WriteInt32LE(UserType);
            writer.WriteInt16LE((int)Flags);
            TypeInfo.Serialize(ref writer);
            writer.WriteByteCchUcs2String(Name);
        }
    }
}