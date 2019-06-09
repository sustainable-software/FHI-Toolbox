using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using FhiModel.Common;

namespace Fhi.Controls.Utils
{
    public class IndicatorMetadataViewModel : ViewModelBase
    {
        private IIndicator _indicator;
        private bool _override;
        
        private Uncertainty _rank;
        private string _notes;
        private int? _overrideValue;
        private string _overrideComment;

        private IndicatorMetadataViewModel(IIndicator indicator)
        {
            _indicator = indicator;
            Name = indicator.Name;
            Rank = indicator.Rank;
            Notes = indicator.Notes;
            Override = indicator.UserOverride != null;
            if (indicator.UserOverride != null)
                OverrideValue = indicator.UserOverride.Value;
            OverrideComment = indicator.OverrideComment;
        }


        public String Name { get; }
        

        public Uncertainty Rank
        {
            get => _rank;
            set => Set(ref _rank, value);
        }

        public String Notes
        {
            get => _notes;
            set => Set(ref _notes, value);
        }

        public Boolean Override
        {
            get => _override;
            set
            {
                Set(ref _override, value);
                if (_override == false)
                    OverrideValue = null;
            }
        }

        public Int32? OverrideValue
        {
            get => _overrideValue;
            set => Set(ref _overrideValue, value);
        }

        public String OverrideComment
        {
            get => _overrideComment;
            set => Set(ref _overrideComment, value);
        }

        public static void Dialog(object foo)
        {
            var indicator = foo as IIndicator;
            
            var vm = new IndicatorMetadataViewModel(indicator);
            var dialog = new IndicatorMetadataWindow { Owner = Application.Current.MainWindow, DataContext = vm};
            if (dialog.ShowDialog() != true) return;

            if (vm.Override)
            {
                indicator.UserOverride = vm.OverrideValue;
                indicator.OverrideComment = vm.OverrideComment;
            }
            else
            {
                indicator.UserOverride = null;
                indicator.OverrideComment = null;
            }

            indicator.Rank = vm.Rank;
            indicator.Notes = vm.Notes;
        }

        public static ICommand DialogCommand { get; } = new RelayCommand(o => Dialog(o));
    }
}
