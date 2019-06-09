using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fhi.Controls.MVVM;
using FhiModel.Common;

namespace Fhi.Controls.Infrastructure
{
    public class ImportAssessmentViewModel : ViewModelBase
    {
        public IList<ImportIndicatorViewModel> Indicators => BuildHierarchy(new[] { Model.EcosystemVitality, Model.EcosystemServices, Model.Governance });
        private IList<ImportIndicatorViewModel> BuildHierarchy(IEnumerable<IIndicator> indicators)
        {
            var rv = new List<ImportIndicatorViewModel>();
            foreach (var indicator in indicators)
            {
                var vm = new ImportIndicatorViewModel(indicator);
                rv.Add(vm);
                if (indicator.Children?.Count > 0)
                    vm.Children = new List<ImportIndicatorViewModel>(BuildHierarchy(indicator.Children));

            }
            return rv;
        }

    }

    public class ImportIndicatorViewModel : ViewModelBase
    {
        public ImportIndicatorViewModel(IIndicator indicator)
        {
            Indicator = indicator;
        }

        public IIndicator Indicator { get; }

        public List<ImportIndicatorViewModel> Children { get; set; }
    }
}
