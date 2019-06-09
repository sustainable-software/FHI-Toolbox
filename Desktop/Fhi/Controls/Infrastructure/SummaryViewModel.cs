using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fhi.Controls.MVVM;

namespace Fhi.Controls.Infrastructure
{
    public abstract class SummaryViewModel : ViewModelBase
    {
        protected readonly Action<NavigationViewModel> Navigate;
        protected readonly NavigationViewModel Back;

        protected SummaryViewModel() { }

        protected SummaryViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
        {
            Navigate = navigate;
            Back = back;
        }
    }
}
