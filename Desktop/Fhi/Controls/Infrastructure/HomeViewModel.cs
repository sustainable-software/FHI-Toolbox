using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.Indicators.Core;
using Fhi.Controls.Indicators.EcosystemServices;
using Fhi.Controls.Indicators.EcosystemVitality;
using Fhi.Controls.Indicators.Governance;
using Fhi.Controls.MVVM;
using Fhi.Controls.Network;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using Microsoft.Win32;

namespace Fhi.Controls.Infrastructure
{
    public class HomeViewModel : NavigationViewModel
    {
        
        private readonly Action<NavigationViewModel> _navigate;
        private Boolean _modified;
        
        private EcosystemServicesViewModel _ecosystemServicesViewModel;
        private EcosystemVitalityViewModel _ecosystemVitalityViewModel;
        private GovernanceViewModel _governanceViewModel;
        private CoreViewModel _coreViewModel;

        private EcosystemServicesViewModel EcosystemServicesViewModel =>
            _ecosystemServicesViewModel = _ecosystemServicesViewModel ?? (_ecosystemServicesViewModel = new EcosystemServicesViewModel(_navigate, this));
        private EcosystemVitalityViewModel EcosystemVitalityViewModel =>
            _ecosystemVitalityViewModel = _ecosystemVitalityViewModel ?? (_ecosystemVitalityViewModel = new EcosystemVitalityViewModel(_navigate, this));
        private GovernanceViewModel GovernanceViewModel => 
            _governanceViewModel ?? (_governanceViewModel = new GovernanceViewModel(_navigate, this));
        private CoreViewModel CoreViewModel => 
            _coreViewModel ?? (_coreViewModel = new CoreViewModel(_navigate, this));

        public HomeViewModel() { }

        public HomeViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back) : base(navigate, back)
        {
            _navigate = navigate;
            BackstageCommand = new RelayCommand((() => Navigate(Backstage)));

            EcosystemServicesCommand = new RelayCommand(() => Navigate(EcosystemServicesViewModel));
            EcosystemVitalityCommand = new RelayCommand(() => Navigate(EcosystemVitalityViewModel));
            GovernanceCommand = new RelayCommand(() => Navigate(GovernanceViewModel));
            CoreCommand = new RelayCommand(() => Navigate(CoreViewModel));
            

            BasinMapViewModel = new BasinMapViewModel(new List<BasinMapLayer>
            {
               new BasinMapReaches(),
               new BasinMapNodes(),
               new BasinMapGauges { Visibility = false },
               new BasinMapSpatialUnits { Visibility = false },
               new BasinMapStations { Visibility = false },
               new BasinMapShapeLayer("BasinShapefile", "Basin")
            });

            ReportCommand = new RelayCommand(async () => await Report());

            Globals.ModelChanged += OnModelChanged;

            Globals.ModelModified += (sender, args) =>
            {
                Modified = true;
                Globals.Model.Attributes.Modified = DateTime.Now;
                //UpdateModel(); // this happens too often, need to create ModelMapInformation
            };

            OnModelChanged(null, null);
        }

        private void OnModelChanged(object sender, EventArgs args)
        {
            var ci = Globals.Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
            ci.PropertyChanged += (s, a) =>
            {
                if (a.PropertyName == "Reaches") UpdateModel();
            };
            Globals.Model.Assets.PropertyChanged += (s, a) =>
            {
                if (a.PropertyName == "Assets") UpdateModel();
            };
            Modified = false;
            UpdateModel();
        }

        public ICommand EcosystemServicesCommand { get; }
        public ICommand EcosystemVitalityCommand { get; }
        public ICommand GovernanceCommand { get; }
        public ICommand CoreCommand { get; }
        public ICommand BackstageCommand { get; }
        public ICommand ReportCommand { get; }
        
        public BackstageViewModel Backstage { get; set; } // this is a bit of a hack to deal with the root of the nav tree.

        public BasinMapViewModel BasinMapViewModel { get; }

        public Boolean Modified
        {
            get => _modified;
            set => Set(ref _modified, value);
        }

        private async Task Report()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Create Assessment Report",
                Filter = "Microsoft Word (*.docx)|*.docx",
                DefaultExt = ".docx"
            };
            if (dialog.ShowDialog() != true) return;
            await Utils.Report.Overview(Globals.Model, BasinMapViewModel, dialog.FileName);
            try
            {
                Process.Start(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't start Microsoft Word on {dialog.FileName}: {ex.Message}");
            }

        }

        private void UpdateModel()
        {
            BasinMapViewModel.Refresh();
        }
    }
}
