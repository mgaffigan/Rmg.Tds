using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class EnvChangeToken : Token
    {
        public override TokenType Type => TokenType.EnvironmentChange;

        public EnvChangeTokenType VariableType { get; }
        public IEnvironmentChange Change { get; }

        internal EnvChangeToken(ref TdsPayloadReader reader)
        {
            int len = reader.ReadInt16LE();
            this.VariableType = (EnvChangeTokenType)reader.ReadByte();

            var dataLen = len - 1;
            this.Change = DeserializeChange(VariableType, ref reader, dataLen);
        }

        private static IEnvironmentChange DeserializeChange(EnvChangeTokenType type, ref TdsPayloadReader reader, int dataLen)
        {
            switch (type)
            {
                case EnvChangeTokenType.Database:
                case EnvChangeTokenType.Language:
                case EnvChangeTokenType.CharacterSet:
                    return new EnvironmentChange<string, string>(dataLen, ref reader);

                case EnvChangeTokenType.PacketSize:
                    return new PacketSizeChange(dataLen, ref reader);

                default:
                    return new UnknownEnvironmentChange(dataLen, ref reader);
            }
        }
    }

    public interface IEnvironmentChange
    {
        object OldValue { get; }

        object NewValue { get; }
    }

    public class UnknownEnvironmentChange : IEnvironmentChange
    {
        public byte[] Data { get; }

        object IEnvironmentChange.OldValue => null;

        object IEnvironmentChange.NewValue => Data;

        internal UnknownEnvironmentChange(int len, ref TdsPayloadReader reader)
        {
            this.Data = reader.ReadData(len);
        }
    }

    public class EnvironmentChange<TOld, TNew> : IEnvironmentChange
    {
        public TOld OldValue { get; }
        public TNew NewValue { get; }

        object IEnvironmentChange.OldValue => OldValue;
        object IEnvironmentChange.NewValue => NewValue;

        internal EnvironmentChange(int len, ref TdsPayloadReader reader)
        {
            var remLen = len;
            NewValue = ReadValue<TNew>(ref remLen, ref reader);
            OldValue = ReadValue<TOld>(ref remLen, ref reader);
            if (remLen != 0)
            {
                throw new ProtocolViolationException();
            }
        }

        private static TValue ReadValue<TValue>(ref int remLen, ref TdsPayloadReader reader)
        {
            if (remLen == 0 || typeof(TValue) == typeof(object))
            {
                return default;
            }
            else if (typeof(TValue) == typeof(string))
            {
                return (TValue)ReadString(ref remLen, ref reader);
            }
            else throw new NotSupportedException();
        }

        private static object ReadString(ref int remLen, ref TdsPayloadReader reader)
        {
            var cch = reader.ReadByte(); remLen -= 1;
            var len = cch * 2;
            var result = reader.ReadUcs2StringCb(len); remLen -= len;
            return result;
        }
    }

    public class PacketSizeChange : EnvironmentChange<string, string>
    {
        public new int OldValue => int.Parse(base.OldValue);
        public new int NewValue => int.Parse(base.NewValue);

        internal PacketSizeChange(int len, ref TdsPayloadReader reader) : base(len, ref reader)
        {
        }
    }
}
