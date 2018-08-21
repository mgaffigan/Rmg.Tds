using Rmg.Tds.Protocol.DataTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public class TdsTypeInfo
    {
        private readonly TdsVersion TdsVersion;

        public TdsDataTypeInfo Type { get; }
        public int? Precision { get; }
        public int? Scale { get; }
        public int? Length { get; }
        public TdsTypeCollation? Collation { get; }

        public XmlTypeInfo XmlTypeInfo { get; }
        public UdtTypeInfo UdtTypeInfo { get; }

        public TvpTypeInfo TvpTypeInfo { get; }

        internal TdsTypeInfo(ref TdsPayloadReader reader, TdsVersion version, bool isPartOfColMetadaData)
        {
            this.TdsVersion = version;

            this.Type = TdsDataTypeInfo.GetForType((TdsDataType)reader.ReadByte());

            if (Type.ParseMode != TdsDataTypeParsingMode.Table)
            {
                if (Type.HasPrecision)
                {
                    this.Precision = reader.ReadByte();
                }
                if (Type.HasScale)
                {
                    this.Scale = reader.ReadByte();
                }
                if (Type.HasLength)
                {
                    if (Type.ParseMode == TdsDataTypeParsingMode.LongLen)
                    {
                        this.Length = reader.ReadInt32LE();
                    }
                    else if (Type.ParseMode == TdsDataTypeParsingMode.UShortLen)
                    {
                        this.Length = reader.ReadInt16LE();
                    }
                    else if (Type.ParseMode == TdsDataTypeParsingMode.ByteLen)
                    {
                        this.Length = reader.ReadByte();
                    }
                    else throw new NotSupportedException();
                }
                if (Type.HasCollation)
                {
                    this.Collation = new TdsTypeCollation(ref reader, version);
                }

                if (Type.ParseMode == TdsDataTypeParsingMode.Xml)
                {
                    this.XmlTypeInfo = new XmlTypeInfo(ref reader, version);
                }
                else if (Type.ParseMode == TdsDataTypeParsingMode.Udt)
                {
                    this.UdtTypeInfo = new UdtTypeInfo(ref reader, version, isPartOfColMetadaData);
                }

                if (Type.IsPartLen)
                {
                    const int MAX_SENTINEL = 0xffff;
                    switch (Type.Type)
                    {
                        case TdsDataType.Udt:
                        case TdsDataType.Xml:
                            ValueParser = new PlpVarBinaryParser();
                            break;
                        case TdsDataType.BigVarBinary:
                            ValueParser = Length == MAX_SENTINEL ? (IDataTypeParser)new PlpVarBinaryParser() : new VarbinaryParser();
                            break;
                        case TdsDataType.BigVarChar:
                            ValueParser = Length == MAX_SENTINEL ? (IDataTypeParser)new PlpVarcharParser() : new VarcharParser();
                            break; ;
                        case TdsDataType.NVarChar:
                            ValueParser = Length == MAX_SENTINEL ? (IDataTypeParser)new PlpNVarcharParser() : new NVarcharParser();
                            break;
                        default: throw new NotSupportedException("Unknown PLP Type");
                    }
                }
                else
                {
                    ValueParser = Type.ValueParser;
                }
            }
            else
            {
                this.TvpTypeInfo = new TvpTypeInfo(ref reader, version);
            }
        }

        internal int GetSerializedValueLength(object value)
        {
            if (Type.Type == TdsDataType.Table)
            {
                return 0;
            }

            return ValueParser.GetSerializedValueLength(value);
        }

        internal void SerializeValue(ref TdsPayloadWriter writer, object value)
        {
            if (Type.Type == TdsDataType.Table)
            {
                return;
            }

            ValueParser.SerializeValue(ref writer, value);
        }

        internal object DeserializeValue(ref TdsPayloadReader reader)
        {
            if (Type.Type == TdsDataType.Table)
            {
                return TvpTypeInfo.Rows;
            }

            return ValueParser.DeserializeValue(ref reader);
        }

        internal int SerializedLength
        {
            get
            {
                if (Type.Type == TdsDataType.Table)
                {
                    return 1 /* type */
                        + TvpTypeInfo.SerializedLength;
                }

                return
                    1 /* type */
                    + (Type.HasPrecision ? 1 : 0)
                    + (Type.HasScale ? 1 : 0)
                    + (Collation?.SerializedLength ?? 0)
                    + (Type.HasLength ? (
                        Type.ParseMode == TdsDataTypeParsingMode.LongLen ? 4 :
                        Type.ParseMode == TdsDataTypeParsingMode.UShortLen ? 2 :
                        Type.ParseMode == TdsDataTypeParsingMode.ByteLen ? 1 :
                        throw new NotSupportedException()) : 0)
                    + (Type.ParseMode == TdsDataTypeParsingMode.Xml ? XmlTypeInfo.SerializedLength : 0)
                    + (Type.ParseMode == TdsDataTypeParsingMode.Udt ? UdtTypeInfo.SerializedLength : 0);
            }
        }

        internal IDataTypeParser ValueParser { get; }

        internal void Serialize(ref TdsPayloadWriter writer)
        {
            writer.WriteByte((byte)Type.Type);

            if (Type.Type != TdsDataType.Table)
            {
                if (Type.HasPrecision)
                {
                    writer.WriteByte((byte)Precision.Value);
                }
                if (Type.HasScale)
                {
                    writer.WriteByte((byte)Scale.Value);
                }
                if (Length != null)
                {
                    if (Type.ParseMode == TdsDataTypeParsingMode.LongLen)
                    {
                        writer.WriteInt32LE(Length.Value);
                    }
                    else if (Type.ParseMode == TdsDataTypeParsingMode.UShortLen)
                    {
                        writer.WriteInt16LE(Length.Value);
                    }
                    else if (Type.ParseMode == TdsDataTypeParsingMode.ByteLen)
                    {
                        writer.WriteByte((byte)Length.Value);
                    }
                    else throw new NotSupportedException();
                }
                if (Type.HasCollation)
                {
                    Collation.Value.Serialize(ref writer);
                }

                if (Type.ParseMode == TdsDataTypeParsingMode.Xml)
                {
                    XmlTypeInfo.Serialize(ref writer);
                }
                else if (Type.ParseMode == TdsDataTypeParsingMode.Udt)
                {
                    UdtTypeInfo.Serialize(ref writer);
                }
            }
            else
            {
                TvpTypeInfo.Serialize(ref writer, this.TdsVersion);
            }
        }

        public override string ToString()
        {
            return Type.Type.ToString();
        }
    }
}
