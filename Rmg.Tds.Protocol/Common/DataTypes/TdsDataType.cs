using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    using Rmg.Tds.Protocol.DataTypes;
    using System.Linq;
    using static TdsDataType;
    using PM = TdsDataTypeParsingMode;

    public enum TdsDataType
    {
        Null = 0x1f,
        Text = 0x23,
        Guid = 0x24,
        VarBinary = 0x25,
        IntN = 0x26,
        VarChar = 0x27,
        Binary = 0x2d,
        Image = 0x22,
        DateN = 0x28,
        TimeN = 0x29,
        DateTime2N = 0x2a,
        DateTimeOffsetN = 0x2b,
        Char = 0x2f,
        Int1 = 0x30,
        Bit = 0x32,
        Int2 = 0x34,
        Decimal = 0x37,
        Int4 = 0x38,
        DateTime4 = 0x3a,
        Float4 = 0x3b,
        Money = 0x3c,
        DateTime = 0x3d,
        Float8 = 0x3e,
        Numeric = 0x3f,
        SSVariant = 0x62,
        NText = 0x63,
        BitN = 0x68,
        DecimalN = 0x6a,
        NumericN = 0x6c,
        FloatN = 0x6d,
        MoneyN = 0x6e,
        DateTimeN = 0x6f,
        Money4 = 0x7a,
        Int8 = 0x7f,
        BigVarBinary = 0xA5,
        BigVarChar = 0xA7,
        BigBinary = 0xAD,
        BigChar = 0xAF,
        NVarChar = 0xe7,
        NChar = 0xef,
        Udt = 0xF0,
        Xml = 0xf1,
        Table = 0xF3
    }

    public struct TdsDataTypeInfo
    {
        private static readonly TdsDataTypeDict KnownTypes = new TdsDataTypeDict()
        {
            { Bit, PM.FixedLen, 1, new BoolParser(), false, false, false, false, false, new SimpleTypeNameFormatter("bit") },
            { DateN, PM.ByteLen, null, new ByteLenParser(), false, false, false, false, true, new SimpleTypeNameFormatter("date") },
            { TimeN, PM.ByteLen, null, new ByteLenParser(), false, false, true, false, true, new ScaleTypeNameFormatter("time") },
            { DateTime2N, PM.ByteLen, null, new FixedByteLenParser(), false, false, true, false, true, new ScaleTypeNameFormatter("datetime2") },
            { DateTimeOffsetN, PM.ByteLen, null, new FixedByteLenParser(), false, false, true, false, true, new ScaleTypeNameFormatter("datetimeoffset") },
            { DecimalN, PM.ByteLen, null, new ByteLenParser(), false, true, true, false, true, new PrecisionScaleTypeNameFormatter("decimal") },
            { NumericN, PM.ByteLen, null, new ByteLenParser(), false, true, true, false, true, new PrecisionScaleTypeNameFormatter("numeric") },
            { Guid, PM.ByteLen, null, new GuidParser(), false, false, false, false, true, new SimpleTypeNameFormatter("uniqueidentifier") },
            { Int1, PM.FixedLen, 1, new Int1Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("tinyint") },
            { Int2, PM.FixedLen, 2, new Int2Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("smallint") },
            { Int4, PM.FixedLen, 4, new Int4Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("int") },
            { DateTime4, PM.FixedLen, 4, new Int4Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("smalldatetime") },
            { Float4, PM.FixedLen, 4, new Int4Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("float(24)") },
            { Money4, PM.FixedLen, 4, new Int4Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("smallmoney") },
            { Money, PM.FixedLen, 8, new Int8Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("money") },
            { DateTime, PM.FixedLen, 8, new Int8Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("datetime") },
            { Float8, PM.FixedLen, 8, new Int8Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("float(53)") },
            { Int8, PM.FixedLen, 8, new Int8Parser(), false, false, false, false, false, new SimpleTypeNameFormatter("bigint") },
            { Image, PM.LongLen, null, new LongLenParser(), false, false, false, false, true, new SimpleTypeNameFormatter("image") },
            { SSVariant, PM.LongLen, null, new LongLenParser(), false, false, false, false, true, new SimpleTypeNameFormatter("sql_variant") },
            { Text, PM.LongLen, null, new TextParser(), false, false, false, true, true, new SimpleTypeNameFormatter("text") },
            { NText, PM.LongLen, null, new NTextParser(), false, false, false, true, true, new SimpleTypeNameFormatter("ntext") },
            { VarBinary, PM.ByteLen, null, new NotSupportedParser(), false, false, false, false, true, new LengthTypeNameFormatter("varbinary") },
            { VarChar, PM.ByteLen, null, new NotSupportedParser(), false, false, false, false, true, new LengthTypeNameFormatter("varbinary") },
            { Binary, PM.ByteLen, null, new NotSupportedParser(), false, false, false, false, true, new LengthTypeNameFormatter("binary") },
            { Char, PM.ByteLen, null, new NotSupportedParser(), false, false, false, false, true, new LengthTypeNameFormatter("char") },
            { Decimal, PM.ByteLen, null, new NotSupportedParser(), false, true, true, false, true, new PrecisionScaleTypeNameFormatter("decimal") },
            { Numeric, PM.ByteLen, null, new NotSupportedParser(), false, true, true, false, true, new PrecisionScaleTypeNameFormatter("numeric") },
            { Table, PM.Table, null, new NotSupportedParser(), false, false, false, false, false, new TableTypeNameFormatter() },
            { Udt, PM.Udt, null, new NotSupportedParser(), true, false, false, false, false, new UdtTypeNameFormatter() },
            { Xml, PM.Xml, null, new NotSupportedParser(), true, false, false, false, false, new SimpleTypeNameFormatter("xml") },
            { BigVarBinary, PM.UShortLen, null, new NotSupportedParser(), true, false, false, false, true, new LengthTypeNameFormatter("varbinary") },
            { BigVarChar, PM.UShortLen, null, new NotSupportedParser(), true, false, false, true, true, new LengthTypeNameFormatter("varchar") },
            { NVarChar, PM.UShortLen, null, new NotSupportedParser(), true, false, false, true, true, new LengthTypeNameFormatter("nvarchar") },
            { IntN, PM.ByteLen, null, new NullableIntParser(), false, false, false, false, true, new IntNTypeNameFormatter() },
            { BitN, PM.ByteLen, null, new NullableIntParser(), false, false, false, false, true, new SimpleTypeNameFormatter("bit") },
            { FloatN, PM.ByteLen, null, new NullableIntParser(), false, false, false, false, true, new FloatTypeNameFormatter() },
            { MoneyN, PM.ByteLen, null, new NullableIntParser(), false, false, false, false, true, new MoneyTypeNameFormatter() },
            { DateTimeN, PM.ByteLen, null, new NullableIntParser(), false, false, false, false, true, new SimpleTypeNameFormatter("datetime") },
            { Null, PM.FixedLen, 0, new NullParser(), false, false, false, false, false, new SimpleTypeNameFormatter("int") },
            { NChar, PM.UShortLen, null, new NVarcharParser(), false, false, false, true, true, new LengthTypeNameFormatter("nchar") },
            { BigBinary, PM.UShortLen, null, new VarbinaryParser(), false, false, false, false, true, new LengthTypeNameFormatter("binary") },
            { BigChar, PM.UShortLen, null, new VarcharParser(), false, false, false, true, true, new LengthTypeNameFormatter("char") },
        };

        internal TdsDataTypeInfo(TdsDataType type, TdsDataTypeParsingMode parsingMode, int? fixedLen, IDataTypeParser parser, bool isPartLen, bool hasPrecision, bool hasScale, bool hasCollation, bool hasLength, IDataTypeNameFormatter nameFormatter)
        {
            this.Type = type;
            this.ParseMode = parsingMode;
            this.ValueParser = parser;
            this.FixedLength = fixedLen;
            this.IsPartLen = isPartLen;
            this.HasPrecision = hasPrecision;
            this.HasScale = hasScale;
            this.HasCollation = hasCollation;
            this.HasLength = hasLength;
            this.NameFormatter = nameFormatter;
        }

        public TdsDataType Type { get; }
        public TdsDataTypeParsingMode ParseMode { get; }
        internal IDataTypeParser ValueParser { get; }
        public int? FixedLength { get; }
        public bool IsPartLen { get; }
        public bool HasPrecision { get; }
        public bool HasScale { get; }
        public bool HasCollation { get; }
        public bool HasLength { get; }
        internal IDataTypeNameFormatter NameFormatter { get; }

        private class TdsDataTypeDict : Dictionary<TdsDataType, TdsDataTypeInfo>
        {
            public void Add(TdsDataType type, TdsDataTypeParsingMode parsingMode, int? fixedLen, IDataTypeParser parser, bool isPartLen, bool hasPrecision, bool hasScale, bool hasCollation, bool hasLength, IDataTypeNameFormatter nameFormatter)
            {
                this.Add(type, new TdsDataTypeInfo(type, parsingMode, fixedLen, parser, isPartLen, hasPrecision, hasScale, hasCollation, hasLength, nameFormatter));
            }
        }

        internal static TdsDataTypeInfo GetForType(TdsDataType @this)
        {
            return KnownTypes[@this];
        }
    }

    public enum TdsDataTypeParsingMode
    {
        FixedLen,
        ByteLen,
        UShortLen,
        LongLen,
        Udt,
        Xml,
        Table
    }

    public static class TdsDataTypeExtensions
    {
        public static TdsDataTypeInfo GetInfo(this TdsDataType @this)
        {
            return TdsDataTypeInfo.GetForType(@this);
        }
    }
}
