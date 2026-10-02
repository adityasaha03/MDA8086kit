using System;
using System.Collections.Generic;

namespace Mda8086Kit.Core
{
    public class BusPoller
    {
        private readonly IPortSource _source;
        private readonly Dictionary<int, List<IPortMonitor>> _monitors = new Dictionary<int, List<IPortMonitor>>();

        public BusPoller(IPortSource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public void Subscribe(int port, IPortMonitor monitor)
        {
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));
            
            if (!_monitors.TryGetValue(port, out var list))
            {
                list = new List<IPortMonitor>();
                _monitors[port] = list;
            }
            list.Add(monitor);
        }

        public void Tick(long currentUs)
        {
            IEnumerable<PortData> changes;
            try
            {
                changes = _source.Poll(currentUs);
            }
            catch (InvalidOperationException)
            {
                _source.Reset();
                return;
            }

            foreach (var data in changes)
            {
                if (_monitors.TryGetValue(data.Port, out var list))
                {
                    foreach (var monitor in list)
                    {
                        monitor.Process(data);
                    }
                }
            }
        }
    }
}
