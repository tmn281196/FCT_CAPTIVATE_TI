using BSL430_NET;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic.Logging;
using Microsoft.Win32;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Touch_Panel.Model;
using Touch_Panel.View;
using Touch_Panel.ViewModel;


namespace Touch_Panel.View_Model
{
    public partial class MicomContext
    {
        public object LockObject;
        public SerialPortStream SerialPort;
        public MICOMData MICOMData;

    }
    public partial class MainViewModel : ObservableObject
    {
        AutoPageView autoPageView;
        ManualPageView manualPageView;
        TunningPageView tunningPageView;
        SettingPageView settingPageView;
        StepsPageView newModelPageView;
        LogPageView logPageView;

        AutoPageViewModel autoPageViewModel;
        ManualPageViewModel manualPageViewModel;
        TunningPageViewModel tunningPageViewModel;
        SettingPageViewModel settingPageViewModel;
        StepsPageViewModel stepsPageViewModel;

        DeviceConnectionListViewModel deviceConnectionListViewModel;

        DeviceConnectionList deviceConnectionList;
        TestLogic testLogic = new TestLogic();

        Model.Model sharedModel;
        AutoState sharedAutoState;

        // Expose để bind IsEnabled trên MainWindow (khóa UI khi đang test).
        public AutoState AutoState => sharedAutoState;

        DeviceManager deviceManager;

        MicomDatabases sharedMicomDatabases;




        public MainViewModel()
        {
            try
            {
                string databaseString = File.ReadAllText("MicomDatabase.json");

                sharedMicomDatabases = Utility.ConvertFromJson<MicomDatabases>(databaseString);
            }
            catch
            {
                sharedMicomDatabases = new MicomDatabases();
            }


            deviceManager = new DeviceManager();

            // Khi auto-load được model gần nhất thì tự kết nối COM luôn (model trắng thì không).
            bool autoConnectComPorts = false;
            try
            {
                // Ưu tiên load lại file model mở gần nhất.
                string recentPath = Utility.GetRecentModelPath();

                if (!string.IsNullOrEmpty(recentPath))
                {
                    string modelStr = File.ReadAllText(recentPath);
                    sharedModel = Utility.ConvertFromJson<Model.Model>(modelStr);

                    // Nhớ làm file đang mở để lệnh Save ghi đè đúng file và hiển thị tên model.
                    Utility.CurrentModelFilePath = recentPath;
                    ModelName = Path.GetFileNameWithoutExtension(recentPath);
                    autoConnectComPorts = true;

                    // Khôi phục serial đã in gần nhất cho model này (lưu thầm theo tên model).
                    var cachedSerial = AppState.GetSerial(ModelName);
                    if (cachedSerial != null)
                    {
                        sharedModel.Settings.SerialNumber = cachedSerial.SerialNumber;
                        sharedModel.LastPrintDate = cachedSerial.LastPrintDate;
                    }
                }
                else
                {
                    // Chưa có file gần nhất -> mở model trắng (new model).
                    sharedModel = new Model.Model();
                }
            }
            catch
            {
                sharedModel = new Model.Model();
            }

            var micomDatabaseSelected = sharedMicomDatabases.MicomDatabaseList.Where(item => item.FirmwareMicom == sharedModel.Devices.SelectedFirmwareMicom).FirstOrDefault();
            if (micomDatabaseSelected != null)
            {
                sharedModel.Devices.MicomData1 = micomDatabaseSelected.MicomData1;
                sharedModel.Devices.MicomData2 = micomDatabaseSelected.MicomData2;

            }


            sharedModel.Devices.DeviceManager = deviceManager;
            sharedModel.Devices.FirmwareList = sharedMicomDatabases.MicomDatabaseList.Select(item => item.FirmwareMicom).ToList();


            autoPageViewModel = new AutoPageViewModel(sharedModel);
            manualPageViewModel = new ManualPageViewModel(sharedModel);
            tunningPageViewModel = new TunningPageViewModel(sharedModel);
            settingPageViewModel = new SettingPageViewModel(sharedModel);
            stepsPageViewModel = new StepsPageViewModel(sharedModel);

            sharedAutoState = new AutoState();
            autoPageViewModel.State = sharedAutoState;
            stepsPageViewModel.AutoState = sharedAutoState;
            settingPageViewModel.AutoState = sharedAutoState;
            manualPageViewModel.AutoState = sharedAutoState;

            deviceConnectionListViewModel = new DeviceConnectionListViewModel(sharedModel.Devices);

            autoPageView = new AutoPageView(autoPageViewModel);
            manualPageView = new ManualPageView(manualPageViewModel);
            tunningPageView = new TunningPageView(tunningPageViewModel);
            settingPageView = new SettingPageView(settingPageViewModel);
            newModelPageView = new StepsPageView(stepsPageViewModel);
            logPageView = new LogPageView();
            deviceConnectionList = new DeviceConnectionList(deviceConnectionListViewModel);
            testLogic.Model = sharedModel;

            autoPageViewModel.TestLogic = testLogic;
            manualPageViewModel.TestLogic = testLogic;
            tunningPageViewModel.TestLogic = testLogic;
            deviceConnectionPage = deviceConnectionList;

            tunningPageViewModel.MainViewModel = this;

            // Tự kết nối COM khi đã auto-load model gần nhất (chạy nền, không chặn khởi động UI).
            if (autoConnectComPorts)
            {
                _ = sharedModel.Devices.ConnectAll();
            }

            Navigate("Home");
        }

        [ObservableProperty]
        private UserControl currentPage;

        [ObservableProperty]
        private UserControl deviceConnectionPage;

        [ObservableProperty]
        private BitmapImage tngLogo = new BitmapImage(new Uri("/CompanyLogo/TNG Logo.png", UriKind.Relative));

        [ObservableProperty]
        private ImageSource humanLogo;

        [ObservableProperty]
        private string modelName;


        [RelayCommand]
        private void TunningPage()
        {
            Navigate("Tunning");
        }
        [RelayCommand]
        private void SettingPage()
        {
            Navigate("Setting");
        }

        [RelayCommand]
        private void HomePage()
        {
            Navigate("Home");
        }

        [RelayCommand]
        private void NewModelPage()
        {
            Navigate("NewModel");
        }




        [RelayCommand]
        private void ManualPage()
        {
            Navigate("Manual");
        }

        [RelayCommand]
        private void LogPage()
        {
            Navigate("Log");
        }
        private void ProgressFirmwareChanged(object source, Bsl430NetEventArgs args)
        {
            var bslSource = (BSL430NET)source;
            var com = bslSource.DefaultDevice.Name;

            string log = $"Writing {args.Progress.ToString("0.00")}%";

            if (sharedModel.Devices.MicomCom1Port == com)
            {
                sharedModel.Devices.MicomData1.FirmwareLog = log;

            }
            if (sharedModel.Devices.MicomCom2Port == com)
            {
                sharedModel.Devices.MicomData2.FirmwareLog = log;

            }


        }


        private void ProgressFirmwareFailed(object? sender, string message)
        {
            string portName = sender as string;

            string log = $"Error: {message}";

            if (portName == sharedModel.Devices.MicomCom1Port)
            {
                sharedModel.Devices.MicomData1.FirmwareLog = log;

            }
            if (portName == sharedModel.Devices.MicomCom2Port)
            {
                sharedModel.Devices.MicomData2.FirmwareLog = log;

            }
        }



        private async Task UpdateMicomAsync(MicomContext micomCtx)
        {
            string portName = micomCtx.SerialPort.PortName;
            var mData = micomCtx.MICOMData;
            var index = mData.Id;
            var timeout = TimeSpan.FromSeconds(5);
            var micomName = $"Micom{index}";

            mData.SignalIntegrity = string.Empty;
            mData.FirmwareLog = string.Empty;

            try
            {
                var sw = Stopwatch.StartNew();
                // Vòng lặp kiểm tra firmware hiện tại
                while (sw.Elapsed < timeout)
                {
                    await sharedModel.Devices.VerifyMICOM(micomCtx);

                    if (micomCtx.MICOMData.MatchFirmware)
                    {
                        break;
                    }

                    await Task.Delay(10);
                }


                if (!micomCtx.MICOMData.MatchFirmware)
                {
                    sharedModel.Devices.CloseDeviceByName(micomName);

                    await Task.Run(() =>
                        FirmwareMICOM.WriteFirmware(portName, $"firmware_bsl_{index}\\{sharedModel.Devices.SelectedFirmwareMicom}.txt", ProgressFirmwareChanged, ProgressFirmwareFailed)
                    );

                    sharedModel.Devices.ConnectDeviceByName(micomName);

                    if (mData.FirmwareLog.Contains("100"))
                    {
                        mData.FirmwareLog = "✓";
                    }
                    else
                    {
                        mData.FirmwareLog = "✕";

                    }

                }
                else
                {
                    mData.FirmwareLog = "✓";
                }
            }
            catch (Exception)
            {
                mData.FirmwareLog = "✕";
            }
        }

        public void SaveMicomDatabase()
        {
            try
            {
                string json = JsonSerializer.Serialize(sharedMicomDatabases, new JsonSerializerOptions
                {
                    WriteIndented = true // Makes JSON output more readable
                });

                File.WriteAllText("MicomDatabase.json", json);

            }
            catch
            {
                throw;
            }
        }


        [RelayCommand]
        private async Task OpenModel()
        {
            OpenFileDialog openFile = new OpenFileDialog()
            {
                DefaultExt = ".json",
                Title = "Open model",
            };
            openFile.Filter = "Touch Panel (*.json)|*.json";
            openFile.RestoreDirectory = true;

            if (openFile.ShowDialog() == true)
            {
                ModelName = Path.GetFileNameWithoutExtension(openFile.FileName);
                Utility.CurrentModelFilePath = openFile.FileName;
                string modelStr = File.ReadAllText(openFile.FileName);

                try
                {

                    sharedModel = Utility.ConvertFromJson<Model.Model>(modelStr);

                    var micomDatabaseSelected = sharedMicomDatabases.MicomDatabaseList.Where(item => item.FirmwareMicom == sharedModel.Devices.SelectedFirmwareMicom).FirstOrDefault();
                    if (micomDatabaseSelected != null)
                    {
                        sharedModel.Devices.MicomData1 = micomDatabaseSelected.MicomData1;
                        sharedModel.Devices.MicomData2 = micomDatabaseSelected.MicomData2;
                    }


                    deviceManager.Dispose();

                    deviceManager = new DeviceManager();

                    sharedModel.Devices.DeviceManager = deviceManager;
                    sharedModel.Devices.FirmwareList = sharedMicomDatabases.MicomDatabaseList.Select(item => item.FirmwareMicom).ToList();

                    autoPageViewModel.Model = sharedModel;
                    manualPageViewModel.Model = sharedModel;
                    tunningPageViewModel.Model = sharedModel;
                    settingPageViewModel.Model = sharedModel;
                    stepsPageViewModel.Model = sharedModel;

                    testLogic.Model = sharedModel;
                    deviceConnectionListViewModel.Devices = sharedModel.Devices;



                    await sharedModel.Devices.ConnectAll();

                    await Task.Delay(1000);

                    MicomContext micomCtx1 = new MicomContext() { LockObject = deviceManager.PortLockMicom1, SerialPort = deviceManager.MicomPort1, MICOMData = sharedModel.Devices.MicomData1 };
                    MicomContext micomCtx2 = new MicomContext() { LockObject = deviceManager.PortLockMicom2, SerialPort = deviceManager.MicomPort2, MICOMData = sharedModel.Devices.MicomData2 };

                    //Chạy song song cả 2 task
                    Task task1 = UpdateMicomAsync(micomCtx1);
                    Task task2 = UpdateMicomAsync(micomCtx2);

                    await Task.WhenAll(task1, task2);


                }
                catch (Exception)
                {
                    MessageBox.Show("Failed to Call Model File!");
                }
            }
        }

        private async Task Navigate(string pageName)
        {

            switch (pageName)
            {
                case "Home":
                    // Chỉ trang Auto mới được KÍCH HOẠT test (main down -> chạy).
                    CurrentPage = autoPageView;
                    autoPageViewModel.autoPageActive = true;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                case "Manual":
                    // Manual điều khiển tay -> phải DỪNG auto test để tránh xung đột vật lý.
                    CurrentPage = manualPageView;
                    autoPageViewModel.autoPageActive = false;
                    autoPageViewModel.runWhileLoop = false;
                    break;
                case "Setting":
                    // Loop chạy nền (giữ state/kết quả) nhưng KHÔNG tự trigger test.
                    CurrentPage = settingPageView;
                    autoPageViewModel.autoPageActive = false;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                case "NewModel":
                    CurrentPage = newModelPageView;
                    autoPageViewModel.autoPageActive = false;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                case "Tunning":
                    CurrentPage = tunningPageView;
                    autoPageViewModel.autoPageActive = false;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                case "Log":
                    // Chỉ xem log; loop chạy nền, không tự trigger test.
                    CurrentPage = logPageView;
                    autoPageViewModel.autoPageActive = false;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                default:
                    break;
            }
        }

        public void Dispose()
        {
            sharedModel.Devices.CloseAll();
            sharedModel.Devices.Dispose();
            autoPageViewModel.Dispose();

        }
    }
}
