namespace Mda8086Kit.Core.Devices
{
    public readonly struct PinState
    {
        public byte Value { get; }
        public byte DrivenMask { get; }

        public PinState(byte value, byte drivenMask)
        {
            Value = value;
            DrivenMask = drivenMask;
        }
    }
}
