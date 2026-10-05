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
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
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

            // Draw dark rounded background
            Rectangle bgRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = GetRoundedRectPath(bgRect, 10))
            {
                using (Brush b = new SolidBrush(Color.FromArgb(15, 17, 21)))
                {
                    e.Graphics.FillPath(b, path);
                }
            }

            // Draw segments
            int pad = 20;
            Rectangle bounds = new Rectangle(pad, pad, Width - pad * 2, Height - pad * 2);
            
            Color activeColor = Color.FromArgb(255, 74, 92);
            Color inactiveColor = Color.FromArgb(40, 255, 74, 92); // very dim

            int thickness = Math.Max(4, Width / 12);
            
            using (Pen pA = new Pen(_state.A ? activeColor : inactiveColor, thickness))
            using (Pen pB = new Pen(_state.B ? activeColor : inactiveColor, thickness))
            using (Pen pC = new Pen(_state.C ? activeColor : inactiveColor, thickness))
            using (Pen pD = new Pen(_state.D ? activeColor : inactiveColor, thickness))
            using (Pen pE = new Pen(_state.E ? activeColor : inactiveColor, thickness))
            using (Pen pF = new Pen(_state.F ? activeColor : inactiveColor, thickness))
            using (Pen pG = new Pen(_state.G ? activeColor : inactiveColor, thickness))
            using (Brush bDP = new SolidBrush(_state.DecimalPoint ? activeColor : inactiveColor))
            {
                pA.StartCap = pA.EndCap = LineCap.Round;
                pB.StartCap = pB.EndCap = LineCap.Round;
                pC.StartCap = pC.EndCap = LineCap.Round;
                pD.StartCap = pD.EndCap = LineCap.Round;
                pE.StartCap = pE.EndCap = LineCap.Round;
                pF.StartCap = pF.EndCap = LineCap.Round;
                pG.StartCap = pG.EndCap = LineCap.Round;

                int w = bounds.Width / 2;
                int h = bounds.Height / 2;
                int offsetX = (bounds.Width - w) / 2;
                
                int gap = thickness / 2;
                
                // Top (A)
                e.Graphics.DrawLine(pA, bounds.Left + offsetX + gap, bounds.Top, bounds.Left + offsetX + w - gap, bounds.Top);
                // Top Right (B)
                e.Graphics.DrawLine(pB, bounds.Left + offsetX + w, bounds.Top + gap, bounds.Left + offsetX + w, bounds.Top + h - gap);
                // Bottom Right (C)
                e.Graphics.DrawLine(pC, bounds.Left + offsetX + w, bounds.Top + h + gap, bounds.Left + offsetX + w, bounds.Bottom - gap);
                // Bottom (D)
                e.Graphics.DrawLine(pD, bounds.Left + offsetX + gap, bounds.Bottom, bounds.Left + offsetX + w - gap, bounds.Bottom);
                // Bottom Left (E)
                e.Graphics.DrawLine(pE, bounds.Left + offsetX, bounds.Top + h + gap, bounds.Left + offsetX, bounds.Bottom - gap);
                // Top Left (F)
                e.Graphics.DrawLine(pF, bounds.Left + offsetX, bounds.Top + gap, bounds.Left + offsetX, bounds.Top + h - gap);
                // Middle (G)
                e.Graphics.DrawLine(pG, bounds.Left + offsetX + gap, bounds.Top + h, bounds.Left + offsetX + w - gap, bounds.Top + h);
                
                // DP
                e.Graphics.FillEllipse(bDP, bounds.Left + offsetX + w + gap, bounds.Bottom - thickness, thickness, thickness);
            }
        }
        
        private GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
