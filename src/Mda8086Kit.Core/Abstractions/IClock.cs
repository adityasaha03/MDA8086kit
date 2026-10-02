using System;

namespace Mda8086Kit.Core.Abstractions
{
    /// <summary>
    /// PRD §5.6: A monotonic clock source.
    /// </summary>
    public interface IClock
    {
        long Ticks { get; }
        long TicksPerMillisecond { get; }
    }
}
