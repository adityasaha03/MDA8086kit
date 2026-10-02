using System.Collections.Generic;

namespace Mda8086Kit.Core
{
    public interface IPortSource
    {
        IEnumerable<PortData> Poll(long currentUs);
        void Reset();
    }
}
