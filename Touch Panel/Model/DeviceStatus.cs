using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Touch_Panel.Model
{
    public partial class DeviceStatus : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private bool connected;

        [ObservableProperty]
        private bool  txSent;

        [ObservableProperty]
        private bool rxReceived;

        [ObservableProperty]
        private int rxCount = 0;


    }
}
