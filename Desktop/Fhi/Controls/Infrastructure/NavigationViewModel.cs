using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;

namespace Fhi.Controls.Infrastructure
{
    public abstract class NavigationViewModel : ViewModelBase
    {
        private readonly Action<NavigationViewModel> _navigate;
        
        protected NavigationViewModel() { }
        protected NavigationViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
        {
            _navigate = navigate;
            BackCommand = new RelayCommand(() => Navigate(back));
        }

        public ICommand BackCommand { get; protected set; }

        protected void Navigate(NavigationViewModel vm)
        {
            Globals.Telemetry.TrackPageView(vm.ToString());
            _navigate(vm);
        }

        protected void NavigateBack()
        {
            BackCommand.Execute(null);
        }
    }
}
