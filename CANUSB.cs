using System;
using System.IO.Ports;
using System.Threading;

namespace ECUSimulator_2
{
    public class CANUSB : IDisposable
    {
        private SerialPort _serialPort;
        private bool _isOpen;
        private Thread _receiveThread;
        private bool _running;

        public event Action<CANFrameEventArgs>? FrameReceived;
        public event Action<string>? RawDataReceived;
        public event Action<string>? RawDataSent;
        public event Action<string>? ProtocolDetected;

        public CANUSB(string portName, int baudRate = 115200)
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            _serialPort.ReadTimeout = 3000;
            _serialPort.WriteTimeout = 2000;
            _serialPort.NewLine = "\r";
            _serialPort.DtrEnable = true;
            _serialPort.RtsEnable = true;
        }

        public bool Open()
        {
            try
            {
                if (_serialPort.IsOpen) _serialPort.Close();
                _serialPort.Open();
                _isOpen = true;
                _running = true;

                Thread.Sleep(2500);
                if (_serialPort.BytesToRead > 0) _serialPort.ReadExisting();

                SendRawCommand("V\r");
                Thread.Sleep(500);
                SendRawCommand("S6\r");
                Thread.Sleep(300);
                SendRawCommand("O\r");
                Thread.Sleep(300);
                SendRawCommand("F\r");
                Thread.Sleep(200);

                _receiveThread = new Thread(ReceiveLoop);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();

                ProtocolDetected?.Invoke("SLCAN (Arduino) - 500 kbps OBD-II");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HATA] Open: {ex.Message}");
                return false;
            }
        }

        public void Close()
        {
            _running = false;
            if (_receiveThread != null && _receiveThread.IsAlive)
                _receiveThread.Join(1000);

            if (_isOpen)
            {
                try
                {
                    SendRawCommand("C\r");
                    _serialPort.Close();
                }
                catch { }
                _isOpen = false;
            }
        }

        private void ReceiveLoop()
        {
            while (_running && _isOpen)
            {
                try
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        string line = _serialPort.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            string trimmedLine = line.Trim();
                            RawDataReceived?.Invoke(trimmedLine);

                            if (trimmedLine.Length >= 5 &&
                                (trimmedLine[0] == 't' || trimmedLine[0] == 'T'))
                            {
                                ParseCANFrame(trimmedLine);
                            }
                            else if (trimmedLine.StartsWith("V") || trimmedLine.Contains("SLCAN") || trimmedLine.Contains("CanHacker"))
                            {
                                Console.WriteLine($"[SLCAN] Versiyon: {trimmedLine}");
                            }
                            else if (trimmedLine.StartsWith("F="))
                            {
                                Console.WriteLine($"[SLCAN] Durum: {trimmedLine}");
                            }
                            else if (trimmedLine == ">" || trimmedLine == "OK" || trimmedLine == "\r")
                            {
                                // ignore
                            }
                            else if (trimmedLine.Contains("ERROR"))
                            {
                                Console.WriteLine($"[SLCAN] HATA: {trimmedLine}");
                            }
                            else
                            {
                                Console.WriteLine($"[SLCAN] {trimmedLine}");
                            }
                        }
                    }
                    Thread.Sleep(5);
                }
                catch (TimeoutException) { }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HATA] ReceiveLoop: {ex.Message}");
                    Thread.Sleep(100);
                }
            }
        }

        private void ParseCANFrame(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data) || data.Length < 5) return;

                bool isExtended = data[0] == 'T';
                string idStr = data.Substring(1, 3);
                int id = Convert.ToInt32(idStr, 16);
                int dlc = Convert.ToInt32(data[4].ToString(), 16);
                if (dlc > 8) dlc = 8;

                byte[] frameData = new byte[dlc];
                int dataStart = 5;
                for (int i = 0; i < dlc && (dataStart + i * 2 + 1) < data.Length; i++)
                {
                    string byteStr = data.Substring(dataStart + i * 2, 2);
                    frameData[i] = Convert.ToByte(byteStr, 16);
                }

                FrameReceived?.Invoke(new CANFrameEventArgs(id, frameData, isExtended));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HATA] ParseCANFrame: {ex.Message} - Data: {data}");
            }
        }

        public bool SendRawCommand(string command)
        {
            try
            {
                if (!_isOpen) return false;
                if (_serialPort.BytesToRead > 0) _serialPort.ReadExisting();

                RawDataSent?.Invoke(command.Trim());
                _serialPort.Write(command);
                Thread.Sleep(50);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HATA] SendRawCommand: {ex.Message}");
                return false;
            }
        }

        public bool SendFrame(int id, byte[] data, bool isExtended = false)
        {
            if (!_isOpen || data == null || data.Length == 0 || data.Length > 8)
                return false;

            try
            {
                string command = isExtended ? "T" : "t";
                command += id.ToString("X3");
                command += data.Length.ToString("X1");   // DLC burada doğru hesaplanıyor
                foreach (byte b in data)
                    command += b.ToString("X2");
                command += "\r";

                RawDataSent?.Invoke($"[CAN TX] {command.Trim()}");
                return SendRawCommand(command);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HATA] SendFrame: {ex.Message}");
                return false;
            }
        }

        public bool SetBitrate(int bitrate)
        {
            string cmd = bitrate switch
            {
                10000 => "S0\r",
                20000 => "S1\r",
                50000 => "S2\r",
                100000 => "S3\r",
                125000 => "S4\r",
                250000 => "S5\r",
                500000 => "S6\r",
                800000 => "S7\r",
                1000000 => "S8\r",
                _ => "S6\r"
            };
            return SendRawCommand(cmd);
        }

        public void Dispose()
        {
            Close();
            _serialPort?.Dispose();
        }

        public bool IsOpen => _isOpen;
    }

    public class CANFrameEventArgs : EventArgs
    {
        public int ID { get; }
        public byte[] Data { get; }
        public bool IsExtended { get; }

        public CANFrameEventArgs(int id, byte[] data, bool isExtended)
        {
            ID = id;
            Data = data;
            IsExtended = isExtended;
        }
    }
}