using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Touch_Panel.View_Model;

namespace Touch_Panel.Model
{
    public partial class Model : ObservableObject
    {
 

        [ObservableProperty]
        private TestStep step1 = new TestStep();

        [ObservableProperty]
        private TestStep step2 = new TestStep();

        [ObservableProperty]
        private Settings settings = new Settings();

        [ObservableProperty]
        private Devices devices = new Devices();




    }
}
