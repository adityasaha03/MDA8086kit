using System;

namespace Mda8086Kit.Core.Devices
{
    public class DotMatrixModel
    {
        public void Decode(PinState portA, PinState portB, PinState portC, DotMatrixOrientation orientation, out ulong redGrid, out ulong greenGrid)
        {
            redGrid = 0;
            greenGrid = 0;
            
            byte valA = portA.Value;
            byte valB = portB.Value;
            byte valC = portC.Value;

            byte scan;
            byte redData;
            byte greenData;

            // Hardware correct mode:
            // Port B = Scan (active-low)
            // Port C = Green Data (active-high)
            // Port A = Red Data (active-low)
            scan = (byte)(~portB.Value & portB.DrivenMask);
            redData = (byte)(~portA.Value & portA.DrivenMask);
            greenData = (byte)(portC.Value & portC.DrivenMask);

            for (int k = 0; k < 8; k++) // scan line (column in Normal)
            {
                if ((scan & (1 << k)) != 0)
                {
                    for (int j = 0; j < 8; j++) // data bit (row in Normal)
                    {
                        bool isRed = (redData & (1 << j)) != 0;
                        bool isGreen = (greenData & (1 << j)) != 0;
                        
                        if (isRed || isGreen)
                        {
                            TransformCoordinates(k, j, orientation, out int c, out int r);
                            int shift = r * 8 + c;
                            if (isRed) redGrid |= (1UL << shift);
                            if (isGreen) greenGrid |= (1UL << shift);
                        }
                    }
                }
            }
        }

        private void TransformCoordinates(int k, int j, DotMatrixOrientation orientation, out int c, out int r)
        {
            // Normal: port C bit k is left-to-right (0=left), port B bit j is bottom-to-top (7=top)
            int logicalC = k;
            int logicalR = 7 - j;

            switch (orientation)
            {
                case DotMatrixOrientation.Normal:
                    c = logicalC; r = logicalR; break;
                case DotMatrixOrientation.FlipH:
                    c = 7 - logicalC; r = logicalR; break;
                case DotMatrixOrientation.FlipV:
                    c = logicalC; r = 7 - logicalR; break;
                case DotMatrixOrientation.Rotate180:
                    c = 7 - logicalC; r = 7 - logicalR; break;
                case DotMatrixOrientation.Rotate90:
                    c = 7 - logicalR; r = logicalC; break;
                case DotMatrixOrientation.Rotate270:
                    c = logicalR; r = 7 - logicalC; break;
                case DotMatrixOrientation.Transpose:
                    c = logicalR; r = logicalC; break;
                case DotMatrixOrientation.AntiTranspose:
                    c = 7 - logicalR; r = 7 - logicalC; break;
                default:
                    c = logicalC; r = logicalR; break;
            }
        }
    }
}
