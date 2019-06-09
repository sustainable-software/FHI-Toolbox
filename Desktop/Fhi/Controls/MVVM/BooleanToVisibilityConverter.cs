using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Fhi.Controls.MVVM
{
    [ValueConversion(typeof(String), typeof(Visibility))]
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        enum Parameters
        {
            Normal, Inverted
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool boolValue = false;
                if (value != null)
                {
                    if (value is bool)
                        boolValue = (bool) value;
                    if (value is decimal)
                        boolValue = (decimal) value > 0;
                    if (value is int)
                        boolValue = (int) value > 0;
                    if (value is string)
                        boolValue = !String.IsNullOrEmpty((string) value);
                    if (value is Int32?)
                        boolValue = true;
                }
                var direction = Parameters.Normal;
                if (parameter != null)
                    direction = (Parameters) Enum.Parse(typeof (Parameters), (string) parameter);

                if (direction == Parameters.Inverted)
                    return !boolValue ? Visibility.Visible : Visibility.Collapsed;
                return boolValue ? Visibility.Visible : Visibility.Collapsed;


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