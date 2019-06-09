using System;
using System.Globalization;
using System.Windows.Data;

namespace Fhi.Controls.Utils
{
    [ValueConversion(typeof(double), typeof(double))]
    public class FractionToPercentConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (!(value is double)) return -1.0;
            return 100.0 * (double) value;
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (!(value is double)) return -1.0;
            return (double) value / 100.0;
        }
    }
}