using System;
using Xunit;
using Mda8086Kit.Core.Devices;

namespace Mda8086Kit.Core.Tests
{
    public class LcdDeviceTests
    {
        [Fact]
        public void Lcd_InitializationAndHelloWorld()
        {
            var lcd = new LcdDevice();

            // Function set
            lcd.OnCpuWrite(0x00, 0x38, 1);
            // Display ON
            lcd.OnCpuWrite(0x00, 0x0C, 2);
            // Entry Mode
            lcd.OnCpuWrite(0x00, 0x06, 3);
            // Clear Display
            lcd.OnCpuWrite(0x00, 0x01, 4);

            Assert.True(lcd.Model.DisplayOn);
            Assert.False(lcd.Model.CursorOn);
            Assert.False(lcd.Model.BlinkOn);
            Assert.Equal(0, lcd.Model.CursorRow);
            Assert.Equal(0, lcd.Model.CursorCol);

            // Write "HELLO"
            lcd.OnCpuWrite(0x00, 0x80, 5); // Line 1
            lcd.OnCpuWrite(0x04, (byte)'H', 6);
            lcd.OnCpuWrite(0x04, (byte)'E', 7);
            lcd.OnCpuWrite(0x04, (byte)'L', 8);
            lcd.OnCpuWrite(0x04, (byte)'L', 9);
            lcd.OnCpuWrite(0x04, (byte)'O', 10);

            Assert.Equal((byte)'H', lcd.Model.DdRam[0, 0]);
            Assert.Equal((byte)'O', lcd.Model.DdRam[0, 4]);
            Assert.Equal(0, lcd.Model.CursorRow);
            Assert.Equal(5, lcd.Model.CursorCol);

            // Write "MDA-8086" on line 2
            lcd.OnCpuWrite(0x00, 0xC0, 11); // Line 2
            Assert.Equal(1, lcd.Model.CursorRow);
            Assert.Equal(0, lcd.Model.CursorCol);

            string line2 = "MDA-8086";
            long tick = 12;
            foreach (char c in line2)
            {
                lcd.OnCpuWrite(0x04, (byte)c, tick++);
            }

            Assert.Equal((byte)'M', lcd.Model.DdRam[1, 0]);
            Assert.Equal((byte)'6', lcd.Model.DdRam[1, 7]);
            Assert.Equal(1, lcd.Model.CursorRow);
            Assert.Equal(8, lcd.Model.CursorCol);
        }
    }
}
