using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.EcosystemVitality;
using FhiModel.Services;
using Microsoft.Win32;
using OSGeo.GDAL;
using OSGeo.OGR;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class LandCoverViewModel : NavigationViewModel
    {
        public LandCoverViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            ImportCommand = new RelayCommand(() => Import());
            SettingsCommand = new RelayCommand(Settings);

            if (LandCoverIndicator.Coverage.Count == 0)
            {
                foreach (var item in LandCoverTableService.GetTable("ESACCI LCCS"))
                    LandCoverIndicator.Coverage.Add(item.Clone());
            }
            LandCoverTableViewModel = new LandCoverTableViewModel(LandCoverIndicator.Coverage);
        }

        public LandCoverIndicator LandCoverIndicator => Model.EcosystemVitality.FetchIndicator<LandCoverIndicator>();

        public LandCoverTableViewModel LandCoverTableViewModel { get; }

        public ICommand ImportCommand { get; }

        public ICommand SettingsCommand { get; }

        private void Settings()
        {
            var dialog = new LandCoverTableSettingsWindow { Owner = Application.Current.MainWindow, DataContext = LandCoverTableViewModel };
            dialog.ShowDialog();
        }

        private void Import(String filename = null)
        {
            var rasterFile = filename;
            if (rasterFile == null)
            {
                LandCoverTableViewModel.ImportStep = false;
                var id = new LandCoverImportWindow { Owner = Application.Current.MainWindow, DataContext = LandCoverTableViewModel };
                if (id.ShowDialog() != true) return;
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

            
            
            var directory = Globals.Model.Assets.PathTo("BasinShapefile");
            if (String.IsNullOrWhiteSpace(directory))
            {
                MessageBox.Show(
                    "The assessment needs to have a basin shapefile before the land cover can be computed.");
                return;
            }
            var shapeFile = Directory.EnumerateFiles(directory, "*.shp").FirstOrDefault();
            if (String.IsNullOrWhiteSpace(shapeFile))
            {
                MessageBox.Show("Basin shapefile in model seems to have an error.");
                return;
            }
            
            GdalConfiguration.ConfigureOgr();
            GdalConfiguration.ConfigureGdal();

            var dataSource = Ogr.Open(shapeFile, 0);
            var layer = dataSource.GetLayerByIndex(0);
            var geometry = layer.GetNextFeature().GetGeometryRef();
            var raster = Gdal.Open(rasterFile, Access.GA_ReadOnly);

            var progress = new ProgressDialog
            {
                Label = $"Processing Land Cover:\n{raster.GeoTiffDescription()}",
                Owner = Application.Current.MainWindow,
                IsCancellable = true,
                IsIndeterminate = false
            };

            Dictionary<byte, int> result = null;
            progress.Execute((ct, p) => GdalRasterUtils.ReadRasterRows(raster, geometry, out result, ct, p));

            var message = new TextWindow
            {
                Owner = Application.Current.MainWindow,
                Text = "Histogram of extracted data:\n" + GdalRasterUtils.DumpResult(result) +
                       "\n\n--- Raster File Details ---\n" + GdalRasterUtils.DumpDatasetInfo(raster)
            };
            message.ShowDialog();
            LandCoverIndicator.Notes = message.Text;
            var coverage = LandCoverIndicator.Coverage.Clone();
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
                LandCoverIndicator.Coverage[i].Area = 100.0 * coverage[i].Area / total;
        }

    }
}