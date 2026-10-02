using System;

namespace Mda8086Kit.Core.Abstractions
{
    public interface IPortDevice
    {
        string Name { get; }
        bool Handles(int port);
        void OnCpuWrite(int port, byte value, long ticks);
        void Tick(long ticks);
        void Reset();
    }
}
