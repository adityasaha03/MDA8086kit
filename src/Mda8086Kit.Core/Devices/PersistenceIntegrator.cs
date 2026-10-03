using System;

namespace Mda8086Kit.Core.Devices
{
    public class PersistenceIntegrator
    {
        private readonly long[] _redAccumulators = new long[64];
        private readonly long[] _greenAccumulators = new long[64];

        public float[] RedDisplay { get; } = new float[64];
        public float[] GreenDisplay { get; } = new float[64];
        public float[] RedDuty { get; } = new float[64];
        public float[] GreenDuty { get; } = new float[64];

        private ulong _currentRedGrid;
        private ulong _currentGreenGrid;
        
        private long _windowMs;
        private float _gain;
        private long _smoothingMs;
        private long _ticksPerMillisecond;

        private long _windowElapsedTicks = 0;
        private long _lastTick = 0;
        private bool _firstTick = true;

        public PersistenceIntegrator(long windowMs, float gain, long smoothingMs, long ticksPerMillisecond)
        {
            _windowMs = windowMs;
            _gain = gain;
            _smoothingMs = smoothingMs;
            _ticksPerMillisecond = ticksPerMillisecond;
        }

        public void Reset()
        {
            Array.Clear(_redAccumulators, 0, 64);
            Array.Clear(_greenAccumulators, 0, 64);
            Array.Clear(RedDisplay, 0, 64);
            Array.Clear(GreenDisplay, 0, 64);
            Array.Clear(RedDuty, 0, 64);
            Array.Clear(GreenDuty, 0, 64);
            
            _windowElapsedTicks = 0;
            _firstTick = true;
        }

        public void SetInput(ulong redGrid, ulong greenGrid)
        {
            _currentRedGrid = redGrid;
            _currentGreenGrid = greenGrid;
        }

        public void Tick(long currentTick)
        {
            if (_firstTick)
            {
                _lastTick = currentTick;
                _firstTick = false;
                return;
            }

            long elapsed = currentTick - _lastTick;
            _lastTick = currentTick;
            
            long windowTicks = _windowMs * _ticksPerMillisecond;
            
            while (elapsed > 0)
            {
                long ticksToNextWindow = windowTicks - _windowElapsedTicks;
                long ticksToApply = Math.Min(elapsed, ticksToNextWindow);

                ApplyTicks(ticksToApply);

                _windowElapsedTicks += ticksToApply;
                elapsed -= ticksToApply;

                if (_windowElapsedTicks >= windowTicks)
                {
                    CommitWindow(windowTicks);
                    _windowElapsedTicks = 0;
                }
            }
        }

        private void ApplyTicks(long ticks)
        {
            for (int i = 0; i < 64; i++)
            {
                if ((_currentRedGrid & (1UL << i)) != 0) _redAccumulators[i] += ticks;
                if ((_currentGreenGrid & (1UL << i)) != 0) _greenAccumulators[i] += ticks;
            }
        }

        private void CommitWindow(long windowTicks)
        {
            float alpha = 1.0f;
            if (_smoothingMs > 0)
            {
                // Simple EMA
                float dt = _windowMs;
                float tau = _smoothingMs;
                alpha = 1.0f - (float)Math.Exp(-dt / tau);
            }

            for (int i = 0; i < 64; i++)
            {
                float rDuty = (float)_redAccumulators[i] / windowTicks;
                float gDuty = (float)_greenAccumulators[i] / windowTicks;
                
                RedDuty[i] = rDuty;
                GreenDuty[i] = gDuty;

                float rDisp = Math.Min(1.0f, rDuty * _gain);
                float gDisp = Math.Min(1.0f, gDuty * _gain);

                if (_smoothingMs > 0)
                {
                    // Instant attack (peak hold) for slow emulator multiplexing, slow decay
                    if (rDisp > RedDisplay[i]) RedDisplay[i] = rDisp;
                    else RedDisplay[i] += alpha * (rDisp - RedDisplay[i]);

                    if (gDisp > GreenDisplay[i]) GreenDisplay[i] = gDisp;
                    else GreenDisplay[i] += alpha * (gDisp - GreenDisplay[i]);
                }
                else
                {
                    RedDisplay[i] = rDisp;
                    GreenDisplay[i] = gDisp;
                }

                _redAccumulators[i] = 0;
                _greenAccumulators[i] = 0;
            }
        }
    }
}
