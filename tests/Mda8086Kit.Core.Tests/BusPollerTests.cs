using System;
using System.Collections.Generic;
using Xunit;
using Mda8086Kit.Core;
using Mda8086Kit.Core.Tests.Doubles;

namespace Mda8086Kit.Core.Tests
{
    public class ThrowingFakeSource : IPortSource
    {
        public bool ResetCalled { get; private set; }

        public IEnumerable<PortData> Poll(long currentUs)
        {
            throw new InvalidOperationException("Simulated truncation");
        }

        public void Reset()
        {
            ResetCalled = true;
        }
    }

    public class BusPollerTests
    {
        [Fact]
        public void Tick_RoutesToCorrectPort()
        {
            var source = new FakePortSource();
            var poller = new BusPoller(source);
            
            var monitor1 = new FakePortMonitor();
            var monitor2 = new FakePortMonitor();
            
            poller.Subscribe(10, monitor1);
            poller.Subscribe(20, monitor2);

            source.EnqueuePollResult(new List<PortData>
            {
                new PortData(10, 0xAA, 100),
                new PortData(20, 0xBB, 101),
                new PortData(10, 0xCC, 102)
            });

            poller.Tick(1000);

            Assert.Equal(2, monitor1.ReceivedData.Count);
            Assert.Equal(0xAA, monitor1.ReceivedData[0].Value);
            Assert.Equal(0xCC, monitor1.ReceivedData[1].Value);

            Assert.Single(monitor2.ReceivedData);
            Assert.Equal(0xBB, monitor2.ReceivedData[0].Value);
        }

        [Fact]
        public void Tick_RoutesToMultipleMonitorsOnSamePort()
        {
            var source = new FakePortSource();
            var poller = new BusPoller(source);
            
            var monitorA = new FakePortMonitor();
            var monitorB = new FakePortMonitor();
            
            poller.Subscribe(10, monitorA);
            poller.Subscribe(10, monitorB);

            source.EnqueuePollResult(new List<PortData>
            {
                new PortData(10, 0xFF, 50)
            });

            poller.Tick(100);

            Assert.Single(monitorA.ReceivedData);
            Assert.Single(monitorB.ReceivedData);
            Assert.Equal(0xFF, monitorA.ReceivedData[0].Value);
            Assert.Equal(0xFF, monitorB.ReceivedData[0].Value);
        }

        [Fact]
        public void Tick_ExceptionTriggersResetAndDropsPoll()
        {
            var source = new ThrowingFakeSource();
            var poller = new BusPoller(source);
            
            poller.Tick(200);

            Assert.True(source.ResetCalled);
        }
    }
}
