using BSL430_NET;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HidSharp;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Touch_Panel.Model;
using Touch_Panel.ViewModel;


namespace Touch_Panel.View_Model
{
    public partial class SettingPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private Model.Model model;

        [ObservableProperty]
        private AutoState autoState;

        // Tham chiếu tới AutoPageVM để 2 nút RESET (thống kê / serial) bind trực tiếp.
        [ObservableProperty]
        private AutoPageViewModel autoPageVM;



        private static readonly HashSet<string> UserSelectFields = new()
        {
            nameof(Settings.PartNumber),
            nameof(Settings.PartnerCode),
            nameof(Settings.CountryCode),
            nameof(Settings.LineCode),
            nameof(Settings.EquipmentSerial),
        };

        public SettingPageViewModel(Model.Model sharedModel)
        {
            this.Model = sharedModel;

            bool changed = false;
            foreach (var field in UserSelectFields)
            {
                changed |= Model.UserSelectOptions.AddIfNew(field, GetSettingValue(field));
            }
            if (changed) Model.UserSelectOptions.Save();

            Model.Settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        private string GetSettingValue(string fieldName) => fieldName switch
        {
            nameof(Settings.PartNumber) => Model.Settings.PartNumber,
            nameof(Settings.PartnerCode) => Model.Settings.PartnerCode,
            nameof(Settings.CountryCode) => Model.Settings.CountryCode,
            nameof(Settings.LineCode) => Model.Settings.LineCode,
            nameof(Settings.EquipmentSerial) => Model.Settings.EquipmentSerial,
            _ => null
        };

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || !UserSelectFields.Contains(e.PropertyName)) return;

            if (Model.UserSelectOptions.AddIfNew(e.PropertyName, GetSettingValue(e.PropertyName)))
            {
                Model.UserSelectOptions.Save();
            }
        }

        [RelayCommand]
        private void DeleteOption(string parameter)
        {
            if (string.IsNullOrEmpty(parameter)) return;
            var parts = parameter.Split('|', 2);
            if (parts.Length != 2) return;
            if (Model.UserSelectOptions.RemoveOption(parts[0], parts[1]))
            {
                Model.UserSelectOptions.Save();
            }
        }


        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task VerifyFirmware(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            var timeout = TimeSpan.FromSeconds(5);
            MicomContext micomCtx1 = new MicomContext() { MICOMData = Model.Devices.MicomData1, LockObject = Model.Devices.DeviceManager.PortLockMicom1, SerialPort = Model.Devices.DeviceManager.MicomPort1 };
            MicomContext micomCtx2 = new MicomContext() { MICOMData = Model.Devices.MicomData2, LockObject = Model.Devices.DeviceManager.PortLockMicom2, SerialPort = Model.Devices.DeviceManager.MicomPort2 };
            MicomContext micomCtx = (testerId == 1) ? micomCtx1 : micomCtx2;


            if (!micomCtx.SerialPort.IsOpen)
            {
                micomCtx.MICOMData.VerifyLog = "✕";
                return;
            }

            await Model.Devices.VerifyMICOM(micomCtx);

            var sw = Stopwatch.StartNew();
            while (!micomCtx.MICOMData.MatchFirmware)
            {
                if (sw.Elapsed > timeout)
                    break;
                Thread.Sleep(10);
            }

            micomCtx.MICOMData.VerifyLog = micomCtx.MICOMData.MatchFirmware ? "✓" : "✕";
        }


        private void ProgressFirmwareChanged(object source, Bsl430NetEventArgs args)
        {
            var bslSource = (BSL430NET)source;
            var com = bslSource.DefaultDevice.Name;

            string log = $"Writing {args.Progress.ToString("0.00")}%";

            if (Model.Devices.MicomCom1Port == com)
            {
                Model.Devices.MicomData1.FirmwareLog = log;
            }
            if (Model.Devices.MicomCom2Port == com)
            {
                Model.Devices.MicomData2.FirmwareLog = log;
            }
        }


        private void ProgressFirmwareFailed(object? sender, string message)
        {
            string? portName = sender as string;

            string log = $"Error: {message}";

            if (portName == Model.Devices.MicomCom1Port)
            {
                Model.Devices.MicomData1.FirmwareLog = log;
            }
            if (portName == Model.Devices.MicomCom2Port)
            {
                Model.Devices.MicomData2.FirmwareLog = log;
            }
        }


        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task WriteFirmware(object parameter)
        {
            // Chặn khi đang TEST: auto Testing (START / main-down) HOẶC manual full test.
            // KHÔNG chặn khi MICOM kia chỉ đang ghi firmware.
            if (AutoState != null && (AutoState.IsTesting || AutoState.ManualTesting))
            {
                MessageBox.Show("Cannot write firmware while a test is in progress.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int testerId = int.Parse((string)parameter);
            string deviceName = $"Micom{testerId}";
            string portName = testerId == 1 ? Model.Devices.MicomCom1Port : Model.Devices.MicomCom2Port;
            var mData = testerId == 1 ? Model.Devices.MicomData1 : Model.Devices.MicomData2;
            string firmwarePath = $"firmware_bsl_{testerId}\\{Model.Devices.SelectedFirmwareMicom}.txt";

            AutoState?.BeginFirmwareWrite(); // đang ghi firmware -> cấm test

            try
            {
                Model.Devices.CloseDeviceByName(deviceName);

                mData.SignalIntegrity = string.Empty;
                mData.VerifyLog = string.Empty;
                mData.FirmwareLog = string.Empty;

                await Task.Run(() =>
                {
                    FirmwareMICOM.WriteFirmware(
                        portName,
                        firmwarePath,
                        ProgressFirmwareChanged,
                        ProgressFirmwareFailed);
                });
            }
            catch (Exception ex)
            {
                mData.FirmwareLog = ex.Message;
            }
            finally
            {
                Model.Devices.ConnectDeviceByName(deviceName);
                mData.VerifyLog = "✓";
                AutoState?.EndFirmwareWrite();
            }
        }

        [ObservableProperty]
        private CAPSensor? selectedCAPSensor1;

        [ObservableProperty]
        private CAPCycle? selectedCAPCycle1;

   

        [ObservableProperty]
        private CAPElement? selectedCAPElement1;



        [ObservableProperty]
        private CAPSensor? selectedCAPSensor2;

        [ObservableProperty]
        private CAPCycle? selectedCAPCycle2;

        [ObservableProperty]
        private CAPElement? selectedCAPElement2; 


        [RelayCommand]
        private async Task AddSensor(object parameter)
        {
            int testerId = int.Parse((string)parameter);

            MICOMData mData = new MICOMData();

            if (testerId == 1)
            {
                mData = Model.Devices.MicomData1;

            }
            if (testerId == 2)
            {
                mData = Model.Devices.MicomData2;
            }
            mData.ListCAPSensor.Add(new CAPSensor() { Id = mData.ListCAPSensor.Count});

        }

        [RelayCommand]
        private async Task AddCycle(object parameter)
        {
            int testerId = int.Parse((string)parameter);

            CAPSensor capSensor = new CAPSensor();

            if (testerId == 1)
            {
                capSensor = SelectedCAPSensor1;
            }
            if (testerId == 2)
            {
                capSensor = SelectedCAPSensor2;
            }

            if(capSensor!=null)
            {
                capSensor.ListCAPCycle.Add(new CAPCycle() { Id = capSensor.ListCAPCycle.Count , StringId = $"{capSensor.Id}.{capSensor.ListCAPCycle.Count}" });
            }
        }

        [RelayCommand]
        private async Task AddElement(object parameter)
        {
            int testerId = int.Parse((string)parameter);

            CAPCycle capCycle = new CAPCycle();

            if (testerId == 1)
            {
                capCycle = SelectedCAPCycle1;
            }
            if (testerId == 2)
            {
                capCycle = SelectedCAPCycle2;
            }

            if (capCycle != null)
            {
                capCycle.ListCAPElement.Add(new CAPElement() { Id = capCycle.ListCAPElement.Count , StringId = $"{capCycle.StringId}.{capCycle.ListCAPElement.Count}" });
            }
        }


        [RelayCommand]
        private async Task DeleteSensor(object parameter)
        {
            int testerId = int.Parse((string)parameter);

            MICOMData mData = new MICOMData();
            CAPSensor capSensor = new CAPSensor();     

            if (testerId == 1)
            {
                mData = Model.Devices.MicomData1;
                capSensor = SelectedCAPSensor1;

            }
            if (testerId == 2)
            {
                mData = Model.Devices.MicomData2;
                capSensor = SelectedCAPSensor2;
            }

            if (capSensor != null) {
                mData.ListCAPSensor.Remove(capSensor);
            }

        }

        [RelayCommand]
        private async Task DeleteCycle(object parameter)
        {
            int testerId = int.Parse((string)parameter);

            CAPSensor capSensor = new CAPSensor();
            CAPCycle capCycle = new CAPCycle();

            if (testerId == 1)
            {
                capSensor = SelectedCAPSensor1;
                capCycle = SelectedCAPCycle1;

            }
            if (testerId == 2)
            {
                capSensor = SelectedCAPSensor2;
                capCycle = SelectedCAPCycle2;
            }

            if (capSensor != null && capCycle != null)
            {
                capSensor.ListCAPCycle.Remove(capCycle);
            }
        }

        [RelayCommand]
        private async Task DeleteElement(object parameter)
        {
            int testerId = int.Parse((string)parameter);

            CAPCycle capCycle = new CAPCycle();
            CAPElement capElement = new CAPElement();

            if (testerId == 1)
            {
                capCycle = SelectedCAPCycle1;
                capElement = SelectedCAPElement1;

            }
            if (testerId == 2)
            {
                capCycle = SelectedCAPCycle2;
                capElement = SelectedCAPElement2;
            }

            if (capCycle != null && capElement != null)
            {
                capCycle.ListCAPElement.Remove(capElement);
            }

        }




        [RelayCommand]
        private void ChangeToNewLogDir()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select Log Folder",
                UseDescriptionForTitle = true
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                Model.Settings.LogDir = dialog.SelectedPath;
            }
        }

      

        [RelayCommand]
        private void DeviceConnect(object parameter)
        {
            string deviceName = (string)parameter;


            Model.Devices.ConnectDeviceByName(deviceName);

        }


        [RelayCommand]
        private void DeviceClose(object parameter)
        {
            string deviceName = (string)parameter;


            Model.Devices.CloseDeviceByName(deviceName);

        }


        [RelayCommand]
        private void RefreshComport()
        {
            Model.Devices.RefreshComPort();
        }

        [RelayCommand]
        private async Task ConnectAllDevices()
        {
            await Model.Devices.ConnectAll();
        }

        [RelayCommand]
        private async Task DisconnectAllDevices()
        {
            await Model.Devices.CloseAll();
        }

        // Save: ghi đè lên file model đang mở (tên cũ). Nếu chưa mở file nào thì
        // chuyển sang Save As để người dùng chọn tên.
        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrEmpty(Utility.CurrentModelFilePath) || !File.Exists(Utility.CurrentModelFilePath))
            {
                SaveAs();
                return;
            }

            try
            {
                Utility.SaveModel(Model, Utility.CurrentModelFilePath, Path.GetFileName(Utility.CurrentModelFilePath));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Fatal Error!" + ex.Message);
            }
        }

        // Save As: luôn cho chọn tên file mới, và file đó trở thành file đang mở.
        [RelayCommand]
        private void SaveAs()
        {
            try
            {
                Microsoft.Win32.SaveFileDialog openFile = new Microsoft.Win32.SaveFileDialog();
                openFile.DefaultExt = ".json";
                openFile.Filter = "Model File (*.json)|*.json";

                if (openFile.ShowDialog() == true)
                {
                    try
                    {
                        Utility.SaveModel(Model, openFile.FileName, openFile.SafeFileName);
                        Utility.CurrentModelFilePath = openFile.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                            ? openFile.FileName
                            : openFile.FileName + ".json";
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Fatal Error!" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Fatal Error!" + ex.Message);
            }
        }
    }
}
