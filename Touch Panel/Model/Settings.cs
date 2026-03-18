using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Touch_Panel.Model
{
    public partial class Settings : ObservableObject
    {
        [ObservableProperty]
        private string logDir;

        [ObservableProperty]
        private bool shouldStopAllWhenAnyFailedStep = false;


        [ObservableProperty]
        private bool shouldMainResetWhenPassTest = true;

        [ObservableProperty]
        private bool shouldMainResetWhenFailTest = false;

        [ObservableProperty]
        private bool shouldSaveLog = true;

    }
}
