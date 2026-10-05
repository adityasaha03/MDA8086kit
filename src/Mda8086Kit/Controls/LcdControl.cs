using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Mda8086Kit.Core.Kit;

namespace Mda8086Kit.Controls
{
    public class LcdControl : Control
    {
        private LcdDisplayState _state;
        
        public LcdControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        public void UpdateState(LcdDisplayState state)
        {
            _state = state;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle bgRect = new Rectangle(0, 0, Width - 1, Height - 1);
            
            // Draw gradient background
            using (GraphicsPath path = GetRoundedRectPath(bgRect, 10))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(bgRect, Color.FromArgb(179, 214, 85), Color.FromArgb(152, 191, 55), LinearGradientMode.Vertical))
                {
                    e.Graphics.FillPath(b, path);
                }
                
                // Inner shadow effect (simple border)
                using (Pen p = new Pen(Color.FromArgb(122, 160, 42), 2))
                {
                    e.Graphics.DrawPath(p, path);
                }
            }
            
            int padX = 15;
            int padY = 10;
            int gridW = Width - padX * 2;
            int gridH = Height - padY * 2;

            int cellW = gridW / 16;
            int cellH = gridH / 2;

            // Draw character cells
            using (Brush bCellOff = new SolidBrush(Color.FromArgb(50, 140, 180, 60)))
            {
                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 16; c++)
                    {
                        Rectangle cellRect = new Rectangle(padX + c * cellW + 1, padY + r * cellH + 1, cellW - 2, cellH - 2);
                        e.Graphics.FillRectangle(bCellOff, cellRect);
                    }
                }
            }

            if (_state == null || !_state.DisplayOn)
            {
                return;
            }

            using (Font f = new Font("Consolas", cellH * 0.7f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Brush bText = new SolidBrush(Color.FromArgb(40, 50, 20)))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 16; c++)
                    {
                        byte charCode = _state.DdRam[r, c];
                        char ch = (char)charCode;
                        
                        Rectangle cellRect = new Rectangle(padX + c * cellW, padY + r * cellH, cellW, cellH);

                        if (_state.CursorOn && _state.CursorRow == r && _state.CursorCol == c)
                        {
                            e.Graphics.FillRectangle(bText, cellRect.X + 2, cellRect.Bottom - 4, cellRect.Width - 4, 3);
                        }

                        if (ch > 32 && ch < 127)
                        {
                            e.Graphics.DrawString(ch.ToString(), f, bText, cellRect, sf);
                        }
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
