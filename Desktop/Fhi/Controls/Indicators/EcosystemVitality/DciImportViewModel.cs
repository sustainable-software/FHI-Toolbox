using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel;
using FhiModel.DendreticConnectivity;
using Microsoft.Win32;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class DciImportViewModel : ViewModelBase
    {
        public DciImportViewModel()
        {
            ImportRiversCommand = new RelayCommand(ImportRivers);
            ImportDamsCommand = new RelayCommand(ImportDams);
            OkCommand = new RelayCommand(Ok);
        }

        public ICommand ImportRiversCommand { get; }
        public ICommand ImportDamsCommand { get; }
        public ICommand OkCommand { get; }

        public Double? Latitude
        {
            get => _latitude;
            set
            {
                Set(ref _latitude, value);
                RaisePropertyChanged(nameof(Valid));
            }
        }

        public Double? Longitude
        {
            get => _longitude;
            set
            {
                Set(ref _longitude, value);
                RaisePropertyChanged(nameof(Valid));
            }
        }

        public String Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        public String RiverFile
        {
            get => _riverFile;
            set => Set(ref _riverFile, value);
        }

        public String DamFile
        {
            get => _damFile;
            set => Set(ref _damFile, value);
        }

        public Boolean Valid => _riverTable != null && _damTable != null && Latitude.HasValue && Longitude.HasValue;

        private List<RiverTableRow> _riverTable;
        private List<DamTableRow> _damTable;
        private string _message;
        private double? _longitude;
        private double? _latitude;
        private string _riverFile;
        private string _damFile;

        private void Ok()
        {
            if (Longitude != null && Latitude != null)
                Globals.Model.Network =
                    new FhiModel.DendreticConnectivity.Network(_riverTable, _damTable, new NetworkPoint(Longitude.Value, Latitude.Value));
        }

        private void ImportRivers()
        {
            var filename = BrowseForFile("Import Rivers CSV", Globals.Model.Network?.RiverFile);
            if (String.IsNullOrWhiteSpace(filename)) return;
            try
            {
                _riverTable = RiverTableRow.Create(filename, 0, 1);
                RiverFile = Path.GetFileNameWithoutExtension(filename);
                Message = $"Read {_riverTable.Count} rows from {filename}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to read {filename}: {ex.Message}");
            }
            RaisePropertyChanged(nameof(Valid));
        }

        private void ImportDams()
        {
            var filename = BrowseForFile("Import Dams CSV", Globals.Model.Network?.DamFile);
            if (String.IsNullOrWhiteSpace(filename)) return;
            try
            {
                _damTable = DamTableRow.Create(filename);
                DamFile = Path.GetFileNameWithoutExtension(filename);
                Message = $"Read {_damTable.Count} rows from {filename}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to read {filename}: {ex.Message}");
                
            }
            RaisePropertyChanged(nameof(Valid));
        }

        private string BrowseForFile(string title, string hint)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                DefaultExt = ".csv",
                FileName = hint,
                CheckFileExists = true
            };
            if (dialog.ShowDialog() != true)
                return null;
            return dialog.FileName;
        }
    }
}
