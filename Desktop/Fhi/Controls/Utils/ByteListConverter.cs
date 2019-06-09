using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Fhi.Controls.Utils
{
    [ValueConversion(typeof(IEnumerable<Byte>), typeof(String))]
    public class ByteListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IEnumerable<byte> bytes)) return String.Empty;
            return String.Join(", ", bytes);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is String s) || String.IsNullOrWhiteSpace(s)) return new List<Byte>();
            var bytes = s.Split(',');
            var rv = new List<Byte>();
            foreach (var b in bytes)
            {
                if (!Byte.TryParse(b, out var bv)) continue;
                rv.Add(bv);
            }
            return rv;
        }
    }
}
