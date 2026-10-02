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
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        public void UpdateState(LcdDisplayState state)
        {
            _state = state;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw background (blue/green LCD look)
            e.Graphics.Clear(Color.FromArgb(170, 210, 30));
            
            if (_state == null)
            {
                using (Font f = new Font("Arial", 10))
                using (Brush b = new SolidBrush(Color.Black))
                    e.Graphics.DrawString("STATE IS NULL", f, b, 5, 5);
                return;
            }
            if (!_state.DisplayOn)
            {
                using (Font f = new Font("Arial", 10))
                using (Brush b = new SolidBrush(Color.Black))
                    e.Graphics.DrawString("DISPLAY OFF", f, b, 5, 5);
                return;
            }

            int cellW = Width / 16;
            int cellH = Height / 2;

            using (Font f = new Font("Courier New", cellH * 0.7f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Brush b = new SolidBrush(Color.FromArgb(40, 40, 40)))
            {


                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 16; c++)
                    {
                        byte charCode = _state.DdRam[r, c];
                        char ch = (char)charCode;
                        
                        if (_state.CursorOn && _state.CursorRow == r && _state.CursorCol == c)
                        {
                            e.Graphics.FillRectangle(b, c * cellW, r * cellH + (cellH - 4), cellW, 4);
                        }

                        if (ch > 32 && ch < 127)
                        {
                            e.Graphics.DrawString(ch.ToString(), f, b, c * cellW, r * cellH);
                        }
                    }
                }
            }
        }
    }
}
