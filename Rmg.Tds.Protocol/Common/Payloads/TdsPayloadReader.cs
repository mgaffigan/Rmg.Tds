using Rmg.Tds.Protocol.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rmg.Tds.Protocol
{
    internal ref struct TdsPayloadReader
    {
        private readonly ReadOnlySpan<byte> Data;

        private int Offset;

        public bool IsFullyConsumed => Offset >= Data.Length;

        public TdsPayloadReader(ReadOnlySpan<byte> data)
        {
            this.Data = data;
            this.Offset = 0;
        }

        public int ReadInt16BE() => ReadIntBE(2);
        public int ReadInt32BE() => ReadIntBE(4);

        public int ReadIntBE(int len)
        {
            var result = Data.ReadBigEndianInt32(Offset, len);
            Offset += len;
            return result;
        }

        public int ReadInt16LE() => ReadIntLE(2);
        public int ReadInt32LE() => ReadIntLE(4);
        public ulong ReadInt64LE()
        {
            var loword = ReadInt32LE();
            var hiword = ReadInt32LE();

            return (((ulong)unchecked((uint)hiword)) << 32)
                | (((ulong)unchecked((uint)loword)) << 0);
        }

        public int ReadIntLE(int len)
        {
            var result = Data.ReadLittleEndianInt32(Offset, len);
            Offset += len;
            return result;
        }

        public byte ReadByte()
        {
            var result = Data[Offset];
            Offset += 1;
            return result;
        }

        public string ReadUcs2StringCch(int length) => ReadUcs2StringCb(length * 2);
        public string ReadUcs2StringCb(int length)
        {
            string result;
            if (length < 2)
            {
                result = "";
            }
            else if (length == 0xffff)
            {
                result = null;
            }
            else
            {
                result = Encoding.Unicode.GetString(Data.Slice(Offset, length));
            }
            Offset += length;
            return result;
        }

        public string ReadByteCchUcs2String()
        {
            var len = ReadByte();
            return ReadUcs2StringCch(len);
        }

        internal string ReadAsciiString(int len)
        {
            if (len == 0)
            {
                return "";
            }
            else if (len == 0xffff)
            {
                return null;
            }

            var result = Encoding.ASCII.GetString(Data.Slice(Offset, len));
            Offset += len;
            return result;
        }

        public byte[] ReadData(int len)
        {
            var result = Data.Slice(Offset, len).ToArray();
            Offset += len;
            return result;
        }

        public void ReadData(int len, Stream s)
        {
            s.Write(Data.Slice(Offset, len));
            Offset += len;
        }
    }
}
