namespace Mda8086Kit.Core
{
    public struct PortData
    {
        public int Port { get; }
        public byte Value { get; }
        public long TimestampUs { get; }

        public PortData(int port, byte value, long timestampUs)
        {
            Port = port;
            Value = value;
            TimestampUs = timestampUs;
        }
    }
}
