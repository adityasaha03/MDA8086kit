using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Mda8086Kit.Core.Kit;

namespace Mda8086Kit.Controls
{
    public class DotMatrixControl : Control
    {
        private DotMatrixState _state;
        
        public DotMatrixControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        public void UpdateState(DotMatrixState state)
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
            using (GraphicsPath path = GetRoundedRectPath(bgRect, 12))
            {
                using (Brush b = new SolidBrush(Color.FromArgb(18, 20, 24)))
                {
                    e.Graphics.FillPath(b, path);
                }
            }

            if (_state == null) return;

            int padX = 10;
            int padY = 10;
            int gridW = Width - padX * 2;
            int gridH = Height - padY * 2;
            int cellW = gridW / 8;
            int cellH = gridH / 8;
            int dotPadding = Math.Max(2, cellW / 8);

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    int index = r * 8 + c;
                    float redDuty = _state.RedDisplay[index];
                    float greenDuty = _state.GreenDisplay[index];

                    int rVal = Math.Min(255, (int)(redDuty * 255));
                    int gVal = Math.Min(255, (int)(greenDuty * 255));
                    
                    Color ledColor;
                    if (rVal == 0 && gVal == 0)
                    {
                        ledColor = Color.FromArgb(28, 33, 40); // Off state
                    }
                    else
                    {
                        // Blend to amber if both are on
                        ledColor = Color.FromArgb(255, rVal, gVal, 0);
                    }

                    Rectangle rect = new Rectangle(padX + c * cellW + dotPadding, padY + r * cellH + dotPadding, cellW - dotPadding * 2, cellH - dotPadding * 2);
                    using (Brush b = new SolidBrush(ledColor))
                    {
                        e.Graphics.FillEllipse(b, rect);
                    }
                }
            }
        }
        
        private GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
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
