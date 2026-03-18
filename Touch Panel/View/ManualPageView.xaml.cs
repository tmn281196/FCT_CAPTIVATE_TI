using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
using Touch_Panel.View_Model;

namespace Touch_Panel.View
{
    public class CountToOrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
                return count > 2 ? Orientation.Horizontal : Orientation.Vertical;

            if (value is ICollection collection)
                return collection.Count > 2 ? Orientation.Horizontal : Orientation.Vertical;

            return Orientation.Vertical;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    /// <summary>
    /// Interaction logic for ManualPageView.xaml
    /// </summary>
    public partial class ManualPageView : UserControl
    {

        public ManualPageView(ManualPageViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
