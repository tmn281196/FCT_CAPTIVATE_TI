using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Touch_Panel.Converters
{
    public class MultiValueJoinConverter : IMultiValueConverter
    {
        public string Separator { get; set; } = "|";

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Join(Separator, values.Select(v => v?.ToString() ?? string.Empty));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
