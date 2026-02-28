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

namespace Touch_Panel.View_Model
{
    public static class FirmwareMICOM
    {
        public static List<string> FirmwareList = new()
        {
            "HUMAN_THAI__ME21K7010DS__241223_0199"
        };



        public static void WriteFirmware(string portName, string firmwareFilePath, Bsl430NetEventHandler progressChangedEventHandler, EventHandler<string> progressFailedEventHandler)
        {
            try
            {
                using (var dev = new BSL430NET(portName))
                {
                    try
                    {
                        dev.ProgressChanged += new Bsl430NetEventHandler(progressChangedEventHandler);

                        Status status_baud = dev.SetBaudRate(BaudRate.BAUD_9600);
                        Status status_mcu = dev.SetMCU(MCU.MSP430_FR2x33);
                        Status status_invoke = dev.SetInvokeMechanism(InvokeMechanism.SHARED_JTAG);                     


                        if (File.Exists(firmwareFilePath)) {
                            StatusEx result = dev.Upload($"{firmwareFilePath}");
                        }
                        else
                        {
                            throw new FileNotFoundException("Firmware file does not exist.");
                        }

                    }
                    catch (Exception ex)
                    {
                        progressFailedEventHandler?.Invoke(portName, ex.Message);
                        return;
                    }



                }

            }
            catch (Exception ex)
            {
                progressFailedEventHandler?.Invoke(null, ex.Message); ;


                return;
            }

        }

    }

    public partial class NewModelPageViewModel : ObservableObject
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
            "MEAS",

        };

        [ObservableProperty]
        private List<string> firmwareList = FirmwareMICOM.FirmwareList;

  


        [ObservableProperty]
        private string firmwareLog;
       



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
                    if (StepSelIdx1 == Model.Step1.Steps.Count - 1)
                    {
                        Model.Step1.AddStep(TestSelItem.ToString(), ObjectIdInput, SpecValueInput, -1, TimeDelayInput, TimeTestInput);
                    }
                    else
                    {
                        Model.Step1.AddStep(TestSelItem.ToString(), ObjectIdInput, SpecValueInput, StepSelIdx1, TimeDelayInput, TimeTestInput);
                    }

                    break;
                case 2:
                    if (StepSelIdx2 == Model.Step2.Steps.Count - 1)
                    {
                        Model.Step2.AddStep(TestSelItem.ToString(), ObjectIdInput, SpecValueInput, -1, TimeDelayInput, TimeTestInput);
                    }
                    else
                    {
                        Model.Step2.AddStep(TestSelItem.ToString(), ObjectIdInput, SpecValueInput, StepSelIdx2, TimeDelayInput, TimeTestInput);
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
                    Model.Step1.DeleteStep(StepSelItem1);


                    break;
                case 2:
                    Model.Step2.DeleteStep(StepSelItem2);

                    break;
                default:

                    break;
            }

        }

        object firmwareLoglock = new object();

        private void ProgressFirmwareChanged(object source, Bsl430NetEventArgs args)
        {
            var bslSource = (BSL430NET)source;
            var com = bslSource.DefaultDevice.Name;

            string log = $"Writing {args.Progress.ToString("0.00")}%";

            if (Model.Devices.MicomCom1Port == com)
            {
                Model.Devices.Micom1FirmwareLog = log;

            }
            if (Model.Devices.MicomCom2Port == com)
            {
                Model.Devices.Micom2FirmwareLog = log;

            }


        }


        private void ProgressFirmwareFailed(object? sender, string message)
        {
            string portName = sender as string;

            string log = $"Error: {message}";

            if(portName == Model.Devices.MicomCom1Port)
            {
                Model.Devices.Micom1FirmwareLog = log;

            }
            if (portName == Model.Devices.MicomCom2Port)
            {
                Model.Devices.Micom2FirmwareLog = log;

            }
        }





        [RelayCommand]
        private async Task VerifyFirmware(object parameter)
        {

            int testerId = int.Parse((string)parameter);
            var timeout = TimeSpan.FromSeconds(5);


            if (testerId == 1)
            {
                if (!Model.Devices.DeviceManager.MicomPort1.IsOpen) return;

                await Model.Devices.VerifyMICOM(testerId);

                var sw = Stopwatch.StartNew();
                while (string.IsNullOrEmpty((string)Model.Devices.FirmwareMicom.Micom1))
                {
                    if (sw.Elapsed > timeout)
                        break;
                    Thread.Sleep(10);
                }

                FirmwareLog = Model.Devices.FirmwareMicom.Micom1 == Model.Devices.SelectedFirmwareMicom ? "MICOM1 | Valid" : "MICOM1 | Invalid";
            }
            if (testerId == 2)
            {
                if (!Model.Devices.DeviceManager.MicomPort2.IsOpen) return;

                await Model.Devices.VerifyMICOM(testerId);
                var sw = Stopwatch.StartNew();
                while (string.IsNullOrEmpty((string)Model.Devices.FirmwareMicom.Micom2))
                {
                    if (sw.Elapsed > timeout)
                        break;
                    Thread.Sleep(10);
                }

                FirmwareLog = Model.Devices.FirmwareMicom.Micom2 == Model.Devices.SelectedFirmwareMicom ? "MICOM2 | Valid" : "MICOM2 | Invalid";
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
            switch (testerId)
            {
                case 1:
                    Model.Devices.CloseDeviceByName("Micom1");


                    try
                    {

                        FirmwareLog = string.Empty;

                        await Task.Run(() =>
                        {
                            FirmwareMICOM.WriteFirmware(Model.Devices.MicomCom1Port, $"firmware_bsl_1\\{Model.Devices.SelectedFirmwareMicom}.txt" , ProgressFirmwareChanged, ProgressFirmwareFailed);
                        });



                    }
                    catch (Exception ex)
                    {
                        FirmwareLog = ex.Message;
                    }

                    Model.Devices.ConnectDeviceByName("Micom1");

                    Model.Devices.Micom1FirmwareLog = "OK";


                    break;
                case 2:


                    Model.Devices.CloseDeviceByName("Micom2");

                    try
                    {
                        FirmwareLog = string.Empty;

                        await Task.Run(() =>
                        {
                            Model.Devices.DeviceManager.MicomPort2.PortName = Model.Devices.MicomCom2Port;
                            FirmwareMICOM.WriteFirmware(Model.Devices.MicomCom2Port, $"firmware_bsl_2\\{Model.Devices.SelectedFirmwareMicom}.txt", ProgressFirmwareChanged, ProgressFirmwareFailed);
                        });
                    }
                    catch (Exception ex)
                    {
                        FirmwareLog = ex.Message;
                    }

                    Model.Devices.ConnectDeviceByName("Micom2");

                    Model.Devices.Micom2FirmwareLog = "OK";

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


        public NewModelPageViewModel(Model.Model sharedModel)
        {
            this.Model = sharedModel;
        }






    }
}
