using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Touch_Panel.Model;
using Touch_Panel.View_Model;

namespace Touch_Panel.ViewModel
{
    public partial class TunningPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private Model.Model model;

        [ObservableProperty]

        private MainViewModel mainViewModel;

        partial void OnModelChanged(Model.Model value)
        {
            ReloadData();
        }

        private void ReloadData()
        {
            AllCAPElementsMicom1 = new ObservableCollection<CAPElement>();
            AllCAPElementsMicom2 = new ObservableCollection<CAPElement>();

            foreach (var item in Model.Devices.MicomData1.ListCAPSensor
                         .SelectMany(x => x.ListCAPCycle)
                         .SelectMany(x => x.ListCAPElement))
            {
                AllCAPElementsMicom1.Add(item);
            }

            foreach (var item in Model.Devices.MicomData2.ListCAPSensor
                         .SelectMany(x => x.ListCAPCycle)
                         .SelectMany(x => x.ListCAPElement))
            {
                AllCAPElementsMicom2.Add(item);
            }
        }

        public TunningPageViewModel(Model.Model sharedModel)
        {
            this.Model = sharedModel;

           
        }


        [ObservableProperty]
        private TestLogic testLogic;

        [ObservableProperty]
        private string selectedElementId1 = "0.0.0";

        [ObservableProperty]
        private string selectedElementId2 = "0.0.0";

        [ObservableProperty]
        private UInt16 selectedTunningValue2 = 0;

        [ObservableProperty]
        private UInt16 selectedTunningValue1 = 0;


        [ObservableProperty]
        private ObservableCollection<CAPElement> allCAPElementsMicom1;

        [ObservableProperty]
        private ObservableCollection<CAPElement> allCAPElementsMicom2;


        [RelayCommand]
        private async Task ResetTunningList(object parameter)
        {
            MICOMData micomData = null;
            int testerId = int.Parse((string)parameter);

            if (testerId == 1) micomData = Model.Devices.MicomData1; else micomData = Model.Devices.MicomData2;

            var elements = micomData.ListCAPSensor
              .SelectMany(s => s.ListCAPCycle)
              .SelectMany(c => c.ListCAPElement)
              .ToList();



            // Header
            foreach (var e in elements)
            {
                e.ListTunningParameter = new ObservableCollection<TunningParameter>();

            }


        }

        [RelayCommand]
        private async Task AddToTunningList(object parameter)
        {
            MICOMData micomData = null;
            int testerId = int.Parse((string)parameter);

            if (testerId == 1) micomData = Model.Devices.MicomData1; else micomData = Model.Devices.MicomData2;

            var elements = micomData.ListCAPSensor
              .SelectMany(s => s.ListCAPCycle)
              .SelectMany(c => c.ListCAPElement)
              .ToList();


            foreach (var e in elements)
            {
                e.ListTunningParameter.Add(new TunningParameter() { OffsetTap = e.OffsetTap, CoarseGainRatio = e.CoarseGainRatio, FineGainRatio = e.FineGainRatio, Lta = e.Lta });

            }


        }

        [RelayCommand]
        private async Task RemoveTheLastFromTunningList(object parameter)
        {
            try
            {
                MICOMData micomData = null;
                int testerId = int.Parse((string)parameter);

                if (testerId == 1) micomData = Model.Devices.MicomData1; else micomData = Model.Devices.MicomData2;

                var elements = micomData.ListCAPSensor
                  .SelectMany(s => s.ListCAPCycle)
                  .SelectMany(c => c.ListCAPElement)
                  .ToList();


                foreach (var e in elements)
                {
                    e.ListTunningParameter.RemoveAt(e.ListTunningParameter.Count - 1);

                }
            }
            catch
            {

            }


        }

        [RelayCommand]
        private async Task CalculateTunning(object parameter)
        {
            MICOMData micomData = null;
            int testerId = int.Parse((string)parameter);

            if (testerId == 1) micomData = Model.Devices.MicomData1; else micomData = Model.Devices.MicomData2;

            var elements = micomData.ListCAPSensor
                .SelectMany(s => s.ListCAPCycle)
                .SelectMany(c => c.ListCAPElement)
                .ToList();

            foreach (var e in elements)
            {

                e.CalculatedCoarseGainRatio = (ushort)CalcMedian(e.ListTunningParameter.Select(v => (double)v.CoarseGainRatio).ToList());
                e.CalculatedOffsetTap = (ushort)CalcMedian(e.ListTunningParameter.Select(v => (double)v.OffsetTap).ToList());
                e.CalculatedFineGainRatio = (ushort)CalcMedian(e.ListTunningParameter.Select(v => (double)v.FineGainRatio).ToList());
                e.CalculatedLta = (ushort)CalcMedian(e.ListTunningParameter.Select(v => (double)v.Lta).ToList());
            }
        }


        [RelayCommand]
        private async Task SetBackCalculatedTunningToCache(object parameter)
        {
            MICOMData micomData = null;
            int testerId = int.Parse((string)parameter);

            if (testerId == 1) micomData = Model.Devices.MicomData1; else micomData = Model.Devices.MicomData2;

            var elements = micomData.ListCAPSensor
                .SelectMany(s => s.ListCAPCycle)
                .SelectMany(c => c.ListCAPElement)
                .ToList();

            foreach (var e in elements)
            {
                e.CoarseGainRatio = e.CalculatedCoarseGainRatio;
                e.FineGainRatio = e.CalculatedFineGainRatio;
                e.OffsetTap = e.CalculatedOffsetTap;
                e.Lta = e.CalculatedLta;

            }
        }

        private static double CalcMedian(List<double> values)
        {
            if (values.Count == 0) return 0;

            var sorted = values.OrderBy(v => v).ToList();
            int mid = sorted.Count / 2;

            return sorted.Count % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];
        }
        private static double CalcMean(List<double> values)
        {
            if (values.Count == 0) return 0;
            return values.Sum() / values.Count;
        }

        [RelayCommand]
        private async Task SetAllTunning(object parameter)
        {

            int testerId = int.Parse((string)parameter);
            Tester tester = null;
            if (testerId == 1) tester = TestLogic.Tester1; else tester = TestLogic.Tester2;


            await Model.Devices.SetAllTunningValuesToMicom(tester);

        }

        [RelayCommand]
        private async Task GetAllTunning(object parameter)
        {
            int testerId = int.Parse((string)parameter);
            Tester tester = null;
            if (testerId == 1) tester = TestLogic.Tester1; else tester = TestLogic.Tester2;


            await Model.Devices.GetAllTunningValuesFromMicom(tester);
        }





    


        [RelayCommand]
        private void Reset(object parameter)
        {
            int testerId = int.Parse((string)parameter);

            switch (testerId)
            {
                case 1:
                    Model.Devices.ResetSolenoid(testLogic.Tester1);
                    testLogic.Tester1.ClearSteps();

                    break;
                case 2:
                    Model.Devices.ResetSolenoid(testLogic.Tester2);
                    testLogic.Tester2.ClearSteps();

                    break;
                default:
                    break;
            }
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

        // Save: ghi tuning vào MicomDatabase.json (database dùng chung theo firmware).
        [RelayCommand]
        private void SaveModel()
        {
            try
            {
                MainViewModel.SaveMicomDatabase();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error!" + ex.Message);
            }
        }



    }
}
