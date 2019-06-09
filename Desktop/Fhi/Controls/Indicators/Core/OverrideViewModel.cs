using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;

namespace Fhi.Controls.Indicators.Core
{
    public class OverrideViewModel : ViewModelBase
    {
        public IList<OverrideIndicatorViewModel> Indicators => BuildHierarchy(new[] {Model.EcosystemVitality, Model.EcosystemServices, Model.Governance });

        private IList<OverrideIndicatorViewModel> BuildHierarchy(IEnumerable<IIndicator> indicators)
        {
            var rv = new List<OverrideIndicatorViewModel>();
            foreach (var indicator in indicators)
            {
                var vm = new OverrideIndicatorViewModel(indicator);
                rv.Add(vm);
                if (indicator.Children?.Count > 0)
                    vm.Children = new List<OverrideIndicatorViewModel>(BuildHierarchy(indicator.Children));
                
            }
            return rv;
        }
    }

    public class OverrideIndicatorViewModel : ViewModelBase
    {
        public OverrideIndicatorViewModel(IIndicator indicator)
        {
            Indicator = indicator;
            Indicator.PropertyChanged += (sender, args) => RaisePropertyChanged("");

            OverrideCommand = new RelayCommand(Override);
            ClearCommand = new RelayCommand(Clear);
        }

        public ICommand OverrideCommand { get; }
        public ICommand ClearCommand { get; }

        public IIndicator Indicator { get; }

        public Boolean Overriden => Indicator.UserOverride != null;

        public Int32? OverrideValue
        {
            get => Indicator.UserOverride;
            set
            {
                if (value == Indicator.UserOverride) return;
                if (value < 0 || value > 100)
                {
                    MessageBox.Show("Indicator values must be between 0 and 100.");
                    return;
                }
                Indicator.UserOverride = value;
                RaisePropertyChanged();
            }
        }

        public List<OverrideIndicatorViewModel> Children { get; set; }

        private void Override()
        {
            IndicatorMetadataViewModel.Dialog(Indicator);
            //var dialog = new OverrideWindow {Owner = Application.Current.MainWindow, DataContext = this};
            //dialog.ShowDialog();
        }

        private void Clear()
        {
            Indicator.UserOverride = null;
            Indicator.OverrideComment = null;
            RaisePropertyChanged("");
        }
    }
}
