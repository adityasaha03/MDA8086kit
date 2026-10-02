using System;
using System.Diagnostics;
using System.Threading;
using Mda8086Kit.Core.Abstractions;

namespace Mda8086Kit.Core.Kit
{
    public class BusPoller : IDisposable
    {
        private readonly IPortSource _source;
        private readonly KitController _controller;
        private readonly Thread _thread;
        private bool _running;
        private readonly byte[] _lastBuffer;
        private readonly byte[] _currentBuffer;

        public BusPoller(IPortSource source, KitController controller)
        {
            _source = source;
            _controller = controller;
            _lastBuffer = new byte[65536];
            _currentBuffer = new byte[65536];
            
            _running = true;
            _thread = new Thread(Loop) { IsBackground = true, Name = "BusPoller" };
        }

        public void Start()
        {
            _thread.Start();
        }

        private void Loop()
        {
            var sw = Stopwatch.StartNew();
            while (_running)
            {
                if (_source.TryReadRange(0, _currentBuffer, 65536))
                {
                    var batch = new PortBatch();
                    for (int i = 0; i < 65536; i++)
                    {
                        if (_currentBuffer[i] != _lastBuffer[i])
                        {
                            batch.Add(new PortChange(i, _lastBuffer[i], _currentBuffer[i], sw.ElapsedMilliseconds));
                            _lastBuffer[i] = _currentBuffer[i];
                        }
                    }

                    if (batch.Count > 0)
                    {
                        _controller.OnPoll(batch, sw.ElapsedMilliseconds);
                    }
                }
                
                // Emulate some device ticks even if no file I/O happened
                _controller.OnPoll(new PortBatch(), sw.ElapsedMilliseconds);

                Thread.Sleep(5); // ~200Hz polling
            }
        }

        public void Dispose()
        {
            _running = false;
            if (!_thread.Join(100))
            {
                // Force close if necessary
            }
        }
    }
}
