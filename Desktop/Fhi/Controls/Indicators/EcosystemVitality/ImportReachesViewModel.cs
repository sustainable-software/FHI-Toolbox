using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Fhi.Controls.MVVM;
using Fhi.Controls.Network;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using Microsoft.Win32;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class ImportReachesViewModel : ViewModelBase
    {
        private BasinMapViewModel _basinMapViewModel;
        private readonly Dictionary<SimpleLineSymbol, Color> _resetColors = new Dictionary<SimpleLineSymbol, Color>();
        private Double _matchDistance = 1.0;
        private Reach _selectedReach;
        private int _reachNumber;
        private double _progress;
        private bool _processing;
        private bool _complete;
        private bool _step1;
        private bool _step2;
        private bool _step3;
        private bool _shapefile;
        private int _wkid;
        private string _completeMessage;

        public ImportReachesViewModel()
        {
            RiverShapefileImportCommand = new RelayCommand(RiverShapefileImport);
            RiverCsvImportCommand = new RelayCommand(() => RiverCsvImport());
            OutletSelectedCommand = new RelayCommand(OutletSelected);

            Shapefile = true;
            Step1 = true;
        }

        public ICommand RiverShapefileImportCommand { get; }
        public ICommand RiverCsvImportCommand { get; }
        public ICommand OutletSelectedCommand { get; }

        public BasinMapViewModel BasinMapViewModel
        {
            get => _basinMapViewModel;
            set
            {
                Set(ref _basinMapViewModel, value);
                if (_basinMapViewModel == null) return;
                _basinMapViewModel.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(BasinMapViewModel.SelectedId))
                    {
                        Step2 = false;
                        Step3 = true;
                        var item = BasinMapViewModel.IdModelTracker[BasinMapViewModel.SelectedId];
                        if (item is MapReach mapReach)
                        {
                            SelectedReach = mapReach.Reach;
                            // debugging:
                            ResetColors();
                            foreach (var kvp in BasinMapViewModel.IdModelTracker)
                            {
                                if (kvp.Value is MapReach mr)
                                {
                                    if (SelectedReach.DownstreamReach == mr.Reach)
                                    {
                                        if (BasinMapViewModel.IdMapTracker[kvp.Key].Symbol is SimpleLineSymbol sl)
                                        {
                                            _resetColors.Add(sl, sl.Color);
                                            sl.Color = Color.DeepPink;
                                        }
                                    }
                                    if (SelectedReach.UpstreamReaches?.Contains(mr.Reach) == true)
                                    {
                                        if (BasinMapViewModel.IdMapTracker[kvp.Key].Symbol is SimpleLineSymbol sl)
                                        {
                                            _resetColors.Add(sl, sl.Color);
                                            sl.Color = Color.GreenYellow;
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }
        }
        
        public List<Reach> Reaches { get; private set; }

        public Boolean Shapefile
        {
            get => _shapefile;
            set => Set(ref _shapefile, value);
        }

        public int Wkid
        {
            get => _wkid;
            set
            {
                if(Set(ref _wkid, value))
                    RaisePropertyChanged(nameof(WkidSet));
            }
        }

        public Boolean WkidSet => Wkid != 0;

        public Boolean Step1
        {
            get => _step1;
            set => Set(ref _step1, value);
        }

        public Boolean Step2
        {
            get => _step2;
            set => Set(ref _step2, value);
        }

        public Boolean Step3
        {
            get => _step3;
            set => Set(ref _step3, value);
        }

        public Double Progress
        {
            get => _progress;
            set => Set(ref _progress, value);
        }

        public Boolean Processing
        {
            get => _processing;
            set => Set(ref _processing, value);
        }

        public Boolean Complete
        {
            get => _complete;
            set => Set(ref _complete, value);
        }

        public String CompleteMessage
        {
            get => _completeMessage;
            set => Set(ref _completeMessage, value);
        }

        #region DEBUGGING

        public Reach SelectedReach
        {
            get => _selectedReach;
            set
            {
                Set(ref _selectedReach, value);
                if (_selectedReach == null) return;
                if (!Debug.Contains(_selectedReach))
                    Debug.Add(_selectedReach);
            }
        }

        public ObservableCollection<Reach> Debug { get; } = new ObservableCollection<Reach>();

        #endregion DEBUGGING

        private void ResetColors()
        {
            foreach (var key in _resetColors.Keys)
                key.Color = _resetColors[key];
            _resetColors.Clear();
        }

        private void OutletSelected()
        {
            var item = BasinMapViewModel.IdModelTracker[BasinMapViewModel.SelectedId];
            if (item is MapReach mapReach)
            {
                mapReach.Reach.Outlet = true;
                Process();
            }
            else
            {
                MessageBox.Show("Selected item on map does not appear to be a reach.");
            }
        }

        private async void RiverShapefileImport()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open River Shapefile",
                Filter = "GIS Shapefile (*.shp)|*.shp",
                DefaultExt = ".shp"
            };
            if (dialog.ShowDialog() != true) return;

            var sf = await ShapefileFeatureTable.OpenAsync(dialog.FileName);
            var qp = new QueryParameters();

            var res = await sf.QueryFeaturesAsync(qp);
            SpatialReference spatialReference = null;

            _reachNumber = 1000;
            var reaches = new List<Reach>();
            foreach (var r in res)
            {
                // dam import is MapPoints
                if (!(r.Geometry is Polyline polyline)) continue;
                if (spatialReference == null)
                    spatialReference = polyline.SpatialReference;
                reaches.AddRange(PolylineToReach(polyline));
            }

            if (spatialReference == null)
            {
                MessageBox.Show("No spatial reference was found in shapefile");
                return;
            }

            Reaches = reaches;
            BasinMapViewModel = new BasinMapViewModel(Reaches, spatialReference.Wkid, false);
            Wkid = spatialReference.Wkid;

            Step1 = false;
            Step2 = true;
        }

        public void RiverCsvImport(string filename = null)
        {

            try
            {
                var nothing = new SpatialReference(Wkid);
            }
            catch (Exception)
            {
                MessageBox.Show($"Unable to translate WKID {Wkid} to a valid projection.");
                return;
            }
            
            if (String.IsNullOrWhiteSpace(filename))
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Open River CSV",
                    Filter = "CSV File (*.csv)|*.csv",
                    DefaultExt = ".csv"
                };
                if (dialog.ShowDialog() != true) return;
                filename = dialog.FileName;
            }

            try
            {
                using (new WaitCursor())
                {
                    var riverTable = RiverTableRow.Create(filename, 0, null);
                    Reaches = riverTable.Select(x => ParseLinestring(x.Wkt, x.ArcId)).ToList();
                    BasinMapViewModel = new BasinMapViewModel(Reaches, Wkid, false);
                    Step1 = false;
                    Step2 = true;
                }
            }
            catch (Exception)
            {
                if (UnitTest) throw;
                MessageBox.Show("There was an error importing the CSV file. It wasn't in an expected format.");
            }
        }
        
        private IEnumerable<Reach> PolylineToReach(Polyline p)
        {
            var reaches = new List<Reach>();
            foreach (var part in p.Parts)
            {
                var reach = new Reach
                {
                    Nodes = new List<Node>(),
                    Id = _reachNumber++
                };
                foreach (var point in part.Points)
                {
                    var n = new Node();
                    n.Location.Longitude = point.X;
                    n.Location.Latitude = point.Y;
                    reach.Nodes.Add(n);
                }
                reaches.Add(reach);
            }
            return reaches;
        }

        /// <summary>
        /// Parses the so-called "well known text" LINESTRING into a list of nodes.
        /// </summary>
        /// <param name="wkt">The well known text string</param>
        /// <param name="arcId">Id of the arc or "reach"</param>
        /// <returns>Parsed reach</returns>
        private static Reach ParseLinestring(String wkt, string arcId)
        {
            var reach = new Reach();
            var nodes = new List<Node>();
            reach.Nodes = nodes;
            reach.Id = int.Parse(arcId);

            // LINESTRING ( x y, x y, ... )
            var s1 = wkt.Replace("LINESTRING (", "");
            var s2 = s1.Replace(")", "");
            var s3 = s2.Split(',');

            foreach (var s in s3)
            {
                var node = new Node();
                var pt = s.Split(' ');
                node.Location.Longitude = Double.Parse(pt[0]);
                node.Location.Latitude = Double.Parse(pt[1]);
                node.Location.Color = Color.Black;
                node.Location.Symbol = Location.MapSymbol.Circle;
                nodes.Add(node);
            }
            return reach;
        }

        public void Process()
        {
            List<Reach> disconnected;
            using (new WaitCursor())
            {
                Processing = true;
                var outlet = Reaches.FirstOrDefault(n => n.Outlet);
                if (outlet == null)
                {
                    MessageBox.Show("Couldn't find the specified outlet in the imported river network.");
                    return;
                }

                var totalLength = 0.0;
                foreach (var reach in Reaches)
                {
                    totalLength += reach.Length;
                    reach.UpstreamReaches = null;
                    reach.DownstreamReach = null;
                }

                // 10% of the average reach length for automatic matching of endpoints
                _matchDistance = totalLength / Reaches.Count * 0.10;
                disconnected = InitializeNetwork(Reaches, outlet);
                Processing = false;
            }

            var clean = disconnected.Count == 0;

            var cleanMessage = $"You have successfully imported {Reaches.Count} reaches. Click the Ok button to incorporate them in your assessment, Cancel will discard the imported data.";
            var disconnectedMessage = $"WARNING: {disconnected.Count} reaches could not be connected to the river network and are highlighted on the map. There are {Reaches.Count} total reaches. For this projection, reach endpoints must be within {_matchDistance:N2} to be connectable. Click the Ok button to ignore the disconnected reaches for the DCI computation. Cancel will discard the imported data.";

            if (!UnitTest)
            {
                MessageBox.Show($"You have imported {Reaches.Count} reaches.",
                    "Reaches Imported",
                    MessageBoxButton.OK,
                    clean ? MessageBoxImage.None : MessageBoxImage.Warning);
            }
            Step3 = false;
            CompleteMessage = clean ? cleanMessage : disconnectedMessage;
            Complete = true;
        }

        private List<Reach> InitializeNetwork(List<Reach> reaches, Reach outlet)
        {

            var outletMatch = false;
            var upstreamList = new Dictionary<Reach, ReachSort>();
            foreach (var reach in reaches)
            {
                upstreamList[reach] = new ReachSort(reach);
                if (!outletMatch && outlet != reach && (outlet.Upstream.Location.Match(reach.Upstream.Location, _matchDistance) ||
                    outlet.Upstream.Location.Match(reach.Upstream.Location, _matchDistance)))
                    outletMatch = true;
            }

            if (!outletMatch)
                outlet.Nodes.Reverse();

            var reachNumber = 0;
            var candidates = new List<Candidate>();
            foreach (var outer in reaches)
            {
                CallLater(() => Progress = (double) reachNumber++ / reaches.Count * 100.0);
                foreach (var inner in reaches)
                {
                    if (outer == inner) continue;

                    candidates.Clear();
                    if (outer.Upstream.Location.Match(inner.Downstream.Location, _matchDistance))
                    {
                        candidates.Add(new Candidate(inner.Downstream, outer.Upstream, inner));
                    }

                    if (outer.Upstream.Location.Match(inner.Upstream.Location, _matchDistance))
                    {
                        candidates.Add(new Candidate(inner.Upstream, outer.Upstream, inner));
                    }

                    if (outer.Downstream.Location.Match(inner.Downstream.Location, _matchDistance))
                    {
                        candidates.Add(new Candidate(inner.Downstream, outer.Downstream, inner));
                    }

                    if (outer.Downstream.Location.Match(inner.Upstream.Location, _matchDistance))
                    {
                        candidates.Add(new Candidate(inner.Upstream, outer.Downstream, inner));
                    }

                    if (candidates.Count == 0) continue;
                    candidates.Sort();
                    upstreamList[outer].Candidates.Add(candidates[0]);
                }
            }
            AssignUpstream(outlet, upstreamList);
            
            var disconnected = new List<Reach>();
            foreach (var reach in reaches)
                if (reach.DownstreamReach == null && !reach.Outlet)
                    disconnected.Add(reach);
            if (disconnected.Count > 0)
            {
                HighlightDisconnectedReaches(disconnected);
                foreach (var reach in disconnected)
                    reach.SegmentId = new List<int> { Int32.MaxValue };
            }

            return disconnected;
        } 

        private void AssignUpstream(Reach root, Dictionary<Reach, ReachSort> dictionary)
        {
            var rs = dictionary[root];
            foreach (var candidate in rs.Candidates)
            {
                if (candidate.Reach.DownstreamReach != null || candidate.Reach.Outlet)
                    continue;
                
                // if the match happened on the "wrong" end of the reach, make upstream match
                if (candidate.Node1 == candidate.Reach.Upstream)
                    candidate.Reach.Nodes.Reverse();
                    
                if (candidate.Distance > _matchDistance) continue;

                candidate.Reach.DownstreamReach = root;
                if (root.UpstreamReaches == null)
                    root.UpstreamReaches = new List<Reach>();
                root.UpstreamReaches.Add(candidate.Reach);
            }

            if (root.UpstreamReaches != null)
            {
                foreach (var us in root.UpstreamReaches)
                    AssignUpstream(us, dictionary);
            }
        }

        private void HighlightDisconnectedReaches(List<Reach> reaches)
        {
            ResetColors();
            foreach (var kvp in BasinMapViewModel.IdModelTracker)
            {
                if (kvp.Value is MapReach mr)
                {
                    if (reaches.Contains(mr.Reach) == true)
                    {
                        if (BasinMapViewModel.IdMapTracker[kvp.Key].Symbol is SimpleLineSymbol sl)
                        {
                            _resetColors.Add(sl, sl.Color);
                            sl.Color = Color.GreenYellow;
                        }
                    }
                }
            }
        }

        private class ReachSort : IComparable<ReachSort>
        {
            private Double? _closest;

            public ReachSort(Reach reach)
            {
                Reach = reach;
                Candidates = new List<Candidate>();
            }

            public List<Candidate> Candidates { get; set; }
            public Reach Reach { get; set; }

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

            public Int32 CompareTo(ReachSort other)
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
            public Candidate(Node node1, Node node2, Reach reach)
            {
                Node1 = node1;
                Node2 = node2;
                Reach = reach;
                Distance = node1.Location.Distance(node2.Location);
            }

            public Node Node1 { get; set; }
            public Node Node2 { get; set; }
            public Reach Reach { get; set; }

            public Double Distance { get; set; }

            public Int32 CompareTo(Candidate other)
            {
                return Distance.CompareTo(other.Distance);
            }

            public override String ToString()
            {
                return $"{Reach.Id} {Distance}";
            }
        }
    }
}
