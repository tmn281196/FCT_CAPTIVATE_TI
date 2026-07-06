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
    /// <summary>Tab đang chọn -> id tester ("1"/"2"). SelectedIndex 0 -> "1", 1 -> "2".</summary>
    public class IndexToTesterConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int idx = value is int i ? i : 0;
            return (idx + 1).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }

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
