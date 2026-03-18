using BSL430_NET;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HidSharp;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Touch_Panel.Model;


namespace Touch_Panel.View_Model
{
    public partial class SettingPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private Model.Model model;

        [ObservableProperty]
        private AutoState autoState;

   

        public SettingPageViewModel(Model.Model sharedModel)
        {
            this.Model = sharedModel;
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
        private void RefreshComport()
        {
            Model.Devices.RefreshComPort();
        }

        [RelayCommand]
        private void Save()
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
