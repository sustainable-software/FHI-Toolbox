using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using FhiModel.Common;
using FhiModel.EcosystemVitality;
using FhiModel.EcosystemVitality.DendreticConnectivity;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class BasinConditionSummaryViewModel : SummaryViewModel
    {
        private readonly Action<NavigationViewModel> _navigate;
        private readonly NavigationViewModel _back;

        private ConnectivityIndicatorViewModel _connectivityIndicatorViewModel;
        private LandCoverViewModel _landCoverViewModel;
        private BankModificationViewModel _bankModificationViewModel;

        private ConnectivityIndicatorViewModel ConnectivityIndicatorViewModel =>
            _connectivityIndicatorViewModel ??
            (_connectivityIndicatorViewModel = new ConnectivityIndicatorViewModel(_navigate, _back));

        private LandCoverViewModel LandCoverViewModel =>
            _landCoverViewModel ?? (_landCoverViewModel = new LandCoverViewModel(_navigate, _back));
        private BankModificationViewModel BankModificationViewModel => 
            _bankModificationViewModel ??
            (_bankModificationViewModel = new BankModificationViewModel(_navigate, _back));
        
        public BasinConditionSummaryViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
        {
            _navigate = navigate;
            _back = back;
            
            EditDamPassabilityCommand = new RelayCommand(() => _navigate(ConnectivityIndicatorViewModel));
            LandCoverCommand = new RelayCommand(() => _navigate(LandCoverViewModel));
            BankModificationCommand = new RelayCommand(() => _navigate(BankModificationViewModel));
        }
        
        public ICommand EditDamPassabilityCommand { get; }
        public ICommand LandCoverCommand { get; }
        public ICommand BankModificationCommand { get; }
        
        public ConnectivityIndicator ConnectivityIndicator => Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
        public BankModificationIndicator BankModificationIndicator => Model.EcosystemVitality.FetchIndicator<BankModificationIndicator>();
        public LandCoverIndicator LandCoverIndicator => Model.EcosystemVitality.FetchIndicator<LandCoverIndicator>();
    }
}
