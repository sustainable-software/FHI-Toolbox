using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.Indicators.EcosystemVitality.DamWizard;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Wizard;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class ConnectivityIndicatorViewModel : NavigationViewModel
    {
        private IList<Dam> _dams;
        private Double _passability;
        private bool _blockWindow;
        private double _weightP;
        private double _weightD;

        public ConnectivityIndicatorViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            RecalculateCommand = new RelayCommand(Recalculate);
            SetAllValuesCommand = new RelayCommand(SetAllValues);

            ImportCommand = new RelayCommand(Import);
            RiverImportCommand = new RelayCommand(RiverImport);
            DamImportCommand = new RelayCommand(DamImport);


            ConnectivityIndicator.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != "Reaches") return;
                _dams = null;
                RaisePropertyChanged("");
            };

            _weightP = ConnectivityIndicator.PotadromousWeight;
            _weightD = ConnectivityIndicator.DiadromousWeight;

        }
        
        public ICommand ImportCommand { get; }
        public ICommand RiverImportCommand { get; }

        public ICommand DamImportCommand { get; }

        public ICommand RecalculateCommand { get; }
        
        public ICommand SetAllValuesCommand { get; }
        
        public ConnectivityIndicator ConnectivityIndicator => Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
        
        public IList<Dam> Dams
        {
            get
            {
                if (_dams != null && _dams.Count > 0) return _dams;
                _dams = new List<Dam>();
                if (ConnectivityIndicator?.Reaches != null)
                    foreach (var reach in ConnectivityIndicator.Reaches)
                    foreach (var node in reach.Nodes)
                        if (node.Dam != null)
                            _dams.Add(node.Dam);
                return _dams;
            }
        }

        private bool _updateWeight;
        public Double WeightP
        {
            get => _weightP;
            set
            {
                Set(ref _weightP, value);
                CallLater(() => ConnectivityIndicator.PotadromousWeight = _weightP);

                if (_updateWeight) return;
                _updateWeight = true;
                WeightD = 1.0 - _weightP;
                _updateWeight = false;
            }
        }

        public Double WeightD
        {
            get => _weightD;
            set
            {
                Set(ref _weightD, value);
                CallLater(() => ConnectivityIndicator.DiadromousWeight = _weightD);

                if (_updateWeight) return;
                _updateWeight = true;
                WeightP = 1.0 - _weightD;
                _updateWeight = false;
            }
        }

        public Double Passability
        {
            get => _passability;
            set => Set(ref _passability, value);
        }

        public Boolean BlockWindow
        {
            get => _blockWindow;
            set => Set(ref _blockWindow, value);
        }

        private void SetAllValues()
        {
            foreach (var dam in _dams)
                dam.Passability = Passability;
        }
        
        private void Recalculate()
        {
            ConnectivityIndicator.Value = null;
        }

        private void Import()
        {
            var dialog = new ConnectivityImportWindow {Owner = Application.Current.MainWindow, DataContext = this};
            dialog.ShowDialog();
        }

        private void DamImport()
        {
            DamWizardViewModel.StartWizard(new Progress<WizardProgressEventArgs>(), b => BlockWindow = b);
        }

        private void RiverImport()
        {
            BlockWindow = true;
            var vm = new ImportReachesViewModel();
            var dialog = new ImportReachesWindow { Owner = Application.Current.MainWindow, DataContext = vm};
            if (dialog.ShowDialog() != true)
            {
                BlockWindow = false;
                return;
            }
            ConnectivityIndicator.Reaches = vm.Reaches;
            Model.Attributes.Wkid = vm.Wkid;
            BlockWindow = false;
        }
    }
}