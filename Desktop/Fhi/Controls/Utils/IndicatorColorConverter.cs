using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace Fhi.Controls.Utils
{
    [ValueConversion(typeof(Double?), typeof(SolidColorBrush))]
    public class IndicatorColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xd1, 0xd3, 0xd4));
            var scheme = ColorScheme.Indicators.Colors;
            if (value is Int32 indicator && indicator >= 0 && indicator <= 100)
            {
                var scale = scheme.Count / 100.0;
                var index = (Int32)(indicator * scale);
                if (index < 0) index = 0;
                if (index > scheme.Count - 1) index = scheme.Count - 1;
                return new SolidColorBrush(new System.Windows.Media.Color
                {
                    A = scheme[index].A,
                    R = scheme[index].R,
                    G = scheme[index].G,
                    B = scheme[index].B
                });
            }
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0,0,0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
