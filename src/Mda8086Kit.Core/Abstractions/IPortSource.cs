using System;

namespace Mda8086Kit.Core.Abstractions
{
    public interface IPortSource : IDisposable
    {
        ConnectionState State { get; }
        DateTime LastUpdated { get; }
        bool TryReadRange(int firstPort, byte[] buffer, int count);
        bool TryWriteByte(int port, byte value);
    }
}
