using System.Collections.Generic;

namespace Mda8086Kit.Core.Kit
{
    public class KitSnapshot
    {
        public Abstractions.ConnectionState ConnectionState { get; }
        public long SequenceNumber { get; }
        public long LastChangeTicks { get; }

        // Phase 2 State
        public Devices.SegmentState SevenSegmentState { get; }
        public Devices.LedState LedState { get; }
        
        // Phase 3 State
        public DotMatrixState MatrixState { get; }

        // Phase 4 State
        public LcdDisplayState LcdState { get; }
        
        public KitSnapshot(
            Abstractions.ConnectionState state, 
            long sequenceNumber, 
            long lastChangeTicks,
            Devices.SegmentState sevenSegmentState,
            Devices.LedState ledState,
            DotMatrixState matrixState,
            LcdDisplayState lcdState)
        {
            ConnectionState = state;
            SequenceNumber = sequenceNumber;
            LastChangeTicks = lastChangeTicks;
            SevenSegmentState = sevenSegmentState;
            LedState = ledState;
            MatrixState = matrixState;
            LcdState = lcdState;
        }
    }
}
