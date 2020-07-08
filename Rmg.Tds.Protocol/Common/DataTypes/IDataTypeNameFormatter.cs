using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal interface IDataTypeNameFormatter
    {
        string GetNameForType(TdsTypeInfo typeInfo);
    }

    internal sealed class SimpleTypeNameFormatter : IDataTypeNameFormatter
    {
        public SimpleTypeNameFormatter(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        public string GetNameForType(TdsTypeInfo typeInfo) => Name;

        public static implicit operator SimpleTypeNameFormatter(string s)
        {
            return new SimpleTypeNameFormatter(s);
        }
    }

    internal sealed class LengthTypeNameFormatter : IDataTypeNameFormatter
    {
        public LengthTypeNameFormatter(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        public string GetNameForType(TdsTypeInfo typeInfo) => $"{Name}({typeInfo.Length})";
    }

    internal sealed class ScaleTypeNameFormatter : IDataTypeNameFormatter
    {
        public ScaleTypeNameFormatter(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        public string GetNameForType(TdsTypeInfo typeInfo) => $"{Name}({typeInfo.Scale})";
    }

    internal sealed class PrecisionScaleTypeNameFormatter : IDataTypeNameFormatter
    {
        public PrecisionScaleTypeNameFormatter(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        public string GetNameForType(TdsTypeInfo typeInfo) => $"{Name}({typeInfo.Precision},{typeInfo.Scale})";
    }

    internal sealed class IntNTypeNameFormatter : IDataTypeNameFormatter
    {
        public string GetNameForType(TdsTypeInfo typeInfo)
        {
            switch (typeInfo.Length)
            {
                case 0: return "int";
                case 1: return "tinyint";
                case 2: return "smallint";
                case 4: return "int";
                case 8: return "bigint";
                default: throw new NotSupportedException($"Unknown length {typeInfo.Length}");
            }
        }
    }

    internal sealed class FloatTypeNameFormatter : IDataTypeNameFormatter
    {
        public string GetNameForType(TdsTypeInfo typeInfo)
        {
            switch (typeInfo.Length)
            {
                case 0: return "float(24)";
                case 4: return "float(24)";
                case 8: return "float(53)";
                default: throw new NotSupportedException($"Unknown length {typeInfo.Length}");
            }
        }
    }

    internal sealed class MoneyTypeNameFormatter : IDataTypeNameFormatter
    {
        public string GetNameForType(TdsTypeInfo typeInfo)
        {
            switch (typeInfo.Length)
            {
                case 0: return "money";
                case 4: return "smallmoney";
                case 8: return "money";
                default: throw new NotSupportedException($"Unknown length {typeInfo.Length}");
            }
        }
    }

    internal sealed class TableTypeNameFormatter : IDataTypeNameFormatter
    {
        public string GetNameForType(TdsTypeInfo typeInfo)
        {
            var table = typeInfo.TvpTypeInfo;
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(table.TypeName))
            {
                if (!string.IsNullOrWhiteSpace(table.DbName))
                {
                    sb.Append(table.DbName);
                    sb.Append(".");
                }
                if (!string.IsNullOrWhiteSpace(table.OwningSchema))
                {
                    sb.Append(table.OwningSchema);
                    sb.Append(".");
                }
                sb.Append(table.TypeName);
            }
            else
            {
                sb.Append("TABLE (");
                bool first = true;
                foreach (var col in table.Columns)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                        first = false;
                    }

                    sb.Append(col.Name);
                    sb.Append(" ");
                    sb.Append(col.TypeInfo.GetSqlTypeName());
                    if (col.Flags == TvpColumnMetadataFlags.Nullable)
                    {
                        sb.Append(" NULL");
                    }
                }
                sb.Append(")");
            }
            return sb.ToString();
        }
    }

    internal sealed class UdtTypeNameFormatter : IDataTypeNameFormatter
    {
        public string GetNameForType(TdsTypeInfo typeInfo)
        {
            var udt = typeInfo.UdtTypeInfo;
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(udt.DbName))
            {
                sb.Append(udt.DbName);
                sb.Append(".");
            }
            if (!string.IsNullOrWhiteSpace(udt.SchemaName))
            {
                sb.Append(udt.SchemaName);
                sb.Append(".");
            }
            sb.Append(udt.TypeName);
            return sb.ToString();
        }
    }
}
