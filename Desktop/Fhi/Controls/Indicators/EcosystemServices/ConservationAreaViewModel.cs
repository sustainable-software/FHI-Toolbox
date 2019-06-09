using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using FhiModel.EcosystemServices;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using Microsoft.Win32;

namespace Fhi.Controls.Indicators.EcosystemServices
{
    public class ConservationAreaViewModel : NavigationViewModel
    {
        private String _assetName;

        public ConservationAreaViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back) : base(navigate, back)
        {
            ImportCommand = new RelayCommand(async () => await Import());
            ClearCommand = new RelayCommand(Clear);
        }

        public ICommand ImportCommand { get; }
        public ICommand ClearCommand { get; }

        public ConservationAreaIndicator ConservationAreaIndicator =>
            Model.EcosystemServices.FetchIndicator<ConservationAreaIndicator>();

        private async Task Import()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Add conservation area shapefile",
                Filter = "GIS Shapefile (*.shp)|*.shp",
                DefaultExt = ".shp"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;

            var rawAssetName = Path.GetFileNameWithoutExtension(filename);
            if (ConservationAreaIndicator.AssetNames.Contains(rawAssetName))
            {
                MessageBox.Show("You have already added a shapefile by that name.");
                return;
            }

            _assetName = rawAssetName;
            int i = 1;
            while (Model.Assets.Exists(_assetName))
            {
                _assetName = $"{rawAssetName}_{i++}";
            }
            ConservationAreaIndicator.AssetNames.Add(_assetName);
            Model.Assets.Create(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename), _assetName);

            try
            {
                var sf = await ShapefileFeatureTable.OpenAsync(filename);
                if (!sf.HasGeometry)
                {
                    MessageBox.Show($"Shapefile {filename} does not have a geometry.");
                    Clear();
                    return;
                }
                var directory = Globals.Model.Assets.PathTo("BasinShapefile");
                if (String.IsNullOrWhiteSpace(directory))
                {
                    MessageBox.Show("You need to have imported a basin shapefile.");
                    Clear();
                    return;
                }
                var ci = Globals.Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
                if (ci?.Reaches == null)
                {
                    MessageBox.Show(
                        "You must import a river network for the Connectivity Indicator in Ecosystem Vitality.");
                    Clear();
                    return;
                }

                var progress = new ProgressDialog
                {
                    Label = $"Processing conservation areas",
                    Owner = Application.Current.MainWindow,
                    IsCancellable = true,
                    IsIndeterminate = false
                };

                progress.Execute((ct, p) => Update(sf, ct, p));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Open of shapefile {filename} failed: {ex.Message}");
                Clear();
            }
        }

        private void Update(ShapefileFeatureTable sf, CancellationToken ct, IProgress<int> p)
        {
            Task.Run(async () =>
            {
                var directory = Globals.Model.Assets.PathTo("BasinShapefile");
                if (String.IsNullOrWhiteSpace(directory))
                {
                    MessageBox.Show("You need to have imported a basin shapefile.");
                    Clear();
                    return;
                }

                var file = Directory.EnumerateFiles(directory, "*.shp").FirstOrDefault();
                if (String.IsNullOrWhiteSpace(file))
                {
                    MessageBox.Show("Error reading the basin shapefile.");
                    Clear();
                    return;
                }

                var ci = Globals.Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
                var reaches = ci?.Reaches;
                if (reaches == null)
                {
                    MessageBox.Show(
                        "You must import a river network for the Connectivity Indicator in Ecosystem Vitality.");
                    Clear();
                    return;
                }

                var bsf = await ShapefileFeatureTable.OpenAsync(file);
                var allQuery = new QueryParameters
                {
                    Geometry = bsf.Extent,
                    SpatialRelationship = SpatialRelationship.Contains
                };
                ConservationAreaIndicator.TotalProtectedArea = 0;
                ConservationAreaIndicator.TotalArea = 0;

                foreach (var mapFeature in await bsf.QueryFeaturesAsync(allQuery))
                {
                    if (ct.IsCancellationRequested)
                    {
                        Clear();
                        return;
                    }

                    ConservationAreaIndicator.TotalArea += GeometryEngine.AreaGeodetic(mapFeature.Geometry);

                    var areaFeatures = await sf.QueryFeaturesAsync(new QueryParameters
                    {
                        Geometry = mapFeature.Geometry,
                        SpatialRelationship = SpatialRelationship.Contains
                    });
                    foreach (var feature in areaFeatures)
                    {
                        ConservationAreaIndicator.TotalProtectedArea += GeometryEngine.AreaGeodetic(feature.Geometry);
                        if (ct.IsCancellationRequested)
                        {
                            Clear();
                            return;
                        }
                    }
                }

                p.Report(25);

                // todo: this is not an efficient algorithm
                var pb = new PolylineBuilder(new SpatialReference(Model.Attributes.Wkid));
                foreach (var reach in reaches)
                    pb.AddPart(
                        reach.Nodes.Select(node => new MapPoint(node.Location.Longitude, node.Location.Latitude)));
                var mapGeometry = pb.ToGeometry();
                ConservationAreaIndicator.TotalLength = GeometryEngine.Length(mapGeometry);

                if (ct.IsCancellationRequested)
                {
                    Clear();
                    return;
                }

                var lengthFeatures = await sf.QueryFeaturesAsync(new QueryParameters
                {
                    Geometry = sf.Extent,
                    SpatialRelationship = SpatialRelationship.Contains
                });

                var tpl = 0.0;
                var reachNumber = 0;
                foreach (var reach in reaches)
                {
                    var poly = new PolylineBuilder(new SpatialReference(Model.Attributes.Wkid));
                    poly.AddPart(reach.Nodes.Select(node =>
                        new MapPoint(node.Location.Longitude, node.Location.Latitude)));
                    var pg = poly.ToGeometry();
                    foreach (var feature in lengthFeatures)
                    {
                        var intersection = GeometryEngine.Intersection(pg, feature.Geometry) as Polyline;
                        if (intersection?.Parts.Count > 0)
                            tpl += GeometryEngine.Length(pg);
                        if (ct.IsCancellationRequested)
                        {
                            Clear();
                            return;
                        }
                    }

                    p.Report(25 + (int) (75.0 * reachNumber++ / reaches.Count));
                }

                ConservationAreaIndicator.TotalProtectedLength = tpl;
            }, ct).Wait(ct);
        }

        private void Clear()
        {
            ConservationAreaIndicator.TotalArea = null;
            ConservationAreaIndicator.TotalProtectedArea = null;
            ConservationAreaIndicator.TotalLength = null;
            ConservationAreaIndicator.TotalProtectedLength = null;
            ConservationAreaIndicator.Value = null;
            ConservationAreaIndicator.AssetNames.Remove(_assetName);
            Model.Assets.Delete(_assetName);
        }
    }
}
