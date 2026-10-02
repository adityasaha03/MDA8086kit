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
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
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
            e.Graphics.Clear(Color.Black);

            if (_state == null) return;

            int cellW = Width / 8;
            int cellH = Height / 8;
            int padding = 2;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    int index = r * 8 + c;
                    float redDuty = _state.RedDisplay[index];
                    float greenDuty = _state.GreenDisplay[index];

                    int rVal = Math.Min(255, (int)(redDuty * 255));
                    int gVal = Math.Min(255, (int)(greenDuty * 255));
                    
                    // Blend to amber if both are on
                    Color ledColor = Color.FromArgb(255, rVal, gVal, 0);
                    
                    if (rVal == 0 && gVal == 0)
                    {
                        ledColor = Color.FromArgb(40, 40, 40); // Off state
                    }

                    Rectangle rect = new Rectangle(c * cellW + padding, r * cellH + padding, cellW - padding * 2, cellH - padding * 2);
                    using (Brush b = new SolidBrush(ledColor))
                    {
                        e.Graphics.FillEllipse(b, rect);
                    }
                }
            }
        }
    }
}
