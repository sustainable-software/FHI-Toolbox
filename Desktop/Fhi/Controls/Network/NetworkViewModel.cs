using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;

namespace Fhi.Controls.Network
{
    public class NetworkViewModel : ViewModelBase
    {
        private readonly Map _map = new Map(BasemapType.Oceans, 0, 0, 0);
        private const String _riverBasinOverlay = "RiverBasin";
        private readonly GraphicsOverlay _overlay = new GraphicsOverlay {Id = _riverBasinOverlay};
        private readonly GraphicsOverlayCollection _overlays = new GraphicsOverlayCollection();
        private object _selectedNetworkObject;
        private Viewpoint _viewpoint;
        private readonly Dictionary<Int32, Reach> _reachLookup = new Dictionary<Int32, Reach>();
        private readonly Dictionary<Int32, Node> _nodeLookup = new Dictionary<Int32, Node>();
        private int _wkid;

        public NetworkViewModel()
        {
            if (!Overlays.Contains(_overlay))
                Overlays.Add(_overlay);
        }
        
        public NetworkViewModel(IList<Reach> reaches, int wkid, bool showNodes = false)
        {
            _wkid = wkid;
            
            if (!Overlays.Contains(_overlay))
                Overlays.Add(_overlay);
            CallLater(() => AddNetworkToMap(reaches, wkid, showNodes));
        }

        public String RiverBasinOverlay => _riverBasinOverlay;

        public GraphicsOverlayCollection Overlays => _overlays;
        
        public Viewpoint Viewpoint
        {
            get => _viewpoint;
            set => Set(ref _viewpoint, value);
        }

        public Boolean HasBasinShapfile { get; set; }
        
        public Map Map => _map;

        private Graphic _previousMarker;
        
        public void AddMarker(Location location)
        {
           
            var markerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, Color.Cyan, 15);
            var spatialReference = new SpatialReference(_wkid);
            if (_previousMarker != null)
                _overlay.Graphics.Remove(_previousMarker);
            var gr = new Graphic(new MapPoint(location.Longitude, location.Latitude, spatialReference), markerSymbol);
            _overlay.Graphics.Add(gr);
            
            _previousMarker = gr;
            Viewpoint = new Viewpoint(new MapPoint(location.Longitude, location.Latitude, spatialReference), 0.5);           
        }

        public void NetworkObjectSelected(String id)
        {
            if (id.StartsWith("Node:"))
            {
                SelectedNetworkObject = _nodeLookup[int.Parse(id.Replace("Node:", ""))];
            }
            else if (id.StartsWith("Reach:"))
            {
                SelectedNetworkObject = _reachLookup[int.Parse(id.Replace("Reach:", ""))];
            }
        }
        
        public Object SelectedNetworkObject
        {
            get => _selectedNetworkObject;
            set => Set(ref _selectedNetworkObject, value);
        }
    
        public void AddNetworkToMap(IList<Reach> reaches, int wkid, bool showNodes = false)
        {
            _overlay.Graphics.Clear();
            var lineSymbols = ColorScheme.Lines.Colors.Select(color => new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, 2)).ToList();
            var damSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 7);
            var nodeSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Black, 4);
            var spatialReference = new SpatialReference(wkid);
            
            var nodeId = 1000;
            foreach (var reach in reaches)
            {
                var points = new PointCollection(spatialReference);
                _reachLookup[reach.Id] = reach;
                
                foreach (var node in reach.Nodes)
                {
                    points.Add(new MapPoint(node.Location.Longitude, node.Location.Latitude));
                    if (node.Dam != null)
                    {
                        var sg = new Graphic(new MapPoint(node.Location.Longitude, node.Location.Latitude, spatialReference), damSymbol);
                        _overlay.Graphics.Add(sg);
                    }
                    else if (showNodes)
                    {
                        var sg = new Graphic(new MapPoint(node.Location.Longitude, node.Location.Latitude, spatialReference), nodeSymbol);
                        _overlay.Graphics.Add(sg);
                        var id = nodeId++;
                        sg.Attributes.Add("Id", $"Node:{id}");
                        _nodeLookup[id] = node;
                    }
                }
                var polyline = new Polyline(points);
                // Create the graphic with polyline and symbol
                var sid = reach.SegmentId?[0] ?? 0;
                var index = sid % lineSymbols.Count;
                var graphic = new Graphic(polyline, lineSymbols[index]);
                graphic.Attributes.Add("Id", $"Reach:{reach.Id}");
                // Add graphic to the graphics overlay
                _overlay.Graphics.Add(graphic);
            }
        
            // Get all of the graphics contained in the graphics overlay
            GraphicCollection myGraphicCollection = _overlay.Graphics;

            // Create a new envelope builder using the same spatial reference as the graphics
            EnvelopeBuilder myEnvelopeBuilder = new EnvelopeBuilder(spatialReference);

            // Loop through each graphic in the graphic collection
            foreach (Graphic oneGraphic in myGraphicCollection)
            {
                // Union the extent of each graphic in the envelope builder
                myEnvelopeBuilder.UnionOf(oneGraphic.Geometry.Extent);
            }

            // Expand the envelope builder by 30%
            myEnvelopeBuilder.Expand(1.2);

            // Adjust the viewable area of the map to encompass all of the graphics in the
            // graphics overlay plus an extra 30% margin for better viewing
            try
            {
                Viewpoint = new Viewpoint(myEnvelopeBuilder.Extent);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }
    }
    
}