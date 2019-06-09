using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using FhiModel.Common;
using FhiModel.EcosystemVitality;
using FhiModel.EcosystemVitality.FlowDeviation;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class WaterQuantitySummaryViewModel : SummaryViewModel
    {
        private readonly Action<NavigationViewModel> _navigate;
        private readonly NavigationViewModel _back;
        
        public WaterQuantitySummaryViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
        {
            _navigate = navigate;
            _back = back;
            
            EditFlowDeviationCommand = new RelayCommand(EditFlowDeviation);
            EditGroundwaterStorageCommand = new RelayCommand(EditGroundwaterStorage);
        }
        
        public ICommand EditFlowDeviationCommand { get; }
        public ICommand EditGroundwaterStorageCommand { get; }
        
        public FlowDeviationIndicator FlowDeviation =>
            Model?.EcosystemVitality?.FetchIndicator<FlowDeviationIndicator>();

        public GroundwaterStorageIndicator GroundwaterStorage =>
            Model?.EcosystemVitality?.FetchIndicator<GroundwaterStorageIndicator>();

        private void EditFlowDeviation()
        {
            var vm = new FlowDeviationViewModel(_navigate, _back);
            _navigate(vm);
        }

        private void EditGroundwaterStorage()
        {
            var vm = new GroundwaterStorageViewModel(_navigate, _back);
            _navigate(vm);
        }
    }
}
