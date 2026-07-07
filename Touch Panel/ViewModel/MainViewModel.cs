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

        // Expose để thanh thiết bị (bottom bar) hiển thị NEXT SERIAL.
        public AutoPageViewModel AutoPageVM => autoPageViewModel;

        // Expose Model để top bar bind HEX FILE (SelectedFirmwareMicom).
        public Model.Model Model => sharedModel;

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

                    // Khôi phục serial đã in gần nhất cho model này (lưu thầm theo ĐƯỜNG DẪN file model).
                    var cachedSerial = AppState.GetSerial(recentPath);
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

            // 2 nút RESET trong Setting bind trực tiếp tới AutoPageVM.
            settingPageViewModel.AutoPageVM = autoPageViewModel;

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

        // Save: ghi đè file model đang mở; chưa có file thì chuyển sang Save As.
        [RelayCommand]
        private void SaveModel()
        {
            if (string.IsNullOrEmpty(Utility.CurrentModelFilePath) || !File.Exists(Utility.CurrentModelFilePath))
            {
                SaveModelAs();
                return;
            }
            try
            {
                Utility.SaveModel(sharedModel, Utility.CurrentModelFilePath, Path.GetFileName(Utility.CurrentModelFilePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error!" + ex.Message);
            }
        }

        // Save As: chọn tên file mới, file đó thành file đang mở.
        [RelayCommand]
        private void SaveModelAs()
        {
            try
            {
                var saveFile = new Microsoft.Win32.SaveFileDialog
                {
                    DefaultExt = ".json",
                    Filter = "Model File (*.json)|*.json"
                };
                if (saveFile.ShowDialog() == true)
                {
                    Utility.SaveModel(sharedModel, saveFile.FileName, saveFile.SafeFileName);
                    Utility.CurrentModelFilePath = saveFile.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                        ? saveFile.FileName
                        : saveFile.FileName + ".json";
                    ModelName = Path.GetFileNameWithoutExtension(Utility.CurrentModelFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error!" + ex.Message);
            }
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
            mData.VerifyLog = string.Empty;

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
                    // Xóa để ô MICOM (thanh bottom) hiện progress "Writing X%" (FirmwareLog ưu tiên thấp nhất,
                    // vòng lặp verify phía trên đã set SignalIntegrity nên phải clear kẻo bị che).
                    mData.SignalIntegrity = string.Empty;
                    mData.VerifyLog = string.Empty;
                    mData.FirmwareLog = string.Empty;

                    sharedAutoState?.BeginFirmwareWrite(); // đang ghi firmware -> cấm test

                    try
                    {
                        sharedModel.Devices.CloseDeviceByName(micomName);

                        await Task.Run(() =>
                            FirmwareMICOM.WriteFirmware(portName, $"firmware_bsl_{index}\\{sharedModel.Devices.SelectedFirmwareMicom}.txt", ProgressFirmwareChanged, ProgressFirmwareFailed)
                        );
                    }
                    finally
                    {
                        sharedModel.Devices.ConnectDeviceByName(micomName);
                        sharedAutoState?.EndFirmwareWrite();
                    }

                    // Kết quả verify -> VerifyLog (tách khỏi FirmwareLog/progress).
                    mData.VerifyLog = mData.FirmwareLog.Contains("100") ? "✓" : "✕";
                }
                else
                {
                    mData.VerifyLog = "✓";
                }
            }
            catch (Exception)
            {
                mData.VerifyLog = "✕";
            }
            // Kết quả verify (✓/✕) GIỮ NGUYÊN, chỉ mất khi RESUME MICOM (xem Devices.ResumeMICOM).
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



                    // Chặn test từ đầu quá trình mở model cho tới khi verify/write firmware xong (cả 2 MICOM).
                    sharedAutoState?.BeginFirmwareWrite();
                    try
                    {
                        await sharedModel.Devices.ConnectAll();

                        await Task.Delay(1000);

                        MicomContext micomCtx1 = new MicomContext() { LockObject = deviceManager.PortLockMicom1, SerialPort = deviceManager.MicomPort1, MICOMData = sharedModel.Devices.MicomData1 };
                        MicomContext micomCtx2 = new MicomContext() { LockObject = deviceManager.PortLockMicom2, SerialPort = deviceManager.MicomPort2, MICOMData = sharedModel.Devices.MicomData2 };

                        // Chỉ verify/write firmware cho MICOM có cổng COM đang MỞ.
                        var micomTasks = new List<Task>();
                        if (DeviceManager.IsPortOpen(deviceManager.MicomPort1))
                            micomTasks.Add(UpdateMicomAsync(micomCtx1));
                        if (DeviceManager.IsPortOpen(deviceManager.MicomPort2))
                            micomTasks.Add(UpdateMicomAsync(micomCtx2));

                        await Task.WhenAll(micomTasks);
                    }
                    finally
                    {
                        sharedAutoState?.EndFirmwareWrite();
                    }

                    // Đảm bảo vòng lặp test đang chạy sau khi mở model -> nhấn START test được ngay.
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
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
                    autoPageViewModel.AutoPageActive = true;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                case "Manual":
                    // Manual điều khiển tay -> phải DỪNG auto test để tránh xung đột vật lý.
                    CurrentPage = manualPageView;
                    autoPageViewModel.AutoPageActive = false;
                    autoPageViewModel.runWhileLoop = false;
                    break;
                case "Setting":
                    // Loop chạy nền (giữ state/kết quả) nhưng KHÔNG tự trigger test.
                    CurrentPage = settingPageView;
                    autoPageViewModel.AutoPageActive = false;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                case "NewModel":
                    CurrentPage = newModelPageView;
                    autoPageViewModel.AutoPageActive = false;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                case "Tunning":
                    CurrentPage = tunningPageView;
                    autoPageViewModel.AutoPageActive = false;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                case "Log":
                    // Chỉ xem log; loop chạy nền, không tự trigger test.
                    CurrentPage = logPageView;
                    autoPageViewModel.AutoPageActive = false;
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
