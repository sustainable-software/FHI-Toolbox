using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel;
using FhiModel.Common;
using FhiModel.EcosystemVitality;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using FhiModel.Services;
using Microsoft.Win32;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class BankModificationViewModel : NavigationViewModel
    {
        public BankModificationViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            ImportCommand = new RelayCommand(() => Import());
            SettingsCommand = new RelayCommand(Settings);

            if (BankModificationIndicator.Coverage.Count == 0)
            {
                foreach (var item in LandCoverTableService.GetTable("ESACCI LCCS"))
                    BankModificationIndicator.Coverage.Add(item.Clone());
            }
            LandCoverTableViewModel = new LandCoverTableViewModel(BankModificationIndicator.Coverage, true);
        }
        
        public BankModificationIndicator BankModificationIndicator => Model.EcosystemVitality.FetchIndicator<BankModificationIndicator>();

        public LandCoverTableViewModel LandCoverTableViewModel { get; }

        public ICommand ImportCommand { get; }

        public ICommand SettingsCommand { get; }

        private void Settings()
        {
            LandCoverTableViewModel.BufferDistance = BankModificationIndicator.BufferDistance;
            var dialog = new LandCoverTableSettingsWindow { Owner = Application.Current.MainWindow, DataContext = LandCoverTableViewModel };
            dialog.ShowDialog();
            BankModificationIndicator.BufferDistance = LandCoverTableViewModel.BufferDistance;
        }

        private void Import(String filename = null)
        {
            var rasterFile = filename;

            if (rasterFile == null)
            {
                LandCoverTableViewModel.BufferDistance = BankModificationIndicator.BufferDistance;
                LandCoverTableViewModel.ImportStep = false;
                var id = new LandCoverImportWindow { Owner = Application.Current.MainWindow, DataContext = LandCoverTableViewModel };
                if (id.ShowDialog() != true) return;
                BankModificationIndicator.BufferDistance = LandCoverTableViewModel.BufferDistance;
                var dialog = new OpenFileDialog
                {
                    Title = "Import Land Cover GeoTIFF",
                    DefaultExt = ".tif",
                    CheckFileExists = true
                };
                if (dialog.ShowDialog() != true)
                    return;
                rasterFile = dialog.FileName;
            }

            if (String.IsNullOrWhiteSpace(rasterFile)) return;
           
            GdalConfiguration.ConfigureOgr();
            GdalConfiguration.ConfigureGdal();

            var ci = Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
            if (!(ci?.Reaches?.Count > 0))
            {
                MessageBox.Show(
                    "You must have reaches for the Connectivity Indicator imported in order to process the channel modification.");
                return;
            }
            
            var raster = Gdal.Open(rasterFile, Access.GA_ReadOnly);
            
            var progress = new ProgressDialog
            {
                Label = $"Processing Channel Modification:\n{raster.GeoTiffDescription()}",
                Owner = Application.Current.MainWindow,
                IsCancellable = true,
                IsIndeterminate = false
            };

            var result = new Dictionary<byte, int>();
            progress.Execute((ct, p) =>
            {
                var ip = new Progress<int>();
                var reachNumber = 0;
                var sr = new SpatialReference(ArcGisUtils.WkidToWktxt(Model.Attributes.Wkid));
                
                foreach (var reach in ci.Reaches)
                {
                    var line = new Geometry(wkbGeometryType.wkbLineString);
                   
                    line.AssignSpatialReference(sr);
                    foreach (var node in reach.Nodes)
                        line.AddPoint_2D(node.Location.Longitude, node.Location.Latitude);
                    // convert from the user specified buffer distance (in meters) to the projected buffer units. no idea how well this works in general.
                    var distance = sr.IsGeographic() == 1 ?  BankModificationIndicator.BufferDistance / 110000 *  Math.Abs(Math.Cos(sr.GetAngularUnits() * reach.Nodes[0].Location.Latitude)) : BankModificationIndicator.BufferDistance / sr.GetLinearUnits();
                    GdalRasterUtils.ReadRasterRows(raster, line.Buffer(distance, 5), out var tally, ct, ip);

                    foreach (var key in tally.Keys)
                    {
                        if (!result.ContainsKey(key)) result[key] = 0;
                        result[key] += tally[key];
                    }
                    
                    p.Report((int)(100.0 * reachNumber++ / ci.Reaches.Count));
                }
            });

            var message = new TextWindow
            {
                Owner = Application.Current.MainWindow,
                Text = "Histogram of extracted data:\n" + GdalRasterUtils.DumpResult(result) +
                       "\n\n--- Raster File Details ---\n" + GdalRasterUtils.DumpDatasetInfo(raster)
            };
            message.ShowDialog();

            BankModificationIndicator.Notes = message.Text;
            var coverage = BankModificationIndicator.Coverage.Clone();
            foreach (var item in coverage)
            {
                item.Area = 0.0;
                if (item.Mapping == null) continue;

                foreach (var map in item.Mapping)
                    if (result.ContainsKey(map))
                        item.Area += result[map];
            }
            var total = coverage.Sum(x => x.Area);
            for (var i = 0; i < coverage.Count; i++)
                BankModificationIndicator.Coverage[i].Area = 100.0 * coverage[i].Area / total;
        }
    }
}