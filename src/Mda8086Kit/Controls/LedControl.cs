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
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
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

            int ledRadius = Math.Min(Width / 8, Height / 2) - 4;
            int spacing = Width / 4;
            
            DrawLed(e.Graphics, "R1", 0 * spacing + spacing / 2, Height / 2, ledRadius, Color.Red, _state.R1);
            DrawLed(e.Graphics, "G",  1 * spacing + spacing / 2, Height / 2, ledRadius, Color.LimeGreen, _state.G);
            DrawLed(e.Graphics, "Y",  2 * spacing + spacing / 2, Height / 2, ledRadius, Color.Gold, _state.Y);
            DrawLed(e.Graphics, "R2", 3 * spacing + spacing / 2, Height / 2, ledRadius, Color.Red, _state.R2);
        }

        private void DrawLed(Graphics g, string label, int cx, int cy, int radius, Color activeColor, bool isOn)
        {
            Rectangle rect = new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2);
            
            using (Brush b = new SolidBrush(isOn ? activeColor : Color.FromArgb(64, activeColor)))
            {
                g.FillEllipse(b, rect);
            }
            
            using (Pen p = new Pen(Color.FromArgb(100, 255, 255, 255), 2))
            {
                g.DrawEllipse(p, rect);
            }

            using (Font f = new Font("Segoe UI", radius / 1.5f, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.White, cx - size.Width / 2, cy - size.Height / 2);
            }
        }
    }
}
