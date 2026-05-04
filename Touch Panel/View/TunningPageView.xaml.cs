using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Touch_Panel.ViewModel;

namespace Touch_Panel.View
{

    /// <summary>
    /// Interaction logic for TunningPageView.xaml
    /// </summary>
    public partial class TunningPageView : UserControl
    {
        public TunningPageView(TunningPageViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
