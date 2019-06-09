using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Fhi.Controls.Utils
{
    [ValueConversion(typeof(Int32?), typeof(String))]
    public class IndicatorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "--";
            if (value is Int32 indicator && indicator >= 0 && indicator <= 100)
            {
                return indicator.ToString("N0");
            }

            return "??";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
