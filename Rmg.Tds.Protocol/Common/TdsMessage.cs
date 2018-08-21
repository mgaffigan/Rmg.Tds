
using System;
using System.Reflection;

namespace Rmg.Tds.Protocol
{
    public sealed class TdsMessage
    {
        public TdsPacketType Type { get; }
        public TdsMessageStatuses Statuses { get; }
        
        public byte[] Data { get; }

        public TdsSerializationContext Context { get; }

        public TdsMessage(TdsPacketType type, TdsMessageStatuses statuses, byte[] data, TdsSerializationContext context)
        {
            this.Type = type;
            this.Statuses = statuses;
            this.Data = data;
            this.Context = context;
        }

        public TdsMessage(TdsMessagePayload payload, TdsMessageStatuses statuses, TdsSerializationContext context)
        {
            if (context.TdsVersion != payload.TdsVersion)
            {
                throw new InvalidOperationException("Payload is for incorrect version");
            }

            this.Statuses = statuses;
            this.Context = context;

            this.Type = payload.PacketType;
            this.Data = payload.ToArray();
        }

        //public TdsMessage WithReplacedPayload(byte[] data)
        //{
        //    return new TdsMessage(Type, Statuses, data, Context);
        //}

        public TdsMessage WithReplacedPayload(TdsMessagePayload payload)
        {
            if (payload.TdsVersion != Context.TdsVersion
                // Prelogin and Login7 can change versions
                && Type != TdsPacketType.Tds7Login
                && Type != TdsPacketType.PreLogin)
            {
                throw new InvalidOperationException("Payload is for incorrect version");
            }
            if (payload.PacketType != Type)
            {
                throw new InvalidOperationException("Payload is for incorrect message type");
            }

            return new TdsMessage(Type, Statuses, payload.ToArray(), Context);
        }

        public TMessagePayload DeserializeAs<TMessagePayload>()
            where TMessagePayload : TdsMessagePayload
        {
            var ctor = typeof(TMessagePayload).GetConstructor(new[] { typeof(TdsMessage) }) 
                ?? throw new MissingMethodException("TMessagePayload is expected to have .ctor(TdsMessage) constructor");

            try
            {
                return (TMessagePayload)ctor.Invoke(new[] { this });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }
    }
}