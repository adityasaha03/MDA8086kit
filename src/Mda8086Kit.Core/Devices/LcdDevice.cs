using System;
using Mda8086Kit.Core.Abstractions;

namespace Mda8086Kit.Core.Devices
{
    public class LcdDevice : IPortDevice
    {
        public string Name => "LCD";
        public Hd44780Lcd Model { get; } = new Hd44780Lcd();

        public void Reset()
        {
            Model.Reset();
        }

        private byte _lastDataValue;
        private long _lastDataWriteTicks;

        public bool Handles(int port)
        {
            // Usually wired to specific ports. The plan notes 00H, 02H, 04H.
            // 0x30 is the PH4-06 optional sequence port to bypass the write-once limitation.
            return port == 0x00 || port == 0x02 || port == 0x04 || port == 0x30;
        }

        public void OnCpuWrite(int port, byte value, long ticks)
        {
            if (port == 0x00)
            {
                // Instruction Register
                Model.WriteCommand(value);
            }
            else if (port == 0x04)
            {
                // Data Register
                _lastDataValue = value;
                _lastDataWriteTicks = ticks;
                Model.WriteData(value);
            }
            else if (port == 0x30)
            {
                // Sequence Port (Mitigation for F1: consecutive identical writes)
                // If 04H was written in this exact same poll batch, we already wrote the data.
                // If 04H was NOT written (because the value was identical to the previous one),
                // we use this sequence port write to trigger the duplicate data write.
                if (_lastDataWriteTicks != ticks)
                {
                    Model.WriteData(_lastDataValue);
                }
            }
        }

        public void Tick(long ticks)
        {
            // No time-based degradation for the LCD state needed yet
        }
    }
}
