using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using Touch_Panel.Model;

namespace Touch_Panel.View_Model
{
    public partial class ManualPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private Model.Model model;



        [ObservableProperty]
        private string onChannels;



        public ManualPageViewModel(Model.Model sharedModel)
        {
            this.Model = sharedModel;
        }

        partial void OnModelChanged(Model.Model? oldModel, Model.Model newModel)
        {

        }



        [ObservableProperty]
        private Step selectedItem1;

        [ObservableProperty]
        private Step selectedItem2;

        [ObservableProperty]
        private TestLogic testLogic;

        [RelayCommand]
        private void DoubleClick(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            switch (testerId)
            {
                case 1:
                    if (SelectedItem1 != null)
                    {
                        TestLogic.manualSingleTest(SelectedItem1, TestLogic.Tester1);
                    }
                    break;
                case 2:
                    if (SelectedItem2 != null)
                    {
                        TestLogic.manualSingleTest(SelectedItem2, TestLogic.Tester2);
                    }
                    break;
                default:

                    break;
            }

        }



        [RelayCommand]
        private void FullTest(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            switch (testerId)
            {
                case 1:
                    TestLogic.manualFullTest(TestLogic.Tester1);
                    break;
                case 2:
                    TestLogic.manualFullTest(TestLogic.Tester2);
                    break;
                default:

                    break;
            }
        }

        [RelayCommand]
        private void StopTest(object parameter)
        {
            int testerId = int.Parse((string)parameter);

            switch (testerId)
            {
                case 1:
                    testLogic.Tester1.stopTest = true;
                    break;
                case 2:
                    testLogic.Tester2.stopTest = true;
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        private void Reset(object parameter)
        {
            int testerId = int.Parse((string)parameter);

            switch (testerId)
            {
                case 1:
                    _ =  Model.Devices.ResetSolenoid(TestLogic.Tester1);
                    TestLogic.Tester1.ClearSteps();

                    break;
                case 2:
                    _ =Model.Devices.ResetSolenoid(TestLogic.Tester2);
                    TestLogic.Tester2.ClearSteps();

                    break;
                default:
                    break;
            }
        }



        [RelayCommand]
        private void ResetCylinder()
        {
            _ = Model.Devices.ResetMainCylinder();
        }

        [RelayCommand]
        private void ReCalibMICOM(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            _ =  Model.Devices.RecalibMICOM(testerId);
        }


        [RelayCommand]
        private void EnableMICOM(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            _ =  Model.Devices.ResumeMICOM(testerId);
        }


        [RelayCommand]
        private void DisableMICOM(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            _ = Model.Devices.HaltMICOM(testerId);
        }

        [RelayCommand]
        private void ConnectorAllDown()
        {
            Model.Devices.ConnectorAllDown();
        }

        [RelayCommand]
        private void ConnectorAllUp()
        {
            Model.Devices.ConnectorAllUp();
        }


        [RelayCommand]
        private async Task SetAllTunning(object parameter)
        {

            int testerId = int.Parse((string)parameter);
            Tester tester = null;
            if (testerId == 1) tester = TestLogic.Tester1; else tester = TestLogic.Tester2;


            await Model.Devices.SetAllTunningValuesToMicom(tester);

        }
    }
}
