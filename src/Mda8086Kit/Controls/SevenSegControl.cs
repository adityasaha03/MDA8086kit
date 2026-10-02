using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Mda8086Kit.Core.Devices;

namespace Mda8086Kit.Controls
{
    public class SevenSegControl : Control
    {
        private SegmentState _state;
        
        public SevenSegControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        public void UpdateState(SegmentState state)
        {
            _state = state;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Simplified bounding box
            Rectangle bounds = new Rectangle(10, 10, Width - 20, Height - 20);
            
            Color activeColor = Color.Red;
            Color inactiveColor = Color.FromArgb(40, Color.Red);

            using (Pen pA = new Pen(_state.A ? activeColor : inactiveColor, 5))
            using (Pen pB = new Pen(_state.B ? activeColor : inactiveColor, 5))
            using (Pen pC = new Pen(_state.C ? activeColor : inactiveColor, 5))
            using (Pen pD = new Pen(_state.D ? activeColor : inactiveColor, 5))
            using (Pen pE = new Pen(_state.E ? activeColor : inactiveColor, 5))
            using (Pen pF = new Pen(_state.F ? activeColor : inactiveColor, 5))
            using (Pen pG = new Pen(_state.G ? activeColor : inactiveColor, 5))
            using (Brush bDP = new SolidBrush(_state.DecimalPoint ? activeColor : inactiveColor))
            {
                int w = bounds.Width - 10;
                int h = bounds.Height / 2;
                
                // Top (A)
                e.Graphics.DrawLine(pA, bounds.Left, bounds.Top, bounds.Left + w, bounds.Top);
                // Top Right (B)
                e.Graphics.DrawLine(pB, bounds.Left + w, bounds.Top, bounds.Left + w, bounds.Top + h);
                // Bottom Right (C)
                e.Graphics.DrawLine(pC, bounds.Left + w, bounds.Top + h, bounds.Left + w, bounds.Bottom);
                // Bottom (D)
                e.Graphics.DrawLine(pD, bounds.Left, bounds.Bottom, bounds.Left + w, bounds.Bottom);
                // Bottom Left (E)
                e.Graphics.DrawLine(pE, bounds.Left, bounds.Top + h, bounds.Left, bounds.Bottom);
                // Top Left (F)
                e.Graphics.DrawLine(pF, bounds.Left, bounds.Top, bounds.Left, bounds.Top + h);
                // Middle (G)
                e.Graphics.DrawLine(pG, bounds.Left, bounds.Top + h, bounds.Left + w, bounds.Top + h);
                
                // DP
                e.Graphics.FillEllipse(bDP, bounds.Right - 8, bounds.Bottom - 8, 8, 8);
            }
        }
    }
}
