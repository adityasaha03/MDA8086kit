using System.Collections.Generic;
using Mda8086Kit.Core;

namespace Mda8086Kit.Core.Tests.Doubles
{
    public class FakePortMonitor : IPortMonitor
    {
        public List<PortData> ReceivedData { get; } = new List<PortData>();

        public void Process(PortData data)
        {
            ReceivedData.Add(data);
        }
    }
}
