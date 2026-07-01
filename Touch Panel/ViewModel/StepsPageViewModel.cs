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
            "LOAD",
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
        private void SkipAllSteps(object parameter)
        {
            if (AutoState?.IsBusy == true)
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
            if (AutoState?.IsBusy == true)
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
            if (AutoState?.IsBusy == true)
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
            else if (TestSelItem == "NOP" || TestSelItem == "CALIB" || TestSelItem == "LOAD")
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
            if (AutoState?.IsBusy == true)
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

       

        // Save: ghi đè lên file model đang mở (tên cũ). Nếu chưa mở file nào thì
        // chuyển sang Save As để người dùng chọn tên.
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
                Utility.SaveModel(Model, Utility.CurrentModelFilePath, Path.GetFileName(Utility.CurrentModelFilePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error!" + ex.Message);
            }
        }

        // Save As: luôn cho chọn tên file mới, và file đó trở thành file đang mở.
        [RelayCommand]
        private void SaveModelAs()
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
                        MessageBox.Show("Fatal Error!" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error!" + ex.Message);
            }
        }

        [RelayCommand]
        private void DuplicateStep(object parameter)
        {
            if (AutoState?.IsBusy == true)
            {
                MessageBox.Show("Cannot modify steps while testing is in progress.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int testerId = int.Parse((string)parameter);
            switch (testerId)
            {
                case 1:
                    Model.Micom1TestStep.DuplicateStep(StepSelItem1);
                    break;
                case 2:
                    Model.Micom2TestStep.DuplicateStep(StepSelItem2);
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        private void MoveStepUp(object parameter)
        {
            if (AutoState?.IsBusy == true)
            {
                MessageBox.Show("Cannot modify steps while testing is in progress.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int testerId = int.Parse((string)parameter);
            switch (testerId)
            {
                case 1:
                    Model.Micom1TestStep.MoveStepUp(StepSelItem1);
                    break;
                case 2:
                    Model.Micom2TestStep.MoveStepUp(StepSelItem2);
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        private void MoveStepDown(object parameter)
        {
            if (AutoState?.IsBusy == true)
            {
                MessageBox.Show("Cannot modify steps while testing is in progress.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int testerId = int.Parse((string)parameter);
            switch (testerId)
            {
                case 1:
                    Model.Micom1TestStep.MoveStepDown(StepSelItem1);
                    break;
                case 2:
                    Model.Micom2TestStep.MoveStepDown(StepSelItem2);
                    break;
                default:
                    break;
            }
        }

        public StepsPageViewModel(Model.Model sharedModel)
        {
            this.Model = sharedModel;
        }
    }
}
