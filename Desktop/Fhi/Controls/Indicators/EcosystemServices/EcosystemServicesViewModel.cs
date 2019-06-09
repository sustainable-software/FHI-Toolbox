using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemServices;
using Microsoft.Win32;
using OfficeOpenXml;

namespace Fhi.Controls.Indicators.EcosystemServices
{
    public class EcosystemServicesViewModel : NavigationViewModel
    {
        private readonly Action<NavigationViewModel> _navigate;
        
        private SummaryViewModel _selectedViewModel;

        private CulturalSummaryViewModel _culturalSummary;
        private EcosystemServicesSummaryViewModel _regulationSummary;
        private EcosystemServicesSummaryViewModel _provisioningSummary;

        private CulturalSummaryViewModel CulturalSummary =>
            _culturalSummary ?? (_culturalSummary = new CulturalSummaryViewModel(_navigate, this));
        private EcosystemServicesSummaryViewModel RegulationSummary =>
            _regulationSummary ?? (_regulationSummary = new EcosystemServicesSummaryViewModel("Regulation", _navigate, this));
        private EcosystemServicesSummaryViewModel ProvisioningSummary =>
            _provisioningSummary ?? (_provisioningSummary = new EcosystemServicesSummaryViewModel("Provisioning", _navigate, this));

        public EcosystemServicesViewModel() { }

        public EcosystemServicesViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back) 
            : base(navigate, back)
        {

            _navigate = navigate;
            
            CulturalCommand = new RelayCommand(() => SelectedViewModel = CulturalSummary);
            RegulationCommand = new RelayCommand(() => SelectedViewModel = RegulationSummary);
            ProvisioningCommand = new RelayCommand(() => SelectedViewModel = ProvisioningSummary);

            

            SelectedViewModel = RegulationSummary;
        }
        
        public ICommand CulturalCommand { get; }
        public ICommand RegulationCommand { get; }
        public ICommand ProvisioningCommand { get; }

        public IIndicator Pillar => Model?.EcosystemServices;
        public IIndicator Cultural => Pillar.FetchIndicator<Indicator>("Cultural");
        public IIndicator Provisioning => Pillar.FetchIndicator<Indicator>("Provisioning");
        public IIndicator Regulation => Pillar.FetchIndicator<Indicator>("Regulation");

        public Boolean CulturalSelected => SelectedViewModel == _culturalSummary;
        public Boolean ProvisioningSelected => SelectedViewModel == _provisioningSummary;
        public Boolean RegulationSelected => SelectedViewModel == _regulationSummary;
       
        public SummaryViewModel SelectedViewModel
        {
            get => _selectedViewModel;
            set
            {
                Set(ref _selectedViewModel, value);
                RaisePropertyChanged(nameof(CulturalSelected));
                RaisePropertyChanged(nameof(ProvisioningSelected));
                RaisePropertyChanged(nameof(RegulationSelected));
            }
        }

    }
}
