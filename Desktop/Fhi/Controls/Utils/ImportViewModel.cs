using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;

namespace Fhi.Controls.Utils
{
    public class ImportViewModel : ViewModelBase
    {
        private ImportViewModel(Action timeseries, Action shapefile, string text)
        {
            ImportShapefileCommand = new RelayCommand(shapefile);
            ImportTimeseriesCommand = new RelayCommand(timeseries);
            Text = text;
        }

        public ICommand ImportTimeseriesCommand { get; }
        public ICommand ImportShapefileCommand { get; }
        public String Text { get; }

        public static void Dialog(Action timeseries, Action shapefile, string text)
        {
            var vm = new ImportViewModel(timeseries, shapefile, text);
            var dialog = new ImportWindow {Owner = Application.Current.MainWindow, DataContext = vm};
            dialog.ShowDialog();
        }
    }
}
