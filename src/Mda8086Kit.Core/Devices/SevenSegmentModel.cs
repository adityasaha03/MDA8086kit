namespace Mda8086Kit.Core.Devices
{
    public readonly struct SegmentState
    {
        public bool A { get; }
        public bool B { get; }
        public bool C { get; }
        public bool D { get; }
        public bool E { get; }
        public bool F { get; }
        public bool G { get; }
        public bool DecimalPoint { get; }
        public bool Driven { get; }

        public SegmentState(bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool dp, bool driven)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
            G = g;
            DecimalPoint = dp;
            Driven = driven;
        }
    }

    public class SevenSegmentModel
    {
        public SegmentState Decode(PinState portA)
        {
            // If the port is not driven (e.g. configured as input), the display is blank.
            // Assumption (Q-07): undriven is blank.
            bool driven = portA.DrivenMask == 0xFF;
            
            if (!driven)
            {
                return new SegmentState(false, false, false, false, false, false, false, false, false);
            }

            // Active low: 0 is lit, 1 is off
            byte val = portA.Value;
            return new SegmentState(
                a: (val & 0x01) == 0,
                b: (val & 0x02) == 0,
                c: (val & 0x04) == 0,
                d: (val & 0x08) == 0,
                e: (val & 0x10) == 0,
                f: (val & 0x20) == 0,
                g: (val & 0x40) == 0,
                dp: (val & 0x80) == 0,
                driven: true
            );
        }
    }
}
