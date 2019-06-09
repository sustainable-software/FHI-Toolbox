using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fhi.Controls.Infrastructure;
using FhiModel;
using FhiModel.Common;
using FhiModel.EcosystemVitality;

namespace Fhi.Controls.Indicators.EcosystemServices
{
    public class RecreationViewModel : NavigationViewModel
    {
        public RecreationViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {

        }

        public ManualIndicator  Recreation =>
            Model?.EcosystemServices?.FetchIndicator<ManualIndicator>("Recreation");
    }
}
