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

namespace Touch_Panel.View
{
    /// <summary>
    /// Interaction logic for DeviceConnectionView.xaml
    /// </summary>
    public partial class DeviceConnectionView : UserControl
    {
        public DeviceConnectionView()
        {
            InitializeComponent();
        }
    }

    /// <summary>Chuyển chuỗi sang IN HOA để hiển thị.</summary>
    public class UpperCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value?.ToString()?.ToUpperInvariant();

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value;
    }

    /// <summary>bool -> Visibility. ConverterParameter="inv" để đảo.</summary>
    public class BoolToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = value is bool v && v;
            if ((parameter as string) == "inv") b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }
}
