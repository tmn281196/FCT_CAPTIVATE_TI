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
        SettingPageView settingPageView;
        StepsPageView newModelPageView;

        AutoPageViewModel autoPageViewModel;
        ManualPageViewModel manualPageViewModel;
        SettingPageViewModel settingPageViewModel;
        StepsPageViewModel stepsPageViewModel;

        DeviceConnectionListViewModel deviceConnectionListViewModel;

        DeviceConnectionList deviceConnectionList;
        TestLogic testLogic = new TestLogic();

        Model.Model sharedModel;
        AutoState sharedAutoState;

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
            try
            {
                string modelStr = File.ReadAllText("template_model.json");

                sharedModel = Utility.ConvertFromJson<Model.Model>(modelStr);
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
            settingPageViewModel = new SettingPageViewModel(sharedModel);
            stepsPageViewModel = new StepsPageViewModel(sharedModel);

            sharedAutoState = new AutoState();
            autoPageViewModel.State = sharedAutoState;
            stepsPageViewModel.AutoState = sharedAutoState;
            settingPageViewModel.AutoState = sharedAutoState;

            deviceConnectionListViewModel = new DeviceConnectionListViewModel(sharedModel.Devices);

            autoPageView = new AutoPageView(autoPageViewModel);
            manualPageView = new ManualPageView(manualPageViewModel);
            settingPageView = new SettingPageView(settingPageViewModel);
            newModelPageView = new StepsPageView(stepsPageViewModel);
            deviceConnectionList = new DeviceConnectionList(deviceConnectionListViewModel);
            testLogic.Model = sharedModel;
            autoPageViewModel.TestLogic = testLogic;
            manualPageViewModel.TestLogic = testLogic;
            deviceConnectionPage = deviceConnectionList;


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

    
                    CurrentPage = autoPageView;
                    autoPageViewModel.runWhileLoop = true;
                    autoPageViewModel.START();
                    break;
                case "Manual":
                    CurrentPage = manualPageView;
                    autoPageViewModel.runWhileLoop = false;
                    break;
                case "Setting":
                    CurrentPage = settingPageView;
                    autoPageViewModel.runWhileLoop = false;

                    break;
                case "NewModel":
                    CurrentPage = newModelPageView;
                    autoPageViewModel.runWhileLoop = false;
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
