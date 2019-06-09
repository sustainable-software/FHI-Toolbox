using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Network;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemServices;
using FhiModel.EcosystemVitality.WaterQuality;
using Microsoft.Win32;
using OfficeOpenXml;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class WaterQualitySummaryViewModel : SummaryViewModel
    {
        private BasinMapViewModel _basinMapViewModel;
        private readonly Action<NavigationViewModel> _navigate;
        
        public WaterQualitySummaryViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            _navigate = navigate;
            
            EditorCommand = new RelayCommand(Editor);
            ImportCommand = new RelayCommand(Import);
            ExportCommand = new RelayCommand(Export);

            Indicator.Gauges.CollectionChanged += (sender, args) => Task.Factory.StartNew(() => { Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.DataBind, (Action)(async () => await BasinMapViewModel.Refresh())); });
        }

        public WaterQualityIndicator Indicator => Model.EcosystemVitality.FetchIndicator<WaterQualityIndicator>();
        
        public ICommand EditorCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        
        public BasinMapViewModel BasinMapViewModel => _basinMapViewModel ?? (_basinMapViewModel = new BasinMapViewModel(new List<BasinMapLayer>
        {
            new BasinMapGauges()
        }));
        
        private void Editor(Object o)
        {
            if (!(o is Gauge gauge))
                gauge = new Gauge();
            var vm = new GaugeViewModel(gauge, _navigate, Back);
            Navigate(vm);
        }


        private void Import()
        {
            ImportViewModel.Dialog(ImportFromExcel, async () => await ImportFromShapefile(), "Import water quality data from either source. Importing from a shapefile will first look to see if the location name in the shapefile matches existing gauges before creating a new one.");
        }

        private async Task ImportFromShapefile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open shapefile with gauge locations",
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
                
                foreach (var gauge in Indicator.Gauges)
                {
                    if (gauge.Name.ToLowerInvariant() != name?.ToLowerInvariant()) continue;
                    gauge.Location.Latitude = point.Y;
                    gauge.Location.Longitude = point.X;
                    gauge.Location.Wkid = point.SpatialReference.Wkid;
                    success = true;
                }
                
                if (success) continue;

                var wkid = point.SpatialReference.Wkid;
                if (wkid == 0) wkid = 4326;    // WGS84

                var ng = new Gauge
                {
                    Name = name,
                    Location = {Latitude = point.Y, Longitude = point.X, Wkid = wkid}
                };
                Indicator.Gauges.Add(ng);
                if (String.IsNullOrWhiteSpace(ng.Name))
                    ng.Name = $"GAUGE [{Indicator.Gauges.IndexOf(ng) + 1}]";
            }
        }

        #region Excel File

            // row and column to report error on for user.
        private int _errorRow;
        private int _errorColumn;

        private void ImportFromExcel()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Water Quality Excel File",
                Filter = "Microsoft Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;
            try
            {
                using(new WaitCursor())
                {
                    using (var excel = new ExcelPackage(new FileInfo(filename)))
                    {
                        if (excel.Workbook.Worksheets.Count == 0)
                        {
                            MessageBox.Show("Import error: no worksheets.");
                            return;
                        }

                        var wq = Indicator;

                        // can make this more complicated, ask user and so forth
                        if (excel.Workbook.Worksheets.Count == 1 &&
                            wq.Gauges.FirstOrDefault(x => x.Name == excel.Workbook.Worksheets[1].Name) == null)
                        {
                            if (excel.Workbook.Worksheets[1].Cells[1, 1].Text.Contains("COUNTRY"))
                            {
                                Type1Special(excel, wq);
                            }
                            else
                            {
                                Type2Special(excel, wq);
                            }

                            MessageBox.Show($"Imported Water Quality from {filename}.");
                            return;
                        }


                        foreach (var worksheet in excel.Workbook.Worksheets)
                        {
                            var gauge = wq.Gauges.FirstOrDefault(x => x.Name == worksheet.Name);
                            if (gauge == null)
                            {
                                gauge = new Gauge {Name = worksheet.Name};
                                wq.Gauges.Add(gauge);
                            }

                            const int baseRow = 3;
                            var dates = new List<DateTime?>();
                            for (var row = baseRow; row <= worksheet.Dimension.End.Row; row++)
                            {
                                _errorRow = row;
                                _errorColumn = 1;
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
                                var name = worksheet.Cells[1, column].Text;
                                var units = worksheet.Cells[2, column].Text;
                                var parameter = gauge.Parameters.FirstOrDefault(x => x.Name == name);
                                if (parameter == null)
                                {
                                    parameter = new WaterQualityParameter {Name = name};
                                    gauge.Parameters.Add(parameter);
                                }

                                parameter.Units = units;
                                parameter.Data.Clear();
                                for (var row = baseRow; row <= worksheet.Dimension.End.Row; row++)
                                {
                                    _errorRow = row;
                                    _errorColumn = column;

                                    var date = dates[row - baseRow];
                                    if (date == null) continue;
                                    var value = worksheet.Cells[row, column].Value as Double?;
                                    if (value == null) continue;
                                    parameter.Data.Add(new TimeseriesDatum {Value = value.Value, Time = date.Value});
                                }
                            }
                        }
                    }
                }
                MessageBox.Show($"Imported Water Quality from {filename}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while importing row {_errorRow}, column {_errorColumn}: {ex.Message}");
            }
        }

        private void Export()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Water Quality Excel File",
                Filter = "Microsoft Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;
            try
            {
                using (var excel = new ExcelPackage())
                {
                    var wq = Indicator;
                    foreach (var gauge in wq.Gauges)
                    {
                        if (String.IsNullOrWhiteSpace(gauge.Name))
                            gauge.Name = $"Gauge {wq.Gauges.IndexOf(gauge) + 1}";
                        excel.Workbook.Worksheets.Add(gauge.Name);
                        var worksheet = excel.Workbook.Worksheets[gauge.Name];
                        
                        const int baseRow = 3;
                        var row = baseRow;

                        var column = 1;
                        var dates = ZipDates(gauge);
                        foreach (var date in dates)
                        {
                            worksheet.Cells[row, column].Style.Numberformat.Format = "dd-mm-yyyy";
                            worksheet.Cells[row++, column].Value = date;
                        }

                        foreach (var parameter in gauge.Parameters)
                        {
                            column++;

                            worksheet.Cells[1, column].Value = parameter.Name;
                            worksheet.Cells[2, column].Value = parameter.Units;

                            foreach (var data in parameter.Data)
                            {
                                row = dates.IndexOf(data.Time) + baseRow;
                                worksheet.Cells[row, column].Value = data.Value;
                            }
                        }
                    }

                    excel.SaveAs(new FileInfo(filename));
                    MessageBox.Show($"Exported Water Quality to {filename}.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while exporting: {ex.Message}");
            }
        }

        private List<DateTime> ZipDates(Gauge gauge)
        {
            var hash = new HashSet<DateTime>();
            foreach (var parameter in gauge.Parameters)
            {
                if (parameter.Data?.Count > 0)
                {
                    foreach (var data in parameter.Data)
                    {
                        if (!hash.Contains(data.Time))
                            hash.Add(data.Time);
                    }
                }
            }
            
            var rv = hash.ToList();
            rv.Sort();
            return rv;
        }

        private void Type1Special(ExcelPackage excel, WaterQualityIndicator wq)
        {
            var worksheet = excel.Workbook.Worksheets[1];
            for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                _errorRow = row;
                
                var comment = new StringBuilder();
                var gaugeName = String.Empty;
                var date = new DateTime();
                var parameters = new List<(string name, string unit, double value)>();

                for (var column = 1; column <= worksheet.Dimension.End.Column; column++)
                {  
                    _errorColumn = column;
                    

                    switch (worksheet.Cells[1, column].Text.ToLowerInvariant())
                    {
                        case "country":
                            comment.AppendLine($"Country: {worksheet.Cells[row, column].Text}");
                            break;
                        case "name":
                            comment.AppendLine($"Name: {worksheet.Cells[row, column].Text}");
                            break;
                        case "water body":
                            comment.AppendLine($"Water body: {worksheet.Cells[row, column].Text}");
                            break;
                        case "river name":
                            comment.AppendLine($"River name: {worksheet.Cells[row, column].Text}");
                            break;
                        case "statid":
                            gaugeName = worksheet.Cells[row, column].Text;
                            break;
                        case "sdate":
                            if (worksheet.Cells[row, column].Value == null) break;
                            date = DateTime.FromOADate((double)worksheet.Cells[row, column].Value);
                            break;
                        case "year":
                            break;
                        case "month":
                            break;
                        default:
                            string n;
                            var u = String.Empty;
                            if (String.IsNullOrWhiteSpace(worksheet.Cells[row, column].Text)) break;
                            if (!Double.TryParse(worksheet.Cells[row, column].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                            {
                                var sv = worksheet.Cells[row, column].Text;
                                if (String.IsNullOrWhiteSpace(sv) || !sv.Contains("x")) break;
                                if (sv.StartsWith("<"))
                                {
                                    v = 0;
                                }
                                else
                                {
                                    var ssv = sv.Split('x');
                                    if (ssv.Length != 2) break;

                                    if (!Double.TryParse(ssv[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var mult)) break;
                                    if (!Double.TryParse(ssv[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var b)) break;
                                    v = b * mult;
                                }
                            }
                            var s = worksheet.Cells[1, column].Text?.Split('_');
                            if (s.Length == 1)
                            {
                                n = worksheet.Cells[1, column].Text;
                            }
                            else if (s.Length == 2)
                            {
                                n = s[0];
                                u = s[1];
                            }
                            else
                            {
                                break;
                            }
                            parameters.Add((n, u, v));
                            break;
                    }
                }

                
                var gauge = wq.Gauges.FirstOrDefault(x => x.Name == gaugeName);
                if (gauge == null)
                {
                    gauge = new Gauge {Name = gaugeName};
                    wq.Gauges.Add(gauge);
                }

                gauge.Notes = comment.ToString();
                foreach (var (name, unit, value) in parameters)
                {
                    var p = gauge.Parameters.FirstOrDefault(x => x.Name == name);
                    if (p == null)
                    {
                        p = new WaterQualityParameter {Name = name, Units = unit};
                        gauge.Parameters.Add(p);
                    }

                    p.Data.Add(new TimeseriesDatum {Time = date, Value = value});
                }
                
            }
        }

        private void Type2Special(ExcelPackage excel, WaterQualityIndicator wq)
        {
            var worksheet = excel.Workbook.Worksheets[1];
            for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                _errorRow = row;
                
                var comment = new StringBuilder();
                var gaugeName = String.Empty;
                var year = 0;
                var month = String.Empty;
                var parameters = new List<(string name, string unit, double value)>();

                for (var column = 1; column <= worksheet.Dimension.End.Column; column++)
                {
                    _errorColumn = column;

                    switch (worksheet.Cells[1, column].Text.ToLowerInvariant())
                    {
                        case "name":
                            comment.AppendLine($"Name: {worksheet.Cells[row, column].Text}");
                            gaugeName = worksheet.Cells[row, column].Text;
                            break;
                        case "water body":
                            comment.AppendLine($"Water body: {worksheet.Cells[row, column].Text}");
                            break;
                        case "river":
                            comment.AppendLine($"River: {worksheet.Cells[row, column].Text}");
                            break;
                        case "year":
                            int.TryParse(worksheet.Cells[row, column].Text, NumberStyles.Any,
                                CultureInfo.InvariantCulture, out year);
                            break;
                        case "month":
                            month = worksheet.Cells[row, column].Text;
                            break;
                        default:
                            string n;
                            var u = String.Empty;
                            if (String.IsNullOrWhiteSpace(worksheet.Cells[row, column].Text)
                                || worksheet.Cells[row, column].Text.ToLowerInvariant().Contains("na")) break;
                            if (!Double.TryParse(worksheet.Cells[row, column].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                            {
                                var sv = worksheet.Cells[row, column].Text;
                                if (String.IsNullOrWhiteSpace(sv) || !sv.Contains("x")) break;
                                if (sv.StartsWith("<"))
                                {
                                    v = 0;
                                }
                                else
                                {
                                    var ssv = sv.Split('x');
                                    if (ssv.Length != 2) break;

                                    if (!Double.TryParse(ssv[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var mult)) break;
                                    if (!Double.TryParse(ssv[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var b)) break;
                                    v = b * mult;
                                }
                            }
                            var s = worksheet.Cells[1, column].Text?.Split('_');
                            if (s.Length == 1)
                            {
                                n = worksheet.Cells[1, column].Text;
                            }
                            else if (s.Length == 2)
                            {
                                n = s[0];
                                u = s[1];
                            }
                            else
                            {
                                break;
                            }
                            parameters.Add((n, u, v));
                            break;
                    }
                }

                if (!_monthNames.ContainsKey(month.ToLowerInvariant())) continue;
                var date = new DateTime(year, _monthNames[month.ToLowerInvariant()], 1);
                
                var gauge = wq.Gauges.FirstOrDefault(x => x.Name == gaugeName);
                if (gauge == null)
                {
                    gauge = new Gauge {Name = gaugeName};
                    wq.Gauges.Add(gauge);
                }

                gauge.Notes = comment.ToString();
                foreach (var (name, unit, value) in parameters)
                {
                    var p = gauge.Parameters.FirstOrDefault(x => x.Name == name);
                    if (p == null)
                    {
                        p = new WaterQualityParameter {Name = name, Units = unit};
                        gauge.Parameters.Add(p);
                    }

                    p.Data.Add(new TimeseriesDatum {Time = date, Value = value});
                }
            }
        }

        private readonly Dictionary<string, int> _monthNames = new Dictionary<string, int>
        {
            { "jan", 1 },
            { "feb", 2 },
            { "mar", 3 },
            { "apr", 4 },
            { "may", 5 },
            { "jun", 6 },
            { "jul", 7 },
            { "aug", 8 },
            { "sep", 9 },
            { "oct", 10 },
            { "nov", 11 },
            { "dec", 12 },
        };
        #endregion Excel File
    }
}
