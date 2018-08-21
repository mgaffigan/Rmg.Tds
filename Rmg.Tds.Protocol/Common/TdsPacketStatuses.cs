using System;

namespace Rmg.Tds.Protocol
{
    // Must be in sync with TdsMessageStatuses
    [Flags]
    public enum TdsPacketStatuses
    {
        None = 0,
        EndOfMessage = 0x1,
        Ignore = 0x2,
        ResetConnection = 0x8,
        ResetConnectionSkipTran = 0x10
    }

    public static class TdsPacketStatusesExtensions
    {
        public static TdsMessageStatuses AsMessageStatuses(this TdsPacketStatuses st)
        {
            return ((TdsMessageStatuses)st) & TdsMessageStatuses.All;
        }
    }
}