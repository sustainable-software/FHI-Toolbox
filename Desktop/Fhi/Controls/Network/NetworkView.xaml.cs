using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.Utils;

namespace Fhi.Controls.Network
{
    public partial class NetworkView : UserControl
    {
        public NetworkView()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ViewpointProperty = DependencyProperty.Register(
            "Viewpoint", typeof(Viewpoint), typeof(NetworkView), new PropertyMetadata(OnViewpointChanged));

        public Viewpoint Viewpoint
        {
            get { return (Viewpoint) GetValue(ViewpointProperty); }
            set { SetValue(ViewpointProperty, value); }
        }

        private static void OnViewpointChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is NetworkView v && v.Viewpoint != null)
                v.MapView.SetViewpointAsync(v.Viewpoint);
        }

        private void Map_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is MapView map)) return;
            if (!(DataContext is NetworkViewModel networkViewModel)) return;
            if (networkViewModel.Viewpoint != null)
                map.SetViewpointAsync(networkViewModel.Viewpoint);
        }

        private async void Map_OnGeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (!(sender is MapView map)) return;
            if (!(DataContext is NetworkViewModel networkViewModel)) return;
            
            var overlayGrid = (System.Windows.Controls.Border)map.Overlays.Items[0];
            var tolerance = 10d; // Use larger tolerance for touch
            var maximumResults = 5; // Only return one graphic  
            var onlyReturnPopups = false; // Don't return only popups

            // Use the following method to identify graphics in a specific graphics overlay
            var identifyResults = await map.IdentifyGraphicsOverlayAsync(
                map.GraphicsOverlays[networkViewModel.RiverBasinOverlay],
                e.Position,
                tolerance,
                onlyReturnPopups,
                maximumResults);
    
            // Check if we got results
            if (identifyResults.Graphics.Count > 0)
            {
                map.GraphicsOverlays[networkViewModel.RiverBasinOverlay].ClearSelection();
                mapTip.Visibility = Visibility.Collapsed;
                foreach (var gr in identifyResults.Graphics)
                {
                    if (gr.Attributes.ContainsKey("Id"))
                        gr.IsSelected = !gr.IsSelected;
                    if (gr.IsSelected)
                    {
                        if (gr.Attributes.ContainsKey("Id"))
                            networkViewModel.NetworkObjectSelected((String)gr.Attributes["Id"]);

                        mapTip.Visibility = Visibility.Visible;
                        tipLine1.Text = networkViewModel.SelectedNetworkObject.ToString();

                        switch (gr.Geometry)
                        {
                            case Polyline line:
                                GeoView.SetViewOverlayAnchor(overlayGrid, line.Parts[0].StartPoint);
                                break;
                            case MapPoint point:
                                GeoView.SetViewOverlayAnchor(overlayGrid, point);
                                break;
                        }
                    }
                }
            }
    
        }

        private void Find_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is NetworkViewModel networkViewModel)) return;
            if (networkViewModel.Viewpoint != null)
                MapView.SetViewpointAsync(networkViewModel.Viewpoint);
        }


        private void MapView_OnUnloaded(Object sender, RoutedEventArgs e)
        {
            // necessary for reasons not well understood by me. without this, an exception occurs in arc gis.
            MapView.Map = null;
        }
    }
}
