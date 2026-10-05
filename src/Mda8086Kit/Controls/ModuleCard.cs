using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Mda8086Kit.Controls
{
    public class ModuleCard : Panel
    {
        private Label _titleLabel;
        private Button _clearButton;
        private Control _contentControl;

        public string Title
        {
            get => _titleLabel.Text;
            set => _titleLabel.Text = value;
        }

        public Control ContentControl
        {
            get => _contentControl;
            set
            {
                if (_contentControl != null)
                {
                    Controls.Remove(_contentControl);
                }
                _contentControl = value;
                if (_contentControl != null)
                {
                    Controls.Add(_contentControl);
                    PositionContent();
                }
            }
        }

        public event EventHandler ClearClicked;

        public ModuleCard()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Padding = new Padding(15, 50, 15, 15);

            _titleLabel = new Label
            {
                Text = "TITLE",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(15, 15)
            };
            Controls.Add(_titleLabel);

            _clearButton = new Button
            {
                Text = "× Clear",
                Font = new Font("Segoe UI", 8f),
                Size = new Size(60, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 247, 250),
                ForeColor = Color.FromArgb(80, 90, 110),
                Cursor = Cursors.Hand
            };
            _clearButton.FlatAppearance.BorderSize = 1;
            _clearButton.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 235);
            _clearButton.Click += (s, e) => ClearClicked?.Invoke(this, EventArgs.Empty);
            Controls.Add(_clearButton);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (_clearButton != null)
            {
                _clearButton.Location = new Point(Width - _clearButton.Width - 15, 12);
            }
            PositionContent();
        }

        private void PositionContent()
        {
            if (_contentControl != null)
            {
                _contentControl.Location = new Point(Padding.Left, Padding.Top);
                _contentControl.Size = new Size(Width - Padding.Left - Padding.Right, Height - Padding.Top - Padding.Bottom);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Draw rounded background
            using (GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, Width - 1, Height - 1), 12))
            {
                using (Brush b = new SolidBrush(Color.White))
                {
                    e.Graphics.FillPath(b, path);
                }
                using (Pen p = new Pen(Color.FromArgb(230, 235, 240)))
                {
                    e.Graphics.DrawPath(p, path);
                }
            }

            // Draw a subtle lines at bottom right
            e.Graphics.DrawLine(Pens.LightGray, Width - 15, Height - 5, Width - 5, Height - 15);
            e.Graphics.DrawLine(Pens.LightGray, Width - 10, Height - 5, Width - 5, Height - 10);
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
