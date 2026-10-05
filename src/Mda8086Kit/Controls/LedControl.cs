using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Mda8086Kit.Core.Devices;

namespace Mda8086Kit.Controls
{
    public class LedControl : Control
    {
        private LedState _state;
        
        public LedControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        public void UpdateState(LedState state)
        {
            _state = state;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int padding = 5;
            int gap = 10;
            int cardW = (Width - padding * 2 - gap) / 2;
            int cardH = (Height - padding * 2 - gap) / 2;

            // R1
            DrawLedCard(e.Graphics, new Rectangle(padding, padding, cardW, cardH), "R1", Color.FromArgb(255, 100, 100), _state.R1);
            // G
            DrawLedCard(e.Graphics, new Rectangle(padding + cardW + gap, padding, cardW, cardH), "G", Color.FromArgb(100, 255, 150), _state.G);
            // Y
            DrawLedCard(e.Graphics, new Rectangle(padding, padding + cardH + gap, cardW, cardH), "Y", Color.FromArgb(255, 220, 100), _state.Y);
            // R2
            DrawLedCard(e.Graphics, new Rectangle(padding + cardW + gap, padding + cardH + gap, cardW, cardH), "R2", Color.FromArgb(255, 100, 100), _state.R2);
        }

        private void DrawLedCard(Graphics g, Rectangle bounds, string label, Color activeColor, bool isOn)
        {
            // Inner card background
            using (GraphicsPath path = GetRoundedRectPath(bounds, 12))
            {
                using (Brush b = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.FillPath(b, path);
                }
                using (Pen p = new Pen(Color.FromArgb(235, 240, 245)))
                {
                    g.DrawPath(p, path);
                }
            }

            // Draw LED
            int ledRadius = Math.Min(bounds.Width / 4, bounds.Height / 3);
            int cx = bounds.Left + bounds.Width / 3;
            int cy = bounds.Top + bounds.Height / 2;

            Rectangle ledRect = new Rectangle(cx - ledRadius, cy - ledRadius, ledRadius * 2, ledRadius * 2);

            Color ledBase = isOn ? activeColor : Color.FromArgb(40, activeColor);
            
            using (SolidBrush sb = new SolidBrush(ledBase))
            {
                g.FillEllipse(sb, ledRect);
            }

            using (Pen borderPen = new Pen(Color.FromArgb(60, Color.Black), 1.5f))
            {
                g.DrawEllipse(borderPen, ledRect);
            }

            // Label
            using (Font f = new Font("Segoe UI", ledRadius / 1.5f, FontStyle.Bold))
            {
                using (Brush b = new SolidBrush(Color.FromArgb(100, 110, 130)))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    Rectangle textRect = new Rectangle(cx + ledRadius, bounds.Top, bounds.Width - (cx - bounds.Left) - ledRadius, bounds.Height);
                    g.DrawString(label, f, b, textRect, sf);
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
