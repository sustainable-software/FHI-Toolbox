using FhiModel.Common;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Fhi.Controls.Utils
{
    [ValueConversion(typeof(String), typeof(Visibility))]
    public class UncertaintyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (!(value is Uncertainty e)) return Visibility.Visible;
                return e == Uncertainty.Undefined ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                Trace.TraceError("BooleanToVisibility: " + ex.Message + " value: " + value);
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
