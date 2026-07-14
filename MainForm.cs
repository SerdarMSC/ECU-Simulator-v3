using System;
using System.Drawing;
using System.Windows.Forms;

namespace ECUSimulator_2
{
    public partial class MainForm : Form
    {
        private ECUSimulator? _ecuSimulator;
        private ListBox _lstELMData;
        private RichTextBox _txtLog;
        private Button _btnStart;
        private Button _btnStop;
        private Button _btnClear;
        private Button _btnSendRaw;
        private TextBox _txtCommand;
        private Label _lblStatus;
        private CheckBox _chkAutoScroll;
        private ComboBox _cmbComPort;
        private ComboBox _cmbBaudRate;
        private int _maxLogLines = 1000;

        private SplitContainer splitContainer;
        private TableLayoutPanel rightTableLayout;

        // Kontroller
        private TrackBar _trackTemp;
        private TextBox _txtTemp;
        private TrackBar _trackVoltage;
        private TextBox _txtVoltage;
        private TrackBar _trackOilTemp;
        private TextBox _txtOilTemp;
        private TrackBar _trackRPM;
        private TextBox _txtRPM;
        private TextBox _txtFaultCode;
        private Button _btnAddFault;
        private Button _btnClearFaults;

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
            //this.Icon += new Icon("app.ico");
            this.Load += (s, e) => SetSplitterMiddle();
            this.Resize += (s, e) => SetSplitterMiddle();
        }

        private void SetSplitterMiddle()
        {
            if (splitContainer != null)
                splitContainer.SplitterDistance = splitContainer.Width / 2 - 170 ;
        }

        private void InitializeUI()
        {
            this.Text = "SerdarMSC's ECU Simulator - v3.0";
            this.Size = new Size(1000, 820);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);

            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = this.Width / 2,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // SOL PANEL (CAN Verileri)
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(25, 25, 25)
            };

            Label lblELM = new Label
            {
                Text = "📡 CAN Verileri",
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 12, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(20, 20, 20)
            };
            leftPanel.Controls.Add(lblELM);

            _lstELMData = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.Cyan,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _lstELMData.DrawItem += ListBox_DrawItem;
            leftPanel.Controls.Add(_lstELMData);

            // SAĞ PANEL (TableLayoutPanel ile Kontrol + Log)
            rightTableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.FromArgb(25, 25, 25),
                Padding = new Padding(10),
                AutoSize = false
            };

            // Satır yüzdeleri: üst satır %55, alt satır %45 (log alanı)
            rightTableLayout.RowStyles.Clear();
            rightTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
            rightTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));

            // ÜST SATIR: Kontroller
            Panel controlPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                AutoScroll = true   // kontroller taşarsa kaydırma çubuğu
            };

            int yPos = 10;
            int spacing = 10;
            int labelWidth = 140;
            int trackWidth = 330;
            int valueWidth = 70;

            // COM Port ve Butonlar
            Label lblCom = new Label
            {
                Text = "COM Port:",
                ForeColor = Color.White,
                Location = new Point(spacing, yPos),
                Size = new Size(70, 25)
            };
            controlPanel.Controls.Add(lblCom);

            _cmbComPort = new ComboBox
            {
                Location = new Point(85, yPos),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            if (ports.Length > 0)
            {
                foreach (string port in ports)
                    _cmbComPort.Items.Add(port);
                _cmbComPort.SelectedIndex = 0;
            }
            else
            {
                for (int i = 1; i <= 20; i++)
                    _cmbComPort.Items.Add($"COM{i}");
                _cmbComPort.SelectedIndex = 4;
            }
            controlPanel.Controls.Add(_cmbComPort);

            Label lblBaud = new Label
            {
                Text = "Baud:",
                ForeColor = Color.White,
                Location = new Point(180, yPos),
                Size = new Size(40, 25)
            };
            controlPanel.Controls.Add(lblBaud);

            _cmbBaudRate = new ComboBox
            {
                Location = new Point(220, yPos),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            _cmbBaudRate.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200", "230400" });
            _cmbBaudRate.SelectedIndex = 4;
            controlPanel.Controls.Add(_cmbBaudRate);

            _btnStart = new Button
            {
                Text = "▶ Başlat",
                Location = new Point(320, yPos - 2),
                Size = new Size(75, 30),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnStart.Click += (s, e) => StartSimulator();
            controlPanel.Controls.Add(_btnStart);

            _btnStop = new Button
            {
                Text = "⏹ Durdur",
                Location = new Point(405, yPos - 2),
                Size = new Size(75, 30),
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _btnStop.Click += (s, e) => StopSimulator();
            controlPanel.Controls.Add(_btnStop);

            Button btnTest = new Button
            {
                Text = "🔧 Test",
                Location = new Point(490, yPos - 2),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnTest.Click += (s, e) => TestConnection();
            controlPanel.Controls.Add(btnTest);

            yPos += 45;

            // 1. Motor Sıcaklığı
            Label lblTemp = new Label
            {
                Text = "🌡️ Motor Sıcaklığı:",
                ForeColor = Color.White,
                Location = new Point(spacing, yPos),
                Size = new Size(labelWidth, 25)
            };
            controlPanel.Controls.Add(lblTemp);

            _trackTemp = new TrackBar
            {
                Location = new Point(spacing + labelWidth, yPos - 5),
                Size = new Size(trackWidth, 30),
                Minimum = -40,
                Maximum = 150,
                Value = 85,
                TickFrequency = 10,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            _trackTemp.ValueChanged += TrackTemp_ValueChanged;
            controlPanel.Controls.Add(_trackTemp);

            _txtTemp = new TextBox
            {
                Location = new Point(spacing + labelWidth + trackWidth + 10, yPos),
                Size = new Size(valueWidth, 25),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Text = "85 °C",
                ReadOnly = true,
                TextAlign = HorizontalAlignment.Center
            };
            controlPanel.Controls.Add(_txtTemp);

            yPos += 40;

            // 2. Akü Voltajı
            Label lblVoltage = new Label
            {
                Text = "🔋 Akü Voltajı:",
                ForeColor = Color.White,
                Location = new Point(spacing, yPos),
                Size = new Size(labelWidth, 25)
            };
            controlPanel.Controls.Add(lblVoltage);

            _trackVoltage = new TrackBar
            {
                Location = new Point(spacing + labelWidth, yPos - 5),
                Size = new Size(trackWidth, 30),
                Minimum = 80,
                Maximum = 160,
                Value = 120,
                TickFrequency = 5,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            _trackVoltage.ValueChanged += TrackVoltage_ValueChanged;
            controlPanel.Controls.Add(_trackVoltage);

            _txtVoltage = new TextBox
            {
                Location = new Point(spacing + labelWidth + trackWidth + 10, yPos),
                Size = new Size(valueWidth, 25),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Text = "12.0 V",
                ReadOnly = true,
                TextAlign = HorizontalAlignment.Center
            };
            controlPanel.Controls.Add(_txtVoltage);

            yPos += 40;

            // 3. Yağ Sıcaklığı
            Label lblOilTemp = new Label
            {
                Text = "🛢️ Yağ Sıcaklığı:",
                ForeColor = Color.White,
                Location = new Point(spacing, yPos),
                Size = new Size(labelWidth, 25)
            };
            controlPanel.Controls.Add(lblOilTemp);

            _trackOilTemp = new TrackBar
            {
                Location = new Point(spacing + labelWidth, yPos - 5),
                Size = new Size(trackWidth, 30),
                Minimum = 0,
                Maximum = 150,
                Value = 90,
                TickFrequency = 10,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            _trackOilTemp.ValueChanged += TrackOilTemp_ValueChanged;
            controlPanel.Controls.Add(_trackOilTemp);

            _txtOilTemp = new TextBox
            {
                Location = new Point(spacing + labelWidth + trackWidth + 10, yPos),
                Size = new Size(valueWidth, 25),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Text = "90 °C",
                ReadOnly = true,
                TextAlign = HorizontalAlignment.Center
            };
            controlPanel.Controls.Add(_txtOilTemp);

            yPos += 40;

            // 4. Motor Devri
            Label lblRPM = new Label
            {
                Text = "⚡ Motor Devri:",
                ForeColor = Color.White,
                Location = new Point(spacing, yPos),
                Size = new Size(labelWidth, 25)
            };
            controlPanel.Controls.Add(lblRPM);

            _trackRPM = new TrackBar
            {
                Location = new Point(spacing + labelWidth, yPos - 5),
                Size = new Size(trackWidth, 30),
                Minimum = 0,
                Maximum = 7000,
                Value = 800,
                TickFrequency = 200,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            _trackRPM.ValueChanged += TrackRPM_ValueChanged;
            controlPanel.Controls.Add(_trackRPM);

            _txtRPM = new TextBox
            {
                Location = new Point(spacing + labelWidth + trackWidth + 10, yPos),
                Size = new Size(valueWidth, 25),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Text = "800 RPM",
                ReadOnly = true,
                TextAlign = HorizontalAlignment.Center
            };
            controlPanel.Controls.Add(_txtRPM);

            yPos += 40;

            // Arıza Kodu
            Label lblFault = new Label
            {
                Text = "⚠️ Arıza Kodu:",
                ForeColor = Color.White,
                Location = new Point(spacing, yPos),
                Size = new Size(labelWidth, 25)
            };
            controlPanel.Controls.Add(lblFault);

            _txtFaultCode = new TextBox
            {
                Location = new Point(spacing + labelWidth, yPos),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Text = "0",
                TextAlign = HorizontalAlignment.Center
            };
            controlPanel.Controls.Add(_txtFaultCode);

            _btnAddFault = new Button
            {
                Text = "➕ Ekle",
                Location = new Point(spacing + labelWidth + 90, yPos),
                Size = new Size(70, 25),
                BackColor = Color.FromArgb(200, 100, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnAddFault.Click += BtnAddFault_Click;
            controlPanel.Controls.Add(_btnAddFault);

            _btnClearFaults = new Button
            {
                Text = "🗑️ Temizle",
                Location = new Point(spacing + labelWidth + 170, yPos),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnClearFaults.Click += (s, e) => _ecuSimulator?.ClearFaults();
            controlPanel.Controls.Add(_btnClearFaults);

            yPos += 45;

            // Komut Gönderme
            Label lblCmd = new Label
            {
                Text = "Komut:",
                ForeColor = Color.White,
                Location = new Point(spacing, yPos),
                Size = new Size(50, 25)
            };
            controlPanel.Controls.Add(lblCmd);

            _txtCommand = new TextBox
            {
                Location = new Point(150, yPos),
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Text = "V"
            };
            _txtCommand.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) SendRawCommand(); };
            controlPanel.Controls.Add(_txtCommand);

            _btnSendRaw = new Button
            {
                Text = "📤 Gönder",
                Location = new Point(350, yPos - 1),
                Size = new Size(75, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _btnSendRaw.Click += (s, e) => SendRawCommand();
            controlPanel.Controls.Add(_btnSendRaw);

            yPos += 45;

            // Temizle ve Otomatik Kaydır
            _btnClear = new Button
            {
                Text = "🗑 Temizle",
                Location = new Point(spacing, yPos),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnClear.Click += (s, e) => _lstELMData.Items.Clear();
            controlPanel.Controls.Add(_btnClear);

            _chkAutoScroll = new CheckBox
            {
                Text = "Otomatik Kaydır",
                Location = new Point(100, yPos),
                Size = new Size(130, 25),
                ForeColor = Color.White,
                Checked = true
            };
            controlPanel.Controls.Add(_chkAutoScroll);

            yPos += 40;

            _lblStatus = new Label
            {
                Text = "⏸ Bağlantı bekleniyor...",
                ForeColor = Color.Yellow,
                Location = new Point(spacing, yPos),
                Size = new Size(600, 25),
                Font = new Font("Consolas", 10, FontStyle.Bold)
            };
            controlPanel.Controls.Add(_lblStatus);

            // Kontrol panelini TableLayout'un ilk satırına ekle
            rightTableLayout.Controls.Add(controlPanel, 0, 0);

            // ALT SATIR: Sistem Logu
            Panel logPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 25)
            };

            Label lblLog = new Label
            {
                Text = "Log",
                ForeColor = Color.LightBlue,
                Font = new Font("Consolas", 10, FontStyle.Bold),
                Dock = DockStyle.None, 
                Height = 0,
                BackColor = Color.FromArgb(20, 20, 20)
            };
            logPanel.Controls.Add(lblLog);

            _txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                WordWrap = false
            };
            logPanel.Controls.Add(_txtLog);

            rightTableLayout.Controls.Add(logPanel, 0, 1);

            // ----- Sağ paneli SplitContainer'a ekle -----
            Panel rightContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(25, 25, 25)
            };
            rightContainer.Controls.Add(rightTableLayout);

            splitContainer.Panel1.Controls.Add(leftPanel);
            splitContainer.Panel2.Controls.Add(rightContainer);
            this.Controls.Add(splitContainer);

            this.FormClosing += (s, e) => StopSimulator();

            AddLog("🚀 ECU Simulator başlatıldı!", Color.LightBlue);
            AddLog("🎛️ Tüm değerler manuel olarak ayarlanabilir.", Color.LightBlue);
            AddLog("⚠️ Arıza kodları ekleyip temizleyebilirsiniz.", Color.LightBlue);
        }

        private void TrackTemp_ValueChanged(object? sender, EventArgs e)
        {
            if (_ecuSimulator != null)
            {
                int val = _trackTemp.Value;
                _ecuSimulator.SetEngineTemperature(val);
                _txtTemp.Text = val + " °C";
            }
        }

        private void TrackVoltage_ValueChanged(object? sender, EventArgs e)
        {
            if (_ecuSimulator != null)
            {
                float val = _trackVoltage.Value / 10.0f;
                _ecuSimulator.SetBatteryVoltage(val);
                _txtVoltage.Text = val.ToString("0.0") + " V";
            }
        }

        private void TrackOilTemp_ValueChanged(object? sender, EventArgs e)
        {
            if (_ecuSimulator != null)
            {
                int val = _trackOilTemp.Value;
                _ecuSimulator.SetOilTemperature(val);
                _txtOilTemp.Text = val + " °C";
            }
        }

        private void TrackRPM_ValueChanged(object? sender, EventArgs e)
        {
            if (_ecuSimulator != null)
            {
                int val = _trackRPM.Value;
                _ecuSimulator.SetEngineRPM(val);
                _txtRPM.Text = val + " RPM";
            }
        }

        private void BtnAddFault_Click(object? sender, EventArgs e)
        {
            if (_ecuSimulator != null)
            {
                if (int.TryParse(_txtFaultCode.Text, out int code))
                {
                    _ecuSimulator.AddFault(code);
                    _txtFaultCode.Text = "0";
                }
                else
                {
                    AddLog("❌ Geçersiz arıza kodu!", Color.Red);
                }
            }
        }


        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            if (e.Index < 0) return;
            e.DrawBackground();
            string text = listBox.Items[e.Index].ToString() ?? "";
            Color textColor = Color.Cyan;
            if (text.Contains("[TX]") || text.Contains("[CAN TX]")) textColor = Color.LightGreen;
            else if (text.Contains("[RX]") || text.Contains("[CAN RX]")) textColor = Color.LightYellow;
            else if (text.Contains("[OBD]")) textColor = Color.LightBlue;
            else if (text.Contains("[HATA]")) textColor = Color.Red;
            else if (text.Contains("[CMD]")) textColor = Color.Orange;
            using (SolidBrush brush = new SolidBrush(textColor))
                e.Graphics.DrawString(text, e.Font, brush, e.Bounds);
            e.DrawFocusRectangle();
        }

        private void TestConnection()
        {
            AddLog("🔧 Test başlatılıyor...", Color.Yellow);
            try
            {
                string comPort = _cmbComPort.Text;
                int baudRate = int.Parse(_cmbBaudRate.Text);
                AddLog($"📡 {comPort} üzerinden {baudRate} baud ile test ediliyor", Color.LightBlue);
                using (var tempSerial = new System.IO.Ports.SerialPort(comPort, baudRate, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One))
                {
                    tempSerial.ReadTimeout = 3000;
                    tempSerial.WriteTimeout = 2000;
                    tempSerial.NewLine = "\r";
                    tempSerial.DtrEnable = true;
                    tempSerial.RtsEnable = true;
                    tempSerial.Open();
                    AddLog($"✅ {comPort} açıldı!", Color.Green);
                    AddLog("⏳ Arduino boot bekleniyor (2 sn)...", Color.Yellow);
                    System.Threading.Thread.Sleep(2500);
                    if (tempSerial.BytesToRead > 0) tempSerial.ReadExisting();
                    AddLog("📤 Komut gönderiliyor: V", Color.Orange);
                    tempSerial.Write("V\r");
                    System.Threading.Thread.Sleep(500);
                    if (tempSerial.BytesToRead > 0)
                    {
                        string response = tempSerial.ReadExisting();
                        AddLog($"📥 Yanıt: {response.Trim()}", Color.Cyan);
                        if (response.Contains("SLCAN") || response.Contains("CAN") || response.Contains("Hacker"))
                            AddLog("✅ SLCAN/CanHacker tespit edildi!", Color.Green);
                        else
                            AddLog($"⚠️ Cihaz: {response.Trim()}", Color.Yellow);
                    }
                    else
                    {
                        AddLog("❌ Yanıt alınamadı!", Color.Red);
                        AddLog("💡 Arduino boot tamamlanmamış olabilir, tekrar deneyin", Color.Yellow);
                    }
                    tempSerial.Close();
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Test hatası: {ex.Message}", Color.Red);
            }
        }

        private void StartSimulator()
        {
            try
            {
                string comPort = _cmbComPort.Text;
                int baudRate = int.Parse(_cmbBaudRate.Text);
                AddLog($"🚀 ECU Simulator başlatılıyor... {comPort} {baudRate} baud", Color.LightBlue);
                _ecuSimulator = new ECUSimulator(comPort, baudRate);
                _ecuSimulator.OnELMDataReceived += OnELMDataReceived;
                _ecuSimulator.OnStatusChanged += OnStatusChanged;
                _ecuSimulator.OnLogMessage += OnLogMessage;
                if (_ecuSimulator.Start())
                {
                    _btnStart.Enabled = false;
                    _btnStop.Enabled = true;
                    _btnSendRaw.Enabled = true;
                    _trackTemp.Value = 85;
                    _txtTemp.Text = "85 °C";
                    _trackVoltage.Value = 120;
                    _txtVoltage.Text = "12.0 V";
                    _trackOilTemp.Value = 90;
                    _txtOilTemp.Text = "90 °C";
                    _trackRPM.Value = 800;
                    _txtRPM.Text = "800 RPM";
                    AddLog("✅ ECU Simulator başarıyla başlatıldı!", Color.Green);
                }
                else
                {
                    AddLog("❌ ECU Simulator başlatılamadı!", Color.Red);
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Hata: {ex.Message}", Color.Red);
            }
        }

        private void StopSimulator()
        {
            if (_ecuSimulator != null)
            {
                _ecuSimulator.OnELMDataReceived -= OnELMDataReceived;
                _ecuSimulator.OnStatusChanged -= OnStatusChanged;
                _ecuSimulator.OnLogMessage -= OnLogMessage;
                _ecuSimulator.Stop();
                _ecuSimulator = null;
                AddLog("⏹ ECU Simulator durduruldu", Color.Orange);
            }
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
            _btnSendRaw.Enabled = false;
        }

        private void SendRawCommand()
        {
            if (_ecuSimulator == null || string.IsNullOrEmpty(_txtCommand.Text))
            {
                AddLog("⚠️ Önce 'Başlat' butonuna tıklayın", Color.Yellow);
                return;
            }
            string command = _txtCommand.Text.Trim();
            AddLog($"📤 Komut gönderiliyor: {command}", Color.Orange);
            _ecuSimulator.SendRawCommand(command);
            _txtCommand.Clear();
        }

        private void OnELMDataReceived(string data)
        {
            if (_lstELMData.InvokeRequired)
            {
                _lstELMData.Invoke(new Action<string>(OnELMDataReceived), data);
                return;
            }
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _lstELMData.Items.Add($"[{timestamp}] {data}");
            if (_lstELMData.Items.Count > _maxLogLines) _lstELMData.Items.RemoveAt(0);
            if (_chkAutoScroll.Checked && _lstELMData.Items.Count > 0)
                _lstELMData.TopIndex = _lstELMData.Items.Count - 1;
        }

        private void OnStatusChanged(string status, Color color)
        {
            if (_lblStatus.InvokeRequired)
            {
                _lblStatus.Invoke(new Action<string, Color>(OnStatusChanged), status, color);
                return;
            }
            _lblStatus.Text = status;
            _lblStatus.ForeColor = color;
        }

        private void OnLogMessage(string message, Color color)
        {
            if (_txtLog.InvokeRequired)
            {
                _txtLog.Invoke(new Action<string, Color>(OnLogMessage), message, color);
                return;
            }
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.SelectionLength = 0;
            _txtLog.SelectionColor = color;
            _txtLog.AppendText($"[{timestamp}] {message}\n");
            _txtLog.ScrollToCaret();
            if (_txtLog.Lines.Length > 500)
            {
                int linesToRemove = _txtLog.Lines.Length - 500;
                string text = _txtLog.Text;
                for (int i = 0; i < linesToRemove; i++)
                {
                    int idx = text.IndexOf('\n');
                    if (idx >= 0) text = text.Substring(idx + 1);
                }
                _txtLog.Text = text;
            }
        }

        private void AddLog(string message, Color color)
        {
            OnLogMessage(message, color);
        }
    }
}