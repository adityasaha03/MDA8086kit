using System;
using Mda8086Kit.Core.Abstractions;

namespace Mda8086Kit.Core.Devices
{
    public class Ppi8255 : IPortDevice
    {
        public string Name { get; }
        
        private readonly int _portA;
        private readonly int _portB;
        private readonly int _portC;
        private readonly int _portCtrl;
        private readonly IWarningSink _warningSink;

        private byte _latchA;
        private byte _latchB;
        private byte _latchC;

        // true = input, false = output
        private bool _dirA = true;
        private bool _dirB = true;
        private bool _dirCUpper = true;
        private bool _dirCLower = true;

        public bool IsConfigured { get; private set; }

        public Ppi8255(string name, int portA, int portB, int portC, int portCtrl, IWarningSink warningSink)
        {
            Name = name;
            _portA = portA;
            _portB = portB;
            _portC = portC;
            _portCtrl = portCtrl;
            _warningSink = warningSink;
            Reset();
        }

        public bool Handles(int port)
        {
            return port == _portA || port == _portB || port == _portC || port == _portCtrl;
        }

        public void Reset()
        {
            _dirA = _dirB = _dirCUpper = _dirCLower = true;
            _latchA = _latchB = _latchC = 0;
            IsConfigured = false;
        }

        public void Tick(long ticks)
        {
            // Ppi doesn't decay
        }

        public void OnCpuWrite(int port, byte value, long ticks)
        {
            if (port == _portCtrl)
            {
                if ((value & 0x80) != 0)
                {
                    // Mode set
                    _dirA = (value & 0x10) != 0;
                    _dirCUpper = (value & 0x08) != 0;
                    _dirB = (value & 0x02) != 0;
                    _dirCLower = (value & 0x01) != 0;
                    
                    if ((value & 0x60) != 0 || (value & 0x04) != 0)
                    {
                        _warningSink?.Warn("PPI-MODE", $"Unsupported mode on {Name}: {value:X2}");
                    }

                    // PRD §5.3: Mode set clears latches
                    _latchA = _latchB = _latchC = 0;
                    IsConfigured = true;
                }
                else
                {
                    // BSR mode
                    if (!IsConfigured) return;
                    int bit = (value >> 1) & 0x07;
                    bool set = (value & 0x01) != 0;
                    
                    bool isOutput = bit >= 4 ? !_dirCUpper : !_dirCLower;
                    if (isOutput)
                    {
                        if (set) _latchC |= (byte)(1 << bit);
                        else _latchC &= (byte)~(1 << bit);
                    }
                }
            }
            else if (port == _portA && !_dirA)
            {
                _latchA = value;
            }
            else if (port == _portB && !_dirB)
            {
                _latchB = value;
            }
            else if (port == _portC)
            {
                byte newC = _latchC;
                if (!_dirCLower)
                {
                    newC = (byte)((newC & 0xF0) | (value & 0x0F));
                }
                if (!_dirCUpper)
                {
                    newC = (byte)((newC & 0x0F) | (value & 0xF0));
                }
                _latchC = newC;
            }
        }

        public PinState GetPinStateA() => new PinState(_latchA, _dirA ? (byte)0 : (byte)0xFF);
        public PinState GetPinStateB() => new PinState(_latchB, _dirB ? (byte)0 : (byte)0xFF);
        public PinState GetPinStateC()
        {
            byte driven = (byte)((_dirCUpper ? 0 : 0xF0) | (_dirCLower ? 0 : 0x0F));
            return new PinState(_latchC, driven);
        }
    }
}
