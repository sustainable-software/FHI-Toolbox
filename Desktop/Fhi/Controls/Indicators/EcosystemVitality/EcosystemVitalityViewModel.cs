using System;
using System.Linq;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class EcosystemVitalityViewModel : NavigationViewModel
    {
        private SummaryViewModel _selectedViewModel;
        private readonly Action<NavigationViewModel> _navigate;

        private BasinConditionSummaryViewModel _basinConditionSummaryViewModel;
        private WaterQualitySummaryViewModel _waterQualitySummaryViewModel;
        private WaterQuantitySummaryViewModel _waterQuantitySummaryViewModel;
        private BiodiversitySummaryViewModel _biodiversitySummaryViewModel;

        private BasinConditionSummaryViewModel BasinConditionSummaryViewModel =>
            _basinConditionSummaryViewModel ?? (_basinConditionSummaryViewModel = new BasinConditionSummaryViewModel(_navigate, this));
        private WaterQualitySummaryViewModel WaterQualitySummaryViewModel =>
            _waterQualitySummaryViewModel ?? (_waterQualitySummaryViewModel = new WaterQualitySummaryViewModel(_navigate, this));
        private WaterQuantitySummaryViewModel WaterQuantitySummaryViewModel =>
            _waterQuantitySummaryViewModel ?? (_waterQuantitySummaryViewModel = new WaterQuantitySummaryViewModel(_navigate, this));
        private BiodiversitySummaryViewModel BiodiversitySummaryViewModel =>
            _biodiversitySummaryViewModel ?? (_biodiversitySummaryViewModel = new BiodiversitySummaryViewModel(_navigate, this));

        public EcosystemVitalityViewModel() { }

        public EcosystemVitalityViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            _navigate = navigate;
            
            BasinConditionCommand = new RelayCommand(() => SelectedViewModel = BasinConditionSummaryViewModel);
            WaterQuantityCommand = new RelayCommand(() => SelectedViewModel = WaterQuantitySummaryViewModel);
            WaterQualityCommand = new RelayCommand(() => SelectedViewModel = WaterQualitySummaryViewModel);
            BiodiversityCommand = new RelayCommand(() => SelectedViewModel = BiodiversitySummaryViewModel);

            SelectedViewModel = BasinConditionSummaryViewModel;
        }
        
        public ICommand BasinConditionCommand { get; }
        public ICommand WaterQuantityCommand { get; }
        public ICommand WaterQualityCommand { get; }
        public ICommand BiodiversityCommand { get; }

        public IIndicator Pillar => Model?.EcosystemVitality;
        public IIndicator Biodiversity => Pillar.FetchIndicator<Indicator>("Biodiversity");
        public IIndicator BasinCondition => Pillar.FetchIndicator<Indicator>("Basin Condition");
        public IIndicator WaterQuality => Pillar.FetchIndicator<Indicator>("Water Quality");
        public IIndicator WaterQuantity => Pillar.FetchIndicator<Indicator>("Water Quantity");

        public Boolean WaterQualitySelected => SelectedViewModel is WaterQualitySummaryViewModel;
        public Boolean WaterQuantitySelected => SelectedViewModel is WaterQuantitySummaryViewModel;
        public Boolean BasinConditionSelected => SelectedViewModel is BasinConditionSummaryViewModel;
        public Boolean BiodiversitySelected => SelectedViewModel is BiodiversitySummaryViewModel;


        public SummaryViewModel SelectedViewModel
        {
            get => _selectedViewModel;
            set
            {
                Set(ref _selectedViewModel, value);
                RaisePropertyChanged(nameof(WaterQualitySelected));
                RaisePropertyChanged(nameof(WaterQuantitySelected));
                RaisePropertyChanged(nameof(BasinConditionSelected));
                RaisePropertyChanged(nameof(BiodiversitySelected));
            }
        }
    }
}
