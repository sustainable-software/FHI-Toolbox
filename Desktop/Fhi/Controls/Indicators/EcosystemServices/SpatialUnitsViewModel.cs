using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Fhi.Controls.Indicators.EcosystemVitality;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Network;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemServices;
using FhiModel.EcosystemVitality.Biodiversity;
using Microsoft.Win32;
using OfficeOpenXml;

namespace Fhi.Controls.Indicators.EcosystemServices
{
    public class SpatialUnitsViewModel : NavigationViewModel
    {
        private BasinMapViewModel _basinMapViewModel;

        public SpatialUnitsViewModel(EcosystemServicesIndicator esi, Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            Indicator = esi;
            EditorCommand = new RelayCommand(Editor);
            RemoveCommand = new RelayCommand(Remove);
            ImportCommand = new RelayCommand(Import);
            ExportCommand = new RelayCommand(Export);

            Indicator.SpatialUnits.CollectionChanged +=
                (sender, args) =>
                {
                    RaisePropertyChanged(nameof(CanChangeConfidence));
                    Task.Factory.StartNew(() => { Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.DataBind,(Action)(async() => await BasinMapViewModel.Refresh())); });
                };
        }

        public ICommand EditorCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        public BasinMapViewModel BasinMapViewModel => _basinMapViewModel ?? (_basinMapViewModel = new BasinMapViewModel(new List<BasinMapLayer>
        {
            new BasinMapReaches(),
            new BasinMapSpatialUnits()
        }));

        public EcosystemServicesIndicator Indicator { get; }

        public Boolean CanChangeConfidence => Indicator.SpatialUnits.Count == 0;

        private void Remove(Object o)
        {
            if (!(o is SpatialUnit su)) return;

            var answer = MessageBox.Show($"Are you sure you want to remove the {su.Name} spatial unit from the assessment?",
                    "Remove Spatial Unit?",
                    MessageBoxButton.YesNo);
            if (answer != MessageBoxResult.Yes) return;

            Indicator.SpatialUnits.Remove(su);
        }

        private void Editor(Object o)
        {
            var su = o as SpatialUnit;
            var create = su == null; // null is "create" mode.

            var width = 800;
            var height = 450;
            SpatialUnitViewModel vm;
            switch (Indicator.EvidenceLevel)
            {
                case Confidence.F1:
                    width = 300;
                    height = 250;
                    if (su == null)
                        su = new F1SpatialUnit { NonCompliant = false };
                    vm = new F1ViewModel(su);
                    break;
                case Confidence.F2:
                    width = 600;
                    if (su == null)
                        su = new F2SpatialUnit();
                    vm = new F2ViewModel(su);
                    break;
                case Confidence.F3Sharp:
                    if (su == null)
                        su = new F3SharpSpatialUnit();
                    vm = new F3SharpViewModel(su);
                    break;
                case Confidence.F3Fuzzy:
                    width = 600;
                    if (su == null)
                        su = new F3FuzzySpatialUnit();
                    vm = new F3FuzzyViewModel(su);
                    break;
                default:
                    throw new ArgumentException("Unknown evidence level.");
            }

            var dialog = new SpatialUnitEditorWindow
            {
                DataContext = vm,
                Owner = Application.Current?.MainWindow,
                Title = create ? "Create Spatial Unit" : "Modify Spatial Unit",
                Width = width,
                Height = height
            };
            if (dialog.ShowDialog() != true) return;

            if (create)
            {
                Indicator.SpatialUnits.Add(vm.SpatialUnit);
            }
            else
            {
                Indicator.SpatialUnits.Remove(su);
                Indicator.SpatialUnits.Add(vm.SpatialUnit);
                // for some reason, replace isn't working with the ListView
                //var index = Indicator.SpatialUnits.IndexOf(su);
                //Indicator.SpatialUnits[index] = vm.SpatialUnit;
            }
        }


        private void Import()
        {
            ImportViewModel.Dialog(ImportFromExcel, async () => await ImportFromShapefile(), "Import spatial unit data from either source. Importing from a shapefile will first look to see if the location name in the shapefile matches existing spatial units, before creating new ones.");
        }

        private async Task ImportFromShapefile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open shapefile with Spatial Unit locations",
                Filter = "Shapefile (*.shp)|*.shp",
                DefaultExt = ".shp"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;
            var sf = await ShapefileFeatureTable.OpenAsync(filename);
            var qp = new QueryParameters();

            var esi = Model.EcosystemServices.FetchIndicators<EcosystemServicesIndicator>().ToList();
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
                foreach (var es in esi)
                {
                    foreach (var su in es.SpatialUnits)
                    {
                        if (su.Name.ToLowerInvariant() != name?.ToLowerInvariant()) continue;
                        su.Location.Latitude = point.Y;
                        su.Location.Longitude = point.X;
                        su.Location.Wkid = point.SpatialReference.Wkid;
                        success = true;
                    }
                }
                if (success) continue;

                var wkid = point.SpatialReference.Wkid;
                if (wkid == 0)
                    wkid = 4326;    // WGS84
                SpatialUnit nsu = null;

                switch (Indicator.EvidenceLevel)
                {
                    case Confidence.F1:
                        nsu = new F1SpatialUnit
                        {
                            Name = name,
                            NonCompliant = false,
                            Location = { Latitude = point.Y, Longitude = point.X, Wkid = wkid }
                        };
                        break;
                    case Confidence.F2:
                        nsu = new F2SpatialUnit
                        {
                            Name = name,
                            Location = { Latitude = point.Y, Longitude = point.X, Wkid = wkid }
                        };

                        break;
                    case Confidence.F3Sharp:
                        nsu = new F3SharpSpatialUnit
                        {
                            Name = name,
                            Location = { Latitude = point.Y, Longitude = point.X, Wkid = wkid }
                        };
                        break;
                    case Confidence.F3Fuzzy:
                        nsu = new F3FuzzySpatialUnit
                        {
                            Name = name,
                            Location = { Latitude = point.Y, Longitude = point.X, Wkid = wkid }
                        };
                        break;
                    default:
                        throw new ArgumentException("Unknown evidence level.");
                }
                Indicator.SpatialUnits.Add(nsu);
                if (String.IsNullOrWhiteSpace(nsu.Name))
                    nsu.Name = $"SU [{Indicator.SpatialUnits.IndexOf(nsu) + 1}]";
            }
        }

        #region Excel File
        private void ImportFromExcel()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Ecosystem Services Excel File",
                Filter = "Microsoft Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;

            using (var excel = new ExcelPackage(new FileInfo(filename)))
            {
                if (excel.Workbook.Worksheets.Count == 0)
                {
                    MessageBox.Show("Import error: no worksheets.");
                    return;
                }
                var worksheet = excel.Workbook.Worksheets.Count > 1 ?
                        SelectWorksheetViewModel.Dialog(excel.Workbook.Worksheets) : excel.Workbook.Worksheets[1];
                if (worksheet == null)
                {
                    MessageBox.Show("Import error: no readable worksheets.");
                    return;
                }
                var esi = Indicator;

                if (!Enum.TryParse(worksheet.Cells[1, 1].Text, true, out Confidence confidence))
                {
                    MessageBox.Show($"Import error: invalid confidence level: {worksheet.Cells[1, 1].Text}");
                    return;
                }

                if (esi.EvidenceLevel == Confidence.F1
                    && esi.EvidenceLevel != confidence
                    && esi.SpatialUnits.Count == 0)
                {
                    esi.EvidenceLevel = confidence;
                }

                if (esi.EvidenceLevel != confidence)
                {
                    MessageBox.Show(
                        "Import error: can't change confidence level because indicator already has spatial units.");
                    return;
                }

                const int baseRow = 3;
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
                    var name = worksheet.Cells[1, column].Text;
                    if (String.IsNullOrWhiteSpace(name)) continue;
                    var units = worksheet.Cells[2, column].Text;

                    var su = esi.SpatialUnits.FirstOrDefault(x => x.Name == name && x.Units == units);
                    if (su == null)
                    {
                        su = CreateSpatialUnit(esi.EvidenceLevel);
                        esi.SpatialUnits.Add(su);
                    }
                    su.Name = name;
                    su.Units = units;

                    switch (su)
                    {
                        case F1SpatialUnit f1:
                            {
                                if (Boolean.TryParse(worksheet.Cells[baseRow, column].Text, out var result))
                                    f1.NonCompliant = result;
                                break;
                            }
                        case F2SpatialUnit f2:
                            {
                                f2.Results.Clear();
                                for (var row = baseRow; row <= worksheet.Dimension.End.Row; row++)
                                {
                                    var date = dates[row - baseRow];
                                    if (date == null) continue;
                                    if (Boolean.TryParse(worksheet.Cells[row, column].Text, out var result))
                                        f2.Results.Add(new ObjectiveResult { NonCompliant = result, Time = date.Value });
                                }
                                break;
                            }
                        case F3FuzzySpatialUnit f3Fuzzy:
                            {
                                f3Fuzzy.Results.Clear();
                                for (var row = baseRow; row <= worksheet.Dimension.End.Row; row++)
                                {
                                    var date = dates[row - baseRow];
                                    if (date == null) continue;
                                    var value = worksheet.Cells[row, column].Value as Double?;
                                    if (value == null) continue;
                                    f3Fuzzy.Results.Add(new ObjectiveResult { Value = value.Value, Time = date.Value });
                                }
                                break;
                            }
                        case F3SharpSpatialUnit f3Sharp:
                            {
                                f3Sharp.Data.Clear();
                                for (var row = baseRow; row <= worksheet.Dimension.End.Row; row++)
                                {
                                    var date = dates[row - baseRow];
                                    if (date == null) continue;
                                    var value = worksheet.Cells[row, column].Value as Double?;
                                    if (value == null) continue;
                                    f3Sharp.Data.Add(new TimeseriesDatum { Value = value.Value, Time = date.Value });
                                }
                                break;
                            }
                    }

                }

            }

            MessageBox.Show($"Imported Ecosystem Services from {filename}.");
        }

        private void Export()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Ecosystem Services Excel File",
                Filter = "Microsoft Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;

            var esiList = Globals.Model.EcosystemServices.FetchIndicators<EcosystemServicesIndicator>();
            using (var excel = new ExcelPackage())
            {
                var esi = Indicator;
                excel.Workbook.Worksheets.Add(esi.Name);
                var worksheet = excel.Workbook.Worksheets[esi.Name];
                worksheet.Cells[1, 1].Value = esi.EvidenceLevel.ToString();


                const int baseRow = 3;
                var row = baseRow;
                var dates = ZipDates(esi.SpatialUnits);
                if (esi.EvidenceLevel != Confidence.F1)
                {
                    foreach (var date in dates)
                    {
                        worksheet.Cells[row, 1].Style.Numberformat.Format = "dd-mm-yyyy";
                        worksheet.Cells[row++, 1].Value = date;
                    }
                }

                var column = 1;
                foreach (var su in esi.SpatialUnits)
                {
                    column++;

                    if (String.IsNullOrWhiteSpace(su.Name))
                        su.Name = $"[SU {esi.SpatialUnits.IndexOf(su) + 1}]";
                    worksheet.Cells[1, column].Value = su.Name;
                    if (String.IsNullOrWhiteSpace(su.Units))
                        su.Units = $"[Unit {esi.SpatialUnits.IndexOf(su) + 1}]";
                    worksheet.Cells[2, column].Value = su.Units;

                    row = baseRow;
                    switch (su)
                    {
                        case F1SpatialUnit f1:
                            worksheet.Cells[row, column].Value = f1.NonCompliant?.ToString();
                            break;
                        case F2SpatialUnit f2:
                            {
                                if (!(f2.Results?.Count > 0)) continue;
                                foreach (var result in f2.Results)
                                {
                                    row = dates.IndexOf(result.Time) + baseRow;
                                    worksheet.Cells[row, column].Value = result.NonCompliant.ToString();
                                }
                                break;
                            }
                        case F3FuzzySpatialUnit f3Fuzzy:
                            {
                                if (!(f3Fuzzy.Results?.Count > 0)) continue;
                                foreach (var result in f3Fuzzy.Results)
                                {
                                    row = dates.IndexOf(result.Time) + baseRow;
                                    worksheet.Cells[row, column].Value = result.Value;
                                }
                                break;
                            }
                        case F3SharpSpatialUnit f3Sharp:
                            {
                                if (f3Sharp.Data == null) continue;
                                if (!(f3Sharp.Data?.Count > 0)) continue;
                                foreach (var data in f3Sharp.Data)
                                {
                                    row = dates.IndexOf(data.Time) + baseRow;
                                    worksheet.Cells[row, column].Value = data.Value;
                                }
                                break;
                            }
                    }
                    worksheet.Cells.AutoFitColumns();
                }


                excel.SaveAs(new FileInfo(filename));
                MessageBox.Show($"Exported Ecosystem Services to {filename}.");
            }
        }

        private List<DateTime> ZipDates(IList<SpatialUnit> units)
        {
            var hash = new HashSet<DateTime>();
            foreach (var su in units)
            {
                switch (su)
                {
                    case F2SpatialUnit f2:
                        {
                            if (!(f2.Results?.Count > 0)) continue;
                            foreach (var result in f2.Results)
                            {
                                if (!hash.Contains(result.Time))
                                    hash.Add(result.Time);
                            }
                            break;
                        }
                    case F3FuzzySpatialUnit f3Fuzzy:
                        {
                            if (!(f3Fuzzy.Results?.Count > 0)) continue;
                            foreach (var result in f3Fuzzy.Results)
                            {
                                if (!hash.Contains(result.Time))
                                    hash.Add(result.Time);
                            }
                            break;
                        }
                    case F3SharpSpatialUnit f3Sharp:
                        {
                            if (!(f3Sharp.Data?.Count > 0)) continue;
                            foreach (var data in f3Sharp.Data)
                            {
                                if (!hash.Contains(data.Time))
                                    hash.Add(data.Time);
                            }
                            break;
                        }
                }
            }

            var rv = hash.ToList();
            rv.Sort();
            return rv;
        }
        private SpatialUnit CreateSpatialUnit(Confidence c)
        {
            switch (c)
            {
                case Confidence.F1:
                    return new F1SpatialUnit();
                case Confidence.F2:
                    return new F2SpatialUnit();
                case Confidence.F3Fuzzy:
                    return new F3FuzzySpatialUnit();
                case Confidence.F3Sharp:
                    return new F3SharpSpatialUnit();
            }

            return null;
        }
        #endregion Excel File
    }
}