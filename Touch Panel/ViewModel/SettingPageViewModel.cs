using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HidSharp;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Touch_Panel.Model;


namespace Touch_Panel.View_Model
{
    public partial class SettingPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private Model.Model model;

        public SettingPageViewModel(Model.Model sharedModel)
        {
            this.Model = sharedModel;
            Model.Devices.RefreshComPort();
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
