using System;

namespace Mda8086Kit.Core.Kit
{
    public class DotMatrixState
    {
        public float[] RedDisplay { get; }
        public float[] GreenDisplay { get; }
        public Devices.DotMatrixOrientation Orientation { get; }

        public DotMatrixState(float[] redDisplay, float[] greenDisplay, Devices.DotMatrixOrientation orientation)
        {
            RedDisplay = new float[64];
            GreenDisplay = new float[64];
            Array.Copy(redDisplay, RedDisplay, 64);
            Array.Copy(greenDisplay, GreenDisplay, 64);
            Orientation = orientation;
        }
    }
}
