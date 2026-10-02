namespace Mda8086Kit.Core.Devices
{
    public readonly struct LedState
    {
        public bool R1 { get; }
        public bool G { get; }
        public bool Y { get; }
        public bool R2 { get; }
        
        public LedState(bool r1, bool g, bool y, bool r2)
        {
            R1 = r1;
            G = g;
            Y = y;
            R2 = r2;
        }
    }

    public class LedBank
    {
        public LedState Decode(PinState portB)
        {
            // Active-high. bit0 = R1, bit1 = G, bit2 = Y, bit3 = R2
            // Only light if the pin is driven.
            bool driven = portB.DrivenMask == 0xFF;
            
            if (!driven)
            {
                return new LedState(false, false, false, false);
            }

            byte val = portB.Value;
            return new LedState(
                r1: (val & 0x01) != 0,
                g:  (val & 0x02) != 0,
                y:  (val & 0x04) != 0,
                r2: (val & 0x08) != 0
            );
        }
    }
}
