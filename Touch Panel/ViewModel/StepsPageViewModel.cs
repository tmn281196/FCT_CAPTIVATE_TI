using BSL430_NET;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Touch_Panel.Model;
using Touch_Panel.ViewModel;

namespace Touch_Panel.View_Model
{
    public partial class StepsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private Model.Model model;

        [ObservableProperty]
        private AutoState autoState;


        [ObservableProperty]
        private List<string> testList = new List<string>()
        {
            "NOP",
            "CALIB",
            "KEY",
            "MEAS"
        };



        [ObservableProperty]
        private Step stepSelItem1;

        [ObservableProperty]
        private Step stepSelItem2;

        [ObservableProperty]
        private int stepSelIdx1;

        [ObservableProperty]
        private int stepSelIdx2;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddStepCommand))]
        private string testSelItem;

        [ObservableProperty]
        private string objectIdInput;


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddStepCommand))]
        private string specValueInput;


        [ObservableProperty]
        private int timeDelayInput;

        [ObservableProperty]
        private int timeTestInput;


        [RelayCommand]
        private async Task VerifyFirmware(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            var timeout = TimeSpan.FromSeconds(5);
            MicomContext micomCtx1 = new MicomContext() { MICOMData = Model.Devices.MicomData1, LockObject = Model.Devices.DeviceManager.PortLockMicom1, SerialPort = Model.Devices.DeviceManager.MicomPort1 };
            MicomContext micomCtx2 = new MicomContext() { MICOMData = Model.Devices.MicomData2, LockObject = Model.Devices.DeviceManager.PortLockMicom2, SerialPort = Model.Devices.DeviceManager.MicomPort2 };
            MicomContext micomCtx = (testerId == 1) ? micomCtx1 : micomCtx2;


            if (!micomCtx.SerialPort.IsOpen)
            {
                micomCtx.MICOMData.FirmwareLog = "✕";
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

            micomCtx.MICOMData.FirmwareLog = micomCtx.MICOMData.MatchFirmware? "✓" : "✕";

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


        [RelayCommand]
        private async Task WriteFirmware(object parameter)
        {
            if (AutoState.Test == TestState.Testing)
            {
                MessageBox.Show("Cannot modify steps while testing is in progress.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int testerId = int.Parse((string)parameter);
            string deviceName = $"Micom{testerId}";
            string portName = testerId == 1 ? Model.Devices.MicomCom1Port : Model.Devices.MicomCom2Port;
            var mData = testerId == 1 ? Model.Devices.MicomData1 : Model.Devices.MicomData2;
            string firmwarePath = $"firmware_bsl_{testerId}\\{Model.Devices.SelectedFirmwareMicom}.txt";

            Model.Devices.CloseDeviceByName(deviceName);

            try
            {
                mData.SignalIntegrity = string.Empty;

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

            Model.Devices.ConnectDeviceByName(deviceName);

            mData.FirmwareLog = "✓";
        }


        [RelayCommand]
        private void SkipAllSteps(object parameter)
        {
            if (AutoState.Test == TestState.Testing)
            {
                MessageBox.Show("Cannot modify steps while testing is in progress.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int testerId = int.Parse((string)parameter);
            switch (testerId)
            {
                case 1:
                    foreach (var item in Model.Micom1TestStep.Steps)
                    {
                        item.NoSkip = false;
                    }
                    break;
                case 2:
                    foreach (var item in Model.Micom2TestStep.Steps)
                    {
                        item.NoSkip = false;
                    }
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]

        private void UnSkipAllSteps(object parameter)
        {
            if (AutoState.Test == TestState.Testing)
            {
                MessageBox.Show("Cannot modify steps while testing is in progress.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int testerId = int.Parse((string)parameter);
            switch (testerId)
            {
                case 1:
                    foreach (var item in Model.Micom1TestStep.Steps)
                    {
                        item.NoSkip = true;
                    }


                    break;
                case 2:
                    foreach (var item in Model.Micom2TestStep.Steps)
                    {
                        item.NoSkip = true;
                    }

                    break;
                default:

                    break;
            }

        }




        [RelayCommand(CanExecute = nameof(CanAddStep))]
        private void AddStep(object parameter)
        {
            if (AutoState.Test == TestState.Testing)
            {
                MessageBox.Show("Cannot modify steps while testing is in progress.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int testerId = int.Parse((string)parameter);
            switch (testerId)
            {
                case 1:
                    if (StepSelIdx1 == Model.Micom1TestStep.Steps.Count - 1)
                    {
                        Model.Micom1TestStep.AddStep(TestSelItem.ToString(), ObjectIdInput, SpecValueInput, -1, TimeDelayInput, TimeTestInput);
                    }
                    else
                    {
                        Model.Micom1TestStep.AddStep(TestSelItem.ToString(), ObjectIdInput, SpecValueInput, StepSelIdx1, TimeDelayInput, TimeTestInput);
                    }

                    break;
                case 2:
                    if (StepSelIdx2 == Model.Micom2TestStep.Steps.Count - 1)
                    {
                        Model.Micom2TestStep.AddStep(TestSelItem.ToString(), ObjectIdInput, SpecValueInput, -1, TimeDelayInput, TimeTestInput);
                    }
                    else
                    {
                        Model.Micom2TestStep.AddStep(TestSelItem.ToString(), ObjectIdInput, SpecValueInput, StepSelIdx2, TimeDelayInput, TimeTestInput);
                    }
                    break;
                default:

                    break;
            }


            switch (testerId)
            {
                case 1:
                    StepSelIdx1 = -1;
                    break;

                case 2:
                    StepSelIdx2 = -1;
                    break;
                default:

                    break;
            }
        }



        private bool CanAddStep()
        {
            if (TestSelItem == "MEAS")
            {
                return TestSelItem != "" && TestSelItem != null && SpecValueInput != "" && SpecValueInput != null && ObjectIdInput != "" && ObjectIdInput != null;
            }
            else if (TestSelItem == "NOP" || TestSelItem == "CALIB")
            {
                return true;
            }
            else
            {
                return TestSelItem != "" && TestSelItem != null;
            }
        }



        [RelayCommand]
        private void DeleteStep(object parameter)
        {
            if (AutoState.Test == TestState.Testing)
            {
                MessageBox.Show("Cannot modify steps while testing is in progress.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int testerId = int.Parse((string)parameter);
            switch (testerId)
            {
                case 1:
                    Model.Micom1TestStep.DeleteStep(StepSelItem1);
                    break;
                case 2:
                    Model.Micom2TestStep.DeleteStep(StepSelItem2);

                    break;
                default:

                    break;
            }

        }

       

        [RelayCommand]
        private void SaveModel()
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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Fatal Error!" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error!" + ex.Message);
            }
        }

        public StepsPageViewModel(Model.Model sharedModel)
        {
            this.Model = sharedModel;
        }
    }
}
