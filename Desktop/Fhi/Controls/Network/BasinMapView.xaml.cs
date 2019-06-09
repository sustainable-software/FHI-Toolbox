using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FhiModel.Common;
using Microsoft.Win32;

namespace Fhi.Controls.Network
{
    public partial class BasinMapView : UserControl
    {
        public BasinMapView()
        {
            InitializeComponent();
        }
        
        public static readonly DependencyProperty ViewpointProperty = DependencyProperty.Register(
            "Viewpoint", typeof(Viewpoint), typeof(BasinMapView), new PropertyMetadata(OnViewpointChanged));

        public Viewpoint Viewpoint
        {
            get { return (Viewpoint) GetValue(ViewpointProperty); }
            set { SetValue(ViewpointProperty, value); }
        }

        private static void OnViewpointChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is BasinMapView v && v.Viewpoint != null)
                v.MapView.SetViewpointAsync(v.Viewpoint);
        }

        private void Map_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is MapView map)) return;
            if (!(DataContext is BasinMapViewModel basinMapViewModel)) return;
            if (basinMapViewModel.Viewpoint != null)
                map.SetViewpointAsync(basinMapViewModel.Viewpoint);
            basinMapViewModel.Snapshot = Snapshot;
        }

        private async Task Snapshot(string filename)
        {
            var img = await MapView.ExportImageAsync();
            var pngImg = await img.GetEncodedBufferAsync();
            using (var file = File.Create(filename))
                await pngImg.CopyToAsync(file);
        }

        private async void Map_Export(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Map Image",
                Filter = "PNG Image (*.png)|*.png",
                DefaultExt = ".png"
            };
            if (dialog.ShowDialog() != true) return;
            await Snapshot(dialog.FileName);

        }

        private async void Map_OnGeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (!(sender is MapView map)) return;
            if (!(DataContext is BasinMapViewModel vm)) return;

            vm.SelectedPoint = e.Location;
            
            var tolerance = 10d; // Use larger tolerance for touch
            var maximumResults = 5; // Only return one graphic  
            var onlyReturnPopups = false; // Don't return only popups
            
            foreach (var overlay in vm.Overlays)
            {
                if (vm.SelectionOverlays.Count > 0 &&
                    !vm.SelectionOverlays.Contains(overlay.Id)) continue;
                try
                {
                    var results = await map.IdentifyGraphicsOverlayAsync(overlay, e.Position, tolerance,
                        onlyReturnPopups, maximumResults);
                    if (results.Graphics.Count == 0) continue;
                    {
                        foreach (var graphic in results.Graphics)
                        {
                            if (!graphic.Attributes.ContainsKey("id")) continue;
                            vm.SelectedOverlay = overlay.Id;
                            vm.SelectedId = (Int32) graphic.Attributes["id"];
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }           
        }

        private void Find_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is BasinMapViewModel basinMapViewModel)) return;
            if (basinMapViewModel.Viewpoint != null)
                MapView.SetViewpointAsync(basinMapViewModel.Viewpoint);
        }

        private void MapView_OnUnloaded(Object sender, RoutedEventArgs e)
        {
            // necessary for reasons not well understood by me. without this, an exception occurs in arc gis.
            MapView.Map = null;
            MapView.GraphicsOverlays = null;
        }
    }
}
