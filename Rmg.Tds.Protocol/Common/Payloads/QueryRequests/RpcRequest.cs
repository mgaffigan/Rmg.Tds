using Rmg.Tds.Protocol.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Rmg.Tds.Protocol
{
    public sealed class RpcRequest
    {
        public int? ProcedureId { get; }

        public string ProcedureName { get; }

        public RpcRequestOptionFlags OptionFlags { get; }

        public IReadOnlyList<RpcRequestParameter> Parameters { get; }

        public RpcRequestBatchFlag BatchFlag { get; }

        internal RpcRequest(ref TdsPayloadReader reader, TdsVersion version)
        {
            var cchProcName = reader.ReadInt16LE();
            if (cchProcName == 0xffff /* ProcedureID flag */)
            {
                this.ProcedureId = reader.ReadInt16LE();
                this.ProcedureName = NumberedProcedures.TryGetNameForProcedureId(ProcedureId.Value);
            }
            else
            {
                this.ProcedureName = reader.ReadUcs2StringCch(cchProcName);
            }

            this.OptionFlags = (RpcRequestOptionFlags)reader.ReadInt16LE();

            //if (version.Is74OrGreater())
            //{
            //    // EnclavePackage repeating field, but no information is available on how
            //    // to process this field
            //}

            // params
            var parameters = new List<RpcRequestParameter>();
            while (!reader.IsFullyConsumed)
            {
                var nameLen = reader.ReadByte();
                if (nameLen == (byte)RpcRequestBatchFlag.NoExec
                    || (!version.Is72OrGreater() && nameLen == (byte)RpcRequestBatchFlag.Pre72Continuation)
                    || (version.Is72OrGreater() && nameLen == (byte)RpcRequestBatchFlag.Continuation))
                {
                    this.BatchFlag = (RpcRequestBatchFlag)nameLen;
                    break;
                }
                parameters.Add(new RpcRequestParameter(nameLen, ref reader, version));
            }
            this.Parameters = parameters.AsReadOnly();
        }


        internal int SerializedLength => 
            2 /* name len */
            + (ProcedureId == null ? ProcedureName.Length * 2 : 2)
            + 2 // flags
            + Parameters.Sum(p => p.SerializedLength)
            + (BatchFlag != RpcRequestBatchFlag.None ? 1 : 0);

        internal void Serialize(ref TdsPayloadWriter writer)
        {
            if (ProcedureId != null)
            {
                writer.WriteInt16LE(0xffff);
                writer.WriteInt16LE(ProcedureId.Value);
            }
            else
            {
                writer.WriteInt16LE(ProcedureName.Length);
                writer.WriteUcs2String(ProcedureName);
            }

            writer.WriteInt16LE((int)OptionFlags);

            foreach (var param in Parameters)
            {
                param.Serialize(ref writer);
            }
            if (BatchFlag != RpcRequestBatchFlag.None)
            {
                writer.WriteByte((byte)BatchFlag);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("EXEC ");
            if (ProcedureName != null)
            {
                sb.Append(ProcedureName);
            }
            else
            {
                sb.Append("<#");
                sb.Append(ProcedureId);
                sb.Append(">");
            }
            sb.Append(" ");
            bool issecond = false;
            foreach (var param in Parameters)
            {
                if (issecond)
                {
                    sb.Append(", ");
                }
                issecond = true;
                sb.Append(param);
            }
            return sb.ToString();
        }
    }
}
