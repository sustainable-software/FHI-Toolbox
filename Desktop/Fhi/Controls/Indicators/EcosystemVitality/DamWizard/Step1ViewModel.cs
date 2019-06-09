using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using Fhi.Controls.Wizard;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using Microsoft.Win32;

namespace Fhi.Controls.Indicators.EcosystemVitality.DamWizard
{
    public class Step1ViewModel : WizardStepViewModel
    {
        private readonly IList<WizardViewModel.Step> _steps;
        private string _filename;
        private List<DamTableRow> _remainingDams;
        private Double _progress;
        private Boolean _importComplete;
        private Boolean _importInProgress;
        private readonly Double _matchDistance;
        private int _wkid;
        private bool _shapefile;
        private readonly SpatialReference _reachSpatialReference;

        public Step1ViewModel(IList<Reach> reaches, int wkid, IList<WizardViewModel.Step> steps)
        {
            Reaches = reaches;
            Wkid = wkid;
            _steps = steps;

            ImportCsvCommand = new RelayCommand(() => ImportCsv());
            ImportShapefileCommand = new RelayCommand(() => ImportShapefile());

            _matchDistance = reaches.Sum(x => x.Length) / reaches.Count * 0.5;
            _reachSpatialReference = new SpatialReference(UnitTest ? wkid : Globals.Model.Attributes.Wkid);
            Shapefile = true;
        }

        public ICommand ImportCsvCommand { get; }
        public ICommand ImportShapefileCommand { get; }


        public override bool ReadyForNext => !String.IsNullOrWhiteSpace(Filename);

        public String Filename
        {
            get => _filename;
            set => Set(ref _filename, value);
        }

        public IList<Reach> Reaches { get; set; }

        public Int32 Wkid
        {
            get => _wkid;
            set
            {
                if (Set(ref _wkid, value))
                    RaisePropertyChanged(nameof(WkidSet));
            }
        }

        public Boolean WkidSet => Wkid != 0;

        public Boolean Shapefile
        {
            get => _shapefile;
            set => Set(ref _shapefile, value);
        }

        public void ImportCsv(string filename = null)
        {
            if (filename == null)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Import Dams CSV",
                    DefaultExt = ".csv",
                    CheckFileExists = true
                };
                if (dialog.ShowDialog() != true)
                    return;

                filename = dialog.FileName;
            }

            if (String.IsNullOrWhiteSpace(filename)) return;

            try
            {
                using (new WaitCursor())
                {
                    ImportInProgress = true;
                    var damTable = DamTableRow.Create(filename);
                    Filename = Path.GetFileNameWithoutExtension(filename);
                    DamCount = damTable.Count;
                    RemainingDams = AddDams(Reaches, damTable);
                    CreateDamSteps(RemainingDams);
                    ImportInProgress = false;
                    ImportComplete = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to read {filename}: {ex.Message}");
            }
        }

        public async void ImportShapefile(string filename = null)
        {
            if (filename == null)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Open Dam Shapefile",
                    Filter = "GIS Shapefile (*.shp)|*.shp",
                    DefaultExt = ".shp",
                    CheckFileExists = true
                };
                if (dialog.ShowDialog() != true)
                    return;

                filename = dialog.FileName;
            }

            if (String.IsNullOrWhiteSpace(filename)) return;
            try
            {
                using (new WaitCursor())
                {
                    ImportInProgress = true;
                    Filename = Path.GetFileNameWithoutExtension(filename);
                    var sf = await ShapefileFeatureTable.OpenAsync(filename);
                    var qp = new QueryParameters();

                    var res = await sf.QueryFeaturesAsync(qp);
                    SpatialReference spatialReference = null;

                    var damTable = new List<DamTableRow>();
                    foreach (var r in res)
                    {
                        if (!(r.Geometry is MapPoint point)) continue;
                        if (spatialReference == null)
                            spatialReference = point.SpatialReference;
                        damTable.Add(MapPointToDamTable(point, r.Attributes));
                    }

                    if (spatialReference == null)
                    {
                        MessageBox.Show("No spatial reference was found in shapefile");
                        return;
                    }
                    DamCount = damTable.Count;
                    RemainingDams = AddDams(Reaches, damTable);
                    CreateDamSteps(RemainingDams);
                    ImportInProgress = false;
                    ImportComplete = true;          
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to read {filename}: {ex.Message}");
            }
        }

        private int _damId = 1000;

        private DamTableRow MapPointToDamTable(MapPoint point, IDictionary<String, object> attributes)
        {
            if (!(GeometryEngine.Project(point, _reachSpatialReference) is MapPoint projected))
                return null;

            var name = $"{_damId}";
            foreach (var key in attributes.Keys)
            {
                if (key.ToLower().Contains("name"))
                {
                    name = attributes[key].ToString();
                    break;
                }
            }

            return new DamTableRow
            {
                Id = _damId++,
                Name = name,
                Location = new Location
                {
                    Latitude = projected.Y,
                    Longitude = projected.X,
                    Wkid = projected.SpatialReference.Wkid
                },
                Passability = 1.0
            };
        }

        public List<DamTableRow> RemainingDams
        {
            get => _remainingDams;
            private set => Set(ref _remainingDams, value);
        }

        public Int32 DamCount { get; private set; }

        private void CreateDamSteps(List<DamTableRow> damTable)
        {
            if (damTable.Count > 0)
            {
                var summary = _steps[_steps.Count - 1];
                _steps.RemoveAt(_steps.Count - 1);
                _steps.Add(new WizardViewModel.Step("", new StepNViewModel(damTable, Reaches, Wkid)));
                _steps.Add(summary);
            }
            RaisePropertyChanged(nameof(ReadyForNext));
        }

        public Double Progress
        {
            get => _progress;
            set => Set(ref _progress, value);
        }

        public Boolean ImportComplete
        {
            get => _importComplete;
            set => Set(ref _importComplete, value);
        }

        public Boolean ImportInProgress
        {
            get => _importInProgress;
            set => Set(ref _importInProgress, value);
        }

        private List<DamTableRow> AddDams(IList<Reach> reaches, List<DamTableRow> damTable)
        {
            // this algorithm works as follows:
            // for each dam, create a list of all the nodes within _matchDistance
            // sort the list ordered by closest match
            // assign the dam to the closest matching node without a dam
            var dams = new Dictionary<DamTableRow, DamSort>();
            foreach (var dam in damTable)
                dams[dam] = new DamSort(dam);

            var count = 0;
            foreach (var reach in reaches)
            {
                // remove prior state
                reach.HasDam = false;
                
                count++;
                CallLater(() => Progress = 100 * (double)count / reaches.Count);
                foreach (var node in reach.Nodes)
                {
                    node.Dam = null; // remove prior state
                    foreach (var dam in damTable)
                    {
                        if (!node.Location.Match(dam.Location, _matchDistance))
                            continue;
                        dams[dam].Candidates.Add(new Candidate(node, dam));
                    }
                }
            }

            var damlist = dams.Values.ToList();
            damlist.Sort();
            var remaining = new List<DamTableRow>();
            foreach (var dam in damlist)
            {
                if (dam.Closest == null || dam.Closest > _matchDistance)
                {
                    remaining.Add(dam.Dam);
                    continue;
                }

                foreach (var candidate in dam.Candidates)
                {
                    if (candidate.Node.Dam != null) continue;
                    candidate.Node.Dam = new Dam(dam.Dam);
                    break;
                }
            }

            foreach (var reach in reaches)
                reach.HasDam = reach.Nodes.Any(x => x.Dam != null);

            return remaining;
        }

        private class DamSort : IComparable<DamSort>
        {
            private Double? _closest;

            public DamSort(DamTableRow dam)
            {
                Dam = dam;
                Candidates = new List<Candidate>();
            }

            public List<Candidate> Candidates { get; set; }
            public DamTableRow Dam { get; set; }

            public Double? Closest
            {
                get
                {
                    if (_closest != null) return _closest;
                    if (Candidates.Count == 0) return null;
                    Candidates.Sort();
                    _closest = Candidates[0].Distance;
                    return _closest;
                }
            }

            public Int32 CompareTo(DamSort other)
            {
                if (!other.Closest.HasValue && !Closest.HasValue)
                    return 0;
                if (other.Closest.HasValue && !Closest.HasValue)
                    return 1;
                if (!other.Closest.HasValue && Closest.HasValue)
                    return -1;

                return Closest.Value.CompareTo(other.Closest.Value);
            }
        }

        private class Candidate : IComparable<Candidate>
        {
            public Candidate(Node node, DamTableRow dam)
            {
                Node = node;
                Distance = node.Location.Distance(dam.Location);
            }

            public Node Node { get; set; }

            public Double Distance { get; set; }

            public Int32 CompareTo(Candidate other)
            {
                return Distance.CompareTo(other.Distance);
            }

            public override String ToString()
            {
                return $"{Distance}";
            }
        }
    }
}

