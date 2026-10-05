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
        private DateTime _lastFileTime = DateTime.MinValue;

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

        public void Reseed()
        {
            Array.Clear(_lastBuffer, 0, _lastBuffer.Length);
        }

        private void Loop()
        {
            var sw = Stopwatch.StartNew();
            while (_running)
            {
                if (_source.TryReadRange(0, _currentBuffer, 65536))
                {
                    DateTime currentFileTime = _source.LastUpdated;
                    if (currentFileTime > _lastFileTime)
                    {
                        if (_lastFileTime != DateTime.MinValue && (currentFileTime - _lastFileTime).TotalMilliseconds > 1000)
                        {
                            // It's been over a second since the last file write. This is likely a NEW run.
                            // Force a sync of all non-zero bytes to detect consecutive identical runs.
                            Array.Clear(_lastBuffer, 0, _lastBuffer.Length);
                        }
                        _lastFileTime = currentFileTime;
                    }

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
                        // Ensure control ports (like 1EH) are processed BEFORE data ports
                        batch.Sort((a, b) => 
                        {
                            bool aCtrl = (a.Port == 0x16 || a.Port == 0x1E || a.Port == 0x1F);
                            bool bCtrl = (b.Port == 0x16 || b.Port == 0x1E || b.Port == 0x1F);
                            if (aCtrl && !bCtrl) return -1;
                            if (!aCtrl && bCtrl) return 1;
                            return a.Port.CompareTo(b.Port);
                        });
                        
                        _controller.OnPoll(batch, sw.ElapsedMilliseconds);
                    }
                }
                
                // Emulate some device ticks even if no file I/O happened
                _controller.OnPoll(new PortBatch(), sw.ElapsedMilliseconds);

                System.Threading.Tasks.Task.Delay(5).Wait(); // ~200Hz polling
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
