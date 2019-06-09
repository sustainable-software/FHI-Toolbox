using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using FhiModel.Common;
using FhiModel.EcosystemServices;

namespace Fhi.Controls.Indicators.EcosystemServices
{
    public class EcosystemServicesSummaryViewModel : SummaryViewModel
    {
        private readonly Action<NavigationViewModel> _navigate;
        private readonly NavigationViewModel _back;
        
        private readonly String _modelName;
        
        public EcosystemServicesSummaryViewModel(String modelName, Action<NavigationViewModel> navigate, NavigationViewModel back)
        {
            _navigate = navigate;
            _back = back;
            
            _modelName = modelName;
            EditIndicatorCommand = new RelayCommand(EditIndicator);
        }
        
        public ICommand EditIndicatorCommand { get; }

        public IIndicator MajorIndicator => Model?.EcosystemServices?.FetchIndicator<Indicator>(_modelName);
        
        public IList<IIndicator> Summary => MajorIndicator?.Children;

        private void EditIndicator(Object o)
        {
            if (!(o is EcosystemServicesIndicator esi)) return;
            var vm = new SpatialUnitsViewModel(esi, _navigate, _back);
            _navigate(vm);
        }
    }
}
