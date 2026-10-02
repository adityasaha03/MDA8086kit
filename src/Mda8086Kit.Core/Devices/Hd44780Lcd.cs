using System;

namespace Mda8086Kit.Core.Devices
{
    public class Hd44780Lcd
    {
        // 2x16 standard HD44780 display
        public byte[,] DdRam { get; } = new byte[2, 16];
        public byte[] CgRam { get; } = new byte[64];
        
        public int CursorRow { get; private set; }
        public int CursorCol { get; private set; }
        public bool DisplayOn { get; private set; }
        public bool CursorOn { get; private set; }
        public bool BlinkOn { get; private set; }

        private bool _isCgRamAddress;
        private int _addressCounter;

        public Hd44780Lcd()
        {
            Reset();
        }

        public void Reset()
        {
            CursorRow = 0;
            CursorCol = 0;
            // Defaulting DisplayOn to true mitigates dropped initialization commands 
            // when emu8086 runs too fast (per PH4-05 power-up options).
            DisplayOn = true;
            CursorOn = false;
            BlinkOn = false;
            _addressCounter = 0;
            _isCgRamAddress = false;
            
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 16; c++)
                    DdRam[r, c] = 0x20; // Space
        }

        public void WriteCommand(byte cmd)
        {
            if (cmd == 0x01)
            {
                // Clear Display (doesn't turn off DisplayOn)
                _addressCounter = 0;
                _isCgRamAddress = false;
                CursorRow = 0;
                CursorCol = 0;
                for (int r = 0; r < 2; r++)
                    for (int c = 0; c < 16; c++)
                        DdRam[r, c] = 0x20; // Space
            }
            else if ((cmd & 0xF8) == 0x08)
            {
                // Display On/Off Control
                DisplayOn = (cmd & 0x04) != 0;
                CursorOn = (cmd & 0x02) != 0;
                BlinkOn = (cmd & 0x01) != 0;
            }
            else if ((cmd & 0x80) == 0x80)
            {
                // Set DDRAM Address
                _isCgRamAddress = false;
                _addressCounter = cmd & 0x7F;
                UpdateCursorFromAddress();
            }
            else if ((cmd & 0xC0) == 0x40)
            {
                // Set CGRAM Address
                _isCgRamAddress = true;
                _addressCounter = cmd & 0x3F;
            }
        }

        public void WriteData(byte data)
        {
            if (_isCgRamAddress)
            {
                CgRam[_addressCounter & 0x3F] = data;
                _addressCounter = (_addressCounter + 1) & 0x3F;
            }
            else
            {
                DdRam[CursorRow, CursorCol] = data;
                _addressCounter++;
                
                // Typical HD44780 auto-wrap for 2x16 displays
                if (_addressCounter == 16) _addressCounter = 0x40; // Line 2 start
                else if (_addressCounter == 0x50) _addressCounter = 0x00; // Loop back
                
                UpdateCursorFromAddress();
            }
        }

        private void UpdateCursorFromAddress()
        {
            if (_addressCounter >= 0x40)
            {
                CursorRow = 1;
                CursorCol = Math.Min(15, _addressCounter - 0x40);
            }
            else
            {
                CursorRow = 0;
                CursorCol = Math.Min(15, _addressCounter);
            }
        }
    }
}
