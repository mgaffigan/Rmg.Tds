using Rmg.Tds.Protocol.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol.Payloads
{
    public abstract class PreloginOption
    {
        public PreloginOptionType Type { get; }

        public abstract int DataLength { get; }

        internal PreloginOption(PreloginOptionType type)
        {
            this.Type = type;
        }

        internal abstract void Serialize(Span<byte> d);

        public static IReadOnlyList<PreloginOption> Deserialize(ReadOnlySpan<byte> d)
        {
            var results = new List<PreloginOption>();
            int headerPos = 0;
            while (d[headerPos] != (byte)PreloginOptionType.Terminator)
            {
                var header = new PreloginOptionHeader(d.Slice(headerPos));
                headerPos += PreloginOptionHeader.HeaderLength;

                results.Add(DeserializeOption(header.Type, d.Slice(header.DataOffset, header.DataLength)));
            }
            return results.AsReadOnly();
        }

        private static PreloginOption DeserializeOption(PreloginOptionType type, ReadOnlySpan<byte> d)
        {
            switch (type)
            {
                case PreloginOptionType.Version:
                    return new PreloginVersionOption(d);
                case PreloginOptionType.Encryption:
                    return new PreloginEncryptionOption(d);
                case PreloginOptionType.InstOpt:
                    {
                        if (d.Length == 1)
                        {
                            return new PreloginInstanceMatchedOption(d);
                        }
                        else
                        {
                            return new PreloginInstanceNameOption(d);
                        }
                    }
                case PreloginOptionType.ThreadID:
                    {
                        if (d.Length == 0)
                        {
                            return new PreloginNullThreadIdOption();
                        }
                        else
                        {
                            return new PreloginThreadIdOption(d);
                        }
                    }
                case PreloginOptionType.MARS:
                    return new PreloginMarsOption(d);
                case PreloginOptionType.TraceID:
                    return new PreloginTraceIdOption(d);
                case PreloginOptionType.FedAuthRequired:
                    return new PreloginFedAuthRequiredOption(d);
                case PreloginOptionType.NonceOpt:
                    return new PreloginNonceOption(d);
                default:
                    throw new NotSupportedException($"Unknown prelogin option {type}");
            }
        }
    }

    internal struct PreloginOptionHeader
    {
        public const int HeaderLength = 5;

        public PreloginOptionType Type { get; }

        public int DataOffset { get; }

        public int DataLength { get; }

        public PreloginOptionHeader(ReadOnlySpan<byte> d)
        {
            Type = (PreloginOptionType)d[0];
            DataOffset = d.ReadBigEndianInt32(1, 2);
            DataLength = d.ReadBigEndianInt32(3, 2);
        }

        public PreloginOptionHeader(PreloginOptionType type, int offset, int len)
        {
            this.Type = type;
            this.DataOffset = offset;
            this.DataLength = len;
        }

        public void Serialize(Span<byte> d)
        {
            d[0] = (byte)Type;
            d.WriteBigEndianInt32(1, 2, DataOffset);
            d.WriteBigEndianInt32(3, 2, DataLength);
        }
    }

    public static class PreloginOptionExtensions
    {
        public static int GetSerializedSize(this IReadOnlyList<PreloginOption> @this)
        {
            var headerSize = @this.Count * PreloginOptionHeader.HeaderLength + 1 /* terminator */;
            var dataSize = @this.Sum(d => d.DataLength);
            return headerSize + dataSize;
        }

        public static void Serialize(this IReadOnlyList<PreloginOption> @this, Span<byte> d)
        {
            int dataPos = @this.Count * PreloginOptionHeader.HeaderLength;
            d[dataPos] = (byte)PreloginOptionType.Terminator; dataPos += 1 /* terminator */;
            int headerPos = 0;
            foreach (var opt in @this)
            {
                var header = new PreloginOptionHeader(opt.Type, dataPos, opt.DataLength);
                dataPos += header.DataLength;
                header.Serialize(d.Slice(headerPos));
                headerPos += PreloginOptionHeader.HeaderLength;

                opt.Serialize(d.Slice(header.DataOffset, header.DataLength));
            }
        }
    }

    public sealed class PreloginVersionOption : PreloginOption
    {
        public int Version { get; }
        public int SubBuild { get; }

        public Version SemVersion => new Version(
            major: (Version >> 24) & 0xff,
            minor: (Version >> 16) & 0xff,
            build: (Version >> 0) & 0xffff,
            revision: SubBuild);

        public override int DataLength => 6;

        internal PreloginVersionOption(ReadOnlySpan<byte> data)
            : base(PreloginOptionType.Version)
        {
            this.Version = data.ReadBigEndianInt32(0, 4);
            this.SubBuild = data.ReadBigEndianInt32(4, 2);
        }

        public PreloginVersionOption(int ver, int subbuild)
            : base(PreloginOptionType.Version)
        {
            this.Version = ver;
            this.SubBuild = subbuild;
        }

        internal override void Serialize(Span<byte> d)
        {
            d.WriteBigEndianInt32(0, 4, Version);
            d.WriteBigEndianInt32(4, 2, SubBuild);
        }
    }

    public abstract class PreloginByteOption : PreloginOption
    {
        protected readonly byte OptionData;

        public override int DataLength => 1;

        internal PreloginByteOption(PreloginOptionType type, ReadOnlySpan<byte> data)
            : base(type)
        {
            this.OptionData = data[0];
        }

        public PreloginByteOption(PreloginOptionType type, byte data)
            : base(type)
        {
            this.OptionData = data;
        }

        internal override void Serialize(Span<byte> d)
        {
            d[0] = OptionData;
        }
    }

    public sealed class PreloginEncryptionOption : PreloginByteOption
    {
        public PreloginEncryptionOption(TdsEncryptionMode mode)
            : base(PreloginOptionType.Encryption, (byte)mode) { }

        internal PreloginEncryptionOption(ReadOnlySpan<byte> data)
            : base(PreloginOptionType.Encryption, data) { }

        public TdsEncryptionMode Mode => (TdsEncryptionMode)OptionData;
    }

    public enum TdsEncryptionMode
    {
        Off = 0,
        On = 1,
        NotSupported = 2,
        Required = 4
    }

    public sealed class PreloginInstanceNameOption : PreloginOption
    {
        public string Instance { get; }

        public override int DataLength
        {
            get
            {
                if (string.IsNullOrEmpty(Instance))
                {
                    return 1;
                }

                return Encoding.Unicode.GetByteCount(Instance);
            }
        }

        internal PreloginInstanceNameOption(ReadOnlySpan<byte> data)
            : base(PreloginOptionType.InstOpt)
        {
            if (data.Length < 1 || (data.Length & 0x01) != 0)
            {
                throw new FormatException($"Unexpected length {data.Length} for PreloginInstanceNameOption");
            }

            if (data.Length == 1)
            {
                if (data[0] == 0)
                {
                    Instance = "";
                }
                else
                {
                    Instance = new string(new[] { (char)data[0] });
                }
            }
            else
            {
                Instance = Encoding.Unicode.GetString(data);
                if (Instance.EndsWith('\0'))
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public PreloginInstanceNameOption(string instname)
            : base(PreloginOptionType.InstOpt)
        {
            this.Instance = instname;
        }

        internal override void Serialize(Span<byte> d)
        {
            if (string.IsNullOrEmpty(Instance))
            {
                d[0] = 0x00;
            }
            else
            {
                Encoding.Unicode.GetBytes(Instance, d);
            }
        }
    }

    public sealed class PreloginInstanceMatchedOption : PreloginByteOption
    {
        public PreloginInstanceMatchedOption(InstanceMatchResult mode)
            : base(PreloginOptionType.InstOpt, (byte)mode) { }

        internal PreloginInstanceMatchedOption(ReadOnlySpan<byte> data)
            : base(PreloginOptionType.InstOpt, data) { }

        public InstanceMatchResult Result => (InstanceMatchResult)OptionData;
    }

    public enum InstanceMatchResult
    {
        Default = 0,
        DidNotMatch = 1
    }

    public sealed class PreloginNullThreadIdOption : PreloginOption
    {
        public override int DataLength => 0;

        internal PreloginNullThreadIdOption()
            : base(PreloginOptionType.ThreadID)
        {
        }

        internal override void Serialize(Span<byte> d)
        {
        }
    }

    public sealed class PreloginThreadIdOption : PreloginOption
    {
        public int ThreadId { get; }

        public override int DataLength => 4;

        internal PreloginThreadIdOption(ReadOnlySpan<byte> data)
            : base(PreloginOptionType.ThreadID)
        {
            if (data.Length > 0)
            {
                ThreadId = data.ReadBigEndianInt32(0, 4);
            }
        }

        public PreloginThreadIdOption(int threadId)
            : base(PreloginOptionType.ThreadID)
        {
            this.ThreadId = threadId;
        }

        internal override void Serialize(Span<byte> d)
        {
            d.WriteBigEndianInt32(0, 4, ThreadId);
        }
    }

    public sealed class PreloginMarsOption : PreloginByteOption
    {
        public PreloginMarsOption(bool marsEnabled)
            : base(PreloginOptionType.MARS, (byte)(marsEnabled ? 1 : 0)) { }

        internal PreloginMarsOption(ReadOnlySpan<byte> data)
            : base(PreloginOptionType.MARS, data) { }

        public bool Enabled => OptionData != 0;
    }

    public sealed class PreloginTraceIdOption : PreloginOption
    {
        public byte[] TraceData { get; }

        public override int DataLength => TraceData.Length;

        internal PreloginTraceIdOption(ReadOnlySpan<byte> data)
            : base(PreloginOptionType.TraceID)
        {
            TraceData = data.ToArray();
        }

        internal override void Serialize(Span<byte> d)
        {
            TraceData.CopyTo(d);
        }
    }

    public sealed class PreloginFedAuthRequiredOption : PreloginByteOption
    {
        public PreloginFedAuthRequiredOption(FedAuthRequiredMode mode)
            : base(PreloginOptionType.FedAuthRequired, (byte)mode) { }

        internal PreloginFedAuthRequiredOption(ReadOnlySpan<byte> data)
            : base(PreloginOptionType.FedAuthRequired, data) { }

        public FedAuthRequiredMode Mode => (FedAuthRequiredMode)OptionData;
    }

    public enum FedAuthRequiredMode
    {
        FedAuthNotRequired = 0x00,
        FedAuthRequired = 0x01,
        Illegal = 0x02
    }

    public sealed class PreloginNonceOption : PreloginOption
    {
        public byte[] Nonce { get; }

        public override int DataLength => Nonce.Length;

        internal PreloginNonceOption(ReadOnlySpan<byte> data)
            : base(PreloginOptionType.NonceOpt)
        {
            Nonce = data.ToArray();
        }

        internal override void Serialize(Span<byte> d)
        {
            Nonce.CopyTo(d);
        }
    }
}
