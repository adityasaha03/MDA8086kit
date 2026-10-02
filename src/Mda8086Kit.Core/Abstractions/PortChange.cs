using System;

namespace Mda8086Kit.Core.Abstractions
{
    public readonly struct PortChange
    {
        public int Port { get; }
        public byte OldValue { get; }
        public byte Value { get; }
        public long Ticks { get; }

        public PortChange(int port, byte oldValue, byte value, long ticks)
        {
            Port = port;
            OldValue = oldValue;
            Value = value;
            Ticks = ticks;
        }
    }
}
