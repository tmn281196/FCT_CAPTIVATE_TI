using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Touch_Panel.Model;

namespace Touch_Panel.View_Model
{
    public partial class DeviceConnectionListViewModel : ObservableObject
    {
        [ObservableProperty]
        private Devices devices;

        public DeviceConnectionListViewModel(Devices device)
        {
            this.Devices = device;
        }

    }
}
