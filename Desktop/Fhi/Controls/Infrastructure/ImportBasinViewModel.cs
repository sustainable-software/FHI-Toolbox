using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using Microsoft.Win32;

namespace Fhi.Controls.Infrastructure
{
    public class ImportBasinViewModel : ViewModelBase
    {
        public ImportBasinViewModel()
        {
            ImportShapefileCommand = new RelayCommand(ImportShapefile);
            ExportShapefileCommand = new RelayCommand(ExportShapefile);
        }

        public ICommand ImportShapefileCommand { get; }
        public ICommand ExportShapefileCommand { get; }

        private void ImportShapefile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Shapefile",
                Filter = "GIS Shapefile (*.shp)|*.shp",
                DefaultExt = ".shp"
            };
            if (dialog.ShowDialog() != true) return;
            if (Model.Assets.Exists("BasinShapefile"))
                Model.Assets.Delete("BasinShapefile");
            Globals.Model.Assets.Create(Path.GetDirectoryName(dialog.FileName), Path.GetFileNameWithoutExtension(dialog.FileName), "BasinShapefile");
            MessageBox.Show("Successfully imported the basin shapefile.");
        }

        private void ExportShapefile()
        {
            if (!Model.Assets.Exists("BasinShapefile"))
            {
                MessageBox.Show("There is no shapefile in this assessment.");
                return;
            }
            var dialog = new SaveFileDialog
            {
                Title = "Save Shapefile",
                Filter = "GIS Shapefile (*.shp)|*.shp",
                DefaultExt = ".shp",
                CheckFileExists = false,
                CheckPathExists = true
            };
            if (dialog.ShowDialog() != true) return;

            var from = Model.Assets.PathTo("BasinShapefile");
            if (!Directory.Exists(from))
            {
                MessageBox.Show("There was an error reading the shapefile.");
                return;
            }

            try
            {
                Copy(from, Path.GetDirectoryName(dialog.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying shapefile {ex.Message}");
            }
        }

        private static void Copy(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
