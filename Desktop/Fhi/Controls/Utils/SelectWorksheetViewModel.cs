using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Fhi.Controls.MVVM;
using OfficeOpenXml;

namespace Fhi.Controls.Utils
{
    public class SelectWorksheetViewModel : ViewModelBase
    {
        public String SelectedWorksheet { get; set; }
        public List<String> Worksheets { get; } = new List<string>();

        public static ExcelWorksheet Dialog(ExcelWorksheets worksheets)
        {
            var vm = new SelectWorksheetViewModel();
            vm.Worksheets.AddRange(worksheets.Select(x => x.Name));
            vm.SelectedWorksheet = vm.Worksheets[0];
            var dialog = new SelectWorksheetWindow { Owner = Application.Current.MainWindow, DataContext = vm };
            if (dialog.ShowDialog() != true) return null;
            var index = vm.Worksheets.IndexOf(vm.SelectedWorksheet);
            return worksheets[index + 1]; // uses '1' based worksheet numbering for indexer.
        }
    }
}