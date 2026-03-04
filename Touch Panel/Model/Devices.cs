using BSL430_NET;
using CommunityToolkit.Mvvm.ComponentModel;
using HidSharp;
using RJCP.IO.Ports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using Touch_Panel.View_Model;

namespace Touch_Panel.Model
{
    public enum Direction
    {
        Stop,
        Up,
        Down,
        Error
    }



    public class SystemData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly object _lock = new object();

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            lock (_lock)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                    return false;

                field = value;
            }

            // Gọi ngoài lock để tránh deadlock UI thread
            OnPropertyChanged(propertyName);
            return true;
        }



        private bool mainUpFlag;
        public bool MainUpFlag
        {
            get { lock (_lock) return mainUpFlag; }
            set => SetProperty(ref mainUpFlag, value);
        }

        private bool mainBottom;
        public bool MainBottom
        {
            get { lock (_lock) return mainBottom; }
            set => SetProperty(ref mainBottom, value);
        }

        private bool mainTop;
        public bool MainTop
        {
            get { lock (_lock) return mainTop; }
            set => SetProperty(ref mainTop, value);
        }



        private Direction mainDirection;

        public Direction MainDirection
        {
            get
            {
                lock (_lock) return mainDirection;
            }
            set
            {
                lock (_lock)
                {
                    mainDirection = value;
                    if (mainDirection == Direction.Up)
                    {
                        mainUpFlag = true;
                    }
                }
                // Gọi ngoài lock để giảm nguy cơ deadlock
                OnPropertyChanged(nameof(MainDirection));
            }
        }
    }
    public partial class FirmwareMICOM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

     

        private string _Micom1;

        public string Micom1
        {
            get
            {
                return _Micom1;

            }
            set
            {
                _Micom1 = value;
                OnPropertyChanged("Micom1");
            }
        }


        private string _Micom2;

        public string Micom2
        {
            get
            {

                return _Micom2;
            }
            set
            {

                _Micom2 = value;
                OnPropertyChanged("Micom2");
            }
        }


    }
    public partial class CAPElement : ObservableObject
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string stringId;

        [ObservableProperty]
        private UInt16 delta;

        [ObservableProperty]
        private bool isMax;
    }
    public partial class CAPCycle : ObservableObject
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private ObservableCollection<CAPElement> listCAPElement;
    }


    public partial class CAPSensor : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<CAPCycle> listCAPCycle;

        [ObservableProperty]
        private int id;
    }
    public partial class ElementSensor : ObservableObject
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private UInt16 lta;

        [ObservableProperty]
        private UInt16 filterCount;

        [ObservableProperty]
        private UInt16 delta;
    }

    public partial class Devices : ObservableObject
    {

        [ObservableProperty]
        private bool enableResetCylinder = true;

        public DeviceManager DeviceManager;



        [ObservableProperty]
        private string micom1FirmwareLog;


        [ObservableProperty]
        private string micom2FirmwareLog;

        [ObservableProperty]
        private string micomCom1Port;
        [ObservableProperty]
        private string micomCom2Port;

        [ObservableProperty]
        private string systemComPort;

        [ObservableProperty]
        private string solenoid1ComPort;

        [ObservableProperty]
        private string solenoid2ComPort;

        [ObservableProperty]
        private string solenoid3ComPort;

        [property: JsonIgnore]
        [ObservableProperty]
        private DeviceStatus micom1;

        [property: JsonIgnore]
        [ObservableProperty]
        private DeviceStatus micom2;



        [ObservableProperty]
        private ObservableCollection<string> comPortList = new ObservableCollection<string>();

        [property: JsonIgnore]
        [ObservableProperty]
        private ObservableCollection<DeviceStatus> devicesStatus = new ObservableCollection<DeviceStatus>()
        {
            new DeviceStatus {Name = "System"},
            new DeviceStatus {Name = "Solenoid 3"},
            new DeviceStatus {Name = "Micom1"},
            new DeviceStatus {Name = "Micom2"},
            new DeviceStatus {Name = "Solenoid 1"},
            new DeviceStatus {Name = "Solenoid 2"},
        };


        public event EventHandler SensorElementDataUpdatedEventHandler;

        [property: JsonIgnore]
        [ObservableProperty]
        private SystemData systemData = new SystemData();

        //[property: JsonIgnore]
        [ObservableProperty]
        private ObservableCollection<CAPSensor> listCAPSensor1 = new ObservableCollection<CAPSensor>();

        //[property: JsonIgnore]
        [ObservableProperty]
        private ObservableCollection<CAPSensor> listCAPSensor2 = new ObservableCollection<CAPSensor>();

        [property: JsonIgnore]
        [ObservableProperty]
        private FirmwareMICOM firmwareMicom = new FirmwareMICOM();

        [ObservableProperty]
        private string selectedFirmwareMicom = string.Empty;

        private byte startPrefix = 0xAA;
        private byte endPrefix = 0xFF;

        const byte STX = 0x02;
        const byte ETX = 0x03;
        const int MICOM_FRAME_LEN = 8;


        public Devices()
        {
      
        }

        public void RefreshComPort()
        {
            string[] ports = SerialPortStream.GetPortNames();
            string micomCom1PortBuf = MicomCom1Port;
            string micomCom2PortBuf = MicomCom2Port;

            string systemComPortBuf = SystemComPort;
            string solenoid1ComPortBuf = Solenoid1ComPort;
            string solenoid2ComPortBuf = Solenoid2ComPort;
            string solenoid3ComPortBuf = Solenoid3ComPort;


            ComPortList.Clear();

            ComPortList.Add("Unset");
            foreach (string port in ports)
            {
                {
                    ComPortList.Add(port);
                }
            }
            if (ComPortList.Contains(micomCom1PortBuf))
            {
                MicomCom1Port = micomCom1PortBuf;
            }
            if (ComPortList.Contains(micomCom2PortBuf))
            {
                MicomCom2Port = micomCom2PortBuf;
            }
            if (ComPortList.Contains(systemComPortBuf))
            {
                SystemComPort = systemComPortBuf;
            }
            if (ComPortList.Contains(solenoid1ComPortBuf))
            {
                Solenoid1ComPort = solenoid1ComPortBuf;
            }
            if (ComPortList.Contains(solenoid2ComPortBuf))
            {
                Solenoid2ComPort = solenoid2ComPortBuf;
            }

            if (ComPortList.Contains(solenoid3ComPortBuf))
            {
                Solenoid3ComPort = solenoid3ComPortBuf;
            }

        }


        private async Task CloseDevice(object portLock, SerialPortStream serialPort, string deviceName, EventHandler<RJCP.IO.Ports.SerialDataReceivedEventArgs> dataReceivedEventHandler)
        {
            lock (portLock)
            {

                var dev = DevicesStatus.FirstOrDefault(d => d.Name == deviceName);

                if (serialPort != null)
                {
                    try
                    {
                        if (serialPort.IsOpen)
                        {

                            serialPort.DataReceived -= dataReceivedEventHandler;
                            serialPort.Close();
                        }

                        dev.Connected = false;


                    }
                    catch (Exception ex)
                    {
                    }
                }

            }
        }


        public async Task ConnectDevice(object portLock, SerialPortStream port, string comPort, string deviceName, EventHandler<RJCP.IO.Ports.SerialDataReceivedEventArgs> dataReceivedEventHandler)
        {
            lock (portLock)
            {
                var dev = DevicesStatus.FirstOrDefault(d => d.Name == deviceName);

                if (port != null && !String.IsNullOrEmpty(comPort))
                {



                    try
                    {
                        if (port.IsOpen)
                        {
                            port.DataReceived -= dataReceivedEventHandler;



                            port.Close();


                        }


                        port.PortName = comPort;
                        port.BaudRate = 9600;
                        port.DataBits = 8;
                        port.Parity = Parity.None;
                        port.StopBits = StopBits.One;

                        if (deviceName.Contains("Micom"))
                        {
                            port.Handshake = Handshake.None;

                        }

                        port.DataReceived += dataReceivedEventHandler;

                        port.Open();



                        if (deviceName.Contains("Micom"))
                        {
                            port.DtrEnable = false;
                            port.RtsEnable = false;
                        }



                        dev.Connected = true;
                    }
                    catch (Exception ex)
                    {
                        dev.Connected = false;


                    }

                }

            }
        }

        public void Dispose()
        {
            if (DeviceManager == null) return;
            DeviceManager.MicomPort1.Dispose();
            DeviceManager.MicomPort2.Dispose();
            DeviceManager.SystemPort.Dispose();
            DeviceManager.Solenoid1Port.Dispose();
            DeviceManager.Solenoid2Port.Dispose();
            DeviceManager.Solenoid3Port.Dispose();
        }
        public async Task CloseAll()
        {
            if (DeviceManager == null) return;
            await CloseDevice(DeviceManager.PortLockMicom1, DeviceManager.MicomPort1, "Micom1", MicomPort1_DataReceived);
            await CloseDevice(DeviceManager.PortLockMicom2, DeviceManager.MicomPort2, "Micom2", MicomPort2_DataReceived);
            await CloseDevice(DeviceManager.PortLockSystem, DeviceManager.SystemPort, "System", SystemPort_DataReceived);
            await CloseDevice(DeviceManager.PortLockSol1, DeviceManager.Solenoid1Port, "Solenoid 1", Solenoid1Port_DataReceived);
            await CloseDevice(DeviceManager.PortLockSol2, DeviceManager.Solenoid2Port, "Solenoid 2", Solenoid2Port_DataReceived);
            await CloseDevice(DeviceManager.PortLockSol3, DeviceManager.Solenoid3Port, "Solenoid 3", Solenoid3Port_DataReceived);
        }

        public async Task ConnectAll()
        {
            if (DeviceManager == null) return;
            await ConnectDevice(DeviceManager.PortLockMicom1, DeviceManager.Solenoid1Port, Solenoid1ComPort, "Solenoid 1", Solenoid1Port_DataReceived);
            await ConnectDevice(DeviceManager.PortLockMicom2, DeviceManager.Solenoid2Port, Solenoid2ComPort, "Solenoid 2", Solenoid2Port_DataReceived);
            await ConnectDevice(DeviceManager.PortLockSystem, DeviceManager.Solenoid3Port, Solenoid3ComPort, "Solenoid 3", Solenoid3Port_DataReceived);
            await ConnectDevice(DeviceManager.PortLockSol1, DeviceManager.MicomPort1, MicomCom1Port, "Micom1", MicomPort1_DataReceived);
            await ConnectDevice(DeviceManager.PortLockSol2, DeviceManager.MicomPort2, MicomCom2Port, "Micom2", MicomPort2_DataReceived);
            await ConnectDevice(DeviceManager.PortLockSol3, DeviceManager.SystemPort, SystemComPort, "System", SystemPort_DataReceived);
        }


        public void CloseDeviceByName(string device)
        {
            switch (device)
            {
                case "Solenoid 1":
                    CloseDevice(DeviceManager.PortLockSol1, DeviceManager.Solenoid1Port, device, Solenoid1Port_DataReceived);
                    break;

                case "Solenoid 2":
                    CloseDevice(DeviceManager.PortLockSol2, DeviceManager.Solenoid2Port, device, Solenoid2Port_DataReceived);
                    break;

                case "Solenoid 3":
                    CloseDevice(DeviceManager.PortLockSol3, DeviceManager.Solenoid3Port, device, Solenoid3Port_DataReceived);
                    break;
                case "Micom1":
                    CloseDevice(DeviceManager.PortLockMicom1, DeviceManager.MicomPort1, device, MicomPort1_DataReceived);
                    break;
                case "Micom2":
                    CloseDevice(DeviceManager.PortLockMicom2, DeviceManager.MicomPort2, device, MicomPort2_DataReceived);
                    break;

                case "System":
                    CloseDevice(DeviceManager.PortLockSystem, DeviceManager.SystemPort, device, SystemPort_DataReceived);
                    break;
                default:
                    break;
            }
        }


        public void ConnectDeviceByName(string device)
        {
            switch (device)
            {
                case "Solenoid 1":
                    ConnectDevice(DeviceManager.PortLockSol1, DeviceManager.Solenoid1Port, Solenoid1ComPort, device, Solenoid1Port_DataReceived);
                    break;

                case "Solenoid 2":
                    ConnectDevice(DeviceManager.PortLockSol2, DeviceManager.Solenoid2Port, Solenoid2ComPort, device, Solenoid2Port_DataReceived);
                    break;

                case "Solenoid 3":
                    ConnectDevice(DeviceManager.PortLockSol3, DeviceManager.Solenoid3Port, Solenoid3ComPort, device, Solenoid3Port_DataReceived);
                    break;
                case "Micom1":
                    ConnectDevice(DeviceManager.PortLockMicom1, DeviceManager.MicomPort1, MicomCom1Port, device, MicomPort1_DataReceived);
                    break;
                case "Micom2":
                    ConnectDevice(DeviceManager.PortLockMicom2, DeviceManager.MicomPort2, MicomCom2Port, device, MicomPort2_DataReceived);
                    break;

                case "System":
                    ConnectDevice(DeviceManager.PortLockSystem, DeviceManager.SystemPort, SystemComPort, device, SystemPort_DataReceived);
                    break;
                default:
                    break;
            }
        }




        List<byte> micom1RXBuffer = new();
        List<byte> micom2RXBuffer = new();


        private List<byte> systemRxBuffer = new List<byte>();
        private List<byte> sol1RxBuffer = new List<byte>();
        private List<byte> sol2RxBuffer = new List<byte>();
        private List<byte> sol3RxBuffer = new List<byte>();

        //public bool reedAllDown = false;
        //public bool cylinderLow = false;
        public bool cylinderReset = false;
        public bool sol3RxPass = false;


        private async void Solenoid2Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPortStream serialPort = (SerialPortStream)sender;

            if (!serialPort.IsOpen) return;

            while (DeviceManager.Solenoid2Port.BytesToRead > 0)
            {
                sol2RxBuffer.Add((byte)DeviceManager.Solenoid2Port.ReadByte());
            }

            while (sol2RxBuffer.Count >= 6)
            {
                int startIndex = sol2RxBuffer.FindIndex(b => b == 0x44);
                if (startIndex == -1)
                {
                    sol2RxBuffer.Clear();
                    return;
                }

                if (sol2RxBuffer.Count < startIndex + 6)
                    return;

                byte b1 = sol2RxBuffer[startIndex];
                byte b2 = sol2RxBuffer[startIndex + 1];
                byte b3 = sol2RxBuffer[startIndex + 2];
                byte b4 = sol2RxBuffer[startIndex + 3];
                byte b5 = sol2RxBuffer[startIndex + 4];
                byte b6 = sol2RxBuffer[startIndex + 5];
                var device = DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 2");
                device.RxReceived = true;

                if (b2 != 0x45 || b6 != 0x56)
                {
                    sol2RxBuffer.RemoveAt(startIndex);
                    continue;
                }

                device.RxCount++;

                sol2RxBuffer.RemoveRange(startIndex, 5);

                await Task.Delay(70);

                device.RxReceived = false;
            }
        }

        private async void Solenoid1Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPortStream serialPort = (SerialPortStream)sender;

            if (!serialPort.IsOpen) return;

            while (DeviceManager.Solenoid1Port.BytesToRead > 0)
            {
                sol1RxBuffer.Add((byte)DeviceManager.Solenoid1Port.ReadByte());
            }

            while (sol1RxBuffer.Count >= 6)
            {
                int startIndex = sol1RxBuffer.FindIndex(b => b == 0x44);
                if (startIndex == -1)
                {
                    sol1RxBuffer.Clear();
                    return;
                }

                if (sol1RxBuffer.Count < startIndex + 6)
                    return;

                byte b1 = sol1RxBuffer[startIndex];
                byte b2 = sol1RxBuffer[startIndex + 1];
                byte b3 = sol1RxBuffer[startIndex + 2];
                byte b4 = sol1RxBuffer[startIndex + 3];
                byte b5 = sol1RxBuffer[startIndex + 4];
                byte b6 = sol1RxBuffer[startIndex + 5];
                var device = DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 1");
                device.RxReceived = true;

                if (b2 != 0x45 || b6 != 0x56)
                {
                    sol1RxBuffer.RemoveAt(startIndex);
                    continue;
                }

                device.RxCount++;

                sol1RxBuffer.RemoveRange(startIndex, 5);

                await Task.Delay(70);

                device.RxReceived = false;
            }
        }


        private async void Solenoid3Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPortStream serialPort = (SerialPortStream)sender;

            if (!serialPort.IsOpen) return;

            while (DeviceManager.Solenoid3Port.BytesToRead > 0)
            {
                sol3RxBuffer.Add((byte)DeviceManager.Solenoid3Port.ReadByte());
            }

            while (sol3RxBuffer.Count >= 6)
            {
                int startIndex = sol3RxBuffer.FindIndex(b => b == 0x44);
                if (startIndex == -1)
                {
                    sol3RxBuffer.Clear();
                    return;
                }

                if (sol3RxBuffer.Count < startIndex + 6)
                    return;

                byte b1 = sol3RxBuffer[startIndex];
                byte b2 = sol3RxBuffer[startIndex + 1];
                byte b3 = sol3RxBuffer[startIndex + 2];
                byte b4 = sol3RxBuffer[startIndex + 3];
                byte b5 = sol3RxBuffer[startIndex + 4];
                byte b6 = sol3RxBuffer[startIndex + 5];
                var device = DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 3");
                device.RxReceived = true;

                if (b2 != 0x45 || b6 != 0x56)
                {
                    sol3RxBuffer.RemoveAt(startIndex);
                    continue;
                }

                device.RxCount++;

                sol3RxBuffer.RemoveRange(startIndex, 5);

                await Task.Delay(70);

                device.RxReceived = false;
            }
        }

        private const int SYSTEM_FRAME_SIZE = 13;

        private void SystemPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (systemRxBuffer)  // KHÔNG lock lồng, lock một lần cho toàn bộ
            {
                if (DeviceManager?.SystemPort == null || !DeviceManager.SystemPort.IsOpen)
                    return;

                // Đọc dữ liệu – catch exception
                try
                {
                    while (DeviceManager.SystemPort.BytesToRead > 0)
                    {
                        systemRxBuffer.Add((byte)DeviceManager.SystemPort.ReadByte());
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is ObjectDisposedException)
                {
                    Debug.WriteLine($"Serial read error: {ex.GetType().Name} → {ex.Message}");
                    return;
                }

                // Parse frames
                while (systemRxBuffer.Count >= SYSTEM_FRAME_SIZE)
                {
                    int startIndex = systemRxBuffer.IndexOf(STX);
                    if (startIndex < 0)
                    {
                        // Không có STX → xóa rác nếu buffer quá dài
                        if (systemRxBuffer.Count > 2048)  // ngưỡng tùy chỉnh
                        {
                            Debug.WriteLine($"Clearing junk buffer: {systemRxBuffer.Count} bytes");
                            systemRxBuffer.Clear();
                        }
                        return;
                    }

                    // Bỏ byte rác trước STX
                    if (startIndex > 0)
                    {
                        systemRxBuffer.RemoveRange(0, startIndex);
                        startIndex = 0;
                    }

                    if (systemRxBuffer.Count < SYSTEM_FRAME_SIZE)
                        return;

                    if (systemRxBuffer[SYSTEM_FRAME_SIZE - 1] != ETX)
                    {
                        // Frame hỏng → bỏ STX thôi, thử tìm frame tiếp theo
                        systemRxBuffer.RemoveAt(0);
                        continue;
                    }

                    // Frame OK → extract an toàn
                    byte[] sensorData = new byte[11];
                    systemRxBuffer.CopyTo(1, sensorData, 0, 11);

                    var device = DevicesStatus?.FirstOrDefault(d => d?.Name == "System");
                    if (device == null)
                    {
                        Debug.WriteLine("Device 'System' not found in list");
                    }
                    else
                    {
                        try
                        {
                            device.RxReceived = true;

                            SystemData.MainDirection = sensorData[0] switch
                            {
                                0x00 => Direction.Stop,
                                0x01 => Direction.Up,
                                0x02 => Direction.Down,
                                0xFF => Direction.Error,
                                _ => Direction.Error
                            };

                            SystemData.MainTop = sensorData[1] == 0xFF;
                            SystemData.MainBottom = sensorData[2] == 0xFF;

                            //Debug.Write($"MainTop:{(SystemData.MainTop ? 1 : 0)}");
                            //Debug.Write($"   ");
                            //Debug.Write($"MainBottom:{(SystemData.MainBottom ? 1 :0)}");
                            //Debug.Write($"   ");
                            //Debug.WriteLine($"MainDirection:{SystemData.MainDirection}");
                        }
                        finally
                        {
                            device.RxReceived = false;  // luôn reset dù có lỗi
                        }
                    }

                    // Xóa frame đã xử lý
                    systemRxBuffer.RemoveRange(0, SYSTEM_FRAME_SIZE);
                }
            }
        }

        void ParseMICOMFrame(byte[] frame, ObservableCollection<CAPSensor> listCAPSensor)
        {
            if (Application.Current == null) return;

            try
            {


                if (frame[1] == 0x00 || frame[1] == 0x01 || frame[1] == 0x02)
                {
                    ushort sensorIndex = frame[1];
                    ushort cycleIndex = frame[2];
                    var currentCycle = listCAPSensor[sensorIndex].ListCAPCycle[cycleIndex];
                    var elements = currentCycle.ListCAPElement;

                    // Update Delta trước (vẫn trên UI thread tạm thời)
                    for (int elementID = 0; elementID < elements.Count; elementID++)
                    {
                        if (elementID * 2 + 4 >= frame.Length) break; // an toàn buffer overrun

                        ushort delta = (ushort)((frame[elementID * 2 + 3] << 8) | frame[elementID * 2 + 4]);
                        //elements[elementID].Delta = delta;

                        elements[elementID].Delta = delta;

                        Debug.WriteLine($"{sensorIndex}.{cycleIndex}.{elementID} : {delta}" );

                    }

                    // Tính Max chỉ 1 lần
                    var allDeltas = listCAPSensor
                        .SelectMany(s => s.ListCAPCycle)
                        .SelectMany(c => c.ListCAPElement)
                        .Select(e => e.Delta);

                    ushort maxDelta = allDeltas.Any() ? allDeltas.Max() : (ushort)0;


                    // Update IsMax chỉ 1 lần
                    foreach (var elem in elements)
                    {
                        elem.IsMax = (elem.Delta == maxDelta);
                    }

                    //Application.Current.Dispatcher.InvokeAsync(() =>
                    //{
                    //    // Update Delta trước (vẫn trên UI thread tạm thời)
                    //    for (int elementID = 0; elementID < elements.Count; elementID++)
                    //    {
                    //        if (elementID * 2 + 4 >= frame.Length) break; // an toàn buffer overrun

                    //        ushort delta = (ushort)((frame[elementID * 2 + 3] << 8) | frame[elementID * 2 + 4]);
                    //        //elements[elementID].Delta = delta;

                    //        elements[elementID].Delta = delta;


                    //    }

                    //    // Tính Max chỉ 1 lần
                    //    var allDeltas = listCAPSensor
                    //        .SelectMany(s => s.ListCAPCycle)
                    //        .SelectMany(c => c.ListCAPElement)
                    //        .Select(e => e.Delta);

                    //    ushort maxDelta = allDeltas.Any() ? allDeltas.Max() : (ushort)0;



                    //    // Update IsMax chỉ 1 lần
                    //    foreach (var elem in elements)
                    //    {
                    //        elem.IsMax = (elem.Delta == maxDelta);
                    //    }
                    //});
                }

                else if (frame[1] == (byte)'M')
                {
                    if (frame[2] == (byte)'L')
                    {
                        var verNum = (frame[6] | frame[5] << 8 | frame[4] << 16 | frame[3] << 32);
                        FirmwareMicom.Micom1 = View_Model.FirmwareMICOM.FirmwareList[verNum];
                        Debug.WriteLine($"MICOM 1: {FirmwareMicom.Micom1}");
                    }
                    if (frame[2] == (byte)'R')
                    {
                        var verNum = (frame[6] | frame[5] << 8 | frame[4] << 16 | frame[3] << 32);
                        FirmwareMicom.Micom2 = View_Model.FirmwareMICOM.FirmwareList[verNum];
                        Debug.WriteLine($"MICOM 2: {FirmwareMicom.Micom2}");
                    }
                }




            }
            catch
            {

            }



        }


        void TryParseMICOMFrame(ObservableCollection<CAPSensor> listCAPSensor, List<byte> micomRXBuffer, ref int readIndex)
        {
            while (micomRXBuffer.Count - readIndex >= MICOM_FRAME_LEN)
            {
                if (micomRXBuffer[readIndex] != STX)
                {
                    readIndex++;
                    continue;
                }

                if (micomRXBuffer[readIndex + MICOM_FRAME_LEN - 1] != ETX)
                {
                    readIndex++;
                    continue;
                }

                byte[] frame = new byte[MICOM_FRAME_LEN];
                micomRXBuffer.CopyTo(readIndex, frame, 0, MICOM_FRAME_LEN);

                readIndex += MICOM_FRAME_LEN;


                ParseMICOMFrame(frame, listCAPSensor);


            }

            // Dọn buffer khi index lớn
            if (readIndex > 100)
            {
                micomRXBuffer.RemoveRange(0, readIndex);
                readIndex = 0;
            }
        }


        int readIndex1 = 0;
        int readIndex2 = 0;
        private async void MicomPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            lock (DeviceManager.PortLockMicom1)
            {
                SerialPortStream serialPort = (SerialPortStream)sender;
                if (!serialPort.IsOpen) return;

                int len = serialPort.BytesToRead;
                byte[] temp = new byte[len];

                serialPort.Read(temp, 0, len);


                lock (micom1RXBuffer)
                {
                    micom1RXBuffer.AddRange(temp);
                    TryParseMICOMFrame(ListCAPSensor1, micom1RXBuffer, ref readIndex1);
                }
            }

        }
        private async void MicomPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (DeviceManager.PortLockMicom2)
            {
                SerialPortStream serialPort = (SerialPortStream)sender;
                if (!serialPort.IsOpen) return;

                int len = serialPort.BytesToRead;
                byte[] temp = new byte[len];

                serialPort.Read(temp, 0, len);


                lock (micom2RXBuffer)
                {
                    micom2RXBuffer.AddRange(temp);
                    TryParseMICOMFrame(ListCAPSensor2, micom2RXBuffer, ref readIndex2);
                }

            }

        }


        internal async Task VerifyMICOM(int id)
        {
            object lockObj = new object();


            SerialPortStream micomPort = new SerialPortStream();

            if (id == 1)
            {
                lockObj = DeviceManager.PortLockMicom1;
                FirmwareMicom.Micom1 = string.Empty;
                micomPort = DeviceManager.MicomPort1;
            }
            if (id == 2)
            {
                lockObj = DeviceManager.PortLockMicom2;
                FirmwareMicom.Micom2 = string.Empty;
                micomPort = DeviceManager.MicomPort2;

            }

            lock (lockObj)
            {
                if (!micomPort.IsOpen) return;
                var device = DevicesStatus.FirstOrDefault(d => d.Name == $"Micom{id}");
                device.TxSent = true;

                byte[] tx = System.Text.Encoding.ASCII.GetBytes("VERIFY\r\n");
                micomPort.Write(tx, 0, tx.Length);



                device.TxSent = false;
            }

        }

        internal async Task HaltMICOM(int id)
        {
            SerialPortStream micomPort = new SerialPortStream();
            if (id == 1)
            {
                micomPort = DeviceManager.MicomPort1;
            }
            else if (id == 2)
            {
                micomPort = DeviceManager.MicomPort2;

            }
            if (!micomPort.IsOpen) return;
            var device = DevicesStatus.FirstOrDefault(d => d.Name == $"Micom{id}");
            device.TxSent = true;

            byte[] tx = System.Text.Encoding.ASCII.GetBytes("DISABLE\r\n");
            micomPort.Write(tx, 0, tx.Length);
            device.TxSent = false;
        }

        internal async Task ResumeMICOM(int id)
        {
            SerialPortStream micomPort = new SerialPortStream();
            if (id == 1)
            {
                micomPort = DeviceManager.MicomPort1;
            }
            else if (id == 2)
            {
                micomPort = DeviceManager.MicomPort2;

            }
            if (!micomPort.IsOpen) return;
            var device = DevicesStatus.FirstOrDefault(d => d.Name == $"Micom{id}");
            device.TxSent = true;

            byte[] tx = System.Text.Encoding.ASCII.GetBytes("ENABLE\r\n");
            micomPort.Write(tx, 0, tx.Length);
            device.TxSent = false;
        }


        internal async Task RecalibMICOM(int id)
        {
            SerialPortStream micomPort = new SerialPortStream();
            if (id == 1)
            {
                micomPort = DeviceManager.MicomPort1;
            }
            else if (id == 2)
            {
                micomPort = DeviceManager.MicomPort2;

            }
            if (!micomPort.IsOpen) return;
            var device = DevicesStatus.FirstOrDefault(d => d.Name == $"Micom{id}");
            device.TxSent = true;

            byte[] tx = System.Text.Encoding.ASCII.GetBytes("RESET\r\n");
            micomPort.Write(tx, 0, tx.Length);
            device.TxSent = false;
        }

        internal async Task ResetMainCylinder()
        {
            if (EnableResetCylinder)
            {
                if (!DeviceManager.Solenoid3Port.IsOpen) return;
                var device = DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 3");
                device.TxSent = true;

                byte[] tx = { 0x44, 0x45, 0x06, 0x53, 0x00, 0x00, 0x10, 0x00, 0x44, 0x56 };

                DeviceManager.Solenoid3Port.Write(tx, 0, tx.Length);
                device.TxSent = false;

                var start = DateTime.Now;
                while (DateTime.Now.Subtract(start).TotalMilliseconds < 2000)
                {
                    if (sol3RxPass)
                    {
                        sol3RxPass = false;
                        break;
                    }
                    await Task.Delay(1);
                }

                await Task.Delay(300);

                device.TxSent = true;
                byte[] tx2 = { 0x44, 0x45, 0x06, 0x53, 0x00, 0x00, 0x00, 0x00, 0x54, 0x56 };
                DeviceManager.Solenoid3Port.Write(tx2, 0, tx2.Length);
                device.TxSent = false;

                var start2 = DateTime.Now;
                while (DateTime.Now.Subtract(start2).TotalMilliseconds < 2000)
                {
                    if (sol3RxPass)
                    {
                        sol3RxPass = false;
                        break;
                    }
                    await Task.Delay(1);
                }
            }

        }

        internal async void ConnectorAllDown()
        {
            if (!DeviceManager.Solenoid3Port.IsOpen) return;

            var device = DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 3");
            device.TxSent = true;

            byte[] tx = { 0x44, 0x45, 0x06, 0x53, 0x00, 0x00, 0x0F, 0x00, 0x5B, 0x56 };

            DeviceManager.Solenoid3Port.Write(tx, 0, tx.Length);
            device.TxSent = false;

            var start = DateTime.Now;
            while (DateTime.Now.Subtract(start).TotalMilliseconds < 2000)
            {
                if (sol3RxPass)
                {
                    sol3RxPass = false;
                    break;
                }
                await Task.Delay(1);
            }
        }

        internal async void ConnectorAllUp()
        {
            if (!DeviceManager.Solenoid3Port.IsOpen) return;

            var device = DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 3");
            device.TxSent = true;

            byte[] tx = { 0x44, 0x45, 0x06, 0x53, 0x00, 0x00, 0x00, 0x00, 0x54, 0x56 };

            DeviceManager.Solenoid3Port.Write(tx, 0, tx.Length);
            device.TxSent = false;

            var start = DateTime.Now;
            while (DateTime.Now.Subtract(start).TotalMilliseconds < 2000)
            {
                if (sol3RxPass)
                {
                    sol3RxPass = false;
                    break;
                }
                await Task.Delay(1);
            }
        }


        internal async Task ResetSolenoid1()
        {
            if (!DeviceManager.Solenoid1Port.IsOpen) return;

            var device = DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 1");
            device.TxSent = true;

            byte[] tx = { 0x44, 0x45, 0x06, 0x53, 0x00, 0x00, 0x00, 0x00, 0x54, 0x56 };

            DeviceManager.Solenoid1Port.Write(tx, 0, tx.Length);
            device.TxSent = false;


        }

        internal async Task ResetSolenoid2()
        {
            if (!DeviceManager.Solenoid2Port.IsOpen) return;

            var device = DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 2");
            device.TxSent = true;

            byte[] tx = { 0x44, 0x45, 0x06, 0x53, 0x00, 0x00, 0x00, 0x00, 0x54, 0x56 };

            DeviceManager.Solenoid2Port.Write(tx, 0, tx.Length);
            device.TxSent = false;


        }
    }
}
