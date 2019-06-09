using System;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using FhiModel.Common;
using FhiModel.EcosystemServices;

namespace Fhi.Controls.Indicators.EcosystemServices
{
    public class CulturalSummaryViewModel : SummaryViewModel
    {
        private readonly Action<NavigationViewModel> _navigate;
        private readonly NavigationViewModel _back;

        private ConservationAreaViewModel _conservationAreaViewModel;
        private RecreationViewModel _recreationViewModel;

        private ConservationAreaViewModel ConservationAreaViewModel =>
            _conservationAreaViewModel ??
            (_conservationAreaViewModel = new ConservationAreaViewModel(_navigate, _back));

        private RecreationViewModel RecreationViewModel =>
            _recreationViewModel ??
            (_recreationViewModel = new RecreationViewModel(_navigate, _back));

        public CulturalSummaryViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
        {
            _navigate = navigate;
            _back = back;

            ConservationAreaCommand = new RelayCommand(() => _navigate(ConservationAreaViewModel));
            RecreationCommand = new RelayCommand(() => _navigate(RecreationViewModel));
        }

        public ICommand ConservationAreaCommand { get; }
        public ICommand RecreationCommand { get; }

        public IIndicator Recreation => Model?.EcosystemServices?.FetchIndicator<ManualIndicator>("Recreation");
        public IIndicator ConservationAreas => Model?.EcosystemServices?.FetchIndicator<ConservationAreaIndicator>();
    }
}