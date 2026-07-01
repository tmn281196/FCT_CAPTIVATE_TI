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
        private bool shouldBuzzerWhenFailTest = true;

        [ObservableProperty]
        private bool shouldSaveLog = true;

        [ObservableProperty]
        private bool shouldPrintQr = true;

        [ObservableProperty]
        private string unitCode = "SV";

        [ObservableProperty]
        private string partnerCode = "E17U";

        [ObservableProperty]
        private string countryCode = "TH";

        [ObservableProperty]
        private string lineCode = "L01";

        [ObservableProperty]
        private string equipmentSerial = "T001";

        [ObservableProperty]
        private string partNumber = string.Empty;

        [ObservableProperty]
        private int serialNumber = 0;

    }
}
