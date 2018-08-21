using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Rmg.Tds.Protocol.DataTypes
{
    internal sealed class PlpVarBinaryParser : DataTypeParser<byte[]>
    {
        private const ulong
            PLP_NULL = 0xffff_ffff__ffff_fffful,
            PLP_UNKNOWN_LEN = 0xffff_ffff__ffff_fffeul;

        public override byte[] DeserializeValue(ref TdsPayloadReader reader)
        {
            ulong lenOrFlag = reader.ReadInt64LE();
            if (lenOrFlag == PLP_NULL)
            {
                return null;
            }

            MemoryStream msResult;
            if (lenOrFlag == PLP_UNKNOWN_LEN)
            {
                msResult = new MemoryStream();
            }
            else
            {
                msResult = new MemoryStream(checked((int)lenOrFlag));
            }

            for (int i = 0; i < unchecked((int)lenOrFlag);)
            {
                var lenOfChunk = reader.ReadInt32LE();
                if (lenOfChunk == 0)
                {
                    if (lenOrFlag != PLP_UNKNOWN_LEN && msResult.Length != unchecked((int)lenOrFlag))
                    {
                        throw new ProtocolViolationException("PLP Stream terminated in less than length");
                    }
                    break;
                }

                reader.ReadData(lenOfChunk, msResult);
            }

            return msResult.ToArray();
        }

        public override void SerializeValue(ref TdsPayloadWriter writer, byte[] value)
        {
            if (value == null)
            {
                writer.WriteInt64LE(PLP_NULL);
                return;
            }

            writer.WriteInt64LE((ulong)value.Length);
            writer.WriteInt32LE(value.Length);
            writer.WriteData(value);
            writer.WriteInt32LE(0);
        }

        public override int GetSerializedValueLength(byte[] value) => GetPlpLen(value?.Length);
        public int GetPlpLen(int? valueByteLen)
        {
            if (valueByteLen == null)
            {
                return 8;
            }
            return 8 /* PLP_LEN */ + 4 /* CHUNK_LEN */ + valueByteLen.Value + 4 /* PLP_TERMINATOR */;
        }
    }
}
