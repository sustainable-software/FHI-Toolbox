using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using Fhi.Properties;
using FhiModel.Common;
using FhiModel.EcosystemServices;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using FhiModel.EcosystemVitality.FlowDeviation;
using FhiModel.EcosystemVitality.WaterQuality;
using Microsoft.Expression.Interactivity.Core;
using Newtonsoft.Json;

namespace Fhi.Controls.Network
{
    public class BasinMapViewModel : ViewModelBase
    {
        private readonly ObservableCollection<BasinMapLayer> _mapLayers;
        private Int32 _id;
        private Viewpoint _viewpoint;
        private Int32 _selectedId;
        private const Int32 LineWidth = 2;
        private const Int32 MarkerSize = 10;
        private SpatialReference _spatialReference;
        private GraphicsOverlay _markerOverlay;
        private String _selectedOverlay;
        private MapPoint _selectedPoint;
        private Map _map;
        
        public BasinMapViewModel(IEnumerable<BasinMapLayer> mapLayers)
        {
            _mapLayers = new ObservableCollection<BasinMapLayer>(mapLayers);
            RefreshCommand = new ActionCommand(_ => CallLater(async () => await Refresh()));

            CallLater(async () => await Refresh());
        }

        public BasinMapViewModel(IList<Reach> reaches, Int32 wkid, bool showNodes)
        {
            _spatialReference = new SpatialReference(wkid);
            _mapLayers = new ObservableCollection<BasinMapLayer> { new BasinMapReaches(reaches) };
            RefreshCommand = new ActionCommand(_ => CallLater(async () => await Refresh()));
            if (showNodes)
                _mapLayers.Add(new BasinMapNodes(reaches));

            CallLater(async () => await Refresh());
        }

        public ICommand RefreshCommand { get; }

        public Map Map
        {
            get => _map;
            set => Set(ref _map, value);
        }

        public GraphicsOverlayCollection Overlays { get; } = new GraphicsOverlayCollection();

        public Viewpoint Viewpoint
        {
            get => _viewpoint;
            set => Set(ref _viewpoint, value);
        }

        public Dictionary<Int32, ILocated> IdModelTracker { get; } = new Dictionary<Int32, ILocated>();
        public Dictionary<Int32, Graphic> IdMapTracker { get; } = new Dictionary<Int32, Graphic>();

        /// <summary>
        /// Id of the object selected by the user on the map.
        /// </summary>
        public Int32 SelectedId
        {
            get => _selectedId;
            set
            {
                if (IdMapTracker.ContainsKey(_selectedId))
                    IdMapTracker[_selectedId].IsSelected = false;
                if (IdMapTracker.ContainsKey(value))
                    IdMapTracker[value].IsSelected = true;
                Set(ref _selectedId, value);
            }
        }

        /// <summary>
        /// Tell the mapview to save the current map to a file (string).
        /// </summary>
        public Func<String, Task> Snapshot { get; set; }

        /// <summary>
        /// Overlay that the object selected by the user is in.
        /// </summary>
        public String SelectedOverlay
        {
            get => _selectedOverlay;
            set => Set(ref _selectedOverlay, value);
        }

        /// <summary>
        /// Geo location of the point on the map the user selected.
        /// </summary>
        public MapPoint SelectedPoint
        {
            get => _selectedPoint;
            set => Set(ref _selectedPoint, value);
        }

        public List<String> SelectionOverlays { get; } = new List<String>();

        public void AddLayer(BasinMapLayer layer)
        {
            _mapLayers.Add(layer);
        }

        public static BasemapSelection SelectedBasemap { get; set; }
        private BasemapSelection _selectedBasemap;

        public async Task Refresh()
        {
            try
            {
                if (_map == null || _selectedBasemap != SelectedBasemap || SelectedBasemap == null)
                {
                    if (SelectedBasemap == null)
                    {
                        try
                        {
                            if (!String.IsNullOrWhiteSpace(Settings.Default.Basemap))
                            {
                                SelectedBasemap =
                                    JsonConvert.DeserializeObject<BasemapSelection>(Settings.Default.Basemap);
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Basemap setting parsing: {ex.Message} {Settings.Default.Basemap}");
                        }

                        if (SelectedBasemap == null)
                        {
                            SelectedBasemap = BasemapSelection.Create().First(x => x.Default);
                        }
                    }

                    _selectedBasemap = SelectedBasemap;

                    if (_selectedBasemap.Builtin)
                    {
                        Map = new Map(_selectedBasemap.Basemap, 0, 0, 0);
                    }
                    else
                    {
                        var arcGisOnline = await ArcGISPortal.CreateAsync(new Uri("https://www.arcgis.com"));
                        var layerPortalItem =
                            await PortalItem.CreateAsync(arcGisOnline, _selectedBasemap.Id);
                        var customVectorTileLayer = new ArcGISVectorTiledLayer(layerPortalItem);
                        Map = new Map(new Basemap(customVectorTileLayer));
                    }
                }

                if (Map.LoadStatus != LoadStatus.Loaded)
                    await Map.LoadAsync();

                Overlays.Clear();
                Map.OperationalLayers.Clear();
                if (_mapLayers.Count == 0) return;

                if (_spatialReference == null && Globals.Model.Attributes.Wkid != 0)
                    _spatialReference = new SpatialReference(Globals.Model.Attributes.Wkid);

                foreach (var layer in _mapLayers)
                {
                    switch (layer)
                    {
                        case BasinMapModelLayer modelLayer:
                            {
                                var overlay = new GraphicsOverlay
                                {
                                    Id = modelLayer.Name,
                                    IsVisible = modelLayer.Visibility,
                                    Opacity = modelLayer.Opacity
                                };
                                var msr = _spatialReference;
                                foreach (var marker in modelLayer.GetItems())
                                {
                                    if (msr?.Wkid != marker.Location.Wkid && marker.Location.Wkid != 0)
                                        msr = new SpatialReference(marker.Location.Wkid);
                                    var id = _id++;
                                    IdModelTracker.Add(id, marker);
                                    var graphic = GetGraphic(marker, msr);
                                    graphic.Attributes.Add("id", id);
                                    IdMapTracker.Add(id, graphic);
                                    overlay.Graphics.Add(graphic);

                                }
                                if (overlay.Graphics.Count > 0)
                                    Overlays.Add(overlay);
                                break;
                            }
                        case BasinMapShapeLayer shapeLayer:
                            CallLater(async () =>
                            {
                                var featureLayer = await shapeLayer.GetLayer();
                                if (featureLayer == null) return;
                                Map.OperationalLayers.Add(featureLayer);
                            });
                            break;
                    }
                }

                if (_spatialReference != null)
                {
                    var envelope = new EnvelopeBuilder(_spatialReference);
                    foreach (var overlay in Overlays)
                    {
                        foreach (var graphic in overlay.Graphics)
                            envelope.UnionOf(graphic.Geometry.Extent);
                    }

                    envelope.Expand(1.2);
                    if (!envelope.IsEmpty)
                        Viewpoint = new Viewpoint(envelope.Extent);
                }
                else
                {
                    if (Map.OperationalLayers.Count > 0)
                    {
                        // want to get fancier here if we support the display of different shapefiles
                        Viewpoint = new Viewpoint(Map.OperationalLayers[0].FullExtent);
                    }
                }
            }
            catch (Exception) // disconnected from the internet
            {
                Map = null;
            }
        }

        public ILocated GetSelectedModel()
        {
            if (!IdModelTracker.ContainsKey(SelectedId)) return null;
            return IdModelTracker[SelectedId];
        }

        public void UpdateModelToMap()
        {
            var id = SelectedId;
            if (!IdMapTracker.ContainsKey(id)) return;

            var graphic = IdMapTracker[id];
            var model = IdModelTracker[id];
            Overlays[SelectedOverlay].Graphics.Remove(graphic);
            graphic.Symbol = GetSymbol(model.Location);
            graphic.Geometry = new MapPoint(model.Location.Longitude, model.Location.Latitude, _spatialReference);
            Overlays[SelectedOverlay].Graphics.Add(graphic);
        }

        public Int32 AddMarker(ILocated marker)
        {
            if (_markerOverlay == null)
            {
                _markerOverlay = new GraphicsOverlay { Id = "Markers" };
                Overlays.Insert(0, _markerOverlay);
            }

            var id = _id++;
            IdModelTracker.Add(id, marker);
            var graphic = new Graphic(new MapPoint(marker.Location.Longitude, marker.Location.Latitude, _spatialReference), GetSymbol(marker.Location, 10));
            graphic.Attributes.Add("id", id);
            _markerOverlay.Graphics.Add(graphic);
            IdMapTracker.Add(id, graphic);
            return id;
        }

        public void RemoveMarker(Int32 id)
        {
            _markerOverlay.Graphics.Remove(IdMapTracker[id]);
            IdMapTracker.Remove(id);
            IdModelTracker.Remove(id);
        }

        public void ZoomToMarker(Int32 id)
        {
            // for the spatial reference we've test with, this is a 1km window.
            var marker = IdModelTracker[id];
            Viewpoint = new Viewpoint(new Envelope(new MapPoint(marker.Location.Longitude, marker.Location.Latitude, _spatialReference), 1000, 1000));
        }

        public void ClearMarkers()
        {
            if (_markerOverlay == null) return;
            foreach (var graphic in _markerOverlay.Graphics)
                IdModelTracker.Remove((Int32)graphic.Attributes["id"]);
            _markerOverlay.Graphics.Clear();
            Overlays.Remove(_markerOverlay);
            _markerOverlay = null;
        }

        private Graphic GetGraphic(ILocated marker, SpatialReference s)
        {

            Graphic graphic;
            if (marker.Location.Symbol == Location.MapSymbol.Line)
            {
                var points = new PointCollection(s);
                points.AddPoints(marker.Location.Points.Select(x => new MapPoint(x.Longitude, x.Latitude)));
                graphic = new Graphic(new Polyline(points), GetSymbol(marker.Location));
            }
            else
            {
                graphic = new Graphic(new MapPoint(marker.Location.Longitude, marker.Location.Latitude, s), GetSymbol(marker.Location));
            }
            return graphic;
        }

        private Symbol GetSymbol(Location location, Int32 size = MarkerSize)
        {
            // todo: cache these?
            switch (location.Symbol)
            {
                case Location.MapSymbol.Circle:
                    return new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, location.Color, size);
                case Location.MapSymbol.Cross:
                    return new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, location.Color, size);
                case Location.MapSymbol.Diamond:
                    return new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, location.Color, size);
                case Location.MapSymbol.Square:
                    return new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, location.Color, size);
                case Location.MapSymbol.Triangle:
                    return new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, location.Color, size);
                case Location.MapSymbol.X:
                    return new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, location.Color, size);
                case Location.MapSymbol.Line:
                    return new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, location.Color, LineWidth);
                default:
                    return new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, Color.DeepPink, size * 10);
            }
        }
    }

    public abstract class BasinMapLayer
    {
        public abstract String Name { get; }
        public virtual Double Opacity { get; set; } = 1.0;
        public Boolean Visibility { get; set; } = true;

    }

    public class BasinMapShapeLayer : BasinMapLayer
    {
        private readonly String _assetName;
        public BasinMapShapeLayer(String assetName, String friendlyName)
        {
            Name = friendlyName;
            _assetName = assetName;
        }

        public override string Name { get; }

        public async Task<FeatureLayer> GetLayer()
        {
            var directory = Globals.Model.Assets.PathTo(_assetName);
            if (String.IsNullOrWhiteSpace(directory)) return null;
            var file = Directory.EnumerateFiles(directory, "*.shp").FirstOrDefault();
            if (String.IsNullOrWhiteSpace(file)) return null;
            var sf = await ShapefileFeatureTable.OpenAsync(file);
            var featureLayer = new FeatureLayer(sf)
            {
                Opacity = Opacity,
                Id = Name,
                IsVisible = Visibility
            };
            return featureLayer;
        }
    }

    public abstract class BasinMapModelLayer : BasinMapLayer
    {
        public abstract IEnumerable<ILocated> GetItems();
        protected static Color Zero { get; }
    }

    public class BasinMapReaches : BasinMapModelLayer
    {
        private readonly IEnumerable<Reach> _reaches;

        public BasinMapReaches() { }

        public BasinMapReaches(IEnumerable<Reach> reaches)
        {
            _reaches = reaches;
        }

        public override String Name => "Reaches";

        public override IEnumerable<ILocated> GetItems()
        {
            var reaches = _reaches;
            if (reaches == null)
            {
                var ci = Globals.Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
                reaches = ci?.Reaches;
                if (reaches == null)
                    yield break;
            }

            var line = new List<Location>();
            foreach (var reach in reaches)
            {
                
                //var sid = reach.Nodes[0].SegmentId;
                foreach (var node in reach.Nodes)
                {
                    /* this doesn't work yet.
                    if (sid != node.SegmentId)
                    {
                        var pmr = new MapReach
                        {
                            Name = reach.Id.ToString(),
                            Reach = reach,
                            Location = new Location
                            {
                                Points = line.Clone(),
                                Symbol = Location.MapSymbol.Line,
                                Color = ColorScheme.Lines.Colors[sid % ColorScheme.Lines.Colors.Count]
                            }
                        };
                        yield return pmr;

                        sid = node.SegmentId;
                        line.Clear();
                    }
                    */
                    line.Add(new Location { Longitude = node.Location.Longitude, Latitude = node.Location.Latitude });
                }
                
                var sid = 0;
                if (reach.SegmentId?.Count > 0)
                    sid = reach.SegmentId[0];
                var c = ColorScheme.Lines.Colors[sid % ColorScheme.Lines.Colors.Count];
                var mr = new MapReach
                {
                    Name = reach.Id.ToString(),
                    Reach = reach,
                    Location = new Location
                    {
                        Points = line.Clone(),
                        Symbol = Location.MapSymbol.Line,
                        Color = c
                    }
                };
                yield return mr;

                line.Clear();
            }
        }
    }

    public class MapReach : ModelBase, ILocated
    {
        public String Name { get; set; }
        public Location Location { get; set; }
        public Reach Reach { get; set; }
    }

    public class BasinMapNodes : BasinMapModelLayer
    {
        private readonly IEnumerable<Reach> _reaches;

        public BasinMapNodes() { }

        public BasinMapNodes(IEnumerable<Reach> reaches)
        {
            _reaches = reaches;
        }

        public override Double Opacity { get; set; } = 0.5;
        public override String Name => "Nodes";

        public override IEnumerable<ILocated> GetItems()
        {
            var reaches = _reaches;
            if (reaches == null)
            {
                var ci = Globals.Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
                reaches = ci?.Reaches;
                if (reaches == null)
                    yield break;
            }

            foreach (var reach in reaches)
            {
                foreach (var node in reach.Nodes)
                {
                    node.Location.Color = node.Dam == null ? Color.Black : Color.Red;
                    yield return node;
                }
            }
        }
    }

    public class BasinMapDams : BasinMapModelLayer
    {
        private readonly IEnumerable<Reach> _reaches;

        public BasinMapDams() { }

        public BasinMapDams(IEnumerable<Reach> reaches)
        {
            _reaches = reaches;
        }

        public override String Name => "Dams";

        public override IEnumerable<ILocated> GetItems()
        {
            var reaches = _reaches;
            if (reaches == null)
            {
                var ci = Globals.Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
                reaches = ci?.Reaches;
                if (reaches == null)
                    yield break;
            }

            foreach (var reach in reaches)
                foreach (var node in reach.Nodes)
                    if (node.Dam != null)
                    {
                        if (node.Dam.Location.Color == Zero)
                            node.Dam.Location.Color = Color.Red;
                        yield return node.Dam;
                    }
        }
    }

    public class BasinMapGauges : BasinMapModelLayer
    {
        public override String Name => "Gauges";

        public override IEnumerable<ILocated> GetItems()
        {
            var wqi = Globals.Model.EcosystemVitality.FetchIndicator<WaterQualityIndicator>();
            if (wqi == null) yield break;
            foreach (var gauge in wqi.Gauges)
            {
                if (gauge.Location.Latitude == 0 && gauge.Location.Longitude == 0) continue;
                if (gauge.Location.Color == Zero)
                    gauge.Location.Color = Color.IndianRed;
                yield return gauge;
            }
        }
    }

    public class BasinMapStations : BasinMapModelLayer
    {
        public override String Name => "Stations";

        public override IEnumerable<ILocated> GetItems()
        {
            var dvnf = Globals.Model.EcosystemVitality.FetchIndicator<FlowDeviationIndicator>();
            if (dvnf == null) yield break;
            foreach (var station in dvnf.Stations)
            {
                if (station.Location.Latitude == 0 && station.Location.Longitude == 0) continue;
                if (station.Location.Color == Zero)
                    station.Location.Color = Color.Brown;
                yield return station;
            }
        }
    }

    public class BasinMapSpatialUnits : BasinMapModelLayer
    {
        public override String Name => "Spatial Units";

        public override IEnumerable<ILocated> GetItems()
        {
            var indicators = Globals.Model.EcosystemServices.FetchIndicators<EcosystemServicesIndicator>();
            if (indicators == null) yield break;
            foreach (var indicator in indicators)
            {
                foreach (var su in indicator.SpatialUnits)
                {
                    if (su.Location.Latitude == 0 && su.Location.Longitude == 0) continue;
                    if (su.Location.Color == Zero)
                        su.Location.Color = Color.DarkOrange;
                    yield return su;
                }
            }
        }
    }
}