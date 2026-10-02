using System;
using System.Drawing;
using System.Windows.Forms;
using Mda8086Kit.Core.Kit;
using Mda8086Kit.Controls;

namespace Mda8086Kit
{
    public class MainForm : Form
    {
        private readonly KitController _controller;
        private readonly Timer _uiTimer;
        private readonly Core.Kit.BusPoller _busPoller;
        private readonly Io.EmuIoPortFile _ioFile;
        
        private SevenSegControl _sevenSeg1;
        private LedControl _ledControl;
        private DotMatrixControl _dotMatrix;
        private LcdControl _lcdControl;

        public MainForm()
        {
            Text = "MDA-8086 Virtual Kit (unofficial)";
            Size = new Size(800, 600);
            BackColor = Color.FromArgb(30, 30, 30);
            
            _controller = new KitController(); 
            
            // Add real devices
            _controller.AddDevice(new Core.Devices.Ppi8255("CS1", 0x10, 0x12, 0x14, 0x16, null));
            _controller.AddDevice(new Core.Devices.Ppi8255("CS2", 0x18, 0x1A, 0x1C, 0x1E, null));
            _controller.AddDevice(new Core.Devices.LcdDevice());

            // Wire up IO and Poller
            _ioFile = new Io.EmuIoPortFile(@"C:\emu8086.io");
            _busPoller = new Core.Kit.BusPoller(_ioFile, _controller);
            _busPoller.Start();

            InitializeControls();

            _uiTimer = new Timer { Interval = 16 }; // ~60fps
            _uiTimer.Tick += OnUiTimerTick;
            _uiTimer.Start();
        }

        private void InitializeControls()
        {
            // 7 Segment
            _sevenSeg1 = new SevenSegControl
            {
                Location = new Point(50, 50),
                Size = new Size(60, 100)
            };
            Controls.Add(_sevenSeg1);

            // LEDs
            _ledControl = new LedControl
            {
                Location = new Point(50, 200),
                Size = new Size(300, 60)
            };
            Controls.Add(_ledControl);

            // Dot Matrix
            _dotMatrix = new DotMatrixControl
            {
                Location = new Point(400, 50),
                Size = new Size(300, 300)
            };
            Controls.Add(_dotMatrix);

            // LCD
            _lcdControl = new LcdControl
            {
                Location = new Point(50, 300),
                Size = new Size(300, 50)
            };
            Controls.Add(_lcdControl);
        }

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            var snap = _controller.GetLatestSnapshot();
            if (snap == null) return;

            _sevenSeg1.UpdateState(snap.SevenSegmentState);
            _ledControl.UpdateState(snap.LedState);
            
            if (snap.MatrixState != null)
            {
                _dotMatrix.UpdateState(snap.MatrixState);
            }
            if (snap.LcdState != null)
            {
                _lcdControl.UpdateState(snap.LcdState);
            }
        }
    }
}
