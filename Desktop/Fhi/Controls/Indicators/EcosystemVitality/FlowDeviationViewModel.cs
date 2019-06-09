using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Network;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemServices;
using FhiModel.EcosystemVitality.FlowDeviation;
using FhiModel.EcosystemVitality.WaterQuality;
using Microsoft.Win32;
using OfficeOpenXml;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class FlowDeviationViewModel : NavigationViewModel
    {
        private BasinMapViewModel _basinMapViewModel;
        
        public FlowDeviationViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            EditorCommand = new RelayCommand(Editor);
            RemoveCommand = new RelayCommand(Remove);
            ImportCommand = new RelayCommand(Import);
            ExportCommand = new RelayCommand(Export);

            FlowDeviation.Stations.CollectionChanged += async (sender, args) => await BasinMapViewModel.Refresh(); 
        }
        
        public BasinMapViewModel BasinMapViewModel => _basinMapViewModel ?? (_basinMapViewModel = new BasinMapViewModel(new List<BasinMapLayer>
        {
            //new BasinMapReaches(),
            new BasinMapStations()
        }));
        
        public FlowDeviationIndicator FlowDeviation =>
            Model?.EcosystemVitality?.FetchIndicator<FlowDeviationIndicator>();
        
        public ICommand EditorCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        private void Remove(Object o)
        {
            if (!(o is Station station)) return;
            var answer = MessageBox.Show($"Are you sure you want to remove the {station.Name} station from the assessment?", 
                "Remove Station?", 
                MessageBoxButton.YesNo);
            if (answer != MessageBoxResult.Yes) return;

            FlowDeviation.Stations.Remove(station);
        }

        private void Editor(Object o)
        {
            var create = false;
            if (!(o is Station station))
            {
                station = new Station();
                create = true;
            }
            var vm = new StationEditorViewModel(station);
            var dialog = new StationEditorWindow { DataContext = vm, Owner = Application.Current?.MainWindow };
            if (dialog.ShowDialog() != true) return;
            if (!create)
                FlowDeviation.Stations.Remove(station);
            FlowDeviation.Stations.Add(vm.Station);
        }

        private void Import()
        {
            ImportViewModel.Dialog(ImportFromExcel, async () => await ImportFromShapefile(), 
                "Import flow station data from either source. Importing from a shapefile will first look to see if the location name in the shapefile matches existing stations before creating a new one.");
        }

        private async Task ImportFromShapefile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open shapefile with station locations",
                Filter = "Shapefile (*.shp)|*.shp",
                DefaultExt = ".shp"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;
            var sf = await ShapefileFeatureTable.OpenAsync(filename);
            var qp = new QueryParameters();

            var res = await sf.QueryFeaturesAsync(qp);

            foreach (var r in res)
            {
                Trace.WriteLine($"{r.Geometry}");
                if (!(r.Geometry is MapPoint point))
                {
                    if (!(r.Geometry is Polygon polygon)) continue;
                    point = polygon.Extent.GetCenter();
                }
                var name = r.Attributes.FirstOrDefault(x => x.Key.ToLowerInvariant() == "name").Value as String;

                var success = false;

                foreach (var station in FlowDeviation.Stations)
                {
                    if (station.Name.ToLowerInvariant() != name?.ToLowerInvariant()) continue;
                    station.Location.Latitude = point.Y;
                    station.Location.Longitude = point.X;
                    station.Location.Wkid = point.SpatialReference.Wkid;
                    success = true;
                }

                if (success) continue;

                var wkid = point.SpatialReference.Wkid;
                if (wkid == 0) wkid = 4326;    // WGS84

                var ns = new Station
                {
                    Name = name,
                    Location = {Latitude = point.Y, Longitude = point.X, Wkid = wkid}
                };
                FlowDeviation.Stations.Add(ns);
                if (String.IsNullOrWhiteSpace(ns.Name))
                    ns.Name = $"STATION [{FlowDeviation.Stations.IndexOf(ns) + 1}]";
            }
        }

        #region Excel File
        private void ImportFromExcel()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Flow Deviation Excel File",
                Filter = "Microsoft Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;
            try
            {
                using (var excel = new ExcelPackage(new FileInfo(filename)))
                {
                    if (excel.Workbook.Worksheets.Count == 0)
                    {
                        MessageBox.Show("Import error: no worksheets.");
                        return;
                    }

                    var dvnf = FlowDeviation;

                    foreach (var worksheet in excel.Workbook.Worksheets)
                    {
                        var station = dvnf.Stations.FirstOrDefault(x => x.Name == worksheet.Name);
                        if (station == null)
                        {
                            station = new Station {Name = worksheet.Name};
                            dvnf.Stations.Add(station);
                        }

                        var units = worksheet.Cells[1, 1].Text;
                        station.Units = units;

                        const int baseRow = 2;
                        var dates = new List<DateTime?>();
                        for (var row = baseRow; row <= worksheet.Dimension.End.Row; row++)
                        {
                            switch (worksheet.Cells[row, 1].Value)
                            {
                                case null:
                                    continue;
                                case DateTime time:
                                    dates.Add(time);
                                    break;
                                case Double d:
                                    dates.Add(DateTime.FromOADate(d));
                                    break;
                                default:
                                    dates.Add(null);
                                    break;
                            }
                        }

                        for (var column = 2; column <= worksheet.Dimension.End.Column; column++)
                        {
                            var data = worksheet.Cells[1, column].Text.Contains("Unreg")
                                ? station.Unregulated
                                : station.Regulated;
                            data.Clear();
                            for (var row = baseRow; row <= worksheet.Dimension.End.Row; row++)
                            {
                                var date = dates[row - baseRow];
                                if (date == null) continue;
                                var value = worksheet.Cells[row, column].Value as Double?;
                                if (value == null) continue;
                                data.Add(new TimeseriesDatum {Value = value.Value, Time = date.Value});
                            }
                        }
                    }
                }

                MessageBox.Show($"Imported Flow Deviation from {filename}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while importing: {ex.Message}");
            }
        }

        private void Export()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Flow Deviation Excel File",
                Filter = "Microsoft Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;
            try
            {
                using (var excel = new ExcelPackage())
                {
                    var dvnf = FlowDeviation;
                    foreach (var station in dvnf.Stations)
                    {
                        if (String.IsNullOrWhiteSpace(station.Name))
                            station.Name = $"Station {dvnf.Stations.IndexOf(station) + 1}";
                        excel.Workbook.Worksheets.Add(station.Name);
                        var worksheet = excel.Workbook.Worksheets[station.Name];

                        worksheet.Cells[1, 1].Value = station.Units;

                        const int baseRow = 2;
                        var row = baseRow;

                        var column = 1;
                        var dates = ZipDates(station);
                        foreach (var date in dates)
                        {
                            worksheet.Cells[row, column].Style.Numberformat.Format = "dd-mm-yyyy";
                            worksheet.Cells[row++, column].Value = date;
                        }

                        column = 2;
                        if (station.Regulated?.Count > 0)
                        {
                            worksheet.Cells[1, column].Value = "Regulated";
                            foreach (var data in station.Regulated)
                            {
                                row = dates.IndexOf(data.Time) + baseRow;
                                worksheet.Cells[row, column].Value = data.Value;
                            }
                        }

                        column = 3;
                        if (station.Unregulated?.Count > 0)
                        {
                            worksheet.Cells[1, column].Value = "Unregulated";
                            foreach (var data in station.Unregulated)
                            {
                                row = dates.IndexOf(data.Time) + baseRow;
                                worksheet.Cells[row, column].Value = data.Value;
                            }
                        }
                    }

                    excel.SaveAs(new FileInfo(filename));
                    MessageBox.Show($"Exported Flow Deviation to {filename}.");

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while exporting: {ex.Message}");
            }
        }

        private List<DateTime> ZipDates(Station station)
        {
            var hash = new HashSet<DateTime>();
            if (station.Regulated?.Count > 0)
            {
                foreach (var data in station.Regulated)
                {
                    if (!hash.Contains(data.Time))
                        hash.Add(data.Time);
                }
            }
            if (station.Unregulated?.Count > 0)
            {
                foreach (var data in station.Unregulated)
                {
                    if (!hash.Contains(data.Time))
                        hash.Add(data.Time);
                }
            }
            var rv = hash.ToList();
            rv.Sort();
            return rv;
        }
        
        #endregion Excel File
    }
}