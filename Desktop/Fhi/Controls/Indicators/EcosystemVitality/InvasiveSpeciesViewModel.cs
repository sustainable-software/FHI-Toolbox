using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using FhiModel.Common;
using FhiModel.EcosystemVitality.Biodiversity;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class InvasiveSpeciesViewModel : NavigationViewModel
    {
        public InvasiveSpeciesViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back) 
            : base(navigate, back)
        {
            AddSpeciesCommand = new RelayCommand(AddSpecies);
            ImportSpeciesCommand = new RelayCommand(ImportSpecies);
        }
        
        public ICommand AddSpeciesCommand { get; }
        public ICommand ImportSpeciesCommand { get; }
        
        public InvasiveSpeciesIndicator Indicator =>
            Model.EcosystemVitality.FetchIndicator<InvasiveSpeciesIndicator>();

        public IList<SpeciesViewModel> IncludedSpecies
        {
            get
            {
                return new List<SpeciesViewModel>(
                    Indicator.IncludedSpecies.Select(x => new SpeciesViewModel(x, null, RemoveSpecies)));
            }
        }

        private void RemoveSpecies(SpeciesViewModel vm)
        {
            var answer = MessageBox.Show($"Are you sure you want to remove {vm.Species.Binomial} from the assessment?", 
                "Remove Species?", 
                MessageBoxButton.YesNo);
            if (answer != MessageBoxResult.Yes) return;
            
            Indicator.IncludedSpecies.Remove(vm.Species);
            RaisePropertyChanged(nameof(IncludedSpecies));
        }
        
        private void AddSpecies()
        {
            var species = new Species
            {
                Custom = true,
                UserCanChangeCode = true,
                Code = RedListCode.NONE
            };
            var vm = new SpeciesViewModel(species, null, RemoveSpecies);
            var window = new AddSpeciesWindow {DataContext = vm, Owner = Application.Current?.MainWindow};
            if (window.ShowDialog() != true) return;
            Indicator.IncludedSpecies.Add(species);
            RaisePropertyChanged(nameof(IncludedSpecies));
        }

        private void ImportSpecies()
        {
            var vm = new BiodiversityViewModel(true);
            var window = new BiodiversityWindow { Owner = Application.Current.MainWindow, DataContext = vm };
            window.ShowDialog();
            RaisePropertyChanged(nameof(IncludedSpecies));
        }
    }
}