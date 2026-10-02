using System;

namespace Mda8086Kit.Core.Abstractions
{
    public enum ConnectionStatus
    {
        Connected,
        WaitingForEmu8086,
        AccessDenied,
        Locked,
        PartialRead,
        IoError,
        Stopped
    }

    public class ConnectionState
    {
        public ConnectionStatus Status { get; }
        public string Message { get; }
        public string Remedy { get; }

        public ConnectionState(ConnectionStatus status, string message, string remedy)
        {
            Status = status;
            Message = message;
            Remedy = remedy;
        }
    }
}
