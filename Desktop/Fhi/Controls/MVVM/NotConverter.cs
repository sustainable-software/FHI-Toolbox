using System;
using System.Windows.Data;

namespace Fhi.Controls.MVVM
{
    public class NotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return false;
            return !(Boolean)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return !(Boolean)value;
        }
    }
}