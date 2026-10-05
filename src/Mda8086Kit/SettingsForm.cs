using System;
using System.Drawing;
using System.Windows.Forms;
using Mda8086Kit.Io;
using System.Collections.Generic;

namespace Mda8086Kit
{
    public class SettingsForm : Form
    {
        private UserSettingsStore _settingsStore;
        private Dictionary<string, string> _currentSettings;

        // Display Tuning Controls
        private GroupBox _grpDisplayTuning;
        private NumericUpDown _numPollInterval;
        private ComboBox _cmbLcdPowerUpState;

        // Hardware Mapping Controls
        private GroupBox _grpHardwareMapping;
        private CheckBox _chkAdvanced;
        private DataGridView _dgvPortMap;
        private Button _btnRestoreDefaults;

        private Button _btnSave;
        private Button _btnCancel;

        public SettingsForm()
        {
            _settingsStore = new UserSettingsStore();
            _currentSettings = _settingsStore.LoadSettings();

            InitializeComponent();
            LoadSettingsIntoUI();
        }

        private void InitializeComponent()
        {
            this.Text = "MDA8086 Kit Settings";
            this.Size = new Size(500, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Display Tuning Group
            _grpDisplayTuning = new GroupBox()
            {
                Text = "Display Tuning",
                Location = new Point(10, 10),
                Size = new Size(460, 150)
            };

            var lblPollInterval = new Label() { Text = "Poll Interval (ms):", Location = new Point(20, 30), AutoSize = true };
            _numPollInterval = new NumericUpDown() { Location = new Point(150, 28), Minimum = 1, Maximum = 1000, Value = 10 };
            
            var lblLcdPowerUp = new Label() { Text = "LCD Power-up State:", Location = new Point(20, 70), AutoSize = true };
            _cmbLcdPowerUpState = new ComboBox() { Location = new Point(150, 68), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbLcdPowerUpState.Items.AddRange(new string[] { "Hd44780", "MonitorLike" });
            _cmbLcdPowerUpState.SelectedIndex = 0;

            _grpDisplayTuning.Controls.AddRange(new Control[] { lblPollInterval, _numPollInterval, lblLcdPowerUp, _cmbLcdPowerUpState });

            // Hardware Mapping Group
            _grpHardwareMapping = new GroupBox()
            {
                Text = "Hardware Mapping",
                Location = new Point(10, 170),
                Size = new Size(460, 300)
            };

            _chkAdvanced = new CheckBox() { Text = "Enable Advanced Port Mapping", Location = new Point(20, 30), AutoSize = true };
            _chkAdvanced.CheckedChanged += ChkAdvanced_CheckedChanged;

            _dgvPortMap = new DataGridView()
            {
                Location = new Point(20, 60),
                Size = new Size(420, 190),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                Enabled = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _dgvPortMap.Columns.Add("Device", "Device");
            _dgvPortMap.Columns.Add("Port", "Port (Hex)");
            
            _btnRestoreDefaults = new Button() { Text = "Restore Defaults", Location = new Point(20, 260), Size = new Size(120, 30), Enabled = false };
            _btnRestoreDefaults.Click += BtnRestoreDefaults_Click;

            _grpHardwareMapping.Controls.AddRange(new Control[] { _chkAdvanced, _dgvPortMap, _btnRestoreDefaults });

            // Action Buttons
            _btnSave = new Button() { Text = "Save", Location = new Point(310, 490), Size = new Size(75, 30) };
            _btnSave.Click += BtnSave_Click;
            
            _btnCancel = new Button() { Text = "Cancel", Location = new Point(395, 490), Size = new Size(75, 30) };
            _btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { _grpDisplayTuning, _grpHardwareMapping, _btnSave, _btnCancel });
        }

        private void ChkAdvanced_CheckedChanged(object sender, EventArgs e)
        {
            if (_chkAdvanced.Checked)
            {
                var result = MessageBox.Show(
                    "Warning: Changing the hardware port map can cause lab programs to stop working. Are you sure you want to proceed?",
                    "Advanced Mode", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    _dgvPortMap.Enabled = true;
                    _btnRestoreDefaults.Enabled = true;
                }
                else
                {
                    _chkAdvanced.Checked = false;
                }
            }
            else
            {
                _dgvPortMap.Enabled = false;
                _btnRestoreDefaults.Enabled = false;
            }
        }

        private void BtnRestoreDefaults_Click(object sender, EventArgs e)
        {
            LoadDefaultPortMap();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _currentSettings["PollInterval"] = _numPollInterval.Value.ToString();
            _currentSettings["LcdPowerUpState"] = _cmbLcdPowerUpState.SelectedItem.ToString();
            
            // Note: Save Port map settings here too.

            _settingsStore.SaveSettings(_currentSettings);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void LoadSettingsIntoUI()
        {
            if (_currentSettings.TryGetValue("PollInterval", out var pollIntervalStr) && decimal.TryParse(pollIntervalStr, out var pollInterval))
            {
                _numPollInterval.Value = Math.Max(_numPollInterval.Minimum, Math.Min(_numPollInterval.Maximum, pollInterval));
            }

            if (_currentSettings.TryGetValue("LcdPowerUpState", out var lcdState))
            {
                _cmbLcdPowerUpState.SelectedItem = lcdState;
            }
            
            LoadDefaultPortMap(); // Populate DataGridView with placeholder or current map
        }

        private void LoadDefaultPortMap()
        {
            _dgvPortMap.Rows.Clear();
            _dgvPortMap.Rows.Add("8255 Base", "18");
            _dgvPortMap.Rows.Add("Dot Matrix", "1C");
            _dgvPortMap.Rows.Add("LCD Command", "04");
            _dgvPortMap.Rows.Add("LCD Data", "06");
            _dgvPortMap.Rows.Add("Keypad", "00");
        }
    }
}
