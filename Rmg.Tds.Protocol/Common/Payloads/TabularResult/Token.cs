using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Rmg.Tds.Protocol
{
    public abstract class Token
    {
        public abstract TokenType Type { get; }

        public static IReadOnlyList<Token> Deserialize(ReadOnlySpan<byte> data, TdsSerializationContext tdsContext)
        {
            var reader = new TdsPayloadReader(data);
            var results = new List<Token>();
            var context = new TokenDeserializationContext(tdsContext);
            while (!reader.IsFullyConsumed)
            {
                var tokenType = (TokenType)reader.ReadByte();
                // Some messages end with single nul, others... don't.
                if (reader.IsFullyConsumed && tokenType == TokenType.Terminator)
                {
                    break;
                }
                results.Add(DeserializeToken(tokenType, ref reader, context));
            }
            return results;
        }

        private static Token DeserializeToken(TokenType tokenType, ref TdsPayloadReader reader, TokenDeserializationContext context)
        {
            switch (tokenType)
            {
                case TokenType.ColumnInfo:
                    return new LengthPrefixedToken(tokenType, 2, ref reader);

                case TokenType.ColumnMetadata:
                    throw new NotImplementedException();

                case TokenType.Done:
                case TokenType.DoneInProc:
                case TokenType.DoneProcedure:
                    {
                        var len = context.TdsVersion.Is72OrGreater() ? 12 : 8;
                        return new FixedLengthToken(tokenType, len, ref reader);
                    }

                case TokenType.EnvironmentChange:
                    return new EnvChangeToken(ref reader);

                case TokenType.Error:
                    return new LengthPrefixedToken(tokenType, 2, ref reader);

                case TokenType.FeatureExtAck:
                    return new FeatureExtAckToken(ref reader);

                case TokenType.FedAuthInfo:
                    return new LengthPrefixedToken(tokenType, 4, ref reader);

                case TokenType.Info:
                case TokenType.LoginAcknowledgement:
                    return new LengthPrefixedToken(tokenType, 2, ref reader);

                case TokenType.NBCRow:
                    throw new NotImplementedException();

                case TokenType.Order:
                    return new LengthPrefixedToken(tokenType, 2, ref reader);

                case TokenType.ReturnStatus:
                    return new FixedLengthToken(tokenType, 4, ref reader);

                case TokenType.ReturnValue:
                    throw new NotImplementedException();

                case TokenType.Row:
                    throw new NotImplementedException();

                case TokenType.SessionState:
                    return new LengthPrefixedToken(tokenType, 4, ref reader);

                case TokenType.SSPI:
                case TokenType.TableName:
                    return new LengthPrefixedToken(tokenType, 2, ref reader);

                case TokenType.AlternativeMetadata:
                case TokenType.AlternativeRow:
                case TokenType.Offset:
                default:
                    throw new NotSupportedException($"Unsupported token {tokenType}");
            }
        }
    }

    internal class LengthPrefixedToken : Token
    {
        public override TokenType Type { get; }
        public byte[] Data { get; }

        public LengthPrefixedToken(TokenType tokenType, int sizeofLength, ref TdsPayloadReader reader)
        {
            this.Type = tokenType;
            var len = reader.ReadIntLE(sizeofLength);
            this.Data = reader.ReadData(len);
        }
    }

    internal class FixedLengthToken : Token
    {
        public override TokenType Type { get; }
        public byte[] Data { get; }

        public FixedLengthToken(TokenType tokenType, int len, ref TdsPayloadReader reader)
        {
            this.Type = tokenType;
            this.Data = reader.ReadData(len);
        }
    }
}
