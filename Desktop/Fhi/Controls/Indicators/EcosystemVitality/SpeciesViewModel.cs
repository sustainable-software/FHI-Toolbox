using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Net.Mime;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using FhiModel.EcosystemVitality.Biodiversity;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class SpeciesViewModel : ViewModelBase, IComparable<SpeciesViewModel>, IEquatable<SpeciesViewModel>
    {
        public SpeciesViewModel(Species species, Action<SpeciesViewModel> add, Action<SpeciesViewModel> remove)
        {
            Species = species;
            Species.PropertyChanged += (sender, args) => RaisePropertyChanged("");

            if (add != null)
            {
                AddCommand = new RelayCommand(() => add(this));
                HasAdd = true;
            }
            if (remove != null)
                RemoveCommand = new RelayCommand(() => remove(this));

            EditCommand = new RelayCommand(Edit);
        }

        public Boolean HasFullName => !String.IsNullOrWhiteSpace(Species.Common);

        public Species Species { get; }

        public String Legend => Species.LegendKey[Species.Legend];

        public Boolean HasCode => Species.Code != RedListCode.NONE;
        
        public String CodeDescription => _redlistCodes[Species.Code];

        public Boolean HasTaxa => !String.IsNullOrWhiteSpace(Species.Family);
        
        public Boolean HasAdd { get; }
        
        public ICommand RemoveCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        
        public Int32 CompareTo(SpeciesViewModel other)
        {
            var comp = String.Compare(Species.Family, other.Species.Family, StringComparison.Ordinal);
            if (comp != 0) return comp;
            comp = String.Compare(Species.Genus, other.Species.Genus, StringComparison.Ordinal);
            if (comp != 0) return comp;
            comp = String.Compare(Species.Binomial, other.Species.Binomial, StringComparison.Ordinal);
            return comp;
        }

        public Boolean Equals(SpeciesViewModel other)
        {
            return other != null && Species.Id.Equals(other.Species.Id);
        }

        private void Edit()
        {
            var dialog = new AddSpeciesWindow { Owner = Application.Current.MainWindow, DataContext = this};
            dialog.ShowDialog();
        }

        private static readonly Dictionary<RedListCode, String> _redlistCodes = new Dictionary<RedListCode, string>
        {
            { RedListCode.CR, "CR - Critically endangered" },
            { RedListCode.DD, "DD - Data deficient" },
            { RedListCode.EN, "EN - Endangered" },
            { RedListCode.LC, "LC - Least concern" },
            { RedListCode.NT, "NT - Near threatened" },
            { RedListCode.VU, "VU - Vulnerable" },
            { RedListCode.NONE, "NONE" }
        };
    }
}