using System;

namespace Mda8086Kit.Core.Abstractions
{
    public interface IDeviceWriteSink
    {
        void Post(int port, byte value);
    }
}
