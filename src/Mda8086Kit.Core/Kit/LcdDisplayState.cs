using System;

namespace Mda8086Kit.Core.Kit
{
    public class LcdDisplayState
    {
        public byte[,] DdRam { get; }
        public byte[] CgRam { get; }
        public int CursorRow { get; }
        public int CursorCol { get; }
        public bool DisplayOn { get; }
        public bool CursorOn { get; }
        public bool BlinkOn { get; }

        public LcdDisplayState(Devices.Hd44780Lcd lcd)
        {
            if (lcd == null) return;
            
            DdRam = new byte[2, 16];
            Array.Copy(lcd.DdRam, DdRam, 32);
            
            CgRam = new byte[64];
            Array.Copy(lcd.CgRam, CgRam, 64);
            
            CursorRow = lcd.CursorRow;
            CursorCol = lcd.CursorCol;
            DisplayOn = lcd.DisplayOn;
            CursorOn = lcd.CursorOn;
            BlinkOn = lcd.BlinkOn;
        }
    }
}
