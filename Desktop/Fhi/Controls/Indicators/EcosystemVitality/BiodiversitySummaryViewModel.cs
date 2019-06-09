using System;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using FhiModel.Common;
using FhiModel.EcosystemVitality.Biodiversity;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class BiodiversitySummaryViewModel : SummaryViewModel
    {
        private readonly Action<NavigationViewModel> _navigate;
        private readonly NavigationViewModel _back;

        private SpeciesOfConcernViewModel _speciesOfConcernViewModel;
        private InvasiveSpeciesViewModel _invasiveSpeciesViewModel;

        private SpeciesOfConcernViewModel SpeciesOfConcernViewModel =>
            _speciesOfConcernViewModel ?? (_speciesOfConcernViewModel = new SpeciesOfConcernViewModel(_navigate, _back));

        private InvasiveSpeciesViewModel InvasiveSpeciesViewModel =>
            _invasiveSpeciesViewModel ?? (_invasiveSpeciesViewModel = new InvasiveSpeciesViewModel(_navigate, _back));
        
        public BiodiversitySummaryViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
        {
            _navigate = navigate;
            _back = back;
            
            EditSpeciesOfConcernCommand = new RelayCommand(() => _navigate(SpeciesOfConcernViewModel));
            EditInvasiveSpeciesCommand = new RelayCommand(() => _navigate(InvasiveSpeciesViewModel));
        }
        
        public ICommand EditSpeciesOfConcernCommand { get; }
        public ICommand EditInvasiveSpeciesCommand { get; }
        
        public InvasiveSpeciesIndicator InvasiveSpecies => Model.EcosystemVitality.FetchIndicator<InvasiveSpeciesIndicator>();
        public SpeciesOfConcernIndicator SpeciesOfConcern => Model.EcosystemVitality.FetchIndicator<SpeciesOfConcernIndicator>();
    }
}
