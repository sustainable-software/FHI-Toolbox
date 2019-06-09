using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.EcosystemVitality.Biodiversity;
using OfficeOpenXml;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class BiodiversityViewModel : ViewModelBase
    {
        private ObservableCollection<SpeciesViewModel> _preview = new ObservableCollection<SpeciesViewModel>();
        private Boolean _invasive;

        public BiodiversityViewModel(bool invasive)
        {
            AddIucnShapefileCommand = new RelayCommand(AddIucnShapefile);
            AddBirdLifeShapefileCommand = new RelayCommand(AddBirdLifeShapefile);
            AddSpreadsheetCommand = new RelayCommand(ImportFromExcel);
            Model.Assets.PropertyChanged += (sender, args) => RaisePropertyChanged(nameof(HasBasinShapefile));
            _invasive = invasive;
        }

        public ObservableCollection<SpeciesViewModel> Preview
        {
            get => _preview;
            set => Set(ref _preview, value);
        }

        public String AddMessage
        {
            get => _addMessage;
            set => Set(ref _addMessage, value);
        }

        private readonly HashSet<int> _previewHash = new HashSet<Int32>();

        public ICommand AddIucnShapefileCommand { get; }
        public ICommand AddBirdLifeShapefileCommand { get; }
        public ICommand AddSpreadsheetCommand { get; }

        public Boolean HasBasinShapefile => Model.Assets.Exists("BasinShapefile"); 
        
        private static Envelope _envelope;

        private async Task AddShapefileToPreviewAsync(String filename, Func<IDictionary<String, Object>, Species> converter)
        {
            var sf = await ShapefileFeatureTable.OpenAsync(filename);
            var qp = new QueryParameters();

            if (_envelope == null)
                await BasinShapefile();
            if (_envelope == null)
            {
                MessageBox.Show(
                    "Assessment requires a basin shapefile to be imported before importing biodiversity data.");
                return;
            }
            qp.Geometry = _envelope;
            qp.SpatialRelationship = SpatialRelationship.Intersects;
            
            var res = await sf.QueryFeaturesAsync(qp);
            
            foreach (var r in res)
            {
                var item = converter(r.Attributes);
                if (item.Code == RedListCode.DD) continue;
                if (!_previewHash.Contains(item.Id))
                {
                    _previewHash.Add(item.Id);
                    Preview.Add(new SpeciesViewModel(item, AddToAssessment, null));
                }
            }         
        }

        private async Task BasinShapefile()
        {
            var directory = Globals.Model.Assets.PathTo("BasinShapefile");
            if (String.IsNullOrWhiteSpace(directory)) return;
            var file = Directory.EnumerateFiles(directory, "*.shp").FirstOrDefault();
            if (String.IsNullOrWhiteSpace(file)) return;
            var sf = await ShapefileFeatureTable.OpenAsync(file);
            _envelope = sf.Extent;
        }

        private void AddToAssessment(SpeciesViewModel vm)
        {
            Preview.Remove(vm);
            if (_invasive)
            {
                var i = Model.EcosystemVitality.FetchIndicator<InvasiveSpeciesIndicator>();
                i.IncludedSpecies.Add(vm.Species);
            }
            else
            {
                var soc = Model.EcosystemVitality.FetchIndicator<SpeciesOfConcernIndicator>();
                soc.IncludedSpecies.Add(vm.Species);
            }
            AddMessage = $"Added {vm.Species.Binomial} to assessment";
        }

        private void ImportFromExcel()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Excel File",
                Filter = "Microsoft Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            AddMessage = $"Reading {dialog.FileName}";
            Preview.Clear();
            _previewHash.Clear();
            using (var excel = new ExcelPackage(new FileInfo(dialog.FileName)))
            {
                if (excel.Workbook.Worksheets.Count == 0)
                {
                    MessageBox.Show("Import error: no worksheets.");
                    return;
                }

                var worksheet = excel.Workbook.Worksheets.Count > 1
                    ? SelectWorksheetViewModel.Dialog(excel.Workbook.Worksheets)
                    : excel.Workbook.Worksheets[1];
                if (worksheet == null)
                {
                    MessageBox.Show("Import error: no readable worksheets.");
                    return;
                }
                const int baseRow = 2;
                var errorRow = 0;
                try
                {
                    for (var row = baseRow; row <= worksheet.Dimension.End.Row; row++)
                    {
                        errorRow = row;
                        if (!Enum.TryParse(worksheet.Cells[row, 3].Value?.ToString(), true, out RedListCode code))
                            code = RedListCode.NONE;
                        var item = new Species
                        {
                            Binomial = worksheet.Cells[row, 1].Value?.ToString(),
                            Family = worksheet.Cells[row, 2].Value?.ToString(),
                            Code = code,
                            UserCanChangeCode = code == RedListCode.NONE
                        };
                        if (String.IsNullOrWhiteSpace(item.Binomial) && String.IsNullOrWhiteSpace(item.Family))
                            continue;
                        Preview.Add(new SpeciesViewModel(item, AddToAssessment, null));
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show($"Error reading spreadsheet row {errorRow}");
                }
            }

            AddMessage = $"{Preview.Count} species to review for possible inclusion";
            var list = new List<SpeciesViewModel>(Preview);
            list.Sort();
            Preview = new ObservableCollection<SpeciesViewModel>(list);
        }

        private async void AddIucnShapefile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open IUCN Shapefile",
                Filter = "GIS Shapefile (*.shp)|*.shp",
                DefaultExt = ".shp"
            };
            if (dialog.ShowDialog() != true) return;
            
            AddMessage = $"Reading {dialog.FileName}";
            Preview.Clear();
            _previewHash.Clear();
            using (new WaitCursor())
            {
                await AddShapefileToPreviewAsync(dialog.FileName, IucnAttributesToSpecies);
            }

            AddMessage = $"{Preview.Count} species to review for possible inclusion";
            var list = new List<SpeciesViewModel>(Preview);
            list.Sort();
            Preview = new ObservableCollection<SpeciesViewModel>(list);
        }

        private async void AddBirdLifeShapefile()
        {
            String directory;
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK) return;
                directory = dialog.SelectedPath;
            }

            AddMessage = $"Reading {directory}";
            Preview.Clear();
            _previewHash.Clear();
            using (new WaitCursor())
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*.shp"))
                {
                    await AddShapefileToPreviewAsync(file, BirdLifeAttributesToSpecies);
                }
            }
            AddMessage = $"{Preview.Count} species to review for possible inclusion";
            var list = new List<SpeciesViewModel>(Preview);
            list.Sort();
            Preview = new ObservableCollection<SpeciesViewModel>(list);
        }
        
        private Species IucnAttributesToSpecies(IDictionary<string, object> attributes)
        {
            var rv = new Species();
            if (attributes.ContainsKey("id_no"))
                rv.Id = Int32.Parse(attributes["id_no"].ToString());
            if (attributes.ContainsKey("binomial"))
                rv.Binomial = attributes["binomial"].ToString();
            if (attributes.ContainsKey("year_"))
                rv.Year = Int32.Parse(attributes["year_"].ToString());
            if (attributes.ContainsKey("citation"))
                rv.Citation = attributes["citation"].ToString();
            if (attributes.ContainsKey("legend"))
                rv.Legend = Species.StringToLegend(attributes["legend"].ToString());
            if (attributes.ContainsKey("kingdom"))
                rv.Kingdom = attributes["kingdom"].ToString();
            if (attributes.ContainsKey("phylum"))
                rv.Phylum = attributes["phylum"].ToString();
            if (attributes.ContainsKey("class"))
                rv.Class = attributes["class"].ToString();
            if (attributes.ContainsKey("order"))
                rv.Order = attributes["order"].ToString();
            if (attributes.ContainsKey("family"))
                rv.Family = attributes["family"].ToString();
            if (attributes.ContainsKey("genus"))
                rv.Genus = attributes["genus"].ToString();
            if (attributes.ContainsKey("code"))
                rv.Code = Enum.TryParse(attributes["code"].ToString(), true, out RedListCode red) ? red : RedListCode.NONE;
            rv.DataSource = DataSource.RedList;
            return rv;
        }
               
        private Species BirdLifeAttributesToSpecies(IDictionary<string, object> attributes)
        {
            var rv = new Species();
            if (attributes.ContainsKey("SISID"))
                rv.Id = Int32.Parse(attributes["SISID"].ToString());
            if (attributes.ContainsKey("SCINAME"))
                rv.Binomial = attributes["SCINAME"].ToString();
            if (attributes.ContainsKey("DATE_ADD"))
            {
                var date = attributes["DATE_ADD"];
                if(DateTime.TryParse(date.ToString(), out var result))
                    rv.Year = result.Year;
            }
            if (attributes.ContainsKey("CITATION"))
                rv.Citation = attributes["CITATION"].ToString();
            
            if (attributes.ContainsKey("SOURCE"))
                rv.Source = attributes["SOURCE"].ToString();
            if (attributes.ContainsKey("PRESENCE"))
            {
                var presence = attributes["PRESENCE"].ToString();
                rv.Legend = _birdlifePresence.ContainsKey(presence) ? _birdlifePresence[presence] : Legend.None;
            }
            rv.Code = RedListCode.NONE;
            rv.DataSource = DataSource.BirdLife;
            rv.UserCanChangeCode = true;
            return rv;
        }
        
        private readonly Dictionary<String, Legend> _birdlifePresence = new Dictionary<String, Legend>
        {
            { "1", Legend.Extant},
            { "2", Legend.ProbablyExtant},
            { "3", Legend.PossiblyExtant},
            { "4", Legend.PossiblyExtant},
            { "5", Legend.PossiblyExtant},
            { "6", Legend.PresenceUncertain}
        };

        private String _addMessage;
    }
}