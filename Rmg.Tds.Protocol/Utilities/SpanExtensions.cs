using System;
using System.Collections.Generic;
using System.Text;

namespace Rmg.Tds.Protocol.Utilities
{
    internal static class SpanExtensions
    {
        public static int ReadBigEndianInt32(this ReadOnlySpan<byte> d, int start, int len)
        {
            // 0xddccbbaa
            if (len == 4)
            {
                return (d[start + 0] << 24)
                    | (d[start + 1] << 16)
                    | (d[start + 2] << 8)
                    | (d[start + 3] << 0);
            }
            else if (len == 2)
            {
                return (d[start + 0] << 8)
                    | (d[start + 1] << 0);
            }
            else throw new ArgumentOutOfRangeException();
        }

        public static void WriteBigEndianInt32(this Span<byte> d, int start, int len, int val)
        {
            // 0xddccbbaa
            if (len == 4)
            {
                d[start + 0] = (byte)((val >> 24) & 0xff);
                d[start + 1] = (byte)((val >> 16) & 0xff);
                d[start + 2] = (byte)((val >> 8) & 0xff);
                d[start + 3] = (byte)((val >> 0) & 0xff);
            }
            else if (len == 2)
            {
                d[start + 0] = (byte)((val >> 8) & 0xff);
                d[start + 1] = (byte)((val >> 0) & 0xff);
            }
            else throw new ArgumentOutOfRangeException();
        }

        public static int ReadLittleEndianInt32(this ReadOnlySpan<byte> d, int start, int len)
        {
            // 0xddccbbaa
            if (len == 4)
            {
                return (d[start + 3] << 24)
                    | (d[start + 2] << 16)
                    | (d[start + 1] << 8)
                    | (d[start + 0] << 0);
            }
            else if (len == 2)
            {
                return (d[start + 1] << 8)
                    | (d[start + 0] << 0);
            }
            else throw new ArgumentOutOfRangeException();
        }

        public static void WriteLittleEndianInt32(this Span<byte> d, int start, int len, int val)
        {
            // 0xddccbbaa
            if (len == 4)
            {
                d[start + 3] = (byte)((val >> 24) & 0xff);
                d[start + 2] = (byte)((val >> 16) & 0xff);
                d[start + 1] = (byte)((val >> 8) & 0xff);
                d[start + 0] = (byte)((val >> 0) & 0xff);
            }
            else if (len == 2)
            {
                d[start + 1] = (byte)((val >> 8) & 0xff);
                d[start + 0] = (byte)((val >> 0) & 0xff);
            }
            else throw new ArgumentOutOfRangeException();
        }

        public static string ReadUnicodeStringCch(this ReadOnlySpan<byte> d, int start, int cch)
        {
            var cb = cch * 2;
            if (cb < 2)
            {
                return "";
            }

            return Encoding.Unicode.GetString(d.Slice(start, cb));
        }
    }
}
