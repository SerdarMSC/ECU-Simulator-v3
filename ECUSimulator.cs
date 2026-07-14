using System;
using System.Collections.Generic;
using System.Drawing;
using System.Timers;
using System.Threading;

namespace ECUSimulator_2
{
    public class ECUSimulator
    {
        private CANUSB _canUsb;
        private System.Timers.Timer _simulationTimer;
        private object _lock = new object();

        public event Action<string>? OnELMDataReceived;
        public event Action<string, Color>? OnStatusChanged;
        public event Action<string, Color>? OnLogMessage;

        // Manuel değerler
        private int _engineRPM = 800;
        private int _engineTemp = 85;
        private float _batteryVoltage = 12.0f;
        private int _oilTemp = 90;
        private bool _engineRunning = true;
        private List<int> _faultCodes = new List<int>();

        // OBD değerleri
        private int _obdCoolantTemp = 0;
        private int _obdRPM = 0;
        private int _obdOilTemp = 0;
        private int _obdVoltage = 0;
        private int _obdSpeed = 0;
        private int _obdThrottle = 0;
        private int _obdFuelLevel = 0;

        public ECUSimulator(string comPort, int baudRate = 115200)
        {
            _canUsb = new CANUSB(comPort, baudRate);
            _canUsb.FrameReceived += OnCANFrameReceived;
            _canUsb.RawDataReceived += OnRawDataReceived;
            _canUsb.ProtocolDetected += OnProtocolDetected;
            UpdateOBDValues();
        }

        public bool Start()
        {
            if (!_canUsb.Open())
            {
                OnStatusChanged?.Invoke("❌ CANUSB açılamadı!", Color.Red);
                return false;
            }

            // Timer sadece OBD değerlerini güncellemek için (frame göndermez)
            _simulationTimer = new System.Timers.Timer(100);
            _simulationTimer.Elapsed += SimulateData;
            _simulationTimer.AutoReset = true;
            _simulationTimer.Start();

            OnStatusChanged?.Invoke("✅ SLCAN (Arduino) bağlantı aktif - 500 kbps OBD-II", Color.Lime);
            OnLogMessage?.Invoke("✓ ECU Simulator başlatıldı - Sadece OBD yanıtları", Color.Lime);
            return true;
        }

        public void Stop()
        {
            _simulationTimer?.Stop();
            _simulationTimer?.Dispose();
            _canUsb.Close();
            OnStatusChanged?.Invoke("⏸ Bağlantı kesildi", Color.Yellow);
            OnLogMessage?.Invoke("✓ ECU Simulator durduruldu", Color.Orange);
        }

        private void OnProtocolDetected(string protocol)
        {
            OnLogMessage?.Invoke($"📡 Protokol: {protocol}", Color.Magenta);
        }

        private void UpdateOBDValues()
        {
            _obdCoolantTemp = _engineTemp + 40;
            _obdRPM = _engineRPM / 4;
            _obdOilTemp = _oilTemp + 40;
            _obdVoltage = (int)(_batteryVoltage * 100);
            _obdSpeed = 0;          // sabit
            _obdThrottle = 0;       // sabit
            _obdFuelLevel = 0x4B;   // %75
        }

        private void SimulateData(object? sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (!_engineRunning)
                    _engineRPM = 0;
                UpdateOBDValues();
                // ARTIK HİÇBİR CAN FRAME GÖNDERMİYORUZ
            }
        }

        private void OnRawDataReceived(string data)
        {
            OnELMDataReceived?.Invoke(data);
        }

        private void OnCANFrameReceived(CANFrameEventArgs e)
        {
            // Sadece OBD isteklerine yanıt ver
            if (e.ID == 0x7DF && e.Data.Length >= 3)
            {
                OnLogMessage?.Invoke($"[OBD] İstek: 0x{e.ID:X3} | {BitConverter.ToString(e.Data)}", Color.Yellow);
                ProcessOBDRequest(e.Data);
            }
        }

        // OBD YANITI 8 BYTE (DLC=8)
        private void ProcessOBDRequest(byte[] data)
        {
            try
            {
                if (data.Length < 3) return;
                byte mode = data[1];
                byte pid = data[2];

                // Yanıt her zaman DLC=8 olarak (dolgu ile) gönderilir, ancak ISO-TP "single frame" PCI baytı (response[0]) klasik CAN'de EN FAZLA 7
                // olabilir (PCI baytının kendisi de 8 baytlık çerçevenin 1'ini kaplar). response[0] = 0x08 yazmak ISO 15765-2'ye göre GEÇERSİZ bir SF_DL
                // değeridir ve gerçek tarayıcılar/CAN analizörleri bunu hatalı/malformed mesaj olarak işaretler
                byte[] response = new byte[8];
                byte dataLen = 1; // mode+pid dışında kaç veri baytı dolduruldu (varsayılan 1)
                response[1] = (byte)(mode | 0x40);
                response[2] = pid;

                switch (pid)
                {
                    case 0x00:
                        // Sadece bu simülatörde gerçekten uygulanan PID'leri işaretler:
                        // 01,03,05  -> byte1 (PID 01-08)
                        // 0C,0D,0F  -> byte2 (PID 09-10), bit0 = sonraki blok mevcut (0x11 destekleniyor)
                        response[3] = 0xA9; // 1010 1001
                        response[4] = 0x1B; // 0001 1011
                        dataLen = 2;
                        break;
                    case 0x01:
                        response[3] = (byte)(_faultCodes.Count > 0 ? 0x80 : 0x00);
                        break;
                    case 0x03:
                        if (_faultCodes.Count > 0)
                        {
                            response[3] = (byte)((_faultCodes[0] >> 8) & 0xFF);
                            response[4] = (byte)(_faultCodes[0] & 0xFF);
                        }
                        else
                        {
                            response[3] = 0x00;
                            response[4] = 0x00;
                        }
                        dataLen = 2;
                        break;
                    case 0x05:
                        response[3] = (byte)_obdCoolantTemp;
                        break;
                    case 0x0C:
                        response[3] = (byte)((_obdRPM >> 8) & 0xFF);
                        response[4] = (byte)(_obdRPM & 0xFF);
                        dataLen = 2;
                        break;
                    case 0x0D:
                        response[3] = (byte)_obdSpeed;
                        break;
                    case 0x0F:
                        response[3] = (byte)(_engineTemp + 40);
                        break;
                    case 0x11:
                        response[3] = (byte)_obdThrottle;
                        break;
                    case 0x2F:
                        response[3] = (byte)_obdFuelLevel;
                        break;
                    case 0x5C:
                        response[3] = (byte)_obdOilTemp;
                        break;
                    case 0x42:
                        response[3] = (byte)((_obdVoltage >> 8) & 0xFF);
                        response[4] = (byte)(_obdVoltage & 0xFF);
                        dataLen = 2;
                        break;
                    default:
                        response[3] = 0x00;
                        OnLogMessage?.Invoke($"[OBD] Desteklenmeyen PID: 0x{pid:X2}", Color.Yellow);
                        break;
                }

                // SF_DL = mode(1) + pid(1) + dataLen, !!! ISO 15765-2 sınırı olan 7'yi asla geçmez !!!
                response[0] = (byte)Math.Min(7, 2 + dataLen);

                _canUsb.SendFrame(0x7E8, response);
                OnLogMessage?.Invoke($"[OBD] Yanıt: 0x7E8 | {BitConverter.ToString(response)}", Color.Green);
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"[HATA] OBD Error: {ex.Message}", Color.Red);
            }
        }

        public void SendRawCommand(string command) => _canUsb.SendRawCommand(command + "\r");

        public void SetEngineTemperature(int value)
        {
            lock (_lock)
            {
                _engineTemp = Math.Min(150, Math.Max(-40, value));
                OnLogMessage?.Invoke($"🌡️ Motor sıcaklığı: {_engineTemp}°C", Color.LightBlue);
            }
        }

        public void SetBatteryVoltage(float value)
        {
            lock (_lock)
            {
                _batteryVoltage = Math.Min(16.0f, Math.Max(8.0f, value));
                OnLogMessage?.Invoke($"🔋 Akü voltajı: {_batteryVoltage:F1}V", Color.LightBlue);
            }
        }

        public void SetOilTemperature(int value)
        {
            lock (_lock)
            {
                _oilTemp = Math.Min(150, Math.Max(0, value));
                OnLogMessage?.Invoke($"🛢️ Yağ sıcaklığı: {_oilTemp}°C", Color.LightBlue);
            }
        }

        public void SetEngineRPM(int value)
        {
            lock (_lock)
            {
                _engineRPM = Math.Min(7000, Math.Max(0, value));
                OnLogMessage?.Invoke($"⚡ Motor devri: {_engineRPM} RPM", Color.LightBlue);
            }
        }

        public void AddFault(int code)
        {
            lock (_lock)
            {
                if (!_faultCodes.Contains(code))
                {
                    _faultCodes.Add(code);
                    OnLogMessage?.Invoke($"⚠️ Arıza kodu {code} eklendi", Color.Red);
                }
                else
                {
                    OnLogMessage?.Invoke($"⚠️ Arıza kodu {code} zaten var", Color.Yellow);
                }
            }
        }

        public void ClearFaults()
        {
            lock (_lock)
            {
                _faultCodes.Clear();
                OnLogMessage?.Invoke("🗑️ Arıza kodları temizlendi", Color.Lime);
            }
        }

        public void StartEngine()
        {
            lock (_lock)
            {
                _engineRunning = true;
                OnLogMessage?.Invoke("🚗 Motor çalıştırıldı", Color.Lime);
            }
        }

        public void StopEngine()
        {
            lock (_lock)
            {
                _engineRunning = false;
                _engineRPM = 0;
                OnLogMessage?.Invoke("⛔ Motor durduruldu", Color.Orange);
            }
        }
    }
}