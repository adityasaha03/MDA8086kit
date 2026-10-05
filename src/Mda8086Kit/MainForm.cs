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
            Size = new Size(880, 520);
            BackColor = Color.FromArgb(235, 240, 246);
            
            _controller = new KitController(); 
            
            // Add real devices
            _controller.AddDevice(new Core.Devices.Ppi8255("CS1", 0x18, 0x1A, 0x1C, 0x1E, null));
            _controller.AddDevice(new Core.Devices.Ppi8255("CS2", 0x19, 0x1B, 0x1D, 0x1F, null));
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
            int margin = 20;

            // 7 Segment
            _sevenSeg1 = new SevenSegControl();
            var card7Seg = new ModuleCard 
            {
                Title = "7-SEGMENT",
                ContentControl = _sevenSeg1,
                Location = new Point(margin, margin),
                Size = new Size(200, 240)
            };
            card7Seg.ClearClicked += (s, e) => _controller.ClearDevice("7-SEGMENT");
            Controls.Add(card7Seg);

            // LEDs
            _ledControl = new LedControl();
            var cardLeds = new ModuleCard 
            {
                Title = "LEDS",
                ContentControl = _ledControl,
                Location = new Point(card7Seg.Right + margin, margin),
                Size = new Size(280, 240)
            };
            cardLeds.ClearClicked += (s, e) => _controller.ClearDevice("LEDS");
            Controls.Add(cardLeds);

            // Dot Matrix
            _dotMatrix = new DotMatrixControl();
            var cardMatrix = new ModuleCard 
            {
                Title = "DOT MATRIX 8 x 8",
                ContentControl = _dotMatrix,
                Location = new Point(cardLeds.Right + margin, margin),
                Size = new Size(400, 440)
            };
            cardMatrix.ClearClicked += (s, e) => _controller.ClearDevice("DOT MATRIX 8 x 8");
            Controls.Add(cardMatrix);

            // LCD
            _lcdControl = new LcdControl();
            var cardLcd = new ModuleCard 
            {
                Title = "LCD 16 x 2",
                ContentControl = _lcdControl,
                Location = new Point(margin, card7Seg.Bottom + margin),
                Size = new Size(cardLeds.Right - margin, 180)
            };
            cardLcd.ClearClicked += (s, e) => _controller.ClearDevice("LCD 16 x 2");
            Controls.Add(cardLcd);

            // Adjust form size to fit perfectly with room for Reset
            ClientSize = new Size(cardMatrix.Right + margin, cardMatrix.Bottom + margin + 60);

            var pinBtn = new Button
            {
                Text = "📌 PIN TOP",
                Location = new Point(margin, cardMatrix.Bottom + margin),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            pinBtn.FlatAppearance.BorderSize = 0;
            pinBtn.Click += (s, e) => 
            {
                this.TopMost = !this.TopMost;
                pinBtn.BackColor = this.TopMost ? Color.FromArgb(40, 167, 69) : Color.FromArgb(108, 117, 125);
            };
            Controls.Add(pinBtn);

            var aboutBtn = new Button
            {
                Text = "ℹ ABOUT",
                Location = new Point(pinBtn.Right + margin, pinBtn.Top),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(23, 162, 184),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            aboutBtn.FlatAppearance.BorderSize = 0;
            aboutBtn.Click += (s, e) => 
            {
                MessageBox.Show(
                    "MDA-8086 Virtual Kit (unofficial)\n\n" +
                    "An emulator interface for the MDA-8086 trainer kit, designed to connect with emu8086.\n\n" +
                    "Version 1.0\n",
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(aboutBtn);

            var portsBtn = new Button
            {
                Text = "? PORTS",
                Location = new Point(ClientSize.Width - 100 - margin, pinBtn.Top),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(253, 126, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            portsBtn.FlatAppearance.BorderSize = 0;
            portsBtn.Click += (s, e) => 
            {
                MessageBox.Show(
                    "MDA-8086 Port Assignments:\n\n" +
                    "CS1 (Dot Matrix)\n- Port A: 18H\n- Port B: 1AH\n- Port C: 1CH\n- Control: 1EH\n\n" +
                    "CS2 (7-Segment & LEDs)\n- Port A: 19H\n- Port B: 1BH\n- Port C: 1DH\n- Control: 1FH\n\n" +
                    "LCD 16x2\n- Control: 30H\n- Data: 32H",
                    "Port Reference Guide", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(portsBtn);
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
