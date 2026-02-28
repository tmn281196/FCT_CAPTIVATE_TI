using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;
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
                        testLogic.manualSingleTest(selectedItem1, testLogic.Tester1);
                    }
                    break;
                case 2:
                    if (SelectedItem2 != null)
                    {
                        testLogic.manualSingleTest(selectedItem2, testLogic.Tester2);
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
                    testLogic.manualFullTest(testLogic.Tester1);
                    break;
                case 2:
                    testLogic.manualFullTest(testLogic.Tester2);
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
                    Model.Devices.ResetSolenoid1();
                    testLogic.Tester1.ClearSteps();

                    break;
                case 2:
                    Model.Devices.ResetSolenoid2();
                    testLogic.Tester2.ClearSteps();

                    break;
                default:
                    break;
            }
        }


        [RelayCommand]
        private void ResetTest()
        {
            testLogic.manualResetTest();
        }

        [RelayCommand]
        private void ResetCylinder()
        {
            Model.Devices.ResetMainCylinder();
        }

        [RelayCommand]
        private void ResetMICOM(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            Model.Devices.RecalibMICOM(testerId);
        }


        [RelayCommand]
        private void EnableMICOM(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            Model.Devices.ResumeMICOM(testerId);
        }


        [RelayCommand]
        private void DisableMICOM(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            Model.Devices.HaltMICOM(testerId);
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

       

    }
}
