using Rmg.Tds.Protocol.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol
{
    internal ref struct TdsPayloadWriter
    {
        private readonly Span<byte> Data;
        private int Offset;

        public bool IsFullyConsumed => Offset >= Data.Length;

        public TdsPayloadWriter(Span<byte> data)
        {
            this.Data = data;
            this.Offset = 0;
        }

        public void WriteInt16BE(int value) => WriteIntBE(2, value);
        public void WriteInt32BE(int value) => WriteIntBE(4, value);

        public void WriteIntBE(int len, int value)
        {
            Data.WriteBigEndianInt32(Offset, len, value);
            Offset += len;
        }

        public void WriteInt16LE(int value) => WriteIntLE(2, value);
        public void WriteInt32LE(int value) => WriteIntLE(4, value);

        public void WriteInt64LE(ulong value)
        {
            var hiword = unchecked((int)((value >> 32) & 0xFFFF_FFFFul));
            var lowword = unchecked((int)((value >> 0) & 0xFFFF_FFFFul));
            WriteInt32LE(lowword);
            WriteInt32LE(hiword);
        }

        public void WriteIntLE(int len, int value)
        {
            Data.WriteLittleEndianInt32(Offset, len, value);
            Offset += len;
        }

        public void WriteByte(byte b)
        {
            Data[Offset] = b;
            Offset += 1;
        }

        public void WriteUcs2StringLECb(int cblen, string s)
        {
            var cb = Encoding.Unicode.GetByteCount(s);
            WriteIntLE(cblen, cb);

            WriteUcs2String(s);
        }

        public void WriteUcs2String(string s)
        {
            int cb = Encoding.Unicode.GetBytes(s, Data.Slice(Offset));
            Offset += cb;
        }

        public void WriteByteCchUcs2String(string s)
        {
            WriteByte((byte)(s ?? "").Length);
            WriteUcs2String(s);
        }

        internal void WriteAsciiString(string value)
        {
            int cb = Encoding.ASCII.GetBytes(value, Data.Slice(Offset));
            Offset += cb;
        }

        public void WriteData(ReadOnlySpan<byte> data)
        {
            data.CopyTo(Data.Slice(Offset));
            Offset += data.Length;
        }
    }
}
